using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.Utilities;
using Terraria.DataStructures;
using System;
using WorldGenMod.Structures.Ice;
using Terraria.UI;
using System.Drawing;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static tModPorter.ProgressUpdate;
using System.Diagnostics.Metrics;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace WorldGenMod.Structures.Underworld
{
    class ChastisedChurch : ModSystem
    {
        readonly int gap = -1; // the horizontal gap between two side room columns
        readonly int wThick = 2; // the thickness of the outer walls and ceilings in code
        readonly int doorHeight = 5; // the height of a connection between two rooms
        readonly int forceEvenRoom = 1; // 1 = force all rooms to have an even XTiles count; 0 = force all side rooms to have an odd XTiles count
        readonly int maxChurchLength = 500; // maximum tile length of the ChastisedChurch
        readonly (int xMin, int xMax, int yMin, int yMax) roomSizes = (12, 80, 12, 30); // possible room dimensions
        readonly (int xMin, int xMax, int yMin, int yMax) belowRoomSizes = (35, 60, 10, 14); // possible below room dimensions
        List<Rectangle2P> belowRoomsAndStairs = []; // list of all created below rooms and staircases to check and prevent overlappings
        List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> PoundAfterSmoothWorld = []; // as the worldgen step "Smooth World" destroys the stairs of below rooms, they get stored here to create the stairs after that step
        

        Dictionary<string, (int id, int style)> Deco = []; // the dictionary where the styles of tiles are stored
        Dictionary<Action, (bool execute, int decoStyle, int decoSubStyle, List<(int x, int y, int tileID)> checkPoints)> runAfterWorldCleanup = []; // structures that get damaged after later world generation steps and need to be redone

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateChastisedChurch)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Underworld"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Chastised Church", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Chastising the crooked church...";

                    PoundAfterSmoothWorld.Clear(); //init for each world generation
                    belowRoomsAndStairs.Clear(); //init for each world generation
                    runAfterWorldCleanup.Clear();

                    int side; //init
                    if (WorldGenMod.chastisedChurchGenerationSide == "Left") side = -1;
                    else if (WorldGenMod.chastisedChurchGenerationSide == "Right") side = 1;
                    else if (WorldGenMod.chastisedChurchGenerationSide == "Both") side = -1; // start on the left
                    else side = WorldGen.genRand.NextBool() ? 1 : -1; // "Random"

                    //TODO: deactivate debug mode!
                    bool debug = false;
                    if (debug)
                    {
                        side = -1;
                        int startPosX = 0; // start on the left world side

                        for (int i = 1; i <= 10; i++)
                        {
                            startPosX = GenerateChastisedChurch(side, startPosX);
                        }
                    }
                    else
                    {
                        GenerateChastisedChurch(side);
                    }

                    if (WorldGenMod.chastisedChurchGenerationSide == "Both")
                    {
                        GenerateChastisedChurch(1); // do the right side
                    }

                }));


                genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Smooth World"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: CC repair stairs", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Repairing the Chastised Church stairs that got damaged during the previous worldgen step...";

                    CreateStairsFromData();
                }));


                genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Quick Cleanup"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: CC repair structure", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Repairing the Chastised Church structures that got damaged during the previous worldgen step...";

                    foreach (KeyValuePair<Action, (bool execute, int decoStyle, int decoSubStyle, List<(int x, int y, int tileID)> checkPoints)> function in runAfterWorldCleanup)
                    {
                        if (function.Value.execute)
                        {
                            FillAndChooseStyle(function.Value.decoStyle, function.Value.decoSubStyle); // reload the style with which the structure was created

                            bool checkOk = true;
                            foreach ((int x, int y, int tileID) point in function.Value.checkPoints) // check if the checkPoint still have the correct TileIDs or if they got overwritten
                            {
                                checkOk &= Main.tile[point.x, point.y].TileType == point.tileID;
                            }

                            if (checkOk) function.Key.Invoke(); // put the structure again

                        }

                    }
                }));
            }
        }

        /// <summary>
        /// Shifting adjacent rooms some blocks away, leaves a gap that has to be filled.
        /// </summary>
        /// <param name="previousRoom">The rectangle of the previous created side room</param>
        /// <param name="actualRoom">The rectangle of the just created side room</param>
        public void FillGap(Rectangle2P previousRoom, Rectangle2P actualRoom)
        {

            Rectangle2P gap = new(0, 0, 0, 0); //init

            //first step: derive the side to which the room expanded
            if (actualRoom.X0 > previousRoom.X0) // the actual room is on the right side of the previous room
            {
                gap.X0 = previousRoom.X1 + 1;
                gap.X1 = actualRoom.X0 - 1;
            }
            else // the actual room is on the left side of the previous room
            {
                gap.X0 = actualRoom.X1 + 1;
                gap.X1 = previousRoom.X0 - 1;
            }

            //second step: find out which rooms reach less farther up (to define Y0 of the gap)
            if (actualRoom.Y0 > previousRoom.Y0) gap.Y0 = actualRoom.Y0;
            else                                 gap.Y0 = previousRoom.Y0;

            gap.Y1 = actualRoom.Y1; // all main-line-rooms share the same base height



            //fill gap
            for (int x = gap.X0; x <= gap.X1; x++)
            {
                for (int y = gap.Y0; y <= gap.Y1; y++)
                {
                    WorldGen.KillTile(x, y);
                    WorldGen.KillWall(x, y);
                    WorldGen.EmptyLiquid(x, y);

                    if (y == previousRoom.Y1 - (wThick - 1)) //doesn't matter if previousRoom or actualRoom, because the floor is at the same height
                    {
                        WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                        WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);
                        WorldGen.PlaceWall(x, y, Deco[S.BackWall].id); //put the designated background wall
                    }
                    else if (y >= previousRoom.Y1 - (wThick - 1) - doorHeight && y <= previousRoom.Y1 - wThick) // the door between the rooms
                    {
                        // don't put bricks, leave the door "free"
                        WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); //put the designated background wall
                    }
                    else
                    {
                        WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true); // fill gap with bricks
                        WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id);
                        WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // put the designated background wall
                    }
                }
            }

            // place backwall left and right of the gap (those were left out when the rooms were created)
            for (int y = gap.Y0; y <= gap.Y1; y++)
            {
                if (y >= previousRoom.Y1 - (wThick - 1) - doorHeight && y <= previousRoom.Y1 - wThick) continue; // doors already have that wall

                WorldGen.PlaceWall(gap.X0 - 1, y, Deco[S.BackWall].id); //put the designated background wall
                WorldGen.PlaceWall(gap.X1 + 1, y, Deco[S.BackWall].id); //put the designated background wall
            }
        }

        /// <summary>
        /// Chooses a random decoration style for the ChastisedChurch or executes the one that it got handed over
        /// </summary>
        /// <param name="forceStyle">Impose a style (useful for redoing steps after "Smooth World" world creation step, to "reload" a style)
        ///                         <br/> --> -1 = don't force, choose at random </param>
        /// <param name="forceSubstyle">Impose a substyle, possible values: 0 or 1 (useful for redoing steps after "Smooth World" world creation step, to "reload" a style)
        ///                         <br/> --> -1 = don't force, choose at random
        ///                         <br/> --> 0 = normal style
        ///                         <br/> --> 1 = substyle </param>
        public void FillAndChooseStyle(int forceStyle = -1, int forceSubstyle = -1)
        {
            Deco.Clear(); // init

            #region create dictionary entries
            Deco.Add(S.StyleSave, (0,0));
            Deco.Add(S.SubStyleSave, (0,0));
            Deco.Add(S.Brick, (0, 0));
            Deco.Add(S.BrickPaint, (0, 0));
            Deco.Add(S.RoofBrick, (0, 0));
            Deco.Add(S.RoofBrickPaint, (0, 0));
            Deco.Add(S.Floor, (0, 0));
            Deco.Add(S.FloorPaint, (0, 0));
            Deco.Add(S.BelowRoomFloor, (0, 0));
            Deco.Add(S.BelowRoomFloorPaint, (0, 0));
            Deco.Add(S.EvilTile, (0, 0));
            Deco.Add(S.BackWall, (0, 0));
            Deco.Add(S.BackWallPaint, (0, 0));
            Deco.Add(S.CrookedWall, (0, 0));
            Deco.Add(S.WindowWall, (0, 0));
            Deco.Add(S.WindowPaint, (0, 0));
            Deco.Add(S.DoorWall, (0, 0));
            Deco.Add(S.DoorPlat, (0, 0));
            Deco.Add(S.DoorPlatPaint, (0, 0));
            Deco.Add(S.Door, (0, 0));
            Deco.Add(S.DoorPaint, (0, 0));
            Deco.Add(S.Chest, (0, 0));
            Deco.Add(S.Campfire, (0, 0));
            Deco.Add(S.CampfirePaint, (0, 0));
            Deco.Add(S.Table, (0, 0));
            Deco.Add(S.Workbench, (0, 0));
            Deco.Add(S.Chair, (0, 0));
            Deco.Add(S.MainPainting, (0, 0));
            Deco.Add(S.Chandelier, (0, 0));
            Deco.Add(S.Candelabra, (0, 0));
            Deco.Add(S.Candle, (0, 0));
            Deco.Add(S.Lamp, (0, 0));
            Deco.Add(S.Torch, (0, 0));
            Deco.Add(S.Lantern, (0, 0));
            Deco.Add(S.Banner, (0, 0));
            Deco.Add(S.DecoPlat, (0, 0));
            Deco.Add(S.StylePaint, (0, 0));
            Deco.Add(S.HangingPot, (0, 0));
            Deco.Add(S.Bookcase, (0, 0));
            Deco.Add(S.Sofa, (0, 0));
            Deco.Add(S.Clock, (0, 0));
            Deco.Add(S.PaintingWallpaper, (0, 0));
            Deco.Add(S.Dresser, (0, 0));
            Deco.Add(S.Column, (0, 0));
            Deco.Add(S.ColumnPaint, (0, 0));

            // altar
            Deco.Add(S.MiddleWall, (0, 0));
            Deco.Add(S.MiddleWallPaint, (0, 0));
            Deco.Add(S.AltarSteps, (0, 0));
            Deco.Add(S.AltarStepsPaint, (0, 0));
            Deco.Add(S.AltarDeco, (0, 0));
            Deco.Add(S.AltarDecoPaint, (0, 0));
            Deco.Add(S.AltarWall, (0, 0));
            Deco.Add(S.AltarWallPaint, (0, 0));
            Deco.Add(S.RunicWallPaint, (0, 0));

            // giant sword
            Deco.Add(S.SwordBrick, (0, 0));
            Deco.Add(S.SwordHandleGemItem, (0, 0));
            Deco.Add(S.SwordHandlePaint, (0, 0));
            Deco.Add(S.SwordCrossGPaint, (0, 0));
            Deco.Add(S.SwordEnergyFlowWall, (0, 0));
            Deco.Add(S.SwordBladeEdgeWall, (0, 0));
            Deco.Add(S.SwordBladeWall, (0, 0));

            // tree
            Deco.Add(S.TreePaint, (0, 0));
            Deco.Add(S.BannerHangPlat, (0, 0));

            // temple
            Deco.Add(S.TempleBrick, (0, 0));
            Deco.Add(S.TempleBrickBottomPaint, (0, 0));
            Deco.Add(S.TempleBrickAltarPaint, (0, 0));
            Deco.Add(S.TempleStreamerPaint, (0, 0));
            Deco.Add(S.TempleSteps, (0, 0));
            Deco.Add(S.TempleStepsPaint, (0, 0));
            Deco.Add(S.TempleColumnPaint, (0, 0));
            Deco.Add(S.TempleCeilingPlat, (0, 0));
        #endregion

        //use the handed over style / choose a random style and define it's IDs
        int chosenStyle, subStyle;

            if (forceStyle >= 0) chosenStyle = forceStyle;
            else                chosenStyle = WorldGen.genRand.Next(3);

            if (forceSubstyle == 0 || forceSubstyle == 1) subStyle = forceSubstyle;
            else
            {
                if (Chance.Simple()) subStyle = 1;
                else                 subStyle = 0;
            }

            switch (chosenStyle)
            {
                case S.StyleHellstone: // Hellstone

                    Deco[S.StyleSave] = (0, S.StyleHellstone);
                    Deco[S.SubStyleSave] = (0, subStyle);

                    Deco[S.Brick] = (TileID.AncientHellstoneBrick, 0);
                    Deco[S.BrickPaint] = (0, 0);
                    Deco[S.RoofBrick] = (TileID.AncientHellstoneBrick, 0);
                    Deco[S.RoofBrickPaint] = (0, 0);
                    Deco[S.Floor] = (TileID.CrimtaneBrick, 0);
                    Deco[S.FloorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.BelowRoomFloor] = (TileID.IronBrick, 0);
                    Deco[S.BelowRoomFloorPaint] = (0, 0);

                    if (subStyle == 1)
                    { 
                        Deco[S.Brick] = (TileID.IridescentBrick, 0);
                        Deco[S.BrickPaint] = (PaintID.RedPaint, 0);
                        Deco[S.RoofBrick] = (TileID.IridescentBrick, 0);
                        Deco[S.RoofBrickPaint] = (PaintID.RedPaint, 0);
                        Deco[S.Floor] = (TileID.AncientHellstoneBrick, 0);
                        Deco[S.FloorPaint] = (0, 0);
                    }

                    Deco[S.EvilTile] = (TileID.Crimstone, 0);
                    Deco[S.BackWall] = (WallID.HellstoneBrickUnsafe, 0);
                    Deco[S.BackWallPaint] = (PaintID.None, 0);
                    Deco[S.CrookedWall] = (WallID.Crimson4Echo, 0);
                    Deco[S.WindowWall] = (WallID.RedStainedGlass, 0);
                    Deco[S.WindowPaint] = (PaintID.DeepRedPaint, 0);
                    Deco[S.DoorWall] = (WallID.CrimtaneBrick, 0);

                    Deco[S.DoorPlat] = (TileID.Platforms, 10); //Brass Shelf
                    Deco[S.DoorPlatPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Door] = (TileID.TallGateClosed, 0);
                    Deco[S.DoorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Chest] = (TileID.Containers, 14); // Shadewood
                    Deco[S.Campfire] = (TileID.Campfire, 2);  //Demon
                    Deco[S.CampfirePaint] = (PaintID.RedPaint, 0);
                    Deco[S.Table] = (TileID.Tables2, 11);  //AshWood
                    Deco[S.Workbench] = (TileID.WorkBenches, 43); //AshWood
                    Deco[S.Chair] = (TileID.Chairs, 11); // Shadewood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 26); //* "Discover"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 19); // Shadewood
                    Deco[S.Candelabra] = (TileID.Candelabras, 14); // Shadewood
                    if (subStyle == 1) Deco[S.Candelabra] = (TileID.Candelabras, 12); // Spooky
                    Deco[S.Candle] = (TileID.PlatinumCandle, 0); // PlatinumCandle
                    Deco[S.Lamp] = (TileID.Lamps, 14); // Shadewood
                    Deco[S.Torch] = (TileID.Torches, 9); //* Ice
                    Deco[S.Lantern] = (TileID.HangingLanterns, 18); //* Frozen
                    Deco[S.Banner] = (TileID.Banners, 2); //* Blue
                    Deco[S.DecoPlat] = (TileID.Platforms, 16); // Spooky
                    Deco[S.StylePaint] = (PaintID.RedPaint, 0);
                    Deco[S.HangingPot] = (TileID.PotsSuspended, 4); //* Shiverthorn
                    Deco[S.Bookcase] = (TileID.Bookcases, 43); // Ash Wood
                    Deco[S.Sofa] = (TileID.Benches, 5); // Shade Wood
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 21); // Shadewood
                    if (Chance.Simple()) Deco[S.Clock] = (TileID.GrandfatherClocks, 43); // AshWood
                    Deco[S.PaintingWallpaper] = (WallID.Spider, 0); // Spider Nest Wall
                    Deco[S.Dresser] = (TileID.Dressers, 30); //* Frozen
                    Deco[S.Piano] = (TileID.Pianos, 15); // Obsidian
                    Deco[S.Column] = (TileID.GraniteColumn, 0);
                    Deco[S.ColumnPaint] = (PaintID.RedPaint, 0);

                    //altar
                    Deco[S.MiddleWall] = (WallID.Lavafall, 0);
                    Deco[S.MiddleWallPaint] = (0, 0);
                    Deco[S.AltarSteps] = (TileID.Platforms, 10); //Brass Shelf
                    Deco[S.AltarStepsPaint] = (0, 0);
                    Deco[S.AltarDeco] = (TileID.Platforms, 10); //Brass Shelf
                    Deco[S.AltarDecoPaint] = (0, 0);
                    Deco[S.AltarWall] = (WallID.Lava3Echo, 0);
                    Deco[S.AltarWallPaint] = (0, 0);
                    Deco[S.RunicWallPaint] = (PaintID.RedPaint, 0);

                    // giant sword
                    Deco[S.SwordBrick] = (TileID.TeamBlockRed, 0);
                    Deco[S.SwordHandleGemItem] = (ItemID.LargeAmber, 0);
                    Deco[S.SwordHandlePaint] = (PaintID.RedPaint, 0);
                    Deco[S.SwordCrossGPaint] = (0, 0);
                    Deco[S.SwordEnergyFlowWall] = (WallID.Lavafall, 0);
                    Deco[S.SwordBladeEdgeWall] = (WallID.RubyGemspark, 0);
                    Deco[S.SwordBladeWall] = (WallID.CrimstoneEcho, 0);

                    // tree
                    Deco[S.TreePaint] = (PaintID.BlackPaint, 0);
                    Deco[S.BannerHangPlat] = (TileID.Platforms, 11); //Wood Shelf

                    // temple
                    Deco[S.TempleBrick] = (TileID.LavaMossBlock, 11);
                    Deco[S.TempleBrickBottomPaint] = (PaintID.BlackPaint, 0);
                    Deco[S.TempleBrickAltarPaint] = (0, 0);
                    Deco[S.TempleStreamerPaint] = (PaintID.RedPaint, 0);
                    Deco[S.TempleSteps] = (TileID.Platforms, 22); //Skyware Platform
                    Deco[S.TempleStepsPaint] = (PaintID.BlackPaint, 0);
                    Deco[S.TempleColumnPaint] = (PaintID.DeepRedPaint, 0);
                    Deco[S.TempleCeilingPlat] = (TileID.Platforms, 10); //Brass Shelf
                    break;

                case S.StyleTitanstone: // Titanstone

                    Deco[S.StyleSave] = (S.StyleTitanstone, 0);
                    Deco[S.SubStyleSave] = (0, subStyle);

                    Deco[S.Brick] = (TileID.Titanstone, 0);
                    Deco[S.BrickPaint] = (0, 0);
                    Deco[S.RoofBrick] = (TileID.Titanstone, 0);
                    Deco[S.RoofBrickPaint] = (0, 0);

                    Deco[S.Floor] = (TileID.CrimtaneBrick, 0);
                    Deco[S.FloorPaint] = (0, 0);
                    Deco[S.BelowRoomFloor] = (TileID.IronBrick, 0);
                    Deco[S.BelowRoomFloorPaint] = (0, 0);
                    if (subStyle == 1)
                    {
                        Deco[S.Floor] = (TileID.AncientObsidianBrick, 0);
                        Deco[S.FloorPaint] = (PaintID.GrayPaint, 0);
                    }

                    Deco[S.EvilTile] = (TileID.Ebonstone, 0);
                    Deco[S.BackWall] = (WallID.GraniteBlock, 0);
                    Deco[S.BackWallPaint] = (PaintID.GrayPaint, 0);
                    Deco[S.CrookedWall] = (WallID.Lava3Echo, 0);
                    Deco[S.WindowWall] = (WallID.RedStainedGlass, 0);
                    Deco[S.WindowPaint] = (PaintID.DeepRedPaint, 0);
                    Deco[S.DoorWall] = (WallID.Shadewood, 0);

                    Deco[S.DoorPlat] = (TileID.Platforms, 13); // Obsidian
                    Deco[S.DoorPlatPaint] = (PaintID.None, 0);
                    Deco[S.Door] = (TileID.TallGateClosed, 0);
                    Deco[S.DoorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Chest] = (TileID.Containers, 14); // Shadewood
                    Deco[S.Campfire] = (TileID.Campfire, 11); // Crimson
                    Deco[S.CampfirePaint] = (PaintID.RedPaint, 0);
                    Deco[S.Table] = (TileID.Tables, 8); // Shadewood
                    Deco[S.Workbench] = (TileID.WorkBenches, 43); // AshWood
                    Deco[S.Chair] = (TileID.Chairs, 11); // Shadewood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 34); //* "Crowno Devours His Lunch"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 25); //* Boreal
                    Deco[S.Candelabra] = (TileID.Candelabras, 14); // Shadewood
                    Deco[S.Candle] = (TileID.PlatinumCandle, 0); // PlatinumCandle
                    Deco[S.Lamp] = (TileID.Lamps, 14); // Shadewood
                    Deco[S.Torch] = (TileID.Torches, 9); //* Ice
                    Deco[S.Lantern] = (TileID.HangingLanterns, 29); //* Boreal
                    Deco[S.Banner] = (TileID.Banners, 2); //* Blue
                    Deco[S.DecoPlat] = (TileID.Platforms, 19); //* Boreal
                    Deco[S.StylePaint] = (PaintID.GrayPaint, 0);
                    Deco[S.HangingPot] = (TileID.PotsSuspended, 5); //* Blinkrot
                    Deco[S.Bookcase] = (TileID.Bookcases, 19); // Shadewood
                    Deco[S.Sofa] = (TileID.Benches, 5); // Shadewood
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 21); // Shadewood
                    if (subStyle == 1) Deco[S.Clock] = (TileID.GrandfatherClocks, 43); // AshWood
                    Deco[S.PaintingWallpaper] = (WallID.CrimstoneEcho, 0); // Crimstone Wall
                    Deco[S.Dresser] = (TileID.Dressers, 18); //* Boreal
                    Deco[S.Piano] = (TileID.Pianos, 4); // Shadewood
                    Deco[S.Column] = (TileID.GraniteColumn, 0);
                    Deco[S.ColumnPaint] = (PaintID.RedPaint, 0);

                    //altar
                    Deco[S.MiddleWall] = (WallID.Lavafall, 0);
                    Deco[S.MiddleWallPaint] = (0, 0);
                    Deco[S.AltarSteps] = (TileID.Platforms, 10); //Brass Shelf
                    Deco[S.AltarStepsPaint] = (0, 0);
                    Deco[S.AltarDeco] = (TileID.Platforms, 10); //Brass Shelf
                    Deco[S.AltarDecoPaint] = (0, 0);
                    Deco[S.AltarWall] = (WallID.GoldBrick, 0);
                    Deco[S.AltarWallPaint] = (PaintID.RedPaint, 0);
                    Deco[S.RunicWallPaint] = (PaintID.RedPaint, 0);

                    // giant sword
                    Deco[S.SwordBrick] = (TileID.TeamBlockRed, 0);
                    Deco[S.SwordHandleGemItem] = (ItemID.LargeAmber, 0);
                    Deco[S.SwordHandlePaint] = (PaintID.RedPaint, 0);
                    Deco[S.SwordCrossGPaint] = (0, 0);
                    Deco[S.SwordEnergyFlowWall] = (WallID.Lavafall, 0);
                    Deco[S.SwordBladeEdgeWall] = (WallID.RubyGemspark, 0);
                    Deco[S.SwordBladeWall] = (WallID.CrimstoneEcho, 0);

                    // tree
                    Deco[S.TreePaint] = (0, 0);
                    Deco[S.BannerHangPlat] = (TileID.Platforms, 11); //Wood Shelf

                    // temple
                    Deco[S.TempleBrick] = (TileID.LavaMossBlock, 11);
                    Deco[S.TempleBrickBottomPaint] = (PaintID.BlackPaint, 0);
                    Deco[S.TempleBrickAltarPaint] = (0, 0);
                    Deco[S.TempleStreamerPaint] = (PaintID.RedPaint, 0);
                    Deco[S.TempleSteps] = (TileID.Platforms, 22); //Skyware Platform
                    Deco[S.TempleStepsPaint] = (PaintID.BlackPaint, 0);
                    Deco[S.TempleColumnPaint] = (PaintID.DeepRedPaint, 0);
                    Deco[S.TempleCeilingPlat] = (TileID.Platforms, 10); //Brass Shelf
                    break;

                case S.StyleBlueBrick:

                    Deco[S.StyleSave] = (S.StyleBlueBrick, 0);
                    Deco[S.SubStyleSave] = (0, subStyle);

                    Deco[S.Brick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.RoofBrick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.Floor] = (TileID.EbonstoneBrick, 0);
                    if (subStyle == 1) Deco[S.Floor] = (TileID.MeteoriteBrick, 0);
                    Deco[S.FloorPaint] = (0, 0);
                    Deco[S.BelowRoomFloor] = (TileID.IronBrick, 0);
                    Deco[S.BelowRoomFloorPaint] = (0, 0);
                    Deco[S.EvilTile] = (TileID.Ebonstone, 0);
                    Deco[S.BackWall] = (WallID.Shadewood, 0);
                    Deco[S.BackWallPaint] = (PaintID.None, 0);
                    Deco[S.CrookedWall] = (WallID.Corruption3Echo, 0);
                    Deco[S.WindowWall] = (WallID.BlueStainedGlass, 0);
                    Deco[S.WindowPaint] = (PaintID.BluePaint, 0);
                    Deco[S.DoorWall] = (WallID.SpookyWood, 0);

                    Deco[S.DoorPlat] = (TileID.Platforms, 16); // Spooky
                    Deco[S.DoorPlatPaint] = (PaintID.BluePaint, 0);
                    if (subStyle == 1)
                    {
                        Deco[S.DoorPlat] = (TileID.Platforms, 27); // Meteorite
                        Deco[S.DoorPlatPaint] = (0, 0);
                    }

                    Deco[S.Door] = (TileID.TallGateClosed, 0);
                    Deco[S.DoorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Chest] = (TileID.Containers, 3); // Shadow
                    Deco[S.Campfire] = (TileID.Campfire, 12); // Hallowed
                    Deco[S.CampfirePaint] = (PaintID.BluePaint, 0);
                    Deco[S.Table] = (TileID.Tables, 1); // Ebonwood
                    Deco[S.Workbench] = (TileID.WorkBenches, 1); //* Ebonwood
                    Deco[S.Chair] = (TileID.Chairs, 2); // Ebonwood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 35); //* "Rare Enchantment"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 32); // Obsidian
                    Deco[S.Candelabra] = (TileID.Candelabras, 2); // Ebonwood
                    if (subStyle == 1) Deco[S.Candelabra] = (TileID.PlatinumCandelabra, 0); // PlatinumCandelabra
                    Deco[S.Candle] = (TileID.Candles, 5); // Ebonwood
                    Deco[S.Lamp] = (TileID.Lamps, 23); // Obsidian
                    Deco[S.Torch] = (TileID.Torches, 7); //* Demon
                    Deco[S.Lantern] = (TileID.HangingLanterns, 2); //* Caged Lantern
                    Deco[S.Banner] = (TileID.Banners, 0); //* Red
                    Deco[S.DecoPlat] = (TileID.Platforms, 16); // Spooky
                    Deco[S.StylePaint] = (PaintID.GrayPaint, 0);
                    Deco[S.HangingPot] = (TileID.PotsSuspended, 6); //* Corrupt Deathweed
                    Deco[S.Bookcase] = (TileID.Bookcases, 7); // Ebonwood
                    Deco[S.Sofa] = (TileID.Benches, 2); // Ebonwood
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 10); //* Ebonwood
                    Deco[S.PaintingWallpaper] = (WallID.BluegreenWallpaper, 0);
                    Deco[S.Dresser] = (TileID.Dressers, 1); //* Ebonwood
                    Deco[S.Piano] = (TileID.Pianos, 1); //* Ebonwood
                    Deco[S.Column] = (TileID.GraniteColumn, 0);
                    Deco[S.ColumnPaint] = (PaintID.GrayPaint, 0);

                    //altar
                    Deco[S.MiddleWall] = (WallID.Bone, 0);
                    Deco[S.MiddleWallPaint] = (PaintID.RedPaint, 0);
                    Deco[S.AltarSteps] = (TileID.Platforms, 10); //Brass Shelf
                    if (subStyle == 1) Deco[S.AltarSteps] = (TileID.Platforms, 22); //Skyware
                    Deco[S.AltarStepsPaint] = (0, 0);
                    Deco[S.AltarDeco] = (TileID.Platforms, 10); //Brass Shelf
                    if (subStyle == 1) Deco[S.AltarDeco] = (TileID.Platforms, 22); //Skyware
                    Deco[S.AltarDecoPaint] = (0, 0);
                    Deco[S.AltarWall] = (WallID.DemoniteBrick, 0);
                    Deco[S.AltarWallPaint] = (0, 0);
                    Deco[S.RunicWallPaint] = (0, 0);

                    // giant sword
                    Deco[S.SwordBrick] = (TileID.TeamBlockBlue, 0);
                    Deco[S.SwordHandleGemItem] = (ItemID.LargeSapphire, 0);
                    Deco[S.SwordHandlePaint] = (PaintID.BluePaint, 0);
                    Deco[S.SwordCrossGPaint] = (PaintID.BluePaint, 0);
                    Deco[S.SwordEnergyFlowWall] = (WallID.GrinchFingerWallpaper, 0);
                    Deco[S.SwordBladeEdgeWall] = (WallID.SapphireGemspark, 0);
                    Deco[S.SwordBladeWall] = (WallID.ShroomitePlating, 0);

                    // tree
                    Deco[S.TreePaint] = (0, 0);
                    Deco[S.BannerHangPlat] = (TileID.Platforms, 11); //Wood Shelf

                    // temple
                    Deco[S.TempleBrick] = (TileID.XenonMossBlock, 11);
                    Deco[S.TempleBrickBottomPaint] = (PaintID.ShadowPaint, 0);
                    Deco[S.TempleBrickAltarPaint] = (PaintID.DeepBluePaint, 0);
                    Deco[S.TempleStreamerPaint] = (PaintID.BluePaint, 0);
                    Deco[S.TempleSteps] = (TileID.Platforms, 22); //Skyware Platform
                    Deco[S.TempleStepsPaint] = (0, 0);
                    Deco[S.TempleColumnPaint] = (PaintID.DeepSkyBluePaint, 0);
                    Deco[S.TempleCeilingPlat] = (TileID.Platforms, 10); //Brass Shelf
                    break;
            }
        }

        public int GenerateChastisedChurch(int generationSide, int startX = 0)
        {
            if (!WorldGenMod.generateChastisedChurch) return 0;

            FillAndChooseStyle();

            // set start positions for the ChastisedChurch
            int startPosX, startPosY;

            if      (generationSide == -1) startPosX =                  WorldGen.genRand.Next(50, 100); // left world side
            else if (generationSide ==  1) startPosX = Main.maxTilesX - WorldGen.genRand.Next(50, 100); // right world side
            else                           startPosX = 0;

            if (generationSide == -1 && startX > 0) startPosX = startX + 10; //TODO: delete debug

            startPosY = Main.maxTilesY - 100;

            // add borders for below room creation
            belowRoomsAndStairs.Add(new Rectangle2P(50                 , startPosY, 1, 1));
            belowRoomsAndStairs.Add(new Rectangle2P(Main.maxTilesX - 50, startPosY, 1, 1));


            int totalTiles = 0;
            int maxTiles = Math.Min(Main.maxTilesX / 8, maxChurchLength);
            int actX = 0, actY = 0;
            int roomWidth = 0, roomHeight = 0;
            bool leftDoor, rightDoor;
            Rectangle2P actRoom, lastRoom = Rectangle2P.Empty; // Rectangle2P for later filling a possible gap between the rooms

            while (totalTiles < maxTiles)
            {
                roomWidth = WorldGen.genRand.Next(roomSizes.xMin, roomSizes.xMax + 1);
                if      (forceEvenRoom == 1) roomWidth -= (roomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) roomWidth -= (roomWidth % 2) + 1; //make room always uneven

                roomHeight = WorldGen.genRand.Next(roomSizes.yMin, roomSizes.yMax + 1);

                float ratio = roomHeight / roomWidth;
                int roofHeight;
                if (ratio > 1.2f) roofHeight = WorldGen.genRand.Next(10, 20);
                else              roofHeight = WorldGen.genRand.Next(5, 10);


                if (generationSide == -1) // left world side --> generating from the left to the right
                {
                    leftDoor = totalTiles != 0;
                    rightDoor = (totalTiles + roomWidth) < maxTiles; //next room would leave the given space
                    actX = startPosX + totalTiles;
                    actY = startPosY - roomHeight;

                    actRoom = GenerateRoom(new Rectangle2P(actX, actY, roomWidth, roomHeight), lastRoom, roofHeight, leftDoor, rightDoor);

                    if (gap > 0 && !actRoom.IsEmpty() && !lastRoom.IsEmpty())   FillGap(lastRoom, actRoom);

                    totalTiles += roomWidth + gap;
                    lastRoom = actRoom;
                }
                else if (generationSide == 1) // right world side --> generating from the right to the left
                {
                    rightDoor = totalTiles != 0;
                    leftDoor = (totalTiles + roomWidth) < maxTiles; //next room would leave the given space
                    actX = startPosX - totalTiles - roomWidth;
                    actY = startPosY - roomHeight;

                    actRoom = GenerateRoom(new Rectangle2P(actX, actY, roomWidth, roomHeight), lastRoom, roofHeight, leftDoor, rightDoor);

                    if (gap > 0 && !actRoom.IsEmpty() && !lastRoom.IsEmpty()) FillGap(lastRoom, actRoom);

                    totalTiles += roomWidth + gap;
                    lastRoom = actRoom;
                }
            }

            return (actX + roomWidth);
        }

        /// <summary>
        /// Creates a room of the Chastised Church 
        /// </summary>
        /// <param name="room">The area of the room, including walls</param>
        /// <param name="previousRoom">The area of the previously placed room, including walls</param>
        /// <param name="roofHeight">Tile height of the roof on top of a room</param>
        /// <param name="leftDoor">States if the room has a left door (e.g. if there is another room on the left)</param>
        /// <param name="rightDoor">States if the room has a right door (e.g. if there is another room on the right)</param>
        /// <param name="isStairCase">Stating if this room is a staircase, leading to below rooms</param>
        /// <param name="isBelowRoom">Stating if this room is a below room (below the main line)</param>
        /// 
        /// <returns>Hands back the room dimensions input or an empty room if the creation failed</returns>
        public Rectangle2P GenerateRoom(Rectangle2P room, Rectangle2P previousRoom, int roofHeight = 0, bool leftDoor = false, bool rightDoor = false, bool isStairCase = false, bool isBelowRoom = false)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");

            int x, y; //temp variables for later calculations;

            if (room.Y1 >= Main.maxTilesY || room.X1 >= Main.maxTilesX || room.X0 <= 0) return Rectangle2P.Empty;



            // calculate if this room will have a "cellar".... is needed now for creating this rooms doors properly
            #region cellar calculation
            (bool left, bool right) checkBelowRoomDistanceResult = Func.CheckBelowRoomDistance(belowRoomsAndStairs, room, belowRoomSizes);
            bool belowRoomLeftPossible = checkBelowRoomDistanceResult.left;
            bool belowRoomRightPossible = checkBelowRoomDistanceResult.right;

            bool downStairsPossible = (belowRoomLeftPossible || belowRoomRightPossible) && !isStairCase && !isBelowRoom;

            bool belowRoomLeftExist = false, belowRoomRightExist = false, downStairsExist = false;
            int staircaseWidth = 8;
            int staircaseHeight = 31 + gap; // each round of the spiral staircase is 8 Tiles
            int staircaseXTiles, staircaseYTiles;

            Rectangle2P belowRoomStaircase = Rectangle2P.Empty;
            if (downStairsPossible)
            {
                // define staircase size
                if (forceEvenRoom == 1) // even room
                {
                    staircaseXTiles = staircaseWidth + wThick * 2;
                    staircaseYTiles = staircaseHeight + wThick * 2;

                    belowRoomStaircase = new(room.XCenter - (((staircaseXTiles) / 2) - 1), freeR.Y1 + 1, staircaseXTiles, staircaseYTiles);
                }
                else if (forceEvenRoom == 0) // uneven room
                {
                    staircaseXTiles = 9 + wThick * 2;
                    staircaseYTiles = 33 + wThick * 2; // each round of the spiral staircase is 11 Tiles

                    belowRoomStaircase = new(room.XCenter - ((staircaseXTiles) / 2), freeR.Y1 + 1, staircaseXTiles, staircaseYTiles);
                }
                else belowRoomStaircase = Rectangle2P.Empty; // nothing because...who knows?


                // define which below rooms to generate
                
                int highChance = 100, lowChance = 100;
                if (belowRoomLeftPossible && belowRoomRightPossible)
                {
                    // decide randomly which room has a high spawn chance
                    if (Chance.Simple())
                    {
                        belowRoomLeftExist = Chance.Perc(highChance);
                        if (belowRoomLeftExist) belowRoomRightExist = Chance.Perc(lowChance);
                        else belowRoomRightExist = Chance.Perc(highChance);
                    }
                    else
                    {
                        belowRoomRightExist = Chance.Perc(highChance);
                        if (belowRoomRightExist) belowRoomLeftExist = Chance.Perc(lowChance);
                        else belowRoomLeftExist = Chance.Perc(highChance);
                    }
                }
                else if (belowRoomLeftPossible) belowRoomLeftExist = Chance.Perc(highChance);
                else if (belowRoomRightPossible) belowRoomRightExist = Chance.Perc(highChance);


                downStairsExist = (room.XTiles <= 25) && !belowRoomStaircase.IsEmpty() && (belowRoomLeftExist || belowRoomRightExist);
            }
            #endregion


            #region door rectangles
            Dictionary<int, (bool doorExist, Rectangle2P doorRect)> doors = []; // a dictionary for working and sending the doors in a compact way

            int leftRightDoorsYTiles = 5; // how many tiles the left and right doors are high
            y = freeR.Y1 - (leftRightDoorsYTiles - 1);
            Rectangle2P leftDoorRect  = new(room.X0     , y, wThick, leftRightDoorsYTiles);
            Rectangle2P rightDoorRect = new(freeR.X1 + 1, y, wThick, leftRightDoorsYTiles);

            int upDownDoorXTiles = staircaseWidth; // how many tiles the up and down doors are wide
            int adjustX = 0; // init
            if (freeR.XTiles % 2 == 1 && upDownDoorXTiles % 2 == 0) upDownDoorXTiles++; // an odd number of x-tiles in the room also requires an odd number of platforms so the door is symmetrical
            else adjustX = -1; //in even XTile rooms there is a 2-tile-center and XCenter will be the left tile of the two. To center an even-numberd door in this room, you have to subtract 1. Odd XTile rooms are fine
            x = (freeR.XCenter) - (upDownDoorXTiles / 2 + adjustX);
            Rectangle2P upDoorRect   = new(x, room.Y0     , upDownDoorXTiles, wThick);
            Rectangle2P downDoorRect = new(x, freeR.Y1 + 1, upDownDoorXTiles, wThick);

            doors.Add(Door.Left, (leftDoor, leftDoorRect));
            doors.Add(Door.Right, (rightDoor, rightDoorRect));
            doors.Add(Door.Up, (isStairCase, upDoorRect));
            doors.Add(Door.Down, (downStairsExist, downDoorRect));
            #endregion


            #region carve out room and place bricks
            for (x = room.X0; x <= room.X1; x++)
            {
                for (y = room.Y0; y <= room.Y1; y++)
                {
                    if (isStairCase && y < freeR.Y0) continue; // staircases overlap with the above laying room (they share the same floor / ceiling), don't touch that!

                    WorldGen.KillWall(x, y);
                    WorldGen.KillTile(x, y);
                    WorldGen.EmptyLiquid(x, y);

                    if (y == freeR.Y1 + 1) // the floor height of this room
                    {
                        if ((!doors[Door.Left].doorExist && x < freeR.X0))       { WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true); WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id); }
                        else if ((!doors[Door.Right].doorExist && x > freeR.X1)) { WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true); WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id); }
                        else if (isBelowRoom || isStairCase)                     { WorldGen.PlaceTile(x, y, Deco[S.BelowRoomFloor].id, true, true); }
                        else                                                     { WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true); WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id); }
                    }
                    else if (!freeR.Contains(x, y)) // x,y  are not in the free room? -> put outer wall bricks!
                    {
                        WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true);
                        WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id);
                    }
                }
            }
            #endregion


            #region place backwall
            bool noBreakPoint1, noBreakPoint2;
            Vector2 wallBreakPoint1, wallBreakPoint2;
            bool awayEnough1, awayEnough2;
            Dictionary<int, (bool exist, Vector2 point)> wallBreak = []; // for later sending it to DecorateRoom()

            if (room.XTiles < 30) // just one breakpoint
            {
                noBreakPoint1 = Chance.Perc(40);
                wallBreakPoint1 = new(room.X0 + WorldGen.genRand.Next(room.XTiles), room.Y0 + WorldGen.genRand.Next(room.YTiles));

                noBreakPoint2 = true;
                wallBreakPoint2 = new(room.X0 + WorldGen.genRand.Next(room.XTiles), room.Y0 + WorldGen.genRand.Next(room.YTiles)); //create a vecor, just to be safe
            }
            else // two breakpoints, one on the left, another one on the right
            {
                noBreakPoint1 = Chance.Perc(40);
                wallBreakPoint1 = new(room.X0 + WorldGen.genRand.Next(room.XTiles) / 2, room.Y0 + WorldGen.genRand.Next(room.YTiles));

                noBreakPoint2 = Chance.Perc(40);
                wallBreakPoint2 = new(room.X1 - WorldGen.genRand.Next(room.XTiles) / 2, room.Y0 + WorldGen.genRand.Next(room.YTiles));
            }

            wallBreak.Add(BP.Left,  (!noBreakPoint1, wallBreakPoint1));
            wallBreak.Add(BP.Right, (!noBreakPoint2, wallBreakPoint2));


            int outdoFreeR = 1; // how many tiles outside of freeR the backwall shall be placed (so the edge of the backwall won't be visible at the border of freeR)
            
            for (int i = freeR.X0 - outdoFreeR; i <= freeR.X1 + outdoFreeR; i++)
            {
                for (int j = freeR.Y0 - outdoFreeR; j <= freeR.Y1 + outdoFreeR; j++)
                {
                    if (noBreakPoint1) awayEnough1 = true;
                    else               awayEnough1 = Vector2.Distance(new Vector2(i, j), wallBreakPoint1) > WorldGen.genRand.NextFloat(3f, 12f);

                    if (noBreakPoint2) awayEnough2 = true;
                    else               awayEnough2 = Vector2.Distance(new Vector2(i, j), wallBreakPoint2) > WorldGen.genRand.NextFloat(3f, 12f);


                    if (awayEnough1 && awayEnough2)
                    {
                        WorldGen.PlaceWall(i, j, Deco[S.BackWall].id);
                        if(Deco[S.BackWallPaint].id > 0) WorldGen.paintWall(i, j, (byte)Deco[S.BackWallPaint].id);
                    } 
                    else WorldGen.PlaceWall(i, j, Deco[S.CrookedWall].id);
                }
            }
            #endregion


            #region put doors
            //carve out doors
            for (int doorNum = 0; doorNum < doors.Count; doorNum++)
            {
                if (isStairCase && doorNum == Door.Up) continue; // this door was already created by the previous room, no need to do it again

                if (doors[doorNum].doorExist)
                {
                    for (int i = doors[doorNum].doorRect.X0; i <= doors[doorNum].doorRect.X1; i++)
                    {
                        for (int j = doors[doorNum].doorRect.Y0; j <= doors[doorNum].doorRect.Y1; j++)
                        {
                            WorldGen.KillTile(i, j);
                        }
                    }
                }
            }

            // place background walls
            for (int doorNum = 0; doorNum < doors.Count; doorNum++)
            {
                if (doors[doorNum].doorExist)
                {
                    for (int i = doors[doorNum].doorRect.X0; i <= doors[doorNum].doorRect.X1; i++)
                    {
                        for (int j = doors[doorNum].doorRect.Y0; j <= doors[doorNum].doorRect.Y1; j++)
                        {
                            if (noBreakPoint1) awayEnough1 = true;
                            else               awayEnough1 = Vector2.Distance(new Vector2(i, j), wallBreakPoint1) > WorldGen.genRand.NextFloat(1f, 7f);

                            if (noBreakPoint2) awayEnough2 = true;
                            else               awayEnough2 = Vector2.Distance(new Vector2(i, j), wallBreakPoint2) > WorldGen.genRand.NextFloat(1f, 7f);

                            if (awayEnough1 && awayEnough2)
                            {
                                WorldGen.KillWall(i, j);
                                WorldGen.PlaceWall(i, j, Deco[S.DoorWall].id);
                            }
                        }
                    }
                }
            }


            // put up/down door platforms and additional background walls at special positions
            if (leftDoor)
            {
                x = leftDoorRect.X1;
                y = leftDoorRect.Y0 - 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will later get a slope. Put the doorWallType there so it looks nicer

                x = leftDoorRect.X0;
                y = leftDoorRect.Y1 + 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.BackWall].id); // There is a one background wall tile missing here as this coordinates used to be on the border of the room. Adding this tile is not a big deal in the end, but little things matter!
            }

            if (rightDoor)
            {
                x = rightDoorRect.X0;
                y = rightDoorRect.Y0 - 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will later get a slope. Put the doorWallType there so it looks nicer

                x = rightDoorRect.X1;
                y = rightDoorRect.Y1 + 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.BackWall].id); // There is a one background wall tile missing here as this coordinates used to be on the border of the room. Adding this tile is not a big deal in the end, but little things matter!
            }

            if (doors[Door.Down].doorExist)
            {
                int j = downDoorRect.Y0;
                for (int i = downDoorRect.X0; i <= downDoorRect.X1; i++)
                {
                    WorldGen.PlaceTile(i, j, Deco[S.DoorPlat].id, mute: true, forced: true, style: Deco[S.DoorPlat].style);
                    WorldGen.paintTile(i, j, (byte)Deco[S.DoorPlatPaint].id);
                }

                x = downDoorRect.X0 - 1;
                y = downDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = downDoorRect.X1 + 1;
                y = downDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer
            }

            if (doors[Door.Up].doorExist)
            {
                //platform is already there, no need to do it again
                //int j = upDoorRect.Y0;
                //for (int i = upDoorRect.X0; i <= upDoorRect.X1; i++)
                //{
                //    WorldGen.PlaceTile(i, j, Deco[S.DoorPlat].id, mute: true, forced: true, style: Deco[S.DoorPlat].style);
                //    WorldGen.paintTile(i, j, (byte)Deco[S.DoorPlatPaint].id);
                //}

                x = upDoorRect.X0 - 1;
                y = upDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = upDoorRect.X1 + 1;
                y = upDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall].id); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer
            }

            #region put actual doors
            if ((previousRoom.X0 < room.X0) || (isStairCase && leftDoor)) // rooms advancing from left to right: put left door
            {
                bool placed;

                x = leftDoorRect.X0; // right side rooms always have a left door
                y = leftDoorRect.Y1;
                placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // left gate

                if (placed )
                {
                    Func.GateTurn(x, y);
                    for (int j = leftDoorRect.Y0; j <= leftDoorRect.Y1; j++) WorldGen.paintTile(x, j, (byte)Deco[S.DoorPaint].id);
                }

                //put a tile in front of the left gate, that looks cool (will get shaped later)
                WorldGen.PlaceTile(leftDoorRect.X1, leftDoorRect.Y0, Deco[S.Brick].id);
                WorldGen.paintTile(leftDoorRect.X1, leftDoorRect.Y0, (byte)Deco[S.BrickPaint].id);



                if (doors[Door.Right].doorExist)
                {
                    if (gap > 0) // in case there is a gap between side rooms and this right side room also has a right door
                    {
                        x = rightDoorRect.X1;
                        y = rightDoorRect.Y1;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed && Deco[S.DoorPaint].id > PaintID.None)
                        {
                            for (int j = rightDoorRect.Y0; j <= rightDoorRect.Y1; j++) WorldGen.paintTile(x, j, (byte)Deco[S.DoorPaint].id);
                        }
                    }

                    //put a tile in front of the right gate, that looks cool (will get shaped later)
                    WorldGen.PlaceTile(rightDoorRect.X0, rightDoorRect.Y0, Deco[S.Brick].id);
                    WorldGen.paintTile(rightDoorRect.X0, rightDoorRect.Y0, (byte)Deco[S.BrickPaint].id);
                }
            }

            else if ((previousRoom.X0 > room.X0) || (isStairCase && rightDoor))// rooms advancing from right to left: put right door
            {
                bool placed;

                x = rightDoorRect.X1; // left side rooms always have a right door
                y = rightDoorRect.Y1;
                placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // right gate

                if (placed && Deco[S.DoorPaint].id > PaintID.None)
                {
                    for (int j = rightDoorRect.Y0; j <= rightDoorRect.Y1; j++) WorldGen.paintTile(x, j, (byte)Deco[S.DoorPaint].id);
                }

                //put a tile in front of the gate, that looks cool (will get shaped later)
                WorldGen.PlaceTile(rightDoorRect.X0, rightDoorRect.Y0, Deco[S.Brick].id);
                WorldGen.paintTile(rightDoorRect.X0, rightDoorRect.Y0, (byte)Deco[S.BrickPaint].id);


                if (doors[Door.Left].doorExist) 
                {
                    if (gap > 0) // in case there is a gap between side rooms and this left side room also has a left door
                    {
                        x = leftDoorRect.X0;
                        y = leftDoorRect.Y1;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed)
                        {
                            Func.GateTurn(x, y);
                            for (int j = leftDoorRect.Y0; j <= leftDoorRect.Y1; j++) WorldGen.paintTile(x, j, (byte)Deco[S.DoorPaint].id);
                        }
                    }

                    //put a tile in front of the left gate, that looks cool (will get shaped later)
                    WorldGen.PlaceTile(leftDoorRect.X1, leftDoorRect.Y0, Deco[S.Brick].id);
                    WorldGen.paintTile(leftDoorRect.X1, leftDoorRect.Y0, (byte)Deco[S.BrickPaint].id);
                }
            }
            #endregion
            #endregion


            #region put roof
            if (!isStairCase && !isBelowRoom) //only the main line rooms have a roof
            {
                int left = room.X0;
                int right = room.X1;
                if (gap < 0 && !previousRoom.IsEmpty()) // roof would overlap with an existing previous room, left or right must be corrected
                {
                    bool leftToRight = (room.X0 - previousRoom.X0) > 0;
                    if      (leftToRight && previousRoom.Y0 < room.Y0) left += Math.Abs(gap); // if a "left-to-right" creating Chastised Church has no left door, then that's the start and there is no overlapping
                    else if (!leftToRight && previousRoom.Y0 < room.Y0) right -= Math.Abs(gap);
                }

                int leftHighest, rightHighest; // x where the roof is heighest
                if (room.XEven)
                { 
                    leftHighest = room.XCenter;
                    rightHighest = room.XCenter + 1; 
                }
                else leftHighest = rightHighest = room.XCenter;

                int leftDiff = Math.Abs(left - leftHighest);
                int rightDiff = Math.Abs(right - rightHighest);

                float currentMultiplier;
                while (left <= right)
                {   
                    currentMultiplier = 1f - ((float)Math.Abs(left - leftHighest) / (float)leftDiff);
                    for (int j1 = 0; j1 < (int)(roofHeight * currentMultiplier); j1++)
                    {
                        int j = room.Y0 - 1 - j1;
                        WorldGen.PlaceTile(left, j, Deco[S.RoofBrick].id, true, true);
                        WorldGen.paintTile(left, j, (byte)Deco[S.RoofBrickPaint].id);
                    }

                    currentMultiplier = 1f - ((float)Math.Abs(right - rightHighest) / (float)rightDiff);
                    for (int j1 = 0; j1 < (int)(roofHeight * currentMultiplier); j1++)
                    {
                        int j = room.Y0 - 1 - j1;
                        WorldGen.PlaceTile(right, j, Deco[S.RoofBrick].id, true, true);
                        WorldGen.paintTile(right, j, (byte)Deco[S.RoofBrickPaint].id);
                    }

                    left++;
                    right--;
                }
            }
            #endregion


            #region slopes
            // if one would form a rhombus: 0 is no slope, 1 is up-right corner, 2 is up-left corner, 3 is down-right corner, 4 is down-left corner.
            if (leftDoor)
            {
                WorldGen.SlopeTile(leftDoorRect.X1, leftDoorRect.Y0, (int)Func.SlopeVal.BotRight); // door right corner
            }
            if (rightDoor)
            {
                WorldGen.SlopeTile(rightDoorRect.X0, rightDoorRect.Y0, (int)Func.SlopeVal.BotLeft); // door left corner
            }
            //if (belowCount > 0) --> staircase without slope
            //{
            //    WorldGen.SlopeTile(upDoorRect.X0 - 1, upDoorRect.Y1, (int)Func.SlopeVal.BotRight); // updoor left corner
            //    WorldGen.SlopeTile(upDoorRect.X1 + 1, upDoorRect.Y1, (int)Func.SlopeVal.BotLeft); // updoor right corner
            //}
            #endregion


            #region create below rooms
            Rectangle2P resultStaircase = Rectangle2P.Empty;
            Rectangle2P resultBelowLeft = Rectangle2P.Empty;
            Rectangle2P resultBelowRight = Rectangle2P.Empty;
            if (downStairsExist)
            {
                resultStaircase = GenerateRoom(belowRoomStaircase, Rectangle2P.Empty, leftDoor: belowRoomLeftExist, rightDoor: belowRoomRightExist, isStairCase: true);
            }
            if (isStairCase && leftDoor)
            {
                int belowRoomWidth = WorldGen.genRand.Next(belowRoomSizes.xMin, belowRoomSizes.xMax + 1);
                if (forceEvenRoom == 1) belowRoomWidth -= (belowRoomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) belowRoomWidth -= (belowRoomWidth % 2) + 1; //make room always uneven

                int belowRoomHeight = WorldGen.genRand.Next(belowRoomSizes.yMin, belowRoomSizes.yMax + 1);

                Rectangle2P belowRoomLeft = new(room.X0 - (belowRoomWidth + gap), room.Y1 - (belowRoomHeight + gap), belowRoomWidth, belowRoomHeight);

                resultBelowLeft = GenerateRoom(belowRoomLeft, room, rightDoor: true, isBelowRoom: true);
            }
            if (isStairCase && rightDoor)
            {
                int belowRoomWidth = WorldGen.genRand.Next(belowRoomSizes.xMin, belowRoomSizes.xMax + 1);
                if (forceEvenRoom == 1) belowRoomWidth -= (belowRoomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) belowRoomWidth -= (belowRoomWidth % 2) + 1; //make room always uneven

                int belowRoomHeight = WorldGen.genRand.Next(belowRoomSizes.yMin, belowRoomSizes.yMax + 1);

                Rectangle2P belowRoomRight = new(room.X1 + 1 + gap, room.Y1 - (belowRoomHeight + gap), belowRoomWidth, belowRoomHeight);

                resultBelowRight = GenerateRoom(belowRoomRight, room, leftDoor: true, isBelowRoom: true);
            }

            // add created rooms to the list of existing ones
            if (isStairCase && !resultBelowLeft.IsEmpty())  belowRoomsAndStairs.Add(resultBelowLeft);
            if (isStairCase && !resultBelowRight.IsEmpty()) belowRoomsAndStairs.Add(resultBelowRight);
            if (isStairCase)                                belowRoomsAndStairs.Add(room);
            #endregion

            //TODO:
            #region "don't know if it stays" stuff
            // Patches of tiles
            //if (belowCount > 0 && Chance.Perc(33) || Chance.Perc(16))
            //{
            //    WorldGen.TileRunner(room.X0 + WorldGen.genRand.Next(room.XTiles), room.Y1 - 1, WorldGen.genRand.NextFloat(6f, 10f), 3, TileID.Obsidian, true);
            //}
            //else if (Chance.Perc(25))
            //{
            //    WorldGen.TileRunner(room.X0 + WorldGen.genRand.Next(room.XTiles), room.Y1 - 1, WorldGen.genRand.NextFloat(3f, 7f), 3, Deco[S.EvilTile], true);
            //}

            // Obsidian platforms
            //if (WorldGen.genRand.NextBool() && room.YTiles >= 12)
            //{
            //    int j = room.Y0 + WorldGen.genRand.Next(4, room.YTiles - 6);
            //    for (int i = 0; i < WorldGen.genRand.Next(3, 7); i++)
            //    {
            //        WorldGen.PlaceTile(i + room.X0 + 2, j, TileID.Platforms, true, false, style: 13); // Obsidian Platform
            //        WorldGen.PlaceTile(i + room.X0 + 2, j - 1, TileID.Books, true, false, style: WorldGen.genRand.Next(6));
            //    }
            //}

            //if (WorldGen.genRand.NextBool() && room.YTiles >= 12)
            //{
            //    int j = room.Y0 + WorldGen.genRand.Next(4, room.YTiles - 6);
            //    for (int i = 0; i < WorldGen.genRand.Next(3, 7); i++)
            //    {
            //        WorldGen.PlaceTile(-i + room.X0 + room.XTiles - 2, j, TileID.Platforms, true, false, style: 13); // Obsidian Platform
            //        WorldGen.PlaceTile(-i + room.X0 + room.XTiles - 2, j - 1, TileID.Books, true, false, style: WorldGen.genRand.Next(6));
            //    }
            //}

            //for (int i = room.X0; i <= room.X1; i++)
            //{
            //    int j = room.Y0 + 2;
            //    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 13); // Obsidian Platform
            //}
            #endregion


            if (!isStairCase && !isBelowRoom) DecorateRoom(room, doors, wallBreak);
            if (isStairCase) DecorateStairCase(room, doors, wallBreak);
            if (isBelowRoom) DecorateBelowRoom(room, doors, wallBreak);


            return room;
        }


        /// <summary>
        /// The main method for choosing and running a rooms decoration
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        /// <param name="wallBreak">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
        public void DecorateRoom(Rectangle2P room, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors, IDictionary<int, (bool exist, Vector2 point)> wallBreak)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");


            // init variables
            bool placed, placed2;
            (bool success, int x, int y) placeResult, placeResult2;
            (bool success, Rectangle2P altar) altarResult;
            Rectangle2P area1, area2, area3, noBlock = Rectangle2P.Empty; // for creating areas for random placement
            List<(int x, int y)> rememberPos = []; // for remembering positions
            List<(ushort TileID, int style, byte chance)> randomItems = [], randomItems2 = []; // for random item placement
            (ushort id, int style, byte chance) randomItem, randomItem2; // for random item placement

            // for pattern placement
            List<int> randomStyles = [];
            List<String> pattern = [];
            Dictionary<char, (int variant, int id, int paint, (int id, int chance) overWrite)> patternData = [];
            (int id, int chance) overWrite = (Deco[S.CrookedWall].id, 60);
            int height, diff;

            int x, y, chestID, unusedXTiles, num, numOld;


            // for window placement
            List<Rectangle2P> windowsPairs = []; // ascending indexes refer to windows in the room like this: 6 windows (0 2 4 5 3 1), 8 windows (0 2 4 6 7 5 3 1) etc.
            List<Rectangle2P> windowsOrder = []; // ascending indexes refer to windows in the room like this: 6 windows (0 1 2 3 4 5), 8 windows (0 1 2 3 4 5 6 7) etc.
            List<Rectangle2P> spacesOrder = []; // ascending indexes refer to the spaces between windows in the room like this: 2 spaces (4 windows) (W 0 W | W 1 W), 4 spaces (6 windows) (W 0 W 1 W | W 2 W 3 W) etc.
            Rectangle2P middleSpace = Rectangle2P.Empty; // the middle space if the room has pairs of windows
            
            int windowXTiles = 4;

            int windowYMargin = 2; // how many Tiles the window shall be away from the ceiling / floor
            int windowY0 = freeR.Y0 + windowYMargin; // height where the window starts
            int windowYTiles = freeR.YTiles - (2 * windowYMargin); // the YTiles height of a window

            bool awayEnough1, awayEnough2, windowsExist, windowPairsExist, middleSpaceExist = false, windowDistanceXTilesOdd = false;

            //choose the upper room decoration at random
            int roomDeco = WorldGen.genRand.Next(1); //TODO: don't forget to put the correct values in the end!

            roomDeco = 0;


            switch (roomDeco)
            {
                case 0: // Statues in front of Windows

                    #region windows
                    windowsPairs.Clear();

                    // create window rectangles
                    if ( freeR.YTiles > (windowXTiles + 2*windowYMargin) && freeR.XTiles >= (windowXTiles + 2*2)) //minimal window size: windowXTiles * windowXTiles
                    {
                        if (freeR.XTiles <= 14) // narrow room, place window in the middle
                        {
                            int windowCenterOffset = (windowXTiles / 2) - 1 + (windowXTiles % 2); // to center the window at a specified x position

                            windowsPairs.Add(new Rectangle2P(freeR.XCenter - windowCenterOffset, windowY0, windowXTiles, windowYTiles));
                        }

                        else // symmetrical window pairs with spaces in between
                        {
                            #region create pairs
                            int windowXMargin = 2; // how many tiles the outer windows-pair shall be away from the left / right wall
                            int windowDistanceXTiles = 4; // XTiles between two windows
                            windowDistanceXTilesOdd = Chance.Simple();
                            if (windowDistanceXTilesOdd)  windowDistanceXTiles++;

                            int windowLeftX0 = freeR.X0 + windowXMargin; // init
                            int windowRightX0 = freeR.X1 - windowXMargin - (windowXTiles - 1); // init

                            while (windowLeftX0 + windowXTiles < freeR.XCenter)
                            {
                                windowsPairs.Add(new Rectangle2P(windowLeftX0, windowY0, windowXTiles, windowYTiles)); // left room side
                                windowsPairs.Add(new Rectangle2P(windowRightX0, windowY0, windowXTiles, windowYTiles)); // right room side

                                windowLeftX0 += (windowXTiles + windowDistanceXTiles);
                                windowRightX0 -= (windowXTiles + windowDistanceXTiles);
                            }
                            #endregion

                            #region get array into room order
                            // from (0 2 4 5 3 1) to (0 1 2 3 4 5)
                            for (int i = 0; i < windowsPairs.Count; i+=2) // left room sided windows
                            {
                                windowsOrder.Add(windowsPairs[i]);
                            }
                            for (int i = windowsPairs.Count - 1; i > 0; i -= 2) // left room sided windows
                            {
                                windowsOrder.Add(windowsPairs[i]);
                            }
                            #endregion

                            #region gather spaces between windows
                            
                            middleSpace = new Rectangle2P(xTopLeft:     windowsPairs[windowsPairs.Count - 2].X1 + 1,
                                                          xBottomRight: windowsPairs[windowsPairs.Count - 1].X0 - 1,
                                                          yTopLeft:     windowsPairs[windowsPairs.Count - 2].Y0,
                                                          yBottomRight: windowsPairs[windowsPairs.Count - 1].Y1,
                                                          dummy: "dummyString");
                            middleSpaceExist = middleSpace.XTiles > 0;


                            for (int i = 1; i < windowsOrder.Count; i++)
                            {
                                if (i == (windowsOrder.Count / 2)) continue; // exclude middle space

                                spacesOrder.Add(new Rectangle2P(xTopLeft:     windowsOrder[i - 1].X1 + 1,
                                                                xBottomRight: windowsOrder[i    ].X0 - 1,
                                                                yTopLeft:     windowsOrder[i - 1].Y0,
                                                                yBottomRight: windowsOrder[i    ].Y1,
                                                                dummy: "dummyString"));
                            }
                            #endregion
                        }
                    }

                    // put windows
                    windowsExist = windowsPairs.Count > 0;
                    windowPairsExist = windowsPairs.Count > 1;
                    if (windowsExist)
                    {
                        foreach (Rectangle2P windowRect in windowsPairs)
                        {
                            for (int i = windowRect.X0; i <= windowRect.X1; i++)
                            {
                                for (int j = windowRect.Y0; j <= windowRect.Y1; j++)
                                {
                                    if (Main.tile[i,j].WallType != Deco[S.CrookedWall].id)
                                    {
                                        WorldGen.KillWall(i,j);
                                        WorldGen.PlaceWall(i,j, Deco[S.WindowWall].id);
                                        if (Deco[S.WindowPaint].id > 0) WorldGen.paintWall(i,j, (byte)Deco[S.WindowPaint].id);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    #region modify shape of windows
                    if (windowsExist)
                    {
                        int windowHeight = freeR.YTiles - 2 * windowYMargin;

                        List<int> windowShapes = [];
                        if (windowsPairs.Count < 2)
                        {
                            windowShapes.Add(0); // completely rectangular
                            windowShapes.Add(1); // upper left & right corner missing
                            windowShapes.Add(2); // upper end is "plus" shaped
                        }
                        else
                        {
                            windowShapes.Add(0); // completely rectangular
                            windowShapes.Add(1); // upper left & right corner missing
                            windowShapes.Add(2); // upper end is "plus" shaped
                            windowShapes.Add(3); // upper end is skewed 
                            windowShapes.Add(4); // cases 0,1,2 in random order
                        }



                        List<System.Drawing.Point> windowPoints = [];
                        switch (windowShapes[WorldGen.genRand.Next(windowShapes.Count)])
                        {
                            // completely rectangular
                            case 0:

                                // nothing to do actually :-P

                                break;

                            // upper left & right corner missing
                            case 1: 

                                foreach (Rectangle2P windowRect in windowsPairs)
                                {
                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X0, y: windowRect.Y0)); // upper left corner
                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X1, y: windowRect.Y0)); // upper right corner
                                }

                                break;

                            // upper end is "plus" shaped
                            case 2:

                                if (windowHeight < 6) break;

                                foreach (Rectangle2P windowRect in windowsPairs)
                                {
                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X0, y: windowRect.Y0)); // upper left corner
                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X1, y: windowRect.Y0)); // upper right corner

                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X0, y: windowRect.Y0 + 3)); // point for cross shaped form
                                    windowPoints.Add(new System.Drawing.Point(x: windowRect.X1, y: windowRect.Y0 + 3)); // point for cross shaped form
                                }

                                break;

                            // upper end is skewed
                            case 3:

                                for (int i = 0; i < windowsPairs.Count; i = i+2) // 2 consecutive indexes belong to a pair of windows --> iterate over pairs
                                {
                                    #region state window pairs shape

                                    (int left, int right) windowShape = (0,0); // 0 = init value, 1 = cut out upper left corner, 2 = "plus" shaped form of the top, 3 = cut out upper right corner
                                    switch (i)
                                    {
                                        // outermost pair
                                        case 0:

                                            windowShape = (1,3);
                                            break;

                                        // second pair
                                        case 2:

                                            if (windowsPairs.Count == 6) windowShape = (2, 2);  // special case: form a "plus" in the middle window and the other pairs are /| + |\
                                            else                         windowShape = (3, 1);
                                            break;

                                        // third pair
                                        case 4:

                                            if (windowsPairs.Count == 6) windowShape = (3, 1); // special case: form a "plus" in the middle window and the other pairs are /| + |\
                                            else                         windowShape = (1, 3);
                                            break;

                                        // fourth pair
                                        case 6:

                                            windowShape = (3, 1);
                                            break;
                                    }
                                    #endregion

                                    #region work the stated shapes of a pair

                                    Rectangle2P win;
                                    for (num = 0; num < 2; num++)
                                    {
                                        int shape;
                                        if (num == 0) // left window of a pair
                                        {
                                            win = windowsPairs[i];
                                            shape = windowShape.left;
                                        }
                                        else // right window of a pair
                                        {
                                            win = windowsPairs[i + 1];
                                            shape = windowShape.right;
                                        }

                                        switch (shape)
                                        {
                                            // cut out upper left corner
                                            case 1:
                                                int ylow = win.Y0 - 1; // init
                                                if (windowHeight > 12 && windowsPairs.Count == 6) ylow = win.Y0 + 3; // special case: let the centered "plus" stand out

                                                for (x = win.X1; x >= win.X0; x--)
                                                {
                                                    for (y = win.Y0; y <= ylow; y++)
                                                    {
                                                        windowPoints.Add(new System.Drawing.Point(x, y)); // taking out the upper left corner of the window
                                                    }
                                                    ylow++;
                                                }
                                                break;

                                            // "plus" shaped form of the top
                                            case 2:

                                                windowPoints.Add(new System.Drawing.Point(win.X0, win.Y0)); // upper left corner
                                                windowPoints.Add(new System.Drawing.Point(win.X1, win.Y0)); // upper right corner

                                                windowPoints.Add(new System.Drawing.Point(win.X0, win.Y0 + 3)); // point for cross shaped form
                                                windowPoints.Add(new System.Drawing.Point(win.X1, win.Y0 + 3)); // point for cross shaped form

                                                break;

                                            // cut out upper left corner
                                            case 3:
                                                ylow = win.Y0 - 1; // init
                                                if (windowHeight > 12 && windowsPairs.Count == 6) ylow = win.Y0 + 3; // special case: let the centered "plus" stand out

                                                for (x = win.X0; x <= win.X1; x++)
                                                {
                                                    for (y = win.Y0; y <= ylow; y++)
                                                    {
                                                        windowPoints.Add(new System.Drawing.Point(x, y)); // taking out the upper right corner of the window
                                                    }
                                                    ylow++;
                                                }
                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    #endregion
                                }

                                break;

                            // cases 0,1,2 in random order
                            case 4:

                                List<int> avaiableShapes = [1, 2, 3]; // 1 = completely rectangular, 2 = upper left & right corner missing, 3 = upper end is "plus" shaped

                                for (int i = 0; i < windowsPairs.Count; i = i + 2) // 2 consecutive indexes belong to a pair of windows --> iterate over pairs
                                {
                                    if (avaiableShapes.Count == 0) // all forms picked?
                                    {
                                        avaiableShapes.Add(1); // refill
                                        avaiableShapes.Add(2);
                                        avaiableShapes.Add(3);
                                    }

                                    int windowShape = 0; // 0 = init value
                                    windowShape = avaiableShapes.PopAt(WorldGen.genRand.Next(avaiableShapes.Count)); // make the shapes not repeat until available ones are depleted!

                                    #region work the stated shapes of a pair

                                    Rectangle2P win;
                                    for (num = 0; num < 2; num++)
                                    {
                                        if (num == 0) win = windowsPairs[i]; // left window of a pair
                                        else          win = windowsPairs[i + 1];// right window of a pair

                                        switch (windowShape)
                                        {
                                            // completely rectangular
                                            case 1:
                                                break;

                                            // upper left & right corner missing
                                            case 2:

                                                windowPoints.Add(new System.Drawing.Point(win.X0, win.Y0)); // upper left corner
                                                windowPoints.Add(new System.Drawing.Point(win.X1, win.Y0)); // upper right corner

                                                break;

                                            // upper end is "plus" shaped
                                            case 3:

                                                windowPoints.Add(new System.Drawing.Point(win.X0, win.Y0)); // upper left corner
                                                windowPoints.Add(new System.Drawing.Point(win.X1, win.Y0)); // upper right corner

                                                windowPoints.Add(new System.Drawing.Point(win.X0, win.Y0 + 3)); // point for cross shaped form
                                                windowPoints.Add(new System.Drawing.Point(win.X1, win.Y0 + 3)); // point for cross shaped form

                                                break;

                                            default:
                                                break;
                                        }
                                    }
                                    #endregion
                                }


                                break;

                            default:
                                break;
                        }

                        Tile windowTile;
                        foreach (System.Drawing.Point windowPoint in windowPoints)
                        {
                            windowTile = Main.tile[windowPoint.X, windowPoint.Y];
                            if (windowTile.WallType == Deco[S.WindowWall].id)
                            {
                                // put normal backwall
                                WorldGen.KillWall(windowPoint.X, windowPoint.Y);
                                WorldGen.PlaceWall(windowPoint.X, windowPoint.Y, Deco[S.BackWall].id);
                                if (Deco[S.BackWallPaint].id > 0) WorldGen.paintWall(windowPoint.X, windowPoint.Y, (byte)Deco[S.BackWallPaint].id);
                            }
                        }
                    }
                    #endregion

                    #region statues in front of windows

                    randomItems.Clear();
                    randomItems.Add((TileID.Statues, 0,  95));//Armor
                    randomItems.Add((TileID.Statues, 1,  95));//Angel
                    randomItems.Add((TileID.Statues, 10, 95));//Skeleton
                    randomItems.Add((TileID.Statues, 11, 95));//Reaper
                    randomItems.Add((TileID.Statues, 13, 95));//Imp
                    randomItems.Add((TileID.Statues, 14, 95));//Gargoyle
                    randomItems.Add((TileID.Statues, 15, 95));//Gloom
                    randomItems.Add((TileID.Statues, 22, 95));//Cross
                    randomItems.Add((TileID.Statues, 30, 95));//Corrupt
                    randomItems.Add((TileID.Statues, 35, 95));//Eyeball
                    randomItems.Add((TileID.Statues, 63, 95));//Wall Creeper
                    randomItems.Add((TileID.Statues, 65, 95));//Drippler
                    randomItems.Add((TileID.Statues, 67, 95));//Bone Skeleton
                    randomItems.Add((TileID.Statues, 71, 95));//Pigron
                    randomItems.Add((TileID.Statues, 74, 95));//Armed Zombie
                    randomItems.Add((TileID.Statues, 75, 95));//Blood Zombie

                    if (windowsExist)
                    {
                        foreach (Rectangle2P windowRect in windowsPairs)
                        {
                            // prevent statue placement on down door
                            x = windowRect.XCenter;
                            y = freeR.Y1 + 1;
                            if ((Main.tile[x , y].TileType == Deco[S.DoorPlat].id) || (Main.tile[x + 1, y].TileType == Deco[S.DoorPlat].id)) continue;


                            // check if the pedestral can be big (4 Tiles)
                            y = freeR.Y1 + 1;
                            bool bigPedestral = ((windowRect.XCenter - 1 >= windowRect.X0) && (Main.tile[windowRect.XCenter - 1, y].TileType != Deco[S.DoorPlat].id)) &&
                                                ((windowRect.XCenter + 2 <= windowRect.X1) && (Main.tile[windowRect.XCenter + 2, y].TileType != Deco[S.DoorPlat].id));


                            // put pedestral in the middle of the window (XCenter and XCenter++)
                            y = freeR.Y1;
                            Func.PlaceSingleTile(windowRect.XCenter    , y, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id);
                            Func.PlaceSingleTile(windowRect.XCenter + 1, y, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id);

                            if (bigPedestral)
                            {
                                Func.PlaceSingleTile(windowRect.XCenter - 1, y, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id);
                                Func.PlaceSingleTile(windowRect.XCenter + 2, y, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id);
                            }


                            // put statue
                            randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                            if (Chance.Perc(randomItem.chance))    WorldGen.PlaceTile(windowRect.XCenter, y - 1, randomItem.id, style: randomItem.style);


                            //delete additional pedestral pieces that sometimes appear on "Smooth World" worldgen step
                            int beforePedestral = windowRect.XCenter - 1;
                            int afterPedestral = windowRect.XCenter + 2;
                            if (bigPedestral)
                            {
                                beforePedestral--;
                                afterPedestral++;
                            }
                            runAfterWorldCleanup.Add(() => { WorldGen.KillTile(beforePedestral, freeR.Y1); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(beforePedestral, freeR.Y1, Deco[S.Floor].id)]));
                            runAfterWorldCleanup.Add(() => { WorldGen.KillTile(afterPedestral, freeR.Y1); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(afterPedestral, freeR.Y1, Deco[S.Floor].id)]));

                        }
                    }
                    #endregion

                    #region fill spaces between windows
                    if (spacesOrder.Count > 0)
                    {
                        foreach (Rectangle2P windowRect in spacesOrder)
                        {
                            if (windowDistanceXTilesOdd) // preferably place odd-x-tiles-object so they come out centered
                            {
                                num = WorldGen.genRand.Next(6);
                                if (Deco[S.StyleSave].id == S.StyleBlueBrick) num = WorldGen.genRand.Next(5); // lava plants don't look nice with the blue style

                                if (Chance.Perc(5)) continue;

                                switch (num)
                                {
                                    case 0: // Lamp with up to 2 chairs next to it

                                        placed = WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                        if (placed) Func.UnlightLamp(windowRect.XCenter, freeR.Y1);

                                        if (Chance.Perc(85)) WorldGen.PlaceTile(windowRect.XCenter - 1, freeR.Y1, Deco[S.Chair].id, style: Deco[S.Chair].style);
                                        if (Chance.Perc(85))
                                        {
                                            WorldGen.PlaceTile(windowRect.XCenter + 1, freeR.Y1, Deco[S.Chair].id, style: Deco[S.Chair].style);
                                            Func.ChairTurnRight(windowRect.XCenter + 1, freeR.Y1);
                                        }
                                        break;

                                    case 1: // Bookcase
                                        WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Bookcase].id, style: Deco[S.Bookcase].style);
                                        break;

                                    case 2: // Table with stuff on it
                                        placed = WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Table].id, style: Deco[S.Table].style);

                                        if (placed)
                                        {
                                            randomItems.Clear();
                                            randomItems.Add((TileID.Candles, Deco[S.Candle].style, 95)); // Candle
                                            randomItems.Add((TileID.Candelabras, Deco[S.Candelabra].style, 95)); // Candelabra
                                            randomItems.Add((TileID.DjinnLamp, 0, 95)); // DjinnLamp
                                            randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book style 1
                                            randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book style 2
                                            randomItems.Add((TileID.Bottles, WorldGen.genRand.Next(0, 4), 95)); // Bottles style 1
                                            randomItems.Add((TileID.Bottles, WorldGen.genRand.Next(0, 4), 95)); // Bottles style 2
                                            randomItems.Add((TileID.Bottles, 8, 95)); // Chalice
                                            List<int> tableItems =
                                            [
                                                ItemID.Candle, ItemID.Candelabra, ItemID.Book, ItemID.DjinnLamp, TileID.Bottles, TileID.Bottles, TileID.Bottles
                                            ];

                                            area1 = new Rectangle2P(windowRect.XCenter - 1, freeR.Y1 - 2, 3, 1);

                                            for (int i = 1; i <= 4; i++)
                                            {
                                                randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                                placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, randomItem.id, style: randomItem.style, chance: randomItem.chance);
                                                if (placeResult.success)
                                                {
                                                    if (randomItem.id == TileID.Candles) Func.Unlight1x1(placeResult.x, placeResult.y);
                                                    if (randomItem.id == TileID.Candelabras) Func.UnlightCandelabra(placeResult.x, placeResult.y);
                                                }
                                            }
                                        }
                                        break;

                                    case 3: // Sofa
                                        WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Sofa].id, style: Deco[S.Sofa].style);
                                        break;

                                    case 4: // WeaponRack

                                        List<int> WeaponRackItems =
                                        [
                                            ItemID.CopperBroadsword, ItemID.TinBroadsword, ItemID.IronBroadsword, ItemID.LeadBroadsword, ItemID.SilverBroadsword, ItemID.TungstenBroadsword, ItemID.GoldBroadsword,
                                            ItemID.PlatinumBroadsword, ItemID.BoneSword, ItemID.BorealWoodSword, ItemID.EbonwoodSword, ItemID.ShadewoodSword, ItemID.AshWoodSword,

                                            ItemID.CopperBow, ItemID.TinBow, ItemID.IronBow, ItemID.LeadBow, ItemID.SilverBow, ItemID.TungstenBow, ItemID.GoldBow, ItemID.BorealWoodBow, ItemID.PalmWoodBow,
                                            ItemID.ShadewoodBow, ItemID.EbonwoodBow, ItemID.RichMahoganyBow,

                                            ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.SapphireStaff, ItemID.EmeraldStaff,

                                            ItemID.FlintlockPistol, ItemID.FlareGun, ItemID.Mace, ItemID.FlamingMace, ItemID.Spear, ItemID.Trident, ItemID.WoodenBoomerang,
                                            ItemID.EnchantedBoomerang, ItemID.BlandWhip
                                        ];
                                        if (!WorldGen.remixWorldGen) // "don't dig up" special worldgen seed changes item stats, these weapons are now considered hardmode!
                                        {
                                            WeaponRackItems.Add(ItemID.WandofSparking);
                                            WeaponRackItems.Add(ItemID.WandofFrosting);
                                            WeaponRackItems.Add(ItemID.ChainKnife);
                                        }

                                        Func.PlaceWeaponRack(windowRect.XCenter, freeR.Y1 - 4, paint: Deco[S.StylePaint].id,
                                                                                               item: WeaponRackItems.PopAt(WorldGen.genRand.Next(WeaponRackItems.Count)),
                                                                                               direction: (windowRect.XCenter < freeR.XCenter) ? -1 : 1); // left room side: -1, else 1
                                        break;

                                    case 5: // Potted Lava Plants

                                        randomItems.Clear();
                                        randomItems.Add((TileID.PottedPlants2, 7, 95)); //Potted Magma Palm
                                        randomItems.Add((TileID.PottedPlants2, 8, 95)); //Potted Brimstone Bush
                                        randomItems.Add((TileID.PottedLavaPlants, 0, 95)); //Potted Fire Brambles
                                        randomItems.Add((TileID.PottedLavaPlants, 1, 95)); //Potted Lava Bulb
                                        randomItems.Add((TileID.PottedLavaPlantTendrils, 0, 95));

                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));

                                        if (Chance.Perc(randomItem.chance)) WorldGen.PlaceObject(windowRect.XCenter, freeR.Y1, randomItem.id, style: randomItem.style);

                                        break;

                                    default:
                                        break;
                                }

                            }
                            else // preferably place even-x-tiles-object so they come out centered
                            {
                                if (Chance.Perc(90))
                                {
                                    switch (WorldGen.genRand.Next(5))
                                    {
                                        case 0: // candelabra on a platform

                                            y = freeR.Y1 - (windowYMargin - 1);
                                            WorldGen.PlaceTile(windowRect.XCenter, y, Deco[S.DecoPlat].id, style: Deco[S.DecoPlat].style);
                                            WorldGen.paintTile(windowRect.XCenter, y, (byte)Deco[S.StylePaint].id);

                                            WorldGen.PlaceTile(windowRect.XCenter + 1, y, Deco[S.DecoPlat].id, style: Deco[S.DecoPlat].style);
                                            WorldGen.paintTile(windowRect.XCenter + 1, y, (byte)Deco[S.StylePaint].id);

                                            placed = WorldGen.PlaceTile(windowRect.XCenter + 1, y - 1, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
                                            if (placed) Func.UnlightCandelabra(windowRect.XCenter + 1, y - 1);
                                            break;

                                        case 1: // Clock
                                            WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Clock].id, style: Deco[S.Clock].style);
                                            break;

                                        case 2: // Workbench with stuff on it
                                            placed = WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Workbench].id, style: Deco[S.Workbench].style);

                                            if (placed)
                                            {
                                                randomItems.Clear();
                                                randomItems.Add((TileID.Candles, Deco[S.Candle].style, 95)); // Candle
                                                randomItems.Add((TileID.Candelabras, Deco[S.Candelabra].style, 95)); // Candelabra
                                                randomItems.Add((TileID.DjinnLamp, 0, 95)); // DjinnLamp
                                                randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book style 1
                                                randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book style 2
                                                randomItems.Add((TileID.Bottles, WorldGen.genRand.Next(0, 4), 95)); // Bottles style 1
                                                randomItems.Add((TileID.Bottles, WorldGen.genRand.Next(0, 4), 95)); // Bottles style 2
                                                randomItems.Add((TileID.Bottles, 8, 95)); // Chalice
                                                List<int> tableItems =
                                                [
                                                    ItemID.Candle, ItemID.Candelabra, ItemID.Book, ItemID.DjinnLamp, TileID.Bottles, TileID.Bottles, TileID.Bottles
                                                ];

                                                area1 = new Rectangle2P(windowRect.XCenter, freeR.Y1 - 1, 2, 1);

                                                for (int i = 1; i <= 3; i++)
                                                {
                                                    randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                                    placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, randomItem.id, style: randomItem.style, chance: randomItem.chance);
                                                    if (placeResult.success)
                                                    {
                                                        if (randomItem.id == TileID.Candles) Func.Unlight1x1(placeResult.x, placeResult.y);
                                                        if (randomItem.id == TileID.Candelabras) Func.UnlightCandelabra(placeResult.x, placeResult.y);
                                                    }
                                                }
                                            }
                                            break;

                                        case 3: // Obsidian Vase

                                            if (Chance.Perc(95)) WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, TileID.Statues, style: 49);
                                            break;

                                        case 4: //ItemFrame

                                            List<int> itemFrameItems =
                                            [
                                                ItemID.Amber, ItemID.Amethyst, ItemID.Diamond, ItemID.Emerald, ItemID.Ruby, ItemID.Sapphire, ItemID.Topaz
                                            ];

                                            if (Chance.Perc(95)) Func.PlaceItemFrame(windowRect.XCenter, freeR.Y1 - 5, paint: Deco[S.StylePaint].id,
                                                                                                       item: itemFrameItems.PopAt(WorldGen.genRand.Next(itemFrameItems.Count)));
                                            break;

                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    //put a chest
                                    if (Chance.Perc(95))
                                    {
                                        placed = WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Chest].id, style: Deco[S.Chest].style);
                                        if (placed)
                                        {
                                            chestID = Chest.FindChest(windowRect.XCenter, freeR.Y1 - 1);
                                            if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2)); // fill it with loot
                                        }
                                    }
                                }

                                
                            }
                        }
                    }
                    #endregion

                    #region fill middle space
                    if (middleSpaceExist)
                    {
                        if (middleSpace.XTiles <= windowXTiles) { } // do nothing

                        else if (!doors[Door.Down].doorExist) // middle space not on a door
                        {
                            #region XTiles <= 8 -> Altar with middle line and fountain
                            if (middleSpace.XTiles <= 8)
                            {
                                # region create cascade with S.MiddleWall
                                int xStart = middleSpace.X0; //init value
                                int xEnd   = middleSpace.X1; //init value
                                for (int j = freeR.Y0; j <= freeR.Y1 + 1; j++)
                                {
                                    for (int i = xStart; i <= xEnd; i++)
                                    {
                                        Func.ReplaceWallTile((i, j), Deco[S.MiddleWall].id, (byte)Deco[S.MiddleWallPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);
                                    }

                                    if ((xEnd - xStart) > 2) { xStart++; xEnd--; } // start with 6, then 4, then 2 tiles wide
                                    if (j == freeR.Y1) // for the spot in the ground...
                                    {
                                        if (middleSpace.XTiles <= 6)  { xStart--; xEnd++; } // again 4 tiles wide, if there is no space for an altar
                                    }
                                }
                                #endregion

                                #region do something with the cascade
                                if (Chance.Perc(40)) // rip open the floor so the cascade can flow into a basin
                                {
                                    y = freeR.Y1 + 1;
                                    for (int i = middleSpace.XCenter - 1; i <= middleSpace.XCenter + 2; i++)
                                    {
                                        WorldGen.KillTile(i, y);
                                        if (Deco[S.MiddleWall].id != WallID.Bone) WorldGen.PlaceLiquid(i, y, (byte)LiquidID.Lava, 255); //255 means tile is full of liquid
                                        else Func.ReplaceWallTile((i, y), Deco[S.MiddleWall].id, (byte)Deco[S.MiddleWallPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);
                                    }
                                }
                                else if (Chance.Perc(60) && middleSpace.XTiles > 6) // small altar
                                {
                                    altarResult = CreateAltar(middleSpace.X0 + 1, middleSpace.X1 - 1, freeR.Y1, 2);

                                    randomItems.Clear();
                                    randomItems.Add((TileID.WaterFountain, 4, 100)); //Corrupt Water Fountain
                                    randomItems.Add((TileID.WaterFountain, 5, 100)); //Crimson Water Fountain
                                    randomItems.Add((TileID.WaterFountain, 7, 100)); //Blood Water Fountain
                                    randomItem = randomItems[WorldGen.genRand.Next(randomItems.Count)];

                                    if (altarResult.success) WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 2, randomItem.id, style: randomItem.style);
                                }
                                else { } //nothing, just leave the cascade "on the ground" / in the background
                                #endregion
                            }
                            #endregion

                            #region XTiles <= 10 -> Altar with Statues or Paintings and RunicWall
                            else if (middleSpace.XTiles <= 10)
                            {
                                altarResult = CreateAltar(middleSpace.X0 + 1, middleSpace.X1 - 1, freeR.Y1, 4);

                                if (altarResult.success)
                                {
                                    if (Chance.Perc(70)) // place statues
                                    {
                                        randomItems.Clear();
                                        switch (WorldGen.genRand.Next(3)) // create pairs
                                        {
                                            case 0:
                                                randomItems.Add((TileID.Statues, 40, 100)); //King
                                                randomItems.Add((TileID.Statues, 41, 100)); //Queen
                                                break;

                                            case 1:
                                                randomItems.Add((TileID.Statues, 66, 100)); //Wraith --> statue needs to be turned
                                                randomItems.Add((TileID.Statues, 69, 100)); //Medusa
                                                break;

                                            case 2:
                                                randomItems.Add((TileID.Statues, 73, 100)); //Golem --> statue needs to be turned
                                                randomItems.Add((TileID.Statues, 12, 100)); //Woman
                                                break;
                                        }

                                        randomItem = randomItems[0];
                                        placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, altarResult.altar.Y0 - 1, randomItem.id, style: randomItem.style);
                                        if (placed && (randomItem.style == 66 || randomItem.style == 73)) Func.StatueTurn(middleSpace.XCenter - 1, altarResult.altar.Y0 - 1);

                                        randomItem = randomItems[1];
                                        WorldGen.PlaceTile(middleSpace.XCenter + 1, altarResult.altar.Y0 - 1, randomItem.id, style: randomItem.style);
                                    }
                                    else // or paintings
                                    {
                                        randomItems.Clear();
                                        randomItems.Add((TileID.Painting2X3, 2, 100)); //Dark Soul Reaper
                                        randomItems.Add((TileID.Painting2X3, 4, 100)); //Trapped Ghost
                                        randomItems.Add((TileID.Painting2X3, 11, 100)); //Wicked Undead
                                        randomItems.Add((TileID.Painting2X3, 20, 100)); //Strange Dead Fellows
                                        randomItems.Add((TileID.Painting2X3, 21, 100)); //Secrets
                                        randomItems.Add((TileID.Painting2X3, 24, 100)); //The Werewolf
                                        randomItems.Add((TileID.Painting2X3, 25, 100)); //Blessing from the Heavens

                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        WorldGen.PlaceTile(middleSpace.XCenter - 1, altarResult.altar.Y0 - 2, randomItem.id, style: randomItem.style);

                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        WorldGen.PlaceTile(middleSpace.XCenter + 1, altarResult.altar.Y0 - 2, randomItem.id, style: randomItem.style);
                                    }
                                    
                                }

                                //place runic wall
                                Func.ReplaceWallArea(new(middleSpace.X0 + 2, freeR.Y1 - 3, 1, 2), WallID.ArcaneRunes, (byte)Deco[S.RunicWallPaint].id, true, 50, Deco[S.CrookedWall].id);
                                Func.ReplaceWallArea(new(middleSpace.X1 - 2, freeR.Y1 - 3, 1, 2), WallID.ArcaneRunes, (byte)Deco[S.RunicWallPaint].id, true, 50, Deco[S.CrookedWall].id);

                                for (int j = freeR.Y1 - 5; j >= middleSpace.Y0; j -= 2)
                                {
                                    Func.ReplaceWallArea(new(middleSpace.X0 + 2, j, 6, 1), WallID.ArcaneRunes, (byte)Deco[S.RunicWallPaint].id, true, 50, Deco[S.CrookedWall].id);
                                }
                            }
                            #endregion

                            #region XTiles <= 12 -> Altar with pianos & flaming "+" or big window or paintings
                            else if (middleSpace.XTiles <= 12)
                            {
                                randomStyles.Clear();
                                if (middleSpace.YTiles >= 10) randomStyles.Add(1); // window type 1 "crystal"
                                if (middleSpace.YTiles >= 10) randomStyles.Add(2); // window type 2 "scales"
                                if (middleSpace.YTiles >= 10) randomStyles.Add(3); // window type 3 "zero / portal"
                                if (middleSpace.YTiles > 15) randomStyles.Add(4); // flaming "+"
                                if (middleSpace.YTiles <= 15) randomStyles.Add(5); // painting with frame

                                switch (randomStyles[WorldGen.genRand.Next(randomStyles.Count())])
                                {
                                    // window type 1 "crystal"
                                    case 1:

                                        if (middleSpace.YTiles < 12)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("  W▓▓W  ");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 16)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  WWWW  ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("  W▓▓W  ");
                                            pattern.Add("  WWWW  ");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles <= 22)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  WWWW  ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("  W▓▓W  ");
                                            pattern.Add("  W▓▓W  ");
                                            pattern.Add("  WWWW  ");
                                            height = 15;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add("  WWWW  ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" W▓▓▓▓W ");
                                            pattern.Add(" WW▓▓WW ");
                                            pattern.Add("  W▓▓W  ");
                                            pattern.Add("  W▓▓W  ");
                                            pattern.Add("  WWWW  ");
                                            height = 19;
                                        }

                                        patternData.Clear();
                                        patternData.Add('W', (10, Deco[S.PaintingWallpaper].id, 0, overWrite));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        if (freeR.YTiles <= 15) Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.Y0 + (diff / 2)) );
                                        else                    Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.Y0 + (diff / 3)) );

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter - 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter + 2, freeR.Y1);
                                        }

                                        if (Chance.Perc(90)) // plattforms with candelabra
                                        {
                                            WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter + 1, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 2, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
                                            if (placed) Func.UnlightCandelabra(middleSpace.XCenter + 1, freeR.Y1 - 2);
                                        }

                                        break;

                                    // window type 2 "scales"
                                    case 2:

                                        if (middleSpace.YTiles < 12)
                                        {
                                            pattern.Clear();
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 16)
                                        {
                                            pattern.Clear();
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles <= 22)
                                        {
                                            pattern.Clear();
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            height = 15;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓▓ ▓▓ ▓▓");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("▓  ▓▓  ▓");
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓");
                                            height = 19;
                                        }

                                        patternData.Clear();
                                        patternData.Add('W', (10, Deco[S.PaintingWallpaper].id, 0, overWrite));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2)));



                                        if(Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter - 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter + 2, freeR.Y1);
                                        }

                                        if (Chance.Perc(90)) // plattforms with candelabra
                                        {
                                            WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter + 1, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 2, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
                                            if (placed) Func.UnlightCandelabra(middleSpace.XCenter + 1, freeR.Y1 - 2);
                                        }


                                        break;

                                    // window type 3 "zero / portal"
                                    case 3:

                                        if (middleSpace.YTiles < 15)
                                        {
                                            pattern.Clear();
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓WWWW▓ ");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add(" ▓WWWW▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 17)
                                        {
                                            pattern.Clear();
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add("▓▓▓WW▓▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add("▓▓▓WW▓▓▓");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            height = 15;
                                        }
                                        else if (middleSpace.YTiles < 20)
                                        {
                                            pattern.Clear();
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add("▓▓▓WW▓▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            height = 17;
                                        }
                                        else 
                                        {
                                            pattern.Clear();
                                            pattern.Add("   ▓▓   ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add("▓▓▓WW▓▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓W▓▓W▓▓");
                                            pattern.Add("▓▓WWWW▓▓");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓▓WW▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("  ▓▓▓▓  ");
                                            pattern.Add("   ▓▓   ");
                                            height = 20;
                                        }

                                        patternData.Clear();
                                        patternData.Add('W', (10, Deco[S.PaintingWallpaper].id, 0, overWrite));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2)));

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter - 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter + 2, freeR.Y1);
                                        }

                                        if (Chance.Perc(90)) // plattforms with candelabra
                                        {
                                            WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter + 1, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 2, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
                                            if (placed) Func.UnlightCandelabra(middleSpace.XCenter + 1, freeR.Y1 - 2);
                                        }


                                        break;

                                    // flaming "+"
                                    case 4:

                                        if (freeR.YTiles < 18) CreateFlamingPlus(freeR.XCenter, middleSpace.Y0 + 3, 1, true);
                                        else CreateFlamingPlus(freeR.XCenter, middleSpace.Y0 + middleSpace.YDiff / 3, 1, true);

                                        #region create lavafall on top of the flaming "+"

                                        //middle
                                        //x = freeR.XCenter;
                                        //y = freeR.Y0 - 1;
                                        //WorldGen.KillTile(x, y);
                                        //WorldGen.KillTile(x + 1, y);
                                        //WorldGen.KillTile(x, y - 1);
                                        //WorldGen.KillTile(x + 1, y - 1);

                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter    , freeR.Y0 - 1); }, (true, 0, 0, [(freeR.XCenter    , freeR.Y0 - 1, Deco[S.Brick].id)] ));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter + 1, freeR.Y0 - 1); }, (true, 0, 0, [(freeR.XCenter + 1, freeR.Y0 - 1, Deco[S.Brick].id)] ));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter    , freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter    , freeR.Y0 - 2, Deco[S.Brick].id)] ));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter + 1, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter + 1, freeR.Y0 - 2, Deco[S.Brick].id)] ));

                                        //left
                                        x = freeR.XCenter - 1;
                                        y = freeR.Y0 - 2;
                                        WorldGen.KillTile(x - 1, y);
                                        WorldGen.PlaceLiquid(x - 1, y, (byte)LiquidID.Lava, 175);
                                        //WorldGen.PoundTile(x, y);
                                        runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(freeR.XCenter - 1, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter - 1, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                        //right
                                        x = freeR.XCenter + 2;
                                        y = freeR.Y0 - 2;
                                        WorldGen.KillTile(x + 1, y);
                                        WorldGen.PlaceLiquid(x + 1, y, (byte)LiquidID.Lava, 175);
                                        //WorldGen.PoundTile(x, y);
                                        runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(freeR.XCenter + 2, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter + 2, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                        // add some nice "v" spike to the middle
                                        x = freeR.XCenter;
                                        y = freeR.Y0 - 3;
                                        Func.SlopeTile(x, y, (int)Func.SlopeVal.BotLeft);

                                        x = freeR.XCenter + 1;
                                        y = freeR.Y0 - 3;
                                        Func.SlopeTile(x, y, (int)Func.SlopeVal.BotRight);

                                        #endregion

                                        altarResult = CreateAltar(middleSpace.X0 + 1, middleSpace.X1 - 1, freeR.Y1, 6);

                                        if (altarResult.success)
                                        {
                                            y = altarResult.altar.Y0 - 1;
                                            WorldGen.PlaceTile(freeR.XCenter - 1, y, Deco[S.Piano].id, style: Deco[S.Piano].style);
                                            WorldGen.PlaceTile(freeR.XCenter + 2, y, Deco[S.Piano].id, style: Deco[S.Piano].style);
                                        }

                                        break;

                                    // painting
                                    case 5:

                                        Func.ReplaceWallArea(new(middleSpace.X0 + 2, middleSpace.YCenter - 2, middleSpace.XTiles - 4, 6), Deco[S.PaintingWallpaper].id, chance: 60, chanceWithType: Deco[S.CrookedWall].id);
                                        Place6x4PaintingByStyle(new(middleSpace.X0 + 3, middleSpace.YCenter - 1, 6, 4), Deco[S.StyleSave].id);

                                        break;
                                }
                            }
                            #endregion

                            #region XTiles <= 14 -> Altar with campfires, fountain & flaming "+" or big windows or paintings
                            else if (middleSpace.XTiles <= 14)
                            {
                                randomStyles.Clear();
                                if (middleSpace.YTiles >= 10) randomStyles.Add(1); // window type 1 "skull"
                                if (middleSpace.YTiles >= 10) randomStyles.Add(2); // window type 2 "crystal"
                                if (middleSpace.YTiles >= 4) randomStyles.Add(3); // window type 3 "hammer"
                                if (middleSpace.YTiles >= 11) randomStyles.Add(4); // window type 4 "little devil"
                                if (middleSpace.YTiles > 17) randomStyles.Add(5); // flaming "+"
                                if (middleSpace.YTiles <= 15) randomStyles.Add(6); // painting with frame

                                switch (randomStyles[WorldGen.genRand.Next(randomStyles.Count())])
                                {
                                    // window type 1 "skull"
                                    case 1:

                                        if (middleSpace.YTiles < 10)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("  ▓ ▓▓ ▓  ");
                                            height = 8;
                                        }

                                        else if (middleSpace.YTiles < 12)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("  ▓ ▓▓ ▓  ");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 14)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓  ▓▓  ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("  ▓ ▓▓ ▓  ");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles < 16)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓  ▓▓  ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add(" ▓  ▓▓  ▓ ");
                                            pattern.Add(" ▓▓    ▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            height = 14;
                                        }
                                        else if (middleSpace.YTiles < 18)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓  ▓▓  ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add(" ▓  ▓▓  ▓ ");
                                            pattern.Add(" ▓▓    ▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            height = 16;
                                        }
                                        else if (middleSpace.YTiles <= 20)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓  ▓▓  ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add(" ▓  ▓▓  ▓ ");
                                            pattern.Add(" ▓▓    ▓▓ ");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            height = 18;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add("  ▓▓  ▓▓  ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add(" ▓  ▓▓  ▓ ");
                                            pattern.Add(" ▓▓    ▓▓ ");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            height = 20;
                                        }

                                        patternData.Clear();
                                        patternData.Add('W', (10, Deco[S.PaintingWallpaper].id, 0, overWrite));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2)));

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.X0 + 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.X0 + 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.X1 - 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.X1 - 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90)) // plattforms with candelabra
                                        {
                                            WorldGen.PlaceTile(freeR.XCenter - 2, freeR.Y1, TileID.Campfire, style: 2);
                                            WorldGen.PlaceTile(freeR.XCenter + 3, freeR.Y1, TileID.Campfire, style: 2);
                                        }

                                        break;
                                    
                                    // window type 2 "crystal"
                                    case 2:

                                        if (middleSpace.YTiles < 12)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  WWWWWW  ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add("   WWWW   ");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 16)
                                        {
                                            pattern.Clear();
                                            pattern.Add("   WWWW   ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add("   W▓▓W   ");
                                            pattern.Add("   WWWW   ");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles <= 22)
                                        {
                                            pattern.Clear();
                                            pattern.Add("   WWWW   ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add("   W▓▓W   ");
                                            pattern.Add("   W▓▓W   ");
                                            pattern.Add("   WWWW   ");
                                            height = 15;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add("   WWWW   ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add("W▓▓▓▓▓▓▓▓W");
                                            pattern.Add("W▓▓▓▓▓▓▓▓W");
                                            pattern.Add("WW▓▓▓▓▓▓WW");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" W▓▓▓▓▓▓W ");
                                            pattern.Add(" WW▓▓▓▓WW ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  W▓▓▓▓W  ");
                                            pattern.Add("  WW▓▓WW  ");
                                            pattern.Add("   W▓▓W   ");
                                            pattern.Add("   W▓▓W   ");
                                            pattern.Add("   WWWW   ");
                                            height = 19;
                                        }

                                        patternData.Clear();
                                        patternData.Add('W', (10, Deco[S.PaintingWallpaper].id, 0, overWrite));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2)));

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter - 1, freeR.Y1);
                                        }

                                        if (Chance.Perc(90))
                                        {
                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                            if (placed) Func.UnlightLamp(middleSpace.XCenter + 2, freeR.Y1);
                                        }

                                        if (Chance.Perc(90)) // plattforms with candelabra
                                        {
                                            WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 1, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style);
                                            WorldGen.paintTile(middleSpace.XCenter + 1, freeR.Y1 - 1, (byte)Deco[S.AltarStepsPaint].id);

                                            placed = WorldGen.PlaceTile(middleSpace.XCenter + 1, freeR.Y1 - 2, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
                                            if (placed) Func.UnlightCandelabra(middleSpace.XCenter + 1, freeR.Y1 - 2);
                                        }

                                        break;

                                    // window type 3 "hammer"
                                    case 3:
                                        
                                        if (middleSpace.YTiles < 6)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            height = 4;
                                        }
                                        else if (middleSpace.YTiles < 8)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 6;
                                        }
                                        else if (middleSpace.YTiles < 10)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 8;
                                        }
                                        else if (middleSpace.YTiles < 12)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 10;
                                        }
                                        else if (middleSpace.YTiles < 14)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles < 16)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 14;
                                        }
                                        else if (middleSpace.YTiles < 18)
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 16;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("S▓▓▓▓▓▓▓▓S");
                                            pattern.Add("SS▓▓▓▓▓▓SS");
                                            pattern.Add(" SSS▓▓SSS ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("    ▓▓    ");
                                            height = 18;
                                        }

                                        patternData.Clear();
                                        patternData.Add('S', (1, TileID.Spikes, 0, (0,0)));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2 - 1)));

                                        for (int i = middleSpace.XCenter - 1; i <= middleSpace.XCenter + 2; i++)
                                        {
                                            WorldGen.PlaceTile(i, freeR.Y1, Deco[S.Floor].id);
                                            WorldGen.paintTile(i, freeR.Y1, (byte)Deco[S.FloorPaint].id);
                                        }

                                        WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 1, TileID.Statues, style: 19); //Hammer statue


                                        break;

                                    // window type 4 "little devil"
                                    case 4:

                                        if (middleSpace.YTiles <= 11)
                                        {
                                            pattern.Clear();
                                            pattern.Add("  S    S  "); // -> the devils horns
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            height = 11;
                                        }
                                        else if (middleSpace.YTiles < 17)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            height = 12;
                                        }
                                        else if (middleSpace.YTiles == 17)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add(" ▓ ▓▓▓▓ ▓ ");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add(" ▓▓▓▓▓▓▓▓ ");
                                            pattern.Add(" ▓▓ ▓▓ ▓▓ ");
                                            pattern.Add(" ▓ ▓▓▓▓ ▓ ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            height = 17;
                                        }
                                        else if (middleSpace.YTiles == 18)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("▓ ▓▓SS▓▓ ▓");
                                            pattern.Add("▓▓ ▓▓▓▓ ▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓ ▓▓▓▓ ▓▓");
                                            pattern.Add("▓ ▓▓▓▓▓▓ ▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            height = 18;
                                        }
                                        else if (middleSpace.YTiles == 19)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓ ▓▓▓▓ ▓▓");
                                            pattern.Add("▓ ▓▓▓▓▓▓ ▓");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            height = 19;
                                        }
                                        else if (middleSpace.YTiles == 20)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            height = 20;
                                        }
                                        else if (middleSpace.YTiles == 21)
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓   ▓▓   ▓");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            height = 21;
                                        }
                                        else
                                        {
                                            pattern.Clear();
                                            pattern.Add("SS      SS"); // -> the devils horns
                                            pattern.Add(" SSSSSSSS ");
                                            pattern.Add(" SS▓▓▓▓SS ");
                                            pattern.Add("SS▓▓SS▓▓SS");
                                            pattern.Add("S▓▓SSSS▓▓S");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓SS▓▓▓▓SS▓");
                                            pattern.Add("▓▓SS▓▓SS▓▓");
                                            pattern.Add(" ▓▓SSSS▓▓ ");
                                            pattern.Add("  ▓▓SS▓▓  ");
                                            pattern.Add("▓  ▓▓▓▓  ▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓▓▓▓▓▓▓▓▓");
                                            pattern.Add("▓▓▓ ▓▓ ▓▓▓");
                                            pattern.Add("▓▓  ▓▓  ▓▓");
                                            pattern.Add("▓   ▓▓   ▓");
                                            pattern.Add("    ▓▓    ");
                                            pattern.Add("   ▓▓▓▓   ");
                                            pattern.Add("  ▓▓▓▓▓▓  ");
                                            pattern.Add(" ▓▓▓  ▓▓▓ ");
                                            height = 22;
                                        }

                                        patternData.Clear();
                                        patternData.Add('S', (1, TileID.Spikes, 0, (0, 0)));
                                        patternData.Add('▓', (10, Deco[S.WindowWall].id, Deco[S.WindowPaint].id, overWrite));

                                        diff = middleSpace.YTiles - height;
                                        Func.DrawPatternFromString(pattern, patternData, (middleSpace.X0 + 2, middleSpace.YCenter - (height / 2 ) - height % 2));


                                        break;

                                    // flaming "+"
                                    case 5:

                                        if (freeR.YTiles < 20) CreateFlamingPlus(freeR.XCenter, middleSpace.Y0 + 3, 2, true);
                                        else CreateFlamingPlus(freeR.XCenter, middleSpace.Y0 + middleSpace.YDiff / 3, 2, true);

                                        #region create lavafall on top of the flaming "+"

                                        //middle
                                        //x = freeR.XCenter;
                                        //y = freeR.Y0 - 1;
                                        //WorldGen.KillTile(x, y);
                                        //WorldGen.KillTile(x + 1, y);
                                        //WorldGen.KillTile(x, y - 1);
                                        //WorldGen.KillTile(x + 1, y - 1);

                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter, freeR.Y0 - 1); }, (true, 0, 0, [(freeR.XCenter, freeR.Y0 - 1, Deco[S.Brick].id)]));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter + 1, freeR.Y0 - 1); }, (true, 0, 0, [(freeR.XCenter + 1, freeR.Y0 - 1, Deco[S.Brick].id)]));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter, freeR.Y0 - 2, Deco[S.Brick].id)]));
                                        runAfterWorldCleanup.Add(() => { WorldGen.KillTile(freeR.XCenter + 1, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter + 1, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                        //left
                                        x = freeR.XCenter - 1;
                                        y = freeR.Y0 - 2;
                                        WorldGen.KillTile(x - 1, y);
                                        WorldGen.PlaceLiquid(x - 1, y, (byte)LiquidID.Lava, 175);
                                        //WorldGen.PoundTile(x, y);
                                        runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(freeR.XCenter - 1, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter - 1, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                        //right
                                        x = freeR.XCenter + 2;
                                        y = freeR.Y0 - 2;
                                        WorldGen.KillTile(x + 1, y);
                                        WorldGen.PlaceLiquid(x + 1, y, (byte)LiquidID.Lava, 175);
                                        //WorldGen.PoundTile(x, y);
                                        runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(freeR.XCenter + 2, freeR.Y0 - 2); }, (true, 0, 0, [(freeR.XCenter + 2, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                        // add some nice "v" spike to the middle
                                        x = freeR.XCenter;
                                        y = freeR.Y0 - 3;
                                        Func.SlopeTile(x, y, (int)Func.SlopeVal.BotLeft);

                                        x = freeR.XCenter + 1;
                                        y = freeR.Y0 - 3;
                                        Func.SlopeTile(x, y, (int)Func.SlopeVal.BotRight);

                                        #endregion


                                        altarResult = CreateAltar(middleSpace.X0 + 1, middleSpace.X1 - 1, freeR.Y1, 8);

                                        if (altarResult.success)
                                        {
                                            y = altarResult.altar.Y0 - 1;
                                            if (Chance.Perc(90))
                                            {
                                                WorldGen.PlaceTile(freeR.XCenter - 2, y, Deco[S.Campfire].id, style: Deco[S.Campfire].style);
                                                Func.PaintArea(new(freeR.XCenter - 3, y - 1, 3, 2), (byte)Deco[S.CampfirePaint].id);
                                            }
                                            if (Chance.Perc(90))
                                            {
                                                WorldGen.PlaceTile(freeR.XCenter + 3, y, Deco[S.Campfire].id, style: Deco[S.Campfire].style);
                                                Func.PaintArea(new(freeR.XCenter + 2, y - 1, 3, 2), (byte)Deco[S.CampfirePaint].id);
                                            }

                                            randomItems.Clear();
                                            randomItems.Add((TileID.WaterFountain, 4, 100)); //Corrupt Water Fountain
                                            randomItems.Add((TileID.WaterFountain, 5, 100)); //Crimson Water Fountain
                                            randomItems.Add((TileID.WaterFountain, 7, 100)); //Blood Water Fountain
                                            randomItem = randomItems[WorldGen.genRand.Next(randomItems.Count)];

                                            if (Chance.Perc(90)) WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1 - 2, randomItem.id, style: randomItem.style);
                                        }

                                        

                                        break;

                                    // painting
                                    case 6:

                                        Func.ReplaceWallArea(new(middleSpace.X0 + 2, middleSpace.YCenter - 2, middleSpace.XTiles - 4, 6), Deco[S.PaintingWallpaper].id, chance: 60, chanceWithType: Deco[S.CrookedWall].id);
                                        Place6x4PaintingByStyle(new(middleSpace.X0 + 4, middleSpace.YCenter - 1, 6, 4), Deco[S.StyleSave].id);

                                        break;

                                    default:
                                        break;
                                }
                            }
                            #endregion

                            #region XTiles <= 16 -> Temple
                            else if (middleSpace.XTiles <= 16)
                            {
                                #region foundation

                                int templeFloor = freeR.Y1 - 1;
                                for (int i = middleSpace.X0 + 3; i <= middleSpace.X1 - 3; i++)
                                {
                                    Func.PlaceSingleTile(i, templeFloor, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id);
                                }

                                // left side
                                int templeFloorStart = middleSpace.X0 + 3;
                                x = templeFloorStart;
                                Func.PlaceSingleTile(x - 1, freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id, slope: (int)Func.SlopeVal.UpLeft);
                                Func.PlaceSingleTile(x    , freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id);
                                Func.PlaceSingleTile(x + 1, freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id, slope: (int)Func.SlopeVal.UpRight);

                                Func.PlaceSingleTile(x - 1, freeR.Y1 - 1, Deco[S.TempleSteps].id, style: Deco[S.TempleSteps].style, paint: PaintID.GrayPaint);
                                Func.PlaceSingleTile(x - 2, freeR.Y1    , Deco[S.TempleSteps].id, style: Deco[S.TempleSteps].style, paint: PaintID.GrayPaint);
                                PoundAfterSmoothWorld.Add((x - 1, freeR.Y1 - 1, 1, Deco[S.TempleSteps].id, Deco[S.TempleSteps].style, PaintID.GrayPaint, false)); // PoundTile after "Smooth World" so the pound stays
                                PoundAfterSmoothWorld.Add((x - 2, freeR.Y1    , 1, Deco[S.TempleSteps].id, Deco[S.TempleSteps].style, PaintID.GrayPaint, false)); // PoundTile after "Smooth World" so the pound stays


                                // right side
                                int templeFloorEnd = middleSpace.X1 - 3;
                                x = templeFloorEnd;
                                Func.PlaceSingleTile(x - 1, freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id, slope: (int)Func.SlopeVal.UpLeft);
                                Func.PlaceSingleTile(x    , freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id);
                                Func.PlaceSingleTile(x + 1, freeR.Y1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickBottomPaint].id, slope: (int)Func.SlopeVal.UpRight);

                                Func.PlaceSingleTile(x + 1, freeR.Y1 - 1, Deco[S.TempleSteps].id, style: Deco[S.TempleSteps].style, paint: PaintID.GrayPaint);
                                Func.PlaceSingleTile(x + 2, freeR.Y1    , Deco[S.TempleSteps].id, style: Deco[S.TempleSteps].style, paint: PaintID.GrayPaint);
                                PoundAfterSmoothWorld.Add((x + 1, freeR.Y1 - 1, 1, Deco[S.TempleSteps].id, Deco[S.TempleSteps].style, PaintID.GrayPaint, false)); // PoundTile after "Smooth World" so the pound stays
                                PoundAfterSmoothWorld.Add((x + 2, freeR.Y1    , 1, Deco[S.TempleSteps].id, Deco[S.TempleSteps].style, PaintID.GrayPaint, false)); // PoundTile after "Smooth World" so the pound stays

                                //lava below floor
                                //for (int i = middleSpace.X0 + 5; i <= middleSpace.X1 - 5; i++)
                                //{
                                //    WorldGen.PlaceLiquid(i, freeR.Y1, (byte)LiquidID.Lava, 200);
                                    
                                //}
                                runAfterWorldCleanup.Add(() => { WorldGen.PlaceLiquid(freeR.XCenter - 1, freeR.Y1, (byte)LiquidID.Lava, 255); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, templeFloor, Deco[S.TempleBrick].id)]));
                                runAfterWorldCleanup.Add(() => { WorldGen.PlaceLiquid(freeR.XCenter    , freeR.Y1, (byte)LiquidID.Lava, 255); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, templeFloor, Deco[S.TempleBrick].id)]));
                                runAfterWorldCleanup.Add(() => { WorldGen.PlaceLiquid(freeR.XCenter + 1, freeR.Y1, (byte)LiquidID.Lava, 255); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, templeFloor, Deco[S.TempleBrick].id)]));
                                runAfterWorldCleanup.Add(() => { WorldGen.PlaceLiquid(freeR.XCenter + 2, freeR.Y1, (byte)LiquidID.Lava, 255); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, templeFloor, Deco[S.TempleBrick].id)]));

                                #endregion


                                #region altar

                                Func.PlaceSingleTile(middleSpace.XCenter    , freeR.Y1 - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                Func.PlaceSingleTile(middleSpace.XCenter + 1, freeR.Y1 - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                Func.PlaceSingleTile(middleSpace.XCenter    , freeR.Y1 - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                Func.PlaceSingleTile(middleSpace.XCenter + 1, freeR.Y1 - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);

                                WorldGen.PlaceObject(middleSpace.XCenter, freeR.Y1 - 4, TileID.LavafishBowl);

                                placed = WorldGen.PlaceTile(middleSpace.XCenter - 1, freeR.Y1 - 2, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                if (placed) Func.UnlightLamp(middleSpace.XCenter - 1, freeR.Y1 - 2);

                                placed = WorldGen.PlaceTile(middleSpace.XCenter + 2, freeR.Y1 - 2, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                if (placed) Func.UnlightLamp(middleSpace.XCenter + 2, freeR.Y1 - 2);

                                #endregion


                                #region altar surroundings until roof

                                int columnsBottom = templeFloor - 1; //freeR.Y1 - 2
                                int columnsTop = 0;

                                if      (freeR.YTiles <= 14) columnsTop = freeR.Y1 - 6; // first let the roof complete
                                else if (freeR.YTiles <= 15) columnsTop = freeR.Y1 - 7; // then raise the roof...
                                else if (freeR.YTiles >= 16) columnsTop = freeR.Y1 - 8; // ...step by step

                                for (int j = columnsBottom; j >= columnsTop; j--)
                                {
                                    Func.PlaceSingleTile(templeFloorStart    , j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                    Func.PlaceSingleTile(templeFloorStart + 1, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);

                                    Func.PlaceSingleTile(templeFloorEnd - 1, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                    Func.PlaceSingleTile(templeFloorEnd    , j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                }

                                Func.ReplaceWallArea(new(templeFloorStart + 1, columnsTop, templeFloorStart + 2, columnsBottom, "dummy"), Deco[S.DoorWall].id);
                                Func.ReplaceWallArea(new(templeFloorEnd - 2  , columnsTop, templeFloorEnd - 1  , columnsBottom, "dummy"), Deco[S.DoorWall].id);

                                Func.ReplaceWallArea(new(templeFloorStart + 3, columnsTop, templeFloorEnd - 3, columnsBottom, "dummy"), WallID.Lavafall);

                                // deco: demon torches or streamers
                                if (Chance.Perc(75) || freeR.YTiles < 15)
                                {
                                    WorldGen.PlaceTile(templeFloorStart + 2, columnsTop + 1, TileID.Torches, style: 7); // Demon Torch
                                    WorldGen.PlaceTile(templeFloorEnd - 2, columnsTop + 1, TileID.Torches, style: 7); // Demon Torch
                                }
                                else if (freeR.YTiles >= 15)
                                {
                                    for (int i = templeFloorStart + 2; i <= templeFloorEnd - 2; i++)
                                    {
                                        Func.PlaceSingleTile(i, columnsTop, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                    }
                                    for (int j = columnsTop + 1; j <= columnsTop + 2; j++)
                                    {
                                        Func.PlaceSingleTile(templeFloorStart + 2, j, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorEnd - 2  , j, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                    }

                                    if (freeR.YTiles > 15)
                                    {
                                        Func.PlaceSingleTile(freeR.XCenter    , columnsTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(freeR.XCenter + 1, columnsTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                    }
                                }

                                #endregion


                                #region altar roof

                                int roofBottom = columnsTop - 2; // first full line of bricks
                                y = columnsTop - 1;
                                for (int i = templeFloorStart - 1; i <= templeFloorEnd + 1; i++)
                                {
                                    WorldGen.PlaceTile(i, y, Deco[S.TempleCeilingPlat].id, style: Deco[S.TempleCeilingPlat].style);
                                    if (i >= templeFloorStart && i <= templeFloorEnd) Func.ReplaceWallTile((i, y), WallID.ObsidianBrick);

                                    Func.PlaceSingleTile(i, roofBottom, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                }

                                List<(int id, int style)> bannerItems =
                                [
                                    //(TileID.HangingLanterns,  2), // Caged Lantern
                                    //(TileID.HangingLanterns,  6), // Oil Rag Sconce
                                    //(TileID.HangingLanterns, 25), // Bone Lantern
                                    (TileID.Banners, 10), // Marching Bones Banner
                                    (TileID.Banners, 11), // Necromantic Sign
                                    (TileID.Banners, 12), // Rusted Company Standard
                                    (TileID.Banners, 15), // Diabolic Sigil
                                    (TileID.Banners, 17), // Hell Hammer Banner
                                    (TileID.Banners, 18), // Helltower Banner
                                    (TileID.Banners, 21)  // Lava Erupts Banner
                                ];
                                if (Deco[S.StyleSave].id == S.StyleBlueBrick) bannerItems.Add((TileID.Banners, 2)); // Blue Banner
                                else                                          bannerItems.Add((TileID.Banners, 0)); // Red Banner

                                (int id, int style) bannerItem;

                                if (Chance.Perc(85))
                                {
                                    bannerItem = bannerItems[WorldGen.genRand.Next(bannerItems.Count())];
                                    WorldGen.PlaceObject(templeFloorStart - 1, roofBottom + 2, bannerItem.id, style: bannerItem.style);
                                }
                                if (Chance.Perc(85))
                                {
                                    bannerItem = bannerItems[WorldGen.genRand.Next(bannerItems.Count())];
                                    WorldGen.PlaceObject(templeFloorEnd + 1, roofBottom + 2, bannerItem.id, style: bannerItem.style);
                                }

                                // roof bricks
                                if (freeR.YTiles >= 9)
                                {
                                    y = roofBottom;
                                    for (int i = templeFloorStart - 1; i <= templeFloorEnd + 1; i++)
                                    {
                                        Func.PlaceSingleTile(i, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                    }

                                    if (freeR.YTiles <= 10) // looks nicer to give it an edge
                                    {
                                        //Func.SlopeTile(templeFloorStart - 1, y, (int)Func.SlopeVal.UpLeft);
                                        //Func.SlopeTile(templeFloorEnd + 1  , y, (int)Func.SlopeVal.UpRight);
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorStart - 1, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 2, roofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorEnd + 1  , y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd - 2  , roofBottom, Deco[S.TempleBrick].id)]));
                                    }

                                    if (freeR.YTiles == 10) // 1 space between temple and ceiling available
                                    {
                                        //Func.PlaceSingleTile(templeFloorStart + 1, y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        //Func.PlaceSingleTile(freeR.XCenter       , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        //Func.PlaceSingleTile(freeR.XCenter + 1   , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        //Func.PlaceSingleTile(templeFloorEnd - 1  , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);

                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorStart + 1, y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 2, roofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter       , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 2, roofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter + 1   , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 2, roofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorEnd - 1  , y - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 2, roofBottom, Deco[S.TempleBrick].id)]));
                                    }
                                }
                                if (freeR.YTiles >= 11)
                                {
                                    y = roofBottom - 1;
                                    for (int i = templeFloorStart; i <= templeFloorEnd; i++)
                                    {
                                        Func.PlaceSingleTile(i, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        if (i == templeFloorStart) Func.SlopeTile(i, y, (int)Func.SlopeVal.UpLeft);
                                        if (i == templeFloorEnd)   Func.SlopeTile(i, y, (int)Func.SlopeVal.UpRight);
                                    }
                                }
                                if (freeR.YTiles >= 12)
                                {
                                    y = roofBottom - 2;
                                    for (int i = templeFloorStart + 1; i <= templeFloorEnd - 1; i++)
                                    {
                                        Func.PlaceSingleTile(i, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        if (i == templeFloorStart + 1) WorldGen.PoundTile(i, y);
                                        if (i == templeFloorEnd - 1)   WorldGen.PoundTile(i, y);
                                    }
                                }
                                if (freeR.YTiles >= 13)
                                {
                                    y = roofBottom - 3;
                                    for (int i = templeFloorStart + 3; i <= templeFloorEnd - 3; i++)
                                    {
                                        Func.PlaceSingleTile(i, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        if (i == templeFloorStart + 3) Func.SlopeTile(i, y, (int)Func.SlopeVal.UpLeft);
                                        if (i == templeFloorEnd - 3)   Func.SlopeTile(i, y, (int)Func.SlopeVal.UpRight);
                                    }
                                }
                                if (freeR.YTiles >= 14)
                                {
                                    y = roofBottom - 4;
                                    //Func.PlaceSingleTile(templeFloorStart + 4, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight);
                                    //Func.PlaceSingleTile(templeFloorEnd - 4  , y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft);

                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorStart + 4, roofBottom - 4, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart + 4, roofBottom - 3, Deco[S.TempleBrick].id)]));
                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorEnd - 4  , roofBottom - 4, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd - 4  , roofBottom - 3, Deco[S.TempleBrick].id)]));
                                }

                                #endregion


                                #region left / right altar roof toppers

                                int sideRoofTop = 0;
                                if (freeR.YTiles >= 17) // left and right roof toppers
                                {
                                    if      (freeR.YTiles == 17) sideRoofTop = freeR.Y1 - 14;
                                    else if (freeR.YTiles >= 18) sideRoofTop = freeR.Y1 - 15;

                                    // brick lines
                                    for (int i = templeFloorStart - 1; i <= templeFloorStart + 2; i++)
                                    {
                                        Func.PlaceSingleTile(i, sideRoofTop, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                    }
                                    for (int i = templeFloorEnd - 2; i <= templeFloorEnd + 1; i++)
                                    {
                                        Func.PlaceSingleTile(i, sideRoofTop, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                    }

                                    // spikes
                                    y = sideRoofTop - 1;
                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorStart    , sideRoofTop - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart, sideRoofTop, Deco[S.TempleBrick].id)]));
                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorStart + 1, sideRoofTop - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorStart, sideRoofTop, Deco[S.TempleBrick].id)]));

                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorEnd - 1, sideRoofTop - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, sideRoofTop, Deco[S.TempleBrick].id)]));
                                    runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(templeFloorEnd    , sideRoofTop - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(templeFloorEnd, sideRoofTop, Deco[S.TempleBrick].id)]));

                                    // columns
                                    for (int j = sideRoofTop + 1; j <= roofBottom - 1; j++)
                                    {
                                                                                         Func.PlaceSingleTile(templeFloorStart - 1, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                        if (!Main.tile[templeFloorStart + 2, j].HasTile) Func.PlaceSingleTile(templeFloorStart + 2, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);

                                        if (!Main.tile[templeFloorEnd - 2  , j].HasTile) Func.PlaceSingleTile(templeFloorEnd - 2, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                                                                         Func.PlaceSingleTile(templeFloorEnd + 1, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                    }

                                    // place backwall
                                    Func.ReplaceWallArea(new(templeFloorStart, sideRoofTop + 1, templeFloorStart + 1, roofBottom - 1, "dummy"), Deco[S.DoorWall].id);
                                    Func.ReplaceWallArea(new(templeFloorEnd - 1, sideRoofTop + 1, templeFloorEnd, roofBottom - 1, "dummy"), Deco[S.DoorWall].id);

                                    // hang decoration
                                    List<(int id, int style)> hangItems = [];
                                    (int id, int style) hangItem;

                                    if (Deco[S.StyleSave].id == S.StyleBlueBrick) hangItems.Add((TileID.HangingLanterns, 15)); // Glass Lantern
                                    else                                          hangItems.Add((TileID.HangingLanterns, 23)); // Shadewood Lantern
                                    if (freeR.YTiles >= 18)
                                    {
                                        hangItems.Add((TileID.Banners, 10)); // Marching Bones Banner
                                        hangItems.Add((TileID.Banners, 11)); // Necromantic Sign
                                        hangItems.Add((TileID.Banners, 12)); // Rusted Company Standard
                                        hangItems.Add((TileID.Banners, 15)); // Diabolic Sigil
                                        hangItems.Add((TileID.Banners, 17)); // Hell Hammer Banner
                                        hangItems.Add((TileID.Banners, 18)); // Helltower Banner
                                        hangItems.Add((TileID.Banners, 21)); // Lava Erupts Banner

                                        if (Deco[S.StyleSave].id == S.StyleBlueBrick) hangItems.Add((TileID.Banners, 2)); // Blue Banner
                                        else                                          hangItems.Add((TileID.Banners, 0)); // Red Banner
                                    }

                                    //left
                                    if (Chance.Perc(65))
                                    {
                                        hangItem = hangItems[WorldGen.genRand.Next(hangItems.Count())];
                                        placed = WorldGen.PlaceObject(templeFloorStart, sideRoofTop + 1, hangItem.id, style: hangItem.style);
                                        if (placed && hangItem.id == TileID.HangingLanterns) Func.UnlightLantern(templeFloorStart, sideRoofTop + 1);
                                    }
                                    else if (Chance.Perc(85))
                                    {
                                        Func.PlaceSingleTile(templeFloorStart, sideRoofTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorStart, sideRoofTop + 2, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorStart + 1, sideRoofTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorStart + 1, sideRoofTop + 2, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                    }

                                    //right
                                    if (Chance.Perc(65))
                                    {
                                        hangItem = hangItems[WorldGen.genRand.Next(hangItems.Count())];
                                        placed = WorldGen.PlaceObject(templeFloorEnd, sideRoofTop + 1, hangItem.id, style: hangItem.style);
                                        if (placed && hangItem.id == TileID.HangingLanterns) Func.UnlightLantern(templeFloorEnd, sideRoofTop + 1);
                                    }
                                    else if (Chance.Perc(85))
                                    {
                                        Func.PlaceSingleTile(templeFloorEnd - 1, sideRoofTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorEnd - 1, sideRoofTop + 2, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorEnd, sideRoofTop + 1, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                        Func.PlaceSingleTile(templeFloorEnd, sideRoofTop + 2, TileID.SillyStreamerBlue, paint: Deco[S.TempleStreamerPaint].id);
                                    }
                                }

                                #endregion


                                #region middle altar roof topper

                                int centerRoofBottom = 0;
                                int centerRoofStart = templeFloorStart + 2;
                                int centerRoofEnd = templeFloorEnd - 2;
                                if (freeR.YTiles >= 20)
                                {
                                    if (freeR.YTiles == 20)      centerRoofBottom = freeR.Y1 - 17;
                                    else if (freeR.YTiles >= 21) centerRoofBottom = freeR.Y1 - 18;

                                    // brick lines
                                    for (int i = centerRoofStart; i <= centerRoofEnd; i++)
                                    {
                                        Func.PlaceSingleTile(i, centerRoofBottom, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                    }
                                    if (freeR.YTiles >= 22)
                                    {
                                        y = centerRoofBottom - 1;
                                        for (int i = centerRoofStart + 1; i <= centerRoofEnd - 1; i++)
                                        {
                                            Func.PlaceSingleTile(i, y, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                            if (i == centerRoofStart + 1) WorldGen.PoundTile(i, y);
                                            if (i == centerRoofEnd - 1)   WorldGen.PoundTile(i, y);
                                        }
                                    }

                                    // spike
                                    y = centerRoofBottom - 1;
                                    if (freeR.YTiles < 22)
                                    {
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter    , centerRoofBottom - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter + 1, centerRoofBottom - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                    }
                                    else if (freeR.YTiles == 22)// one more up because of the second brick line
                                    {
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter    , centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft ); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter + 1, centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                    }
                                    else
                                    {
                                        // normal brick line first to get on height with side arms
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter    , centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter + 1, centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));


                                        // spike on top
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter    , centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(freeR.XCenter + 1, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(freeR.XCenter, centerRoofBottom, Deco[S.TempleBrick].id)]));
                                    }


                                    // columns
                                    for (int j = centerRoofBottom + 1; j <= sideRoofTop - 1; j++)
                                    {
                                        Func.PlaceSingleTile(centerRoofStart, j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                        Func.PlaceSingleTile(centerRoofEnd  , j, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id);
                                    }

                                    // place backwall
                                    Func.ReplaceWallArea(new(freeR.XCenter - 1, centerRoofBottom + 1, freeR.XCenter - 1, sideRoofTop + 2, "dummy"), Deco[S.DoorWall].id);
                                    Func.ReplaceWallArea(new(freeR.XCenter + 2, centerRoofBottom + 1, freeR.XCenter + 2, sideRoofTop + 2, "dummy"), Deco[S.DoorWall].id);

                                    Func.ReplaceWallArea(new(freeR.XCenter, centerRoofBottom + 1, freeR.XCenter + 1, sideRoofTop + 2, "dummy"), WallID.Lavafall);

                                    // hang decoration
                                    if (Chance.Perc(85)) placed = WorldGen.PlaceTile(freeR.XCenter - 1, centerRoofBottom + 1, TileID.Platforms, style: 12); // Dungeon Shelf
                                    if (Chance.Perc(85)) placed = WorldGen.PlaceTile(freeR.XCenter + 2, centerRoofBottom + 1, TileID.Platforms, style: 12); // Dungeon Shelf


                                    // side arms for high rooms
                                    if (freeR.YTiles >= 23 && Chance.Perc(75))
                                    {
                                        // left
                                        Func.PlaceSingleTile(centerRoofStart - 1, centerRoofBottom    , Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.BotLeft);
                                        Func.PlaceSingleTile(centerRoofStart - 1, centerRoofBottom - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        Func.PlaceSingleTile(centerRoofStart - 1, centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        Func.PlaceSingleTile(centerRoofStart - 1, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight);
                                        Func.PlaceSingleTile(centerRoofStart - 2, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        //Func.PlaceSingleTile(centerRoofStart - 3, centerRoofBottom - 3, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id, slope: (int)Func.SlopeVal.UpLeft);
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(centerRoofStart - 3, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(centerRoofStart - 2, centerRoofBottom - 3, Deco[S.TempleBrick].id)]));
                                        
                                        //WorldGen.PlaceObject(centerRoofStart - 3, centerRoofBottom - 2, TileID.PotsSuspended, style: 0); // Hanging Pot
                                        runAfterWorldCleanup.Add(() => { WorldGen.PlaceObject(centerRoofStart - 3, centerRoofBottom - 2, TileID.PotsSuspended, style: 0); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(centerRoofStart - 2, centerRoofBottom - 3, Deco[S.TempleBrick].id)]));

                                        // right
                                        Func.PlaceSingleTile(centerRoofEnd + 1, centerRoofBottom, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.BotRight);
                                        Func.PlaceSingleTile(centerRoofEnd + 1, centerRoofBottom - 1, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        Func.PlaceSingleTile(centerRoofEnd + 1, centerRoofBottom - 2, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        Func.PlaceSingleTile(centerRoofEnd + 1, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpLeft);
                                        Func.PlaceSingleTile(centerRoofEnd + 2, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id);
                                        //Func.PlaceSingleTile(centerRoofEnd + 3, centerRoofBottom - 3, TileID.MarbleColumn, paint: Deco[S.TempleColumnPaint].id, slope: (int)Func.SlopeVal.UpRight);
                                        runAfterWorldCleanup.Add(() => { Func.PlaceSingleTile(centerRoofEnd + 3, centerRoofBottom - 3, Deco[S.TempleBrick].id, paint: Deco[S.TempleBrickAltarPaint].id, slope: (int)Func.SlopeVal.UpRight); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(centerRoofEnd + 2, centerRoofBottom - 3, Deco[S.TempleBrick].id)]));

                                        //WorldGen.PlaceObject(centerRoofEnd + 3, centerRoofBottom - 2, TileID.PotsSuspended, style: 0); // Hanging Pot
                                        runAfterWorldCleanup.Add(() => { WorldGen.PlaceObject(centerRoofEnd + 2, centerRoofBottom - 2, TileID.PotsSuspended, style: 0); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, [(centerRoofEnd + 2, centerRoofBottom - 3, Deco[S.TempleBrick].id)]));

                                    }
                                }

                                #endregion
                            }
                            #endregion

                            #region XTiles <= 18 -> Tree of Death
                            else if (middleSpace.XTiles <= 18)
                            {
                                if (Chance.Perc(50))
                                {
                                    // 2 trees and planters in middle
                                    #region prepare soil

                                    y = freeR.Y1 + 1; // delete floor brick and place ash
                                    for (int i = middleSpace.X0; i <= middleSpace.X1; i++)
                                    {
                                        WorldGen.KillTile(i, y);
                                        WorldGen.PlaceTile(i, y, TileID.Ash);
                                    }

                                    y = freeR.Y1;
                                    int xTree1 = middleSpace.X0 + 4;
                                    int xTree2 = middleSpace.X1 - 4;

                                    List<int> xOnTree =
                                    [
                                        xTree1 - 1, xTree1, xTree1 + 1,
                                        xTree2 - 1, xTree2, xTree2 + 1
                                    ];

                                    for (int i = middleSpace.X0 + 1; i <= middleSpace.X1 - 1; i++)
                                    {
                                        if (xOnTree.Contains(i))
                                        {
                                            WorldGen.PlaceTile(i, y, TileID.Ash);
                                            WorldGen.PlaceTile(i, y, TileID.AshGrass);
                                        }
                                        else if (Chance.Perc(15))
                                        {
                                            WorldGen.PlaceTile(i, y, TileID.Ash);
                                            WorldGen.PoundTile(i, y);
                                        }
                                        else
                                        {
                                            WorldGen.PlaceLiquid(i, y, (byte)LiquidID.Lava, 255);
                                        }
                                    }
                                    #endregion

                                    #region plant trees

                                    int ySaplingPlacement = freeR.Y1 - 1;
                                    y = ySaplingPlacement;
                                    WorldGen.PlaceObject(xTree1, y, TileID.Saplings, style: 10);
                                    WorldGen.KillWall(xTree1, y);

                                    WorldGen.PlaceObject(xTree2, y, TileID.Saplings, style: 10);
                                    WorldGen.KillWall(xTree2, y);

                                    WorldGen.GrowTreeSettings treeOfDeathSettings = WorldGen.GrowTreeSettings.Profiles.Tree_Ash; // fetch settings

                                    num = ((y - ((freeR.Y0 + 1) - 1)) - treeOfDeathSettings.TreeTopPaddingNeeded) - 1; // 1 tile between tree top and ceiling
                                    int height1 = num - WorldGen.genRand.Next(3);
                                    if (height1 < 2) height1 = 2;
                                    int yTrunkEnd1 = ySaplingPlacement - (height1 - 1); // height where the trunk of the tree ends, the next higher tile is the trees top
                                    treeOfDeathSettings.TreeHeightMin = height1;
                                    treeOfDeathSettings.TreeHeightMax = height1;
                                    WorldGen.GrowTreeWithSettings(xTree1, y, treeOfDeathSettings);
                                    Func.PaintArea(new(xTree1, freeR.Y0, xTree1, y, "dummy"), Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree1 - 1, y, (byte)Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree1 + 1, y, (byte)Deco[S.TreePaint].id);

                                    int height2 = num - WorldGen.genRand.Next(3);
                                    if (height2 < 2) height2 = 2;
                                    int yTrunkEnd2 = ySaplingPlacement - (height2 - 1); // height where the trunk of the tree ends, the next higher tile is the trees top
                                    treeOfDeathSettings.TreeHeightMin = height2;
                                    treeOfDeathSettings.TreeHeightMax = height2;
                                    WorldGen.GrowTreeWithSettings(xTree2, y, treeOfDeathSettings);
                                    Func.PaintArea(new(xTree2, freeR.Y0, xTree2, y, "dummy"), Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree2 - 1, y, (byte)Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree2 + 1, y, (byte)Deco[S.TreePaint].id);

                                    #endregion

                                    #region create lavafalls at sides

                                    x = middleSpace.X0;
                                    y = freeR.Y0 - 2;
                                    WorldGen.KillTile(x + 2, y);
                                    WorldGen.PlaceLiquid(x + 2, y, (byte)LiquidID.Lava, 175);
                                    //WorldGen.KillTile(x, y    );
                                    //WorldGen.KillTile(x, y + 1);
                                    //WorldGen.PoundTile(x + 1, y);
                                    runAfterWorldCleanup.Add(() => { WorldGen.KillTile(middleSpace.X0, freeR.Y0 - 2); }, (true, 0, 0, [(middleSpace.X0, freeR.Y0 - 2, Deco[S.Brick].id)]));
                                    runAfterWorldCleanup.Add(() => { WorldGen.KillTile(middleSpace.X0, freeR.Y0 - 1); }, (true, 0, 0, [(middleSpace.X0, freeR.Y0 - 1, Deco[S.Brick].id)]));
                                    runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(middleSpace.X0 + 1, freeR.Y0 - 2); }, (true, 0, 0, [(middleSpace.X0 + 1, freeR.Y0 - 2, Deco[S.Brick].id)]));

                                    x = middleSpace.X1;
                                    y = freeR.Y0 - 2;
                                    WorldGen.KillTile(x - 2, y);
                                    WorldGen.PlaceLiquid(x - 2, y, (byte)LiquidID.Lava, 175);
                                    //WorldGen.KillTile(x, y    );
                                    //WorldGen.KillTile(x, y + 1);
                                    //WorldGen.PoundTile(x + 1, y);
                                    runAfterWorldCleanup.Add(() => { WorldGen.KillTile(middleSpace.X1, freeR.Y0 - 2); }, (true, 0, 0, [(middleSpace.X1, freeR.Y0 - 2, Deco[S.Brick].id)]));
                                    runAfterWorldCleanup.Add(() => { WorldGen.KillTile(middleSpace.X1, freeR.Y0 - 1); }, (true, 0, 0, [(middleSpace.X1, freeR.Y0 - 1, Deco[S.Brick].id)]));
                                    runAfterWorldCleanup.Add(() => { WorldGen.PoundTile(middleSpace.X1 - 1, freeR.Y0 - 2); }, (true, 0, 0, [(middleSpace.X1 - 1, freeR.Y0 - 2, Deco[S.Brick].id)]));
                                    #endregion

                                    #region create planters in the middle

                                    bool spotFound = false;
                                    int ySpotPlanter = 0; // height where the planter can be placed
                                    num = Math.Max(yTrunkEnd1, yTrunkEnd2) + 1; //look which tree reaches less far up. "+1" so that the planter doesn't cover the tree top

                                    int endUpperThird = freeR.Y0 + freeR.YTiles / 3;
                                    if (num > freeR.Y0 + freeR.YTiles / 3) // tree tops are in the middle third of the room
                                    {
                                        endUpperThird = num;
                                    }

                                    for (int j = freeR.Y1 - (freeR.YTiles / 3 - 1); j >= endUpperThird; j--) // planter in the central third would look the best, the lower the better
                                    {
                                        spotFound = !Main.tile[xTree1 + 1, j].HasTile && !Main.tile[xTree2 - 1, j].HasTile; // check if free of tree branches
                                        if (spotFound)
                                        {
                                            ySpotPlanter = j;
                                            break;
                                        }
                                    }

                                    if (!spotFound) // still no spot found
                                    {
                                        for (int j = freeR.Y1 - (freeR.YTiles / 3 - 1); j <= ySaplingPlacement - 1; j++) // look in the lower area, the higher the better
                                        {
                                            spotFound = !Main.tile[xTree1 + 1, j].HasTile && !Main.tile[xTree2 - 1, j].HasTile; // check if free of tree branches
                                            if (spotFound)
                                            {
                                                ySpotPlanter = j;
                                                break;
                                            }
                                        }
                                    }


                                    if (spotFound)
                                    {
                                        #region place planter and column
                                        for (int i = xTree1 + 2; i <= xTree2 - 2; i++)
                                        {
                                            WorldGen.PlaceTile(i, ySpotPlanter, TileID.PlanterBox, style: 7); // Fireblossom Planter Box
                                            WorldGen.PlaceTile(i, ySpotPlanter - 1, TileID.ImmatureHerbs, style: 5); // growing Fireblossom
                                        }

                                        for (int j = ySpotPlanter + 1; j <= freeR.Y1; j++)
                                        {
                                            Func.PlaceSingleTile(freeR.XCenter, j, TileID.MarbleColumn, paint: PaintID.DeepRedPaint);
                                            Func.PlaceSingleTile(freeR.XCenter + 1, j, TileID.MarbleColumn, paint: PaintID.DeepRedPaint);
                                        }
                                        #endregion

                                        #region place side planters
                                        bool leftSpotFound = false;
                                        int yleftSpot = 0;

                                        x = xTree1 + 1;
                                        for (int j = ySpotPlanter + 2; j < ySaplingPlacement; j++)
                                        {
                                            leftSpotFound = !Main.tile[x, j].HasTile; // check if free of tree branches
                                            if (leftSpotFound)
                                            {
                                                yleftSpot = j;
                                                if (Main.tile[x, j - 1].HasTile && !Main.tile[x, j + 1].HasTile && (j + 1 < ySaplingPlacement)) //if there is a branch right above the planter, peek at the below height
                                                {
                                                    yleftSpot++; // take the more suitable spot
                                                }
                                                break;
                                            }
                                        }
                                        if (leftSpotFound)
                                        {
                                            for (int i = xTree1 + 1; i <= xTree1 + 3; i++)
                                            {
                                                WorldGen.PlaceTile(i, yleftSpot, TileID.PlanterBox, style: 7); // Fireblossom Planter Box
                                                WorldGen.PlaceTile(i, yleftSpot - 1, TileID.ImmatureHerbs, style: 5); // growing Fireblossom
                                            }

                                            for (int j = yleftSpot + 1; j <= freeR.Y1; j++)
                                            {
                                                Func.PlaceSingleTile(xTree1 + 2, j, TileID.MarbleColumn, paint: PaintID.DeepRedPaint);
                                            }
                                        }


                                        bool rightSpotFound = false;
                                        int yrightSpot = 0;

                                        x = xTree2 - 1;
                                        for (int j = ySpotPlanter + 2; j < ySaplingPlacement; j++)
                                        {
                                            rightSpotFound = !Main.tile[x, j].HasTile; // check if free of tree branches
                                            if (rightSpotFound)
                                            {
                                                yrightSpot = j;

                                                if (Main.tile[x, j - 1].HasTile && !Main.tile[x, j + 1].HasTile && (j + 1 < ySaplingPlacement)) //if there is a branch right above the planter, peek at the below height
                                                {
                                                    yrightSpot++; // take the more suitable spot
                                                }

                                                break;
                                            }
                                        }
                                        if (rightSpotFound)
                                        {
                                            for (int i = xTree2 - 3; i <= xTree2 - 1; i++)
                                            {
                                                WorldGen.PlaceTile(i, yrightSpot, TileID.PlanterBox, style: 7); // Fireblossom Planter Box
                                                WorldGen.PlaceTile(i, yrightSpot - 1, TileID.ImmatureHerbs, style: 5); // growing Fireblossom
                                            }

                                            for (int j = yrightSpot + 1; j <= freeR.Y1; j++)
                                            {
                                                if (!Main.tile[xTree2 - 2, j].HasTile) Func.PlaceSingleTile(xTree2 - 2, j, TileID.MarbleColumn, paint: PaintID.DeepRedPaint);
                                            }
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        // has never happened during testing...
                                    }

                                    #endregion
                                }
                                else
                                {
                                    // 3 trees
                                    #region prepare soil

                                    y = freeR.Y1 + 1; // delete floor brick and place ash
                                    for (int i = middleSpace.X0; i <= middleSpace.X1; i++)
                                    {
                                        WorldGen.KillTile(i, y);
                                        WorldGen.PlaceTile(i, y, TileID.Ash);
                                    }

                                    y = freeR.Y1;
                                    int xTree2 = middleSpace.XCenter + WorldGen.genRand.Next(2); // to alternate position
                                    int xTree1 = xTree2 - 6;
                                    int xTree3 = xTree2 + 6;

                                    List<int> xOnTree =
                                    [
                                        xTree1 - 1, xTree1, xTree1 + 1,
                                        xTree2 - 1, xTree2, xTree2 + 1,
                                        xTree3 - 1, xTree3, xTree3 + 1
                                    ];

                                    for (int i = middleSpace.X0 + 1; i <= middleSpace.X1 - 1; i++)
                                    {
                                        if (xOnTree.Contains(i))
                                        {
                                            Func.PlaceSingleTile(i, y, TileID.Ash, overlayID: TileID.AshGrass);
                                        }
                                        else if (Chance.Perc(25))
                                        {
                                            Func.PlaceSingleTile(i, y, TileID.Ash, overlayID: TileID.AshGrass);
                                            WorldGen.PoundTile(i, y);
                                        }
                                        else
                                        {
                                            WorldGen.PlaceLiquid(i, y, (byte)LiquidID.Lava, 255);
                                        }

                                    }
                                    #endregion

                                    #region plant trees

                                    int ySaplingPlacement = y = freeR.Y1 - 1;
                                    WorldGen.GrowTreeSettings ashTreeSettings = WorldGen.GrowTreeSettings.Profiles.Tree_Ash; // fetch settings

                                    WorldGen.PlaceObject(xTree1, y, TileID.Saplings, style: 10);
                                    WorldGen.KillWall(xTree1, y);

                                    WorldGen.PlaceObject(xTree2, y, TileID.Saplings, style: 10);
                                    WorldGen.KillWall(xTree2, y);

                                    WorldGen.PlaceObject(xTree3, y, TileID.Saplings, style: 10);
                                    WorldGen.KillWall(xTree3, y);


                                    // highest tree first
                                    int height2 = ((y - ((freeR.Y0 + 1) - 1)) - ashTreeSettings.TreeTopPaddingNeeded) - 1; // 1 tile between tree top and ceiling
                                    if (height2 < 2) height2 = 2;
                                    int yTrunkEnd1 = ySaplingPlacement - (height2 - 1); // height where the trunk of the tree ends, the next higher tile is the trees top
                                    ashTreeSettings.TreeHeightMin = height2;
                                    ashTreeSettings.TreeHeightMax = height2;
                                    WorldGen.GrowTreeWithSettings(xTree2, y, ashTreeSettings);
                                    Func.PaintArea(new(xTree2, freeR.Y0, xTree2, y, "dummy"), Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree2 - 1, y, (byte)Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree2 + 1, y, (byte)Deco[S.TreePaint].id);


                                    int height1 = height2 - WorldGen.genRand.Next(3,6);
                                    if (height1 < 2) height1 = 2;
                                    int yTrunkEnd2 = ySaplingPlacement - (height1 - 1); // height where the trunk of the tree ends, the next higher tile is the trees top
                                    ashTreeSettings.TreeHeightMin = height1;
                                    ashTreeSettings.TreeHeightMax = height1;
                                    WorldGen.GrowTreeWithSettings(xTree1, y, ashTreeSettings);
                                    Func.PaintArea(new(xTree1, freeR.Y0, xTree1, y, "dummy"), Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree1 - 1, y, (byte)Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree1 + 1, y, (byte)Deco[S.TreePaint].id);


                                    int height3 = height2 - WorldGen.genRand.Next(3, 6);
                                    if (height3 < 2) height3 = 2;
                                    int yTrunkEnd3 = ySaplingPlacement - (height3 - 1); // height where the trunk of the tree ends, the next higher tile is the trees top
                                    ashTreeSettings.TreeHeightMin = height3;
                                    ashTreeSettings.TreeHeightMax = height3;
                                    WorldGen.GrowTreeWithSettings(xTree3, y, ashTreeSettings);
                                    Func.PaintArea(new(xTree3, freeR.Y0, xTree3, y, "dummy"), Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree3 - 1, y, (byte)Deco[S.TreePaint].id);
                                    WorldGen.paintTile(xTree3 + 1, y, (byte)Deco[S.TreePaint].id);

                                    #endregion

                                    #region look for branches to hang stuff
                                    List<int> bannerStyle =
                                    [
                                        12, // Rusted Company Standard
                                        15, // Diabolic Sigil
                                        17, // Hell Hammer
                                        19, // Lost Hopes of Man
                                        20, // Obsidian Watcher
                                    ];

                                    int xTree = 0, xFirst, xSecond, yStart = 0, xStart = 0;
                                    for (num = 1; num <= 3; num++) // all 3 trees
                                    {
                                        placed = false; //init
                                        if (num == 1)      { xTree = xTree1; yStart = yTrunkEnd1; }
                                        else if (num == 2) { xTree = xTree2; yStart = yTrunkEnd2; }
                                        else if (num == 3) { xTree = xTree3; yStart = yTrunkEnd3; }

                                        if (Chance.Simple()) // randomly start left or right of the tree
                                        {
                                            xFirst = xTree - 1;
                                            xSecond = xTree + 1;
                                        }
                                        else
                                        {
                                            xFirst = xTree + 1;
                                            xSecond = xTree - 1;
                                        }

                                        for (int j = yStart; j < ySaplingPlacement; j++) // go down the tree
                                        {
                                            int dir = 0;
                                            for (int num2 = 1; num2 <= 2; num2++) // for both sides of the tree
                                            {
                                                if      (num2 == 1) xStart = xFirst;
                                                else if (num2 == 2) xStart = xSecond;
                                                dir = (xStart - xTree); // the direction from the trunk to the branch (-1 = left side, +1 = right side)

                                                if (Main.tile[xStart, j].HasTile)
                                                {
                                                    if (!Main.tile[xStart, j + 1].HasTile && !Main.tile[xStart, j + 2].HasTile) // enough space for hanging a lantern
                                                    {
                                                        if (!Main.tile[xStart, j + 3].HasTile && Chance.Perc(70)) // a third free tile, so a banner can be hanged
                                                        {
                                                            Func.PlaceSingleTile(xStart + dir, j, Deco[S.BannerHangPlat].id, style: Deco[S.BannerHangPlat].style, coat: PaintCoatingID.Echo);
                                                            WorldGen.PlaceObject(xStart + dir, j + 1, TileID.Banners, style: bannerStyle.PopAt(WorldGen.genRand.Next(bannerStyle.Count())));

                                                            placed = true;
                                                            break; // just 1 item per tree
                                                        }
                                                        else if (Chance.Perc(80))
                                                        {
                                                            Func.PlaceSingleTile(xStart + dir, j, Deco[S.BannerHangPlat].id, style: Deco[S.BannerHangPlat].style, coat: PaintCoatingID.Echo);
                                                            WorldGen.PlaceObject(xStart + dir, j + 1, TileID.LavaflyinaBottle);

                                                            placed = true; 
                                                            break; // just 1 item per tree
                                                        }
                                                    }
                                                }
                                            }

                                            if (placed)
                                            {
                                                if (Main.tile[xStart, j].TileFrameY < 198) // trees use empty parts of their spritesheet for rendering the "longer" branches. The 1 tile branches are normal ones from the spritesheet
                                                {                                          // --> if it's just a 1 tile branch, there is nothing the banner can hang onto, so make the platform visible and create a chain above!
                                                    x = xStart + dir;
                                                    WorldGen.paintCoatTile(x, j, 0); // remove echo coat

                                                    for (int num2 = freeR.Y0; num2 < j; num2++)
                                                    {
                                                        WorldGen.PlaceTile(x, num2, TileID.Chain); //place chains till the ceiling
                                                    }
                                                }
                                                break; // just 1 item per tree
                                            }  
                                        }
                                    }



                                    #endregion

                                    #region create planters between trees

                                    for (num = 0; num < 2; num++)
                                    {
                                        bool spotFound = false;
                                        int ySpotPlanter = 0; // height where the planter can be placed

                                        int num2 = 0; // look which tree reaches less far up.
                                        int firstTreeX = 0, secondTreeX = 0;

                                        if (num == 0)
                                        {
                                            firstTreeX = xTree1;
                                            secondTreeX = xTree2;
                                            num2 = Math.Max(yTrunkEnd1, yTrunkEnd2) + 1; // "+1" so that the planter doesn't cover the tree top
                                        }
                                        else if (num == 1)
                                        {
                                            firstTreeX = xTree2;
                                            secondTreeX = xTree3;
                                            num2 = Math.Max(yTrunkEnd2, yTrunkEnd3) + 1; // "+1" so that the planter doesn't cover the tree top
                                        }


                                        int upperEnd = freeR.Y1 - (freeR.YTiles / 3 - 1);
                                        if (num2 > upperEnd) upperEnd = num2; // tree tops are in the lower third of the room

                                        for (int j = upperEnd; j <= ySaplingPlacement - 1; j++) // look in the lower area, the higher the better
                                        {
                                            spotFound = (!Main.tile[firstTreeX + 1, j    ].HasTile && !Main.tile[secondTreeX - 1, j    ].HasTile) && // check if free of tree branches
                                                        (!Main.tile[firstTreeX + 2, j - 1].HasTile && !Main.tile[secondTreeX - 2, j - 1].HasTile); // and free of banners / lanterns
                                            if (spotFound)
                                            {
                                                ySpotPlanter = j;
                                                break;
                                            }
                                        }


                                        if (spotFound)
                                        {
                                            //place planter and column
                                            for (int i = firstTreeX + 2; i <= secondTreeX - 2; i++)
                                            {
                                                WorldGen.PlaceTile(i, ySpotPlanter, TileID.PlanterBox, style: 7); // Fireblossom Planter Box
                                                WorldGen.PlaceTile(i, ySpotPlanter - 1, TileID.ImmatureHerbs, style: 5); // growing Fireblossom
                                            }

                                            x = firstTreeX + 3;
                                            for (int j = ySpotPlanter + 1; j <= freeR.Y1; j++)
                                            {
                                                Func.PlaceSingleTile(x, j, TileID.MarbleColumn, paint: PaintID.DeepRedPaint);
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                            #endregion

                            #region XTiles <= 20 -> Giant Sword structuree
                            else if (middleSpace.XTiles <= 20)
                            {
                                (bool success, Rectangle2P pommelR, Rectangle2P handleR, Rectangle2P crossGuardR, Rectangle2P crossGuardCenterR, Rectangle2P bladeR, List<(int x, int y, int tileID)> checkPoints) result;
                                
                                if (freeR.YTiles >= 17)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));

                                    // small stone podest for the statue where it can stuck into
                                    int podestXStart = freeR.XCenter - 5;//result.bladeR.X0 - 3;
                                    int podestXEnd = freeR.XCenter + 6;//result.bladeR.X1 + 3;
                                    int podestYEnd = freeR.Y1 - 1;
                                    if (freeR.YTiles == 17)
                                    {
                                        podestXStart++; // just one line of podest
                                        podestXEnd++;
                                        podestYEnd++;
                                    }

                                    for (int j = freeR.Y1; j >= podestYEnd; j--)
                                    {
                                        for (int i = podestXStart; i <= podestXEnd; i++)
                                        {
                                            if (i == podestXStart)    Func.PlaceSingleTile(i, j, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id, slope: (int)Func.SlopeVal.UpLeft);
                                            else if (i == podestXEnd) Func.PlaceSingleTile(i, j, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id, slope: (int)Func.SlopeVal.UpRight);
                                            else                      Func.PlaceSingleTile(i, j, Deco[S.Floor].id, paint: Deco[S.FloorPaint].id);
                                        }
                                        podestXStart++;
                                        podestXEnd--;
                                    }

                                    WorldGen.PlaceTile(middleSpace.XCenter, podestYEnd - 1, TileID.Statues, style: 1);

                                    WorldGen.PlaceTile(middleSpace.X0 + 2, podestYEnd - 3, TileID.Painting3X3, style: 45); //SwordRack
                                    WorldGen.PlaceTile(middleSpace.X1 - 2, podestYEnd - 3, TileID.Painting3X3, style: 45); //SwordRack
                                }
                                else if (freeR.YTiles >= 16)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));

                                    WorldGen.PlaceTile(middleSpace.XCenter, freeR.Y1, TileID.Statues, style: 1);

                                    WorldGen.PlaceTile(middleSpace.X0 + 2, freeR.Y1 - 2, TileID.Painting3X3, style: 45); //SwordRack
                                    WorldGen.PlaceTile(middleSpace.X1 - 2, freeR.Y1 - 2, TileID.Painting3X3, style: 45); //SwordRack
                                }
                                else if (freeR.YTiles >= 13)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6, smallPommel: false, smallCrossGuard: false, actuated: true);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6, smallPommel: false, smallCrossGuard: false, actuated: true); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));
                                }
                                else if (freeR.YTiles == 12)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6, smallPommel: true, smallCrossGuard: false, actuated: true);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 4), 6, smallPommel: true, smallCrossGuard: false, actuated: true); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));
                                }
                                else if (freeR.YTiles == 11)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 3), 6, smallPommel: true, smallCrossGuard: false, actuated: true);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 3), 6, smallPommel: true, smallCrossGuard: false, actuated: true); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));
                                }
                                else if (freeR.YTiles == 10)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 2), 6, smallPommel: true, smallCrossGuard: false, actuated: true);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 2), 6, smallPommel: true, smallCrossGuard: false, actuated: true); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));
                                }
                                else if (freeR.YTiles == 9)
                                {
                                    result = CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 2), 6, smallPommel: true, smallCrossGuard: true, actuated: true);
                                    runAfterWorldCleanup.Add(() => { CreateGiantSword(new(middleSpace.X0, freeR.Y0, middleSpace.X1, freeR.Y1, "dummy"), (4, 2), 6, smallPommel: true, smallCrossGuard: true, actuated: true); }, (true, Deco[S.StyleSave].id, Deco[S.SubStyleSave].id, result.checkPoints));
                                }

                                // WeaponRacks with swords
                                randomItems.Clear();
                                randomItems.Add(((ushort)ItemID.GoldBroadsword, 0, 0));
                                randomItems.Add(((ushort)ItemID.PlatinumBroadsword, 0, 0));
                                randomItems.Add(((ushort)ItemID.GoldShortsword, 0, 0));
                                randomItems.Add(((ushort)ItemID.PlatinumShortsword, 0, 0));
                                randomItems.Add(((ushort)ItemID.Gladius, 0, 0));

                                randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count()));
                                Func.PlaceWeaponRack(middleSpace.X0 + 3, middleSpace.Y0 + 1, paint: Deco[S.StylePaint].id, direction: -1, item: randomItem.id);

                                randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count()));
                                Func.PlaceWeaponRack(middleSpace.X1 - 3, middleSpace.Y0 + 1, paint: Deco[S.StylePaint].id, direction:  1, item: randomItem.id);

                                //TODO: idea for low rooms....the sword is alomost unrecognizable there, maybe hand it horizontally? But there is the problem of 
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region deco for rooms without windows
                    if (!windowsExist)
                    {
                        num = WorldGen.genRand.Next(2);
                        switch (num)
                        {
                            // single big panoramic window
                            case 0:
                                Func.ReplaceWallArea(new(freeR.X0 + 1, freeR.Y0 + 2, freeR.X1 - 1, freeR.Y1 - 1, "dummyString"), WallID.Glass, chanceWithType: Deco[S.CrookedWall].id, chance: overWrite.chance);

                                for (int i = freeR.X0; i <= freeR.X1; i++)
                                {
                                    WorldGen.PlaceTile(i, freeR.Y0, Deco[S.DoorPlat].id, style: Deco[S.DoorPlat].style);
                                    WorldGen.paintTile(i, freeR.Y0, (byte)Deco[S.DoorPlatPaint].id);
                                }

                                if (freeR.XTiles >= 18)
                                {
                                    if (Chance.Perc(70)) // left couch
                                    {
                                        x = freeR.X0 + (freeR.XTiles / 3);
                                        WorldGen.PlaceTile(x, freeR.Y1, Deco[S.Sofa].id, style: Deco[S.Sofa].style);

                                        // workbench on the right
                                        WorldGen.PlaceTile(x + 2, freeR.Y1, Deco[S.Workbench].id, style: Deco[S.Workbench].style);

                                        randomItems.Clear();
                                        randomItems.Add((TileID.Candles, Deco[S.Candle].style, 95)); // Candle
                                        randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book
                                        randomItems.Add((TileID.Bottles, 6, 95)); // Wine Glass
                                        randomItems.Add((TileID.Bottles, 8, 95)); // Chalice

                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        if (Chance.Perc(randomItem.chance))
                                        {
                                            WorldGen.PlaceTile(x + 2, freeR.Y1 - 1, randomItem.id, style: randomItem.style);
                                            if (randomItem.id == TileID.Candles) Func.Unlight1x1(x + 2, freeR.Y1 - 1);
                                        }
                                        
                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        if (Chance.Perc(randomItem.chance))
                                        {
                                            WorldGen.PlaceTile(x + 3, freeR.Y1 - 1, randomItem.id, style: randomItem.style);
                                            if (randomItem.id == TileID.Candles) Func.Unlight1x1(x + 3, freeR.Y1 - 1);
                                        }

                                        // lamp on the left
                                        WorldGen.PlaceTile(x - 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                        Func.UnlightLamp(x - 2, freeR.Y1);
                                    }

                                    if (Chance.Perc(70)) // right couch
                                    {
                                        x = freeR.X1 - (freeR.XTiles / 3);
                                        WorldGen.PlaceTile(x, freeR.Y1, Deco[S.Sofa].id, style: Deco[S.Sofa].style);

                                        // workbench on the left
                                        WorldGen.PlaceTile(x - 3, freeR.Y1, Deco[S.Workbench].id, style: Deco[S.Workbench].style);

                                        randomItems.Clear();
                                        randomItems.Add((TileID.Candles, Deco[S.Candle].style, 95)); // Candle
                                        randomItems.Add((TileID.Books, WorldGen.genRand.Next(0, 5), 95)); // Book
                                        randomItems.Add((TileID.Bottles, 6, 95)); // Wine Glass
                                        randomItems.Add((TileID.Bottles, 8, 95)); // Chalice

                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        if (Chance.Perc(randomItem.chance))
                                        {
                                            WorldGen.PlaceTile(x - 3, freeR.Y1 - 1, randomItem.id, style: randomItem.style);
                                            if (randomItem.id == TileID.Candles) Func.Unlight1x1(x - 3, freeR.Y1 - 1);
                                        } 
                                        randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                                        if (Chance.Perc(randomItem.chance))
                                        {
                                            WorldGen.PlaceTile(x - 2, freeR.Y1 - 1, randomItem.id, style: randomItem.style);
                                            if (randomItem.id == TileID.Candles) Func.Unlight1x1(x - 2, freeR.Y1 - 1);
                                        }
                                        

                                        // lamp on the right
                                        WorldGen.PlaceTile(x + 2, freeR.Y1, Deco[S.Lamp].id, style: Deco[S.Lamp].style);
                                        Func.UnlightLamp(x + 2, freeR.Y1);
                                    }
                                }
                                else
                                {
                                    WorldGen.PlaceTile(freeR.XCenter, freeR.Y1, TileID.Statues, style: 22);
                                }    
                                break;

                            // columns with candelabras and chains in between
                            case 1:
                                // outer columns and ceiling
                                for (int i = freeR.X0; i <= freeR.X1; i++)
                                {
                                    WorldGen.PlaceTile(i, freeR.Y0, Deco[S.Column].id, style: Deco[S.Column].style);
                                    WorldGen.paintTile(i, freeR.Y0, (byte)Deco[S.ColumnPaint].id);
                                }
                                for (int j = freeR.Y0; j <= freeR.Y1; j++)
                                {
                                    WorldGen.PlaceTile(freeR.X0, j, Deco[S.Column].id, style: Deco[S.Column].style);
                                    WorldGen.paintTile(freeR.X0, j, (byte)Deco[S.ColumnPaint].id);

                                    WorldGen.PlaceTile(freeR.X1, j, Deco[S.Column].id, style: Deco[S.Column].style);
                                    WorldGen.paintTile(freeR.X1, j, (byte)Deco[S.ColumnPaint].id);
                                }
                                int sectionXTiles = 10; // width of each section
                                int sectionColumnsXTiles = 2; // how many columns are between each section
                                int middleSectionMinXTiles = 4; // minimal width, the central section shall have to look good
                                Rectangle2P leftSection = new(freeR.X0 + 1, freeR.Y0 + 1, sectionXTiles, freeR.YTiles - 1); // init
                                Rectangle2P rightSection = new(freeR.X1 - sectionXTiles, freeR.Y0 + 1, sectionXTiles, freeR.YTiles - 1); // init
                                Rectangle2P middleSection = new(1,1,1,1); //init so the compiler doesn't complain


                                if (rightSection.X0 > leftSection.X1) // at least one complete pair of sections
                                {
                                    while (rightSection.X0 > leftSection.X1)
                                    {
                                        if (((rightSection.X0 - sectionColumnsXTiles) - (leftSection.X1 + sectionColumnsXTiles)) - 1 >= middleSectionMinXTiles)
                                        {
                                            // put section columns
                                            for (int j = freeR.Y0; j <= freeR.Y1; j++)
                                            {
                                                for (int i = leftSection.X1 + 1; i <= leftSection.X1 + sectionColumnsXTiles; i++)
                                                {
                                                    WorldGen.PlaceTile(i, j, Deco[S.Column].id, style: Deco[S.Column].style);
                                                    WorldGen.paintTile(i, j, (byte)Deco[S.ColumnPaint].id);
                                                }

                                                for (int i = rightSection.X0 - sectionColumnsXTiles; i <= rightSection.X0 - 1; i++)
                                                {
                                                    WorldGen.PlaceTile(i, j, Deco[S.Column].id, style: Deco[S.Column].style);
                                                    WorldGen.paintTile(i, j, (byte)Deco[S.ColumnPaint].id);
                                                }
                                            }
                                            middleSection = new(leftSection.X1 + sectionColumnsXTiles + 1, freeR.Y0 + 1, rightSection.X0 - sectionColumnsXTiles - 1, freeR.Y1, "dummyString");
                                        }
                                        else middleSection = new(leftSection.X1 + 1, freeR.Y0 + 1, rightSection.X0 - 1, freeR.Y1, "dummyString");


                                        // left candelabra
                                        x = leftSection.XCenter;
                                        y = leftSection.YCenter;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id, Deco[S.DecoPlat].style, Deco[S.StylePaint].id), unlight: true);

                                        Func.PlaceHangingChains(leftSection, (TileID.Chain, 0, 0), 4);

                                        // right candelabra
                                        x = rightSection.XCenter;
                                        y = rightSection.YCenter;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id, Deco[S.DecoPlat].style, Deco[S.StylePaint].id), unlight: true);

                                        Func.PlaceHangingChains(rightSection, (TileID.Chain, 0, 0), 4);

                                        leftSection.Move(         sectionXTiles + sectionColumnsXTiles, 0); // update
                                        rightSection.Move((-1) * (sectionXTiles + sectionColumnsXTiles), 0); // update
                                    }

                                    // middle section
                                    if (middleSection.XTiles < sectionXTiles)
                                    {
                                        if (middleSection.XTiles < 1) num = 1; //do nothing
                                        else if (Main.tile[middleSection.X0 - 1, middleSection.Y0].TileType == Deco[S.Column].id) // middle section between columns
                                        {
                                            Func.ReplaceWallArea(middleSection, Deco[S.WindowWall].id, (byte)Deco[S.WindowPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);
                                        }
                                        else
                                        {
                                            Func.ReplaceWallArea(new(middleSection.X0, middleSection.Y0, 1, middleSection.YTiles),
                                                                 Deco[S.MiddleWall].id, (byte)Deco[S.MiddleWallPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);

                                            Func.ReplaceWallArea(new(middleSection.X0 + 1, middleSection.Y0, middleSection.X1 - 1, middleSection.Y1, "dummy"),
                                                                 Deco[S.WindowWall].id, (byte)Deco[S.WindowPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);

                                            Func.ReplaceWallArea(new(middleSection.X1, middleSection.Y0, 1, middleSection.YTiles),
                                                                 Deco[S.MiddleWall].id, (byte)Deco[S.MiddleWallPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);
                                        }
                                    }
                                    else if (middleSection.XTiles <= 14)
                                    {
                                        // right candelabra
                                        x = middleSection.XCenter;
                                        y = middleSection.YCenter;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id, Deco[S.DecoPlat].style, Deco[S.StylePaint].id), unlight: true);

                                        Func.PlaceHangingChains(middleSection, (TileID.Chain, 0, 0), 4);
                                    }
                                    else
                                    {
                                        Func.ReplaceWallArea(new(middleSection.X0, middleSection.Y0, 4, middleSection.YTiles),
                                                             Deco[S.WindowWall].id, (byte)Deco[S.WindowPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);

                                        Func.ReplaceWallArea(new(middleSection.X1 - 3, middleSection.Y0, 4, middleSection.YTiles),
                                                             Deco[S.WindowWall].id, (byte)Deco[S.WindowPaint].id, chance: overWrite.chance, chanceWithType: overWrite.id);

                                        Place6x4PaintingByStyle(new(middleSection.XCenter - 2, middleSection.YCenter - 2, 6, 4), Deco[S.StyleSave].id);

                                    }
                                }
                                else
                                {
                                    if ((freeR.XTiles - 2) > 12)
                                    {
                                        // put two candelabras on platforms
                                        x = freeR.X0 + (freeR.XTiles / 3);
                                        y = freeR.YCenter + 1;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id  , Deco[S.DecoPlat].style  , Deco[S.StylePaint].id), unlight: true);

                                        x = freeR.X1 - (freeR.XTiles / 3) - 1;
                                        y = freeR.YCenter + 1;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id  , Deco[S.DecoPlat].style  , Deco[S.StylePaint].id), unlight: true);

                                        Func.PlaceHangingChains(freeR, (TileID.Chain, 0, 0), 4, maxChains: 6);
                                    }
                                    else
                                    {
                                        // just one centered candelabra
                                        x = freeR.XCenter;
                                        y = freeR.YCenter + 1;
                                        Func.PlaceCandelabraOnBase((x, y), (Deco[S.Candelabra].id, Deco[S.Candelabra].style, 0),
                                                                           (Deco[S.DecoPlat].id  , Deco[S.DecoPlat].style  , Deco[S.StylePaint].id), unlight: true);

                                        Func.PlaceHangingChains(freeR, (TileID.Chain, 0, 0), 4, maxChains: 5);
                                    }
                                }

                                
                                break;
                        }
                    }
                    #endregion

                    //TODO: Stuff on ceiling? HangingPots, Chandeliers, banners

                    Func.PlaceStinkbug(freeR);

                    break;

                case 100: // empty room for display
                      
                    break;

            }


            Func.PlaceCobWeb(freeR, 1, WorldGenMod.configChastisedChurchCobwebFilling);
        }

        /// <summary>
        /// The main method for decorating a staircase.
        /// <br/> As the world generation step "Smooth World" destroys the stairs (sloped platforms), in this function only the tiles for the staircase get computed and
        /// for putting the staircase in the world, the function "CreateStairsFromData" is used.
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        /// <param name="wallBreak">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
        public void DecorateStairCase(Rectangle2P room, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors, IDictionary<int, (bool exist, Vector2 point)> wallBreak)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");

            // init variables
            int x, y;
            (int id, int chance) overWrite = (Deco[S.CrookedWall].id, 60);

            #region put middle beam ("pole")

            int startY = doors[Door.Up].doorRect.Y1 + 1;
            int startX = freeR.XCenter; // 2 tiles middle beam
            if (!freeR.XEven) startX--; // 3 tiles middle beam

            for (int j = startY; j <= freeR.Y1; j++)
            {
                for (int i = startX; i <= (freeR.XCenter + 1); i++)
                {
                    //if (Main.tile[i, j].WallType == Deco[S.CrookedWall].id) continue;
                    WorldGen.KillWall(i, j);
                    WorldGen.PlaceWall(i, j, Deco[S.DoorWall].id);
                }
            }
            int beamWidth = ((freeR.XCenter + 1) - startX) + 1;

            List<int> onPole = [];
            for (int i = startX; i <= (freeR.XCenter + 1); i++) onPole.Add(i);

            #endregion

            #region put stairs
            Dictionary<(int x, int y), (int pounds, bool echoCoat)> stairs = [];// local variant of the global "PoundAfterSmoothWorld" that is easier to alter

            int creationDir = Func.RandPlus1Minus1(); // 1 = from left to right; -1 = from right to left
            int behindPole = creationDir * (-1); // first line is always in front of the middle beam

            y = startY = doors[Door.Up].doorRect.Y0 + 1;
            if      (creationDir == 1)  x = startX = onPole.Min();
            else if (creationDir == -1) x = startX = onPole.Max();
            else x = freeR.XCenter; // just so the compiler doesn't complain

            int rightTurningPoint = onPole.Max() + 1;
            int leftTurningPoint  = onPole.Min() - 1;

            List<int> poundRange = []; // each position in this field needs a hammer pound to actually have the platform look like stairs
            for (int i = onPole.Min() - 1; i <= (onPole.Max() + 1); i++) poundRange.Add(i);


            // pound platform tile of the already existing down door of the room above
            (int x, int y) stairsStart = (startX - creationDir, startY - 1);
            stairs.Add((stairsStart.x, stairsStart.y), (1, false));


            // define backwall deco strip length and put first one
            int decoStripYTiles = 2 * beamWidth + 1;
            if ((stairsStart.y + 1 + decoStripYTiles) <= freeR.Y1)
            {
                Func.ReplaceWallArea(new(stairsStart.x - creationDir, stairsStart.y + 3, 1, decoStripYTiles - 1), WallID.Bone, chance: overWrite.chance, chanceWithType: overWrite.id);
            }

            bool first = true;
            while (y <= freeR.Y1)
            {
                stairs.Add((x, y), (0, false)); // "place tile"
                if (poundRange.Contains(x))    Func.AddPoundToStairTile(stairs, (x, y), 1);
                if (first && creationDir == 1) Func.AddPoundToStairTile(stairs, (x, y), 1); //don't know why this tile of shit does need an additional pound!
                first = false;

                if (onPole.Contains(x) && creationDir == behindPole) Func.AddCoatingToStairTile(stairs, (x, y));

                if (x == rightTurningPoint)
                {
                    creationDir *= (-1);
                    y++;
                    if (y > freeR.Y1) continue; // last tile was already at the bottom

                    for (int i = x; i <= freeR.X1; i++)
                    {
                        stairs.Add((i, y), (0, false)); // "place tile"

                        if (poundRange.Contains(i)) Func.AddPoundToStairTile(stairs, (i, y), 1);
                    }

                    // place backwall deco strip
                    if ((y + 1 + decoStripYTiles) <= freeR.Y1)
                    {
                        Func.ReplaceWallArea(new(x + 1, y + 2, 1, decoStripYTiles), WallID.Bone, chance: overWrite.chance, chanceWithType: overWrite.id);
                    }
                }
                else if (x == leftTurningPoint)
                {
                    creationDir *= (-1);
                    y++;
                    if (y > freeR.Y1) continue; // last tile was already at the bottom

                    for (int i = x; i >= freeR.X0; i--)
                    {
                        stairs.Add((i, y), (0, false)); // "place tile"

                        if (poundRange.Contains(i)) Func.AddPoundToStairTile(stairs, (i, y), 1);
                    }

                    // place backwall deco strip
                    if ((y + 1 + decoStripYTiles) <= freeR.Y1)
                    {
                        Func.ReplaceWallArea(new(x - 1, y + 2, 1, decoStripYTiles), WallID.Bone, chance: overWrite.chance, chanceWithType: overWrite.id);
                    }
                }

                x += creationDir;
                y++;
            }
            #endregion

            #region transform local data into global form
            List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> poundList = [];
            (int pounds, bool echoCoat) values;
            int platType = Deco[S.DoorPlat].id;
            int platStyle = Deco[S.DoorPlat].style;
            byte platPaint = (byte)Deco[S.DoorPlatPaint].id;

            foreach ((int x, int y) point in stairs.Keys.ToArray())
            {
                values = stairs[point];

                poundList.Add((point.x, point.y, values.pounds, platType, platStyle, platPaint, values.echoCoat));
            }

            PoundAfterSmoothWorld.AddRange(poundList);
            #endregion

            CreateStairsFromData(poundList);

            #region place bones on the floor
            List<(ushort tileID, (int y, int x) sprite, byte chance)> randomBones =
            [
                (TileID.LargePiles, (0, 0) , 75 ), // 3x2 BonePile1
                (TileID.LargePiles, (0, 1) , 75 ), // 3x2 BonePile2
                (TileID.LargePiles, (0, 2) , 75 ), // 3x2 BonePile3
                (TileID.LargePiles, (0, 3) , 75 ), // 3x2 BonePile4
                (TileID.LargePiles, (0, 4) , 75 ), // 3x2 BonePile5
                (TileID.LargePiles, (0, 5) , 75 ), // 3x2 BonePile6
                                             
                (TileID.SmallPiles, (1, 6) , 75 ), // 2x1 BonePile1
                (TileID.SmallPiles, (1, 7) , 75 ), // 2x1 BonePile2
                (TileID.SmallPiles, (1, 8) , 75 ), // 2x1 BonePile3
                (TileID.SmallPiles, (1, 9) , 75 ), // 2x1 BonePile4
                (TileID.SmallPiles, (1, 10), 75 ), // 2x1 BonePile5
                (TileID.SmallPiles, (1, 10), 75 ), // 2x1 BonePile5 again, to have the an equal amount for each size
                                              
                (TileID.SmallPiles, (0, 12), 75 ), // 1x1 BonePile1
                (TileID.SmallPiles, (0, 13), 75 ), // 1x1 BonePile2
                (TileID.SmallPiles, (0, 14), 75 ), // 1x1 BonePile3 --> taken out to have the an equal amount for each size
                (TileID.SmallPiles, (0, 15), 75 ), // 1x1 BonePile4 --> taken out to have the an equal amount for each size
                (TileID.SmallPiles, (0, 16), 75 ), // 1x1 BonePile5
                (TileID.SmallPiles, (0, 17), 75 ), // 1x1 BonePile6
                (TileID.SmallPiles, (0, 18), 75 ), // 1x1 BonePile7
                (TileID.SmallPiles, (0, 19), 75 )  // 1x1 BonePile8
            ];

            (ushort tileID, (int y, int x) sprite, byte chance) randomBone; // for random item placement, to make interaction with randomBones List shorter
            Rectangle2P area1 = new(freeR.X0, freeR.Y1, freeR.XTiles, 1);
            Rectangle2P noBlock = Rectangle2P.Empty;

            for (int i = 1; i <= 3; i++)
            {
                randomBone = randomBones[WorldGen.genRand.Next(randomBones.Count)];

                Func.TryPlaceTile(area1, noBlock, randomBone.tileID, chance: randomBone.chance,
                                  add: new() { { "Piles", [randomBone.sprite.x, randomBone.sprite.y] } });
            }
            #endregion


            Func.PlaceCobWeb(freeR, 1, WorldGenMod.configChastisedChurchCobwebFilling);
        }

        /// <summary>
        /// The main method for choosing and running a below rooms decoration
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        /// <param name="wallBreak">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
        public void DecorateBelowRoom(Rectangle2P room, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors, IDictionary<int, (bool exist, Vector2 point)> wallBreak)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");


            // init variables
            bool placed, placed2;
            (bool success, int x, int y) placeResult, placeResult2;
            Rectangle2P area1, area2, area3, noBlock = Rectangle2P.Empty; // for creating areas for random placement
            List<(int x, int y)> rememberPos = []; // for remembering positions
            List<(ushort TileID, int style, byte chance)> randomItems = [], randomItems2 = []; // for random item placement
            (ushort id, int style, byte chance) randomItem, randomItem2; // for random item placement
            int x, y, chestID, unusedXTiles, num, numOld;


            LineAutomat automat;
            int actX;
            Rectangle2P cell; // area of the cell (including surroundings, and 
            Rectangle2P roomWoCell; // area of the room without the cell



            #region prison / torture room at rooms end

            #region prepare lists
            List<(int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add)> prisonItems_WallSkeletons =
            [
                (TileID.Painting3X3, 16, (3,3), (1,-1), 75, []), // wall skeleton
                (TileID.Painting3X3, 17, (3,3), (1,-1), 75, [])  // hanging skeleton
            ];
            List<(int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add)> prisonItems_LargePiles =
            [
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 0] } }),  // BonePile1
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 1] } }),  // BonePile2
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 2] } }),  // BonePile3
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 3] } }),  // BonePile4
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 4] } }),  // BonePile5
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 5] } }),  // BonePile6
                (TileID.LargePiles,  0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 6] } }),  // Skeleton Pierced by a Sword
                (TileID.LargePiles2, 0, (3,2), (1,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 13] } })  // a Dead Body covered in Web
            ];
            List<(int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add)> prisonItems_SmallPiles =
            [
                (TileID.SmallPiles, 0, (2,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [1, 6] } }),  // BonePile1
                (TileID.SmallPiles, 0, (2,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [1, 7] } }),  // BonePile2
                (TileID.SmallPiles, 0, (2,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [1, 8] } }),  // BonePile3
                (TileID.SmallPiles, 0, (2,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [1, 9] } }),  // BonePile4
                (TileID.SmallPiles, 0, (2,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [1, 10] } })  // BonePile5
            ];
            List<(int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add)> prisonItems_SinglePiles =
            [
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 12] } }),  // BonePile1
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 13] } }),  // BonePile2
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 14] } }),  // BonePile3
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 15] } }),  // BonePile4
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 16] } }),  // BonePile5
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 17] } }),  // BonePile6
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 18] } }),  // BonePile7
                (TileID.SmallPiles, 0, (1,1), (0,0), 75, new(){ {(int)LineAutomat.Adds.Piles, [0, 19] } })   // BonePile8
            ];
            List<List<(int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add)>> prisonItems_all =
            [
                prisonItems_WallSkeletons,
                prisonItems_LargePiles,
                prisonItems_SmallPiles,
                prisonItems_SinglePiles
            ];
            List<int> bannerStyle =
            [
                12, // Rusted Company Standard
                15, // Diabolic Sigil
                17, //  Hell Hammer
                19, // Lost Hopes of Man
                20, // Obsidian Watcher
            ];
            #endregion

            int cellheight = 5; // 4 tiles free, 1 tile ceiling bricks
            int cellLength = 11; //9 tiles free, 1 tile door spikes, 1 tile where the lamp hangs
            int brickHeight = freeR.Y1 - (cellheight - 1); //init; the height of the topmost row of iron bricks of a prison cell
            int bricksEnd, bricksStart;
            Rectangle2P cellFree; // the free inside of the cell

            bool leftRoom = doors[Door.Right].doorExist && !doors[Door.Left].doorExist; // TRUE  -> a "left of the stairs" below room, prison is on the left wall,
                                                                                        // FALSE -> a "right of the stairs" below room, prison is on the right wall
            if (leftRoom) // a "right" below room, prison is on the right wall
            {
                bricksStart = freeR.X0;
                bricksEnd = freeR.X0 + (cellLength - 1);

                roomWoCell = new(bricksEnd + 1, freeR.Y0, freeR.X1, freeR.Y1, "dummy");
            }
            else
            {
                bricksStart = freeR.X1 - (cellLength - 1);
                bricksEnd = freeR.X1;

                roomWoCell = new(freeR.X0, freeR.Y0, bricksStart - 1, freeR.Y1, "dummy");
            }
            cell = new(bricksStart, freeR.Y0, bricksEnd, freeR.Y1, "dummy");

            #region create and fill cells
            while (freeR.Y0 <= brickHeight)
            {
                if (leftRoom) cellFree = new(bricksStart, brickHeight + 1, cellLength - 2, cellheight - 1);
                else          cellFree = new(bricksStart + 2, brickHeight + 1, cellLength - 2, cellheight - 1);

                #region place bricks and wall
                // place ceiling bricks
                y = brickHeight;
                for (int i = bricksStart; i <= bricksEnd; i++)
                {
                    WorldGen.PlaceTile(i, y, Deco[S.BelowRoomFloor].id);
                    WorldGen.paintTile(i, y, (byte)Deco[S.BelowRoomFloorPaint].id);
                }

                // spikes "doors"
                for (int j = brickHeight + 1; j <= brickHeight + (cellheight - 1); j++)
                {
                    if(leftRoom) WorldGen.PlaceTile(bricksEnd - 1, j, TileID.Spikes);
                    else         WorldGen.PlaceTile(bricksStart + 1, j, TileID.Spikes);
                }

                // place backwall
                Func.PlaceWallArea(cellFree, WallID.WroughtIronFence, (byte)Deco[S.StylePaint].id);
                #endregion

                #region put lanterns or banners on the ledge and slope it
                placed = false; //init
                if (leftRoom) x = bricksEnd;
                else          x = bricksStart;
                y = brickHeight + 1;
                if (Chance.Perc(50))
                {
                    placed = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: 2); // Caged Lantern
                    if (placed) Func.UnlightLantern(x, y);
                }
                else if (Chance.Perc(50))
                {
                    placed = WorldGen.PlaceObject(x, y, TileID.Banners, style: bannerStyle[WorldGen.genRand.Next(bannerStyle.Count)]); // Rusted Company Standard
                }

                // slope ledge
                if(leftRoom) Func.SlopeTile(bricksEnd, brickHeight, (int)Func.SlopeVal.UpRight);
                else         Func.SlopeTile(bricksStart, brickHeight, (int)Func.SlopeVal.UpLeft);
                #endregion


                #region fill cell with bones
                int randNum, limX;
                (int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add) prisonItem;
                
                actX = cellFree.X0;
                limX = cellFree.X1;
                automat = new((actX, cellFree.Y1), (int)LineAutomat.Dirs.xPlus);


                while (actX <= limX)
                {
                    // get one decoration item
                    randNum = WorldGen.genRand.Next(4); // all prisonItem categories
                    switch (randNum)
                    {
                        case 0:
                            prisonItem = prisonItems_WallSkeletons[WorldGen.genRand.Next(prisonItems_WallSkeletons.Count)];
                            break;
                        case 1:
                            prisonItem = prisonItems_LargePiles[WorldGen.genRand.Next(prisonItems_LargePiles.Count)];
                            break;
                        case 2:
                            prisonItem = prisonItems_SmallPiles[WorldGen.genRand.Next(prisonItems_SmallPiles.Count)];
                            break;
                        default:
                            prisonItem = prisonItems_SinglePiles[WorldGen.genRand.Next(prisonItems_SinglePiles.Count)];
                            break;
                    }

                    if (prisonItem.TileID == TileID.Painting3X3 && (prisonItem.style == 16 || prisonItem.style == 17))
                    {
                        prisonItem.toAnchor.y -= WorldGen.genRand.Next(2); // randomly hang wall skeletons 1 tile higher
                    }

                    // analyze if left space is enought
                    if ((actX - 1) + prisonItem.size.x <= limX)
                    {
                        automat.Steps.Add(((int)LineAutomat.Cmds.Tile, prisonItem.TileID, prisonItem.style, prisonItem.size, prisonItem.toAnchor, prisonItem.chance, prisonItem.add));
                        actX += prisonItem.size.x;
                    }
                    else // if not, just put 1 empty space
                    {
                        automat.Steps.Add(((int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, []));
                        actX += 1;
                    }
                }
                automat.Start();
                #endregion

                brickHeight -= cellheight;
            }
            #endregion

            #region stuff above prisons / on ceiling
            area1 = Rectangle2P.Empty; //init: line area for the LineAutomat
            int pileKindStart = 0, pileKindEnd = 4; //init:  define which entries of prisonItems_all are going to be used for random item selection
            if (freeR.YTiles % cellheight > 0)
            {
                switch (freeR.YTiles % cellheight) //every cell is 5 Tiles high, get the remaining space till the ceiling
                {
                    case 1: // put spikes
                        y = freeR.Y0;
                        for (int i = bricksStart; i <= bricksEnd; i++)
                        {
                            WorldGen.PlaceTile(i, y, TileID.Spikes);
                        }
                        break;

                    case 2: // put bones
                        y = freeR.Y0 + 1;
                        if (leftRoom) area1 = new Rectangle2P(bricksStart, y, bricksEnd - 1, y, "dummyString");
                        else          area1 = new Rectangle2P(bricksStart + 1, y, bricksEnd, y, "dummyString");
                        pileKindStart = 1; // only large piles (wall skeletons don't fit) until..
                        pileKindEnd = 4;  // ..single piles

                        break;

                    case 3: // put bones and spikes
                        y = freeR.Y0 + 2;
                        if (leftRoom) area1 = new Rectangle2P(bricksStart, y, bricksEnd - 1, y, "dummyString");
                        else          area1 = new Rectangle2P(bricksStart + 1, y, bricksEnd, y, "dummyString");
                        pileKindStart = 1; // large piles until..
                        pileKindEnd = 4;  // ..single piles

                        //Spikes
                        y = freeR.Y0;
                        for (int i = bricksStart; i <= bricksEnd; i++)
                        {
                            WorldGen.PlaceTile(i, y, TileID.Spikes);
                        }

                        if (leftRoom) x = bricksStart;
                        else x = bricksEnd;
                        for (int j = freeR.Y0; j <= freeR.Y0 + 2; j++)
                        {
                            WorldGen.PlaceTile(x, j, TileID.Spikes);
                        }

                        break;

                    case 4: // put bones and spikes and banners
                        y = freeR.Y0 + 3;
                        if (leftRoom) area1 = new Rectangle2P(bricksStart, y, bricksEnd - 1, y, "dummyString");
                        else          area1 = new Rectangle2P(bricksStart + 1, y, bricksEnd, y, "dummyString");
                        pileKindStart = 1; // large piles until..
                        pileKindEnd = 4;  // ..single piles

                        // banner
                        bool placedBanner = false; //init
                        if (leftRoom) x = bricksEnd;
                        else          x = bricksStart;
                        y = freeR.Y0;
                        if (Chance.Perc(70))
                        {
                            placedBanner = WorldGen.PlaceObject(x, y, TileID.Banners, style: bannerStyle[WorldGen.genRand.Next(bannerStyle.Count)]);
                        }

                        //Spikes
                        y = freeR.Y0;
                        for (int i = bricksStart; i <= bricksEnd; i++)
                        {
                            if (i == bricksEnd && placedBanner) continue;
                            WorldGen.PlaceTile(i, y, TileID.Spikes);
                        }

                        if (leftRoom) x = bricksStart;
                        else x = bricksEnd;
                        for (int j = freeR.Y0; j <= freeR.Y0 + 3; j++)
                        {
                            WorldGen.PlaceTile(x, j, TileID.Spikes);
                        }

                        break;

                    default: break;
                }

                if (!area1.IsEmpty())
                {
                    int availableX = area1.XTiles; // init
                    while (availableX > 0)
                    {
                        if (availableX == 1) pileKindStart = 3; // choose single piles if nothing else fits anyway
                        else if (availableX < 3 && pileKindStart < 2) pileKindStart = 2; // large piles would fail to place anyway

                        int pilekind = WorldGen.genRand.Next(pileKindStart, pileKindEnd); // get a pile variant at random
                        int item = WorldGen.genRand.Next(prisonItems_all[pilekind].Count); // get a specific pile item at random

                        int type = prisonItems_all[pilekind][item].TileID;
                        int style = prisonItems_all[pilekind][item].style;
                        int xSprite = prisonItems_all[pilekind][item].add[(int)LineAutomat.Adds.Piles][1];
                        int ySprite = prisonItems_all[pilekind][item].add[(int)LineAutomat.Adds.Piles][0];
                        List<int> checkAdd;
                        if (pilekind == 0) checkAdd = [1, 1, 1, 1];      // Wall skeletons
                        else if (pilekind == 1) checkAdd = [1, 1, 1, 0]; // LargePiles
                        else if (pilekind == 2) checkAdd = [0, 1, 1, 0]; // SmallPiles
                        else checkAdd = [0, 0, 0, 0];               // SinglePiles

                        Func.TryPlaceTile(area1, noBlock, (ushort)type, style: style, chance: 75, add: new() { { "Piles", [xSprite, ySprite] }, { "CheckFree", checkAdd } });

                        availableX -= prisonItems_all[pilekind][item].size.x;
                    }
                }
            }

            #endregion

            #endregion


            #region place torturing tools

            // place cauldron first
            Rectangle2P toolArea = new(roomWoCell.XCenter - 2, freeR.Y1, roomWoCell.XCenter + 2, freeR.Y1, "dummy"); // some random spot in the middle of the still remaining room
            placeResult = Func.TryPlaceTile(toolArea, noBlock, TileID.CookingPots, style: 1);

            if (placeResult.success)
            {
                // hanging skeleton above the cauldron
                placed = false;
                if (Chance.Perc(80)) placed = WorldGen.PlaceTile(placeResult.x, placeResult.y - 3, TileID.Painting3X3, style: 17);
                if (placed) WorldGen.PlaceTile(placeResult.x, placeResult.y - 5, TileID.Chain);

                // cooking pot next to the cualdron
                if (Chance.Simple()) WorldGen.PlaceTile(placeResult.x - 2, placeResult.y, TileID.CookingPots, style: 0); // cooking pot left
                else                 WorldGen.PlaceTile(placeResult.x + 2, placeResult.y, TileID.CookingPots, style: 0); // cooking pot right
            }
            else //should actually never happen
            {
                Func.TryPlaceTile(toolArea, noBlock, TileID.CookingPots, style: 0); ; // cooking pot in the middle of the remaing room
            }

            toolArea.X0 -= 4;
            toolArea.X1 += 4; // widen toolArea for placing the workshop, the wall skeleton, and the racks

            Func.TryPlaceTile(toolArea, noBlock, TileID.HeavyWorkBench);
            Func.TryPlaceTile(toolArea.CloneAndMove(0, -1), noBlock, TileID.Painting3X3, style: 16);

            Func.TryPlaceTile(toolArea.CloneAndMove(0, -4), noBlock, TileID.Painting3X3, style: 41); // Blacksmith Rack
            Func.TryPlaceTile(toolArea.CloneAndMove(0, -4), noBlock, TileID.Painting3X3, style: 42); // Carpentry Rack


            // WeaponRack with whip
            for (int tries = 1; tries <= 4; tries++)
            {
                y = WorldGen.genRand.Next(roomWoCell.Y0 + 1, roomWoCell.Y1 - 3); // floor is already quite packed
                x = WorldGen.genRand.Next(toolArea.X0, toolArea.X1);

                placed = Func.PlaceWeaponRack(x, y, item: ItemID.BlandWhip, direction: Func.RandPlus1Minus1(), paint: PaintID.GrayPaint);
                if (placed) break;
            }

            #endregion


            #region fire pit with skeleton hanging above

            area1 = new(roomWoCell.X0 + 2, freeR.Y0, roomWoCell.X1 - 2, freeR.Y1, "dummy"); // leave some minimal distance to the entrance and the prison cell
            Func.PlaceFirePitSkeleton(area1, Rectangle2P.Empty, (TileID.Titanstone, 0), (TileID.LivingFire, 0));

            #endregion


            #region place bones on the floor

            int numBones = roomWoCell.XTiles / 3; // big bones are 3 XTiles wide, so this is the extreme case, that only big bones get placed...is also a good max amount in general
            area1 = new(roomWoCell.X0, freeR.Y1, roomWoCell.X1, freeR.Y1, "dummy"); // ground floor

            pileKindStart = 1; // large piles until..
            pileKindEnd = 4;  // ..single piles

            for (int i = 0; i < numBones; i++)
            {
                int pilekind = WorldGen.genRand.Next(pileKindStart, pileKindEnd); // get a pile variant at random
                int item = WorldGen.genRand.Next(prisonItems_all[pilekind].Count); // get a specific pile item at random

                int type = prisonItems_all[pilekind][item].TileID;
                int style = prisonItems_all[pilekind][item].style;
                int xSprite = prisonItems_all[pilekind][item].add[(int)LineAutomat.Adds.Piles][1];
                int ySprite = prisonItems_all[pilekind][item].add[(int)LineAutomat.Adds.Piles][0];
                List<int> checkAdd;
                if (pilekind == 0) checkAdd = [1, 1, 1, 1];      // Wall skeletons
                else if (pilekind == 1) checkAdd = [1, 1, 1, 0]; // LargePiles
                else if (pilekind == 2) checkAdd = [0, 1, 1, 0]; // SmallPiles
                else checkAdd = [0, 0, 0, 0];               // SinglePiles

                Func.TryPlaceTile(area1, noBlock, (ushort)type, style: style, chance: 75, add: new() { { "Piles", [xSprite, ySprite] }, { "CheckFree", checkAdd } });
            }

            #endregion


            #region place wall catacombs

            List<(int TileID, int style, (int x, int y) size, byte chance)> WallItems =
            [
                (TileID.Painting3X3, 16, (3,3), 75), // wall skeleton
                (TileID.Painting3X3, 17, (3,3), 75),  // hanging skeleton
                (TileID.Painting4X3,  0, (4,3), 75),  // Catacomb style 1, skeleton head left
                (TileID.Painting4X3,  1, (4,3), 75),  // Catacomb style 1, skeleton head right
                (TileID.Painting4X3,  2, (4,3), 75),  // Catacomb style 1, skeleton arm hanging out
                (TileID.Painting4X3,  3, (4,3), 75),  // Catacomb style 2, skeleton head left
                (TileID.Painting4X3,  4, (4,3), 75),  // Catacomb style 2, skeleton head right
                (TileID.Painting4X3,  5, (4,3), 75),  // Catacomb style 2, skeleton arm hanging out
                (TileID.Painting4X3,  6, (4,3), 75),  // Catacomb style 3, skeleton head left
                (TileID.Painting4X3,  7, (4,3), 75),  // Catacomb style 3, skeleton head right
                (TileID.Painting4X3,  8, (4,3), 75)   // Catacomb style 3, skeleton arm hanging out
            ];
            (int TileID, int style, (int x, int y) size, byte chance) wallItem;
            int numWallItem = roomWoCell.XTiles / 4; // catacombs are 4 XTiles wide, so this is the extreme case, that only big bones get placed...is also a good max amount in general

            for (int i = 0; i < numWallItem; i++)
            {
                wallItem = WallItems[WorldGen.genRand.Next(WallItems.Count)]; // get a wall item at random

                int type = wallItem.TileID;
                int style = wallItem.style;

                List<int> checkAdd;
                if      (type == TileID.Painting3X3) checkAdd = [1, 1, 1, 1]; // Wall skeletons
                else if (type == TileID.Painting4X3) checkAdd = [1, 2, 1, 1]; // Catacomb Skeletons
                else                                 checkAdd = [0, 0, 0, 0]; // should never happen

                y = WorldGen.genRand.Next(roomWoCell.Y0 + 1, roomWoCell.Y1); // random heigth: Y0 + 1  <=   y  <= Y1 - 1
                area1 = new(roomWoCell.X0, y, roomWoCell.X1, y, "dummy"); // placing height

                Func.TryPlaceTile(area1, noBlock, (ushort)type, style: style, chance: 75, add: new() { { "CheckFree", checkAdd } });
            }

            #endregion


            #region place hanging chains

            int maxChains = roomWoCell.XTiles / 5; // hang a chain about every fifth tile
            Func.PlaceHangingChains(roomWoCell, (TileID.Chain, 0, 0), 5, maxChains: maxChains, scanRoom: true);

            #endregion


            #region place banners / lantern at ceiling

            int maxCeiling = roomWoCell.XTiles / 5; // hang something about every fifth tile

            List<(int TileID, int style, (int x, int y) size, byte chance)> CeilingItems =
            [
                (TileID.HangingLanterns,  2, (1,3), 75), // Caged Lantern
                (TileID.HangingLanterns,  6, (1,3), 75), // Oil Rag Sconce
                (TileID.HangingLanterns, 25, (1,3), 75), // Bone Lantern
                (TileID.Banners, 10, (1,3), 75), // Marching Bones Banner
                (TileID.Banners, 11, (1,3), 75), // Necromantic Sign
                (TileID.Banners, 12, (1,3), 75), // Rusted Company Standard
                (TileID.Banners, 15, (1,3), 75), // Diabolic Sigil
                (TileID.Banners, 17, (1,3), 75), // Hell Hammer Banner
                (TileID.Banners, 18, (1,3), 75), // Helltower Banner
                (TileID.Banners, 21, (1,3), 75)  // Lava Erupts Banner
            ];
            (int TileID, int style, (int x, int y) size, byte chance) ceilingItem;

            for (int i = 0; i < maxCeiling; i++)
            {
                if (CeilingItems.Count == 0) break; 

                ceilingItem = CeilingItems.PopAt(WorldGen.genRand.Next(CeilingItems.Count)); // get a wall item at random and don't repeat!

                int type = ceilingItem.TileID;
                int style = ceilingItem.style;
                List<int> checkAdd = [0, 2, 0, 0];

                area1 = new(roomWoCell.X0, roomWoCell.Y0, roomWoCell.X1, roomWoCell.Y0, "dummy"); // ceiling

                placeResult = Func.TryPlaceTile(area1, noBlock, (ushort)type, style: style, chance: 75, add: new() { { "CheckFree", checkAdd } });
                if (type == TileID.HangingLanterns && placeResult.success) Func.UnlightLantern(placeResult.x, placeResult.y);
            }

            #endregion


            Func.PlaceCobWeb(freeR, 1, WorldGenMod.configChastisedChurchCobwebFilling);
        }


        /// <summary>
        /// Creating the stairs with the data from DecorateStairCase()
        /// </summary>
        /// <param name="localList">Same structure as the global "PoundAfterSmoothWorld". Handing it over, uses this data instead of the global data to create the staircase</param>
        public void CreateStairsFromData(List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> localList = null)
        {
            List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> workList = [];
            bool doTileTypeCheck = true;

            if (localList is not null)
            {
                workList = localList;
                doTileTypeCheck = false; // local mode means first time placing the stairs. The check is only for "reconstructing" the stairs
            }
            else workList = PoundAfterSmoothWorld;


            foreach ((int x, int y, int pounds, int type, int style, byte paint, bool echoCoating) point in workList)
            {
                if (doTileTypeCheck && Main.tile[point.x, point.y].TileType != point.type) continue; // in case any other mod overwrote my structure

                WorldGen.KillTile(point.x, point.y);
                WorldGen.PlaceTile(point.x, point.y, point.type, style: point.style);
            }

            foreach ((int x, int y, int pounds, int type, int style, byte paint, bool echoCoating) point in workList)
            {
                if (doTileTypeCheck && Main.tile[point.x, point.y].TileType != point.type) continue; // in case any other mod overwrote my structure

                for (int i = 1; i <= point.pounds; i++)
                {
                    WorldGen.PoundPlatform(point.x, point.y);
                }

                // refresh texture
                WorldGen.ReplaceTile(point.x, point.y, (ushort)point.type, point.style);
                WorldGen.paintTile(point.x, point.y, point.paint);

                //apply echo coating
                if (point.echoCoating) WorldGen.paintCoatTile(point.x, point.y, PaintCoatingID.Echo);
            }
        }


        /// <summary>
        /// Creates an altar made out of platforms in the given space and with the defined deco elements in the global "Deco" field.
        /// <br/>All unpounded platforms get created directly, the pounded ones get packed into data like in "DecorateStaircase".
        /// </summary>
        /// <param name="xAltarStart">The x-coordinate where the altar shall start</param>
        /// <param name="xAltarEnd">The x-coordinate where the altar shall end</param>
        /// <param name="topWidth">The width of the altars flat surface (where items can be placed); 2 = 2 flat tiles and 1 "stairs" tile on each end....so technically 4 Tiles</param>
        public (bool success, Rectangle2P altar) CreateAltar(int xAltarStart, int xAltarEnd, int yAltarFloor, int topWidth)
        {
            Dictionary<(int x, int y), (int pounds, int type, int style, byte paint, bool echoCoat)> stairs = [];// local variant of the global "PoundAfterSmoothWorld" that is easier to alter

            int altarXTiles = (xAltarEnd - xAltarStart) + 1;
            int altarYDiff = (altarXTiles - (topWidth + 2)) / 2;
            if (altarYDiff < 1) return (false, Rectangle2P.Empty); //request cannot be fulfilled

            Rectangle2P altar = new (xAltarStart, (yAltarFloor - altarYDiff), xAltarEnd, yAltarFloor, "dummyString"); // XTiles is the altars width and YTiles is the altars height

            #region background
            int backwallStart = altar.X0 + 1;
            int backwallEnd = altar.X1 - 1;

            for (int j = altar.Y1; j >= altar.Y0; j--)
            {
                for (int i = backwallStart; i <= backwallEnd; i++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.PlaceWall(i, j, Deco[S.AltarWall].id);
                    WorldGen.paintWall(i, j, (byte)Deco[S.AltarWallPaint].id);
                }
                backwallStart++;
                backwallEnd--;
            }
            #endregion


            #region outer altar platforms
            int platStart = altar.X0;
            int platEnd = altar.X1;

            for (int j = altar.Y1; j >= altar.Y0; j--)
            {
                stairs.Add((platStart, j), (1, Deco[S.AltarSteps].id, Deco[S.AltarSteps].style, (byte)Deco[S.AltarStepsPaint].id, false));
                stairs.Add((platEnd, j),   (2, Deco[S.AltarSteps].id, Deco[S.AltarSteps].style, (byte)Deco[S.AltarStepsPaint].id, false));

                if (j == altar.Y1) Func.AddPoundToStairTileFull(stairs, (platEnd, j), -1); // this needs just one pound because there is no "inner platform" in the line below it...pounding is strange

                if (j == altar.Y0)
                {
                    for (int i = (platStart + 1); i <= (platEnd - 1); i++)
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.AltarSteps].id, style: Deco[S.AltarSteps].style); //place the unpounded tiles at once, as they are not affecty by the "Smooth World" worldgen step
                        WorldGen.paintTile(i, j, (byte)Deco[S.AltarStepsPaint].id);
                    }
                }
                
                platStart++;
                platEnd--;
            }
            #endregion


            #region inner (deco) altar platforms
            platStart = altar.X0 + 1;
            platEnd = altar.X1 - 1;
            int actualDecoWidth;
            int altarPlateauWidth = altar.XTiles - 2 * altar.YTiles;
            LineAutomat automat = new((platStart, platEnd), (int)LineAutomat.Dirs.xPlus);

            for (int j = altar.Y1; j > altar.Y0; j--)
            {
                automat.Reset();
                automat = new((platStart, j), (int)LineAutomat.Dirs.xPlus);

                actualDecoWidth = (platEnd - platStart) + 1;
                switch (actualDecoWidth)
                {
                    case 1:
                    case 3:
                    case 5:
                    case 7:
                    case 9:
                    case 11:
                    case 13:
                    case 15: // odd widths not implemented, rooms are always even X-tiled
                        break;
                    case 2: // not needed until now because the top of the altar is 2 XTiles wide
                        break;

                    case 4:
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                        automat.MirrorSteps();
                        break;

                    case 6:
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                        automat.MirrorSteps();
                        break;

                    case 8:
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                        automat.MirrorSteps();
                        break;

                    case 10:
                        if (Chance.Simple())
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.MirrorSteps();
                        }
                        else
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.MirrorSteps();
                        }
                        break;

                    case 12:
                        if (Chance.Simple())
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.MirrorSteps();
                        }
                        else
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.MirrorSteps();
                        }
                        break;

                    case 14:
                        if (Chance.Simple())
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.MirrorSteps();
                        }
                        else
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.MirrorSteps();
                        }
                        break;

                    case 16:
                        if (Chance.Simple())
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.MirrorSteps();
                        }
                        else
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: Deco[S.AltarDeco].id, Deco[S.AltarDeco].style, size: (1, 1), toAnchor: (0, 0), chance: 100, add: []));
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, (1, 1), (0, 0), 0, []));
                            automat.MirrorSteps();
                        }
                        break;


                    default:
                        break;
                }

                automat.Start();

                platStart++;
                platEnd--;
            }
            #endregion


            #region transform local data into global form
            List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> poundList = [];
            (int pounds, int type, int style, byte paint, bool echoCoat) values;

            foreach ((int x, int y) point in stairs.Keys.ToArray())
            {
                values = stairs[point];

                poundList.Add((point.x, point.y, values.pounds, values.type, values.style, values.paint, values.echoCoat));
            }

            PoundAfterSmoothWorld.AddRange(poundList);
            #endregion

            CreateStairsFromData(poundList);

            return (true, altar);
        }


        /// <summary>
        /// Creates a flying "+" shaped background wall, surrounded by spikes. The center is a 2x2 block and the elongation of the "+" is a parameter.
        /// <br/>Hint: by now only for even-Y-Tiled-rooms
        /// </summary>
        /// <param name="xTopLeftCenter">The left x-coordinate of the 2x2 tiles center block</param>
        /// <param name="yTopLeftCenter">The top y-coordinate of the 2x2 tiles center block</param>
        /// <param name="plusLength">The tiles that the "+" shaped background wall shall strech away from the 2x2 center block to each side.</param>
        /// <param name="livingFlames">If the "+" shall be filled with and the upper side of the spikes shall be covered in living flames</param>
        public (bool success, Rectangle2P spikeRect) CreateFlamingPlus(int xTopLeftCenter, int yTopLeftCenter, int plusLength, bool livingFlames = false)
        {
            if (plusLength < 0) return (false, Rectangle2P.Empty); // invalid parameter

            int plusWidth = 2; // the width of the two "plus beams" -> meaning the vertical beam is 2 tiles wide and the horizontal one is 2 tiles high

            Rectangle2P coreRect = new(xTopLeftCenter, yTopLeftCenter, plusWidth, plusWidth);

            Rectangle2P plusRect = new(coreRect.X0 - plusLength, coreRect.Y0 - plusLength, coreRect.X1 + plusLength, coreRect.Y1 + plusLength, "dummyString");
            
            Rectangle2P spikeRect = new(plusRect.X0 - 2,         plusRect.Y0 - 2,          plusRect.X1 + 2,          plusRect.Y1 + 2, "dummyString");

            #region create the +
            for (int i = plusRect.X0; i <= plusRect.X1; i++)
            {
                for (int j = coreRect.Y0; j <= coreRect.Y1; j++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.PlaceWall(i, j , Deco[S.WindowWall].id);

                    if (livingFlames) WorldGen.PlaceTile(i, j, TileID.LivingFire);
                }
            }

            for (int j = plusRect.Y0; j <= plusRect.Y1; j++)
            {
                for (int i = coreRect.X0; i <= coreRect.X1; i++)
                {
                    if (!coreRect.Contains(i, j))
                    {
                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, Deco[S.WindowWall].id);

                        if (livingFlames) WorldGen.PlaceTile(i, j, TileID.LivingFire);
                    }
                }
            }
            #endregion

            #region add the spikes

            // do the outmost spikes first
            for (int i = spikeRect.X0; i <= spikeRect.X1; i++)
            {
                for (int j = coreRect.Y0; j <= coreRect.Y1; j++)
                {
                    if (!plusRect.Contains(i, j))
                    {
                        WorldGen.PlaceTile(i, j, TileID.Spikes);
                    }
                }
            }
            for (int j = spikeRect.Y0; j <= spikeRect.Y1; j++)
            {
                for (int i = coreRect.X0; i <= coreRect.X1; i++)
                {
                    if (!plusRect.Contains(i, j))
                    {
                        WorldGen.PlaceTile(i, j, TileID.Spikes);
                    }
                }
            }

            // do the surrounding spikes next
            for (int i = plusRect.X0 - 1; i <= plusRect.X1 + 1; i++)
            {
                for (int j = coreRect.Y0 - 1; j <= coreRect.Y1 + 1; j++)
                {
                    if (i < coreRect.X0 || i > coreRect.X1)
                    {
                        WorldGen.PlaceTile(i, j, TileID.Spikes);
                    }
                }
            }
            for (int j = plusRect.Y0 - 1; j <= plusRect.Y1 + 1; j++)
            {
                for (int i = coreRect.X0 - 1; i <= coreRect.X1 + 1; i++)
                {
                    if (j < coreRect.Y0 || j > coreRect.Y1)
                    {
                        WorldGen.PlaceTile(i, j, TileID.Spikes);
                    }
                }
            }
            #endregion

            #region living flames on top of the spikes
            if (livingFlames)
            {
                int xAct, yAct;

                // flames on the left side of the "+"
                xAct = spikeRect.X0;
                yAct = coreRect.Y0 - 1;
                while (xAct <= coreRect.XCenter)
                {
                    WorldGen.PlaceTile(xAct, yAct, TileID.LivingFire);
                    if (Main.tile[xAct + 1, yAct].TileType == TileID.Spikes) yAct--;
                    else xAct++;
                }

                // flames on the right side of the "+"
                xAct = spikeRect.X1;
                yAct = coreRect.Y0 - 1;
                while (xAct >= coreRect.XCenter + 1)
                {
                    WorldGen.PlaceTile(xAct, yAct, TileID.LivingFire);
                    if (Main.tile[xAct - 1, yAct].TileType == TileID.Spikes) yAct--;
                    else xAct--;
                }
            }
            #endregion

            return (true, spikeRect);
        }


        /// <summary>
        /// Creates a giant sword decoration in the middle of the given space, leaves 1 tile space on left / right and none on top / bottom
        /// <br/>Hint: by now only for even-X-Tiled-rooms
        /// </summary>
        /// <param name="area">The area where the giant sword shall be placed -> leaves 1 tile space on left / right and none on top / bottom</param>
        /// <param name="handle">Dimensions of the swords handle. Minimum is (2, 2)</param>
        /// <param name="bladeXTiles">Width of the swords blade. Minimum is 2</param>
        /// <param name="smallPommel">Reduce the pommels height by 1 tile (take out the topmost tip)</param>
        /// <param name="smallCrossGuard">Reduce the crossguards height by 1 tile (move the bottom line 1 tile up)</param>
        /// <param name="actuated">Make all bricks of the sword actuated so it can be walked through</param>
        /// <returns><br/>Tupel item1 <b>success</b>: True if the giant sword pommel was placed successfully
        ///          <br/>Tupel item2 <b>pommelR</b>: The actual area covered by the giant swords pommel</returns>
        ///          <br/>Tupel item3 <b>handleR</b>: The actual area covered by the giant swords handle</returns>
        ///          <br/>Tupel item4 <b>crossGuardR</b>: The actual area covered by the giant swords whole crossguard</returns>
        ///          <br/>Tupel item5 <b>crossGuardCenterR</b>: The actual area covered by the center of the giant swords crossguard</returns>
        ///          <br/>Tupel item6 <b>bladeR</b>: The actual area covered by the center of the giant swords blade</returns>
        ///          <br/>Tupel item7 <b>checkPoints</b>: Return some remarkable points of the structure with which later can be checked if later worlgen steps overwrote the structure</returns>
        public (bool success, Rectangle2P pommelR, Rectangle2P handleR, Rectangle2P crossGuardR, Rectangle2P crossGuardCenterR, Rectangle2P bladeR, List<(int x, int y, int tileID)> checkPoints) CreateGiantSword(
            Rectangle2P area, (int xTiles, int yTiles) handle, int bladeXTiles, bool smallPommel = false, bool smallCrossGuard = false, bool actuated = false)
        {
            Rectangle2P empty = Rectangle2P.Empty;
            List<(int x, int y, int tileID)> checkPoints = [];

            int minYTiles = 11; // 14 --> 5 (pommel) + 2 (handle) + 4 (crossguard) + 0 (blade)
            if (smallPommel) minYTiles--;
            if (smallCrossGuard) minYTiles--;

            if (area.IsEmpty() || area.XTiles < 14 || area.YTiles < 0) return (false, empty, empty, empty, empty, empty, [] ); //XTiles < 14 --> crossguard minimal design
            if (handle.xTiles < 2 || handle.yTiles < 2 || bladeXTiles < 2) return (false, empty, empty, empty, empty, empty, []);

            int x, y, x1, x2;
            (int x, int y) lastPartEnd; // the connection point of the pommel to the handle, to the crossguard, to the blade (always the bottom left corner of the previous structure)



            #region sword pommel

            Rectangle2P pommelArea;
            
            if (area.IsEvenX())
            {
                pommelArea = new(area.XCenter - 2, area.Y0, 6, 4);
                if (smallPommel) pommelArea.Move(0, -1);
            }
                
            else pommelArea = new(area.XCenter - 3, area.Y0, 7, 6); // space reserved for GemLock instead of ItemFrame, not tested!



            #region place ItemFrame

            x = pommelArea.XCenter;
            y = pommelArea.Y0 + 2;

            Func.ReplaceWallArea(new(x, y, 2, 2), Deco[S.SwordEnergyFlowWall].id);

            bool placed = Func.PlaceItemFrame(x, y, item: Deco[S.SwordHandleGemItem].id);

            WorldGen.paintCoatTile(x    , y    , PaintCoatingID.Echo);
            WorldGen.paintCoatTile(x    , y + 1, PaintCoatingID.Echo);
            WorldGen.paintCoatTile(x + 1, y    , PaintCoatingID.Echo);
            WorldGen.paintCoatTile(x + 1, y + 1, PaintCoatingID.Echo);

            #endregion


            #region place full bricks

            x = pommelArea.XCenter; // start at the pommel gems upper left corner
            y = pommelArea.Y0 + 2;

            // left
            Func.PlaceSingleTile(x - 1, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
            Func.PlaceSingleTile(x - 1, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);

            // top
            Func.PlaceSingleTile(x    , y - 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
            Func.PlaceSingleTile(x + 1, y - 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);

            // right
            Func.PlaceSingleTile(x + 2, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
            Func.PlaceSingleTile(x + 2, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);

            // bottom (last line before sword handle)
            for (int i = x - 1; i <= x + 2; i++)
            {
                Func.PlaceSingleTile(i, y + 2, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
            }

            checkPoints.Add((x    , y - 1, Deco[S.SwordBrick].id));
            checkPoints.Add((x + 2, y + 2, Deco[S.SwordBrick].id));
            #endregion


            #region place and shape bricks for pommel tips

            x = pommelArea.XCenter; // start at the pommel gems upper left corner
            y = pommelArea.Y0 + 2;

            // left side
            Func.PlaceSingleTile(x - 2, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);

            Func.PlaceSingleTile(x - 2, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);

            // top
            if (!smallPommel)
            {
                Func.PlaceSingleTile(x    , y - 2, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);

                Func.PlaceSingleTile(x + 1, y - 2, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);
            }

            // right side
            Func.PlaceSingleTile(x + 3, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);

            Func.PlaceSingleTile(x + 3, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.BotRight, actuated: actuated);

            // top left corner
            Func.PlaceSingleTile(x - 1, y - 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);


            // top right corner
            Func.PlaceSingleTile(x + 2, y - 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);

            #endregion


            lastPartEnd = (x - 1, y + 2);

            #endregion



            #region sword handle

            Rectangle2P handleArea = new(lastPartEnd.x, lastPartEnd.y + 1, handle.xTiles, handle.yTiles);
            bool shortHandle = handle.yTiles <= 2;

            for (int j = handleArea.Y0; j <= handleArea.Y1; j++)
            {
                if (j == handleArea.Y1 && !shortHandle) continue;

                for (int i = handleArea.X0; i <= handleArea.X1; i++)
                {
                    if (i == handleArea.X0 || i == handleArea.X1)
                    {
                        Func.PlaceSingleTile(i, j, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
                    }
                    else
                    {
                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, Deco[S.SwordEnergyFlowWall].id);
                    }
                }
            }
            checkPoints.Add((handleArea.X0, handleArea.Y0, Deco[S.SwordBrick].id));

            // last complete line before sword crossguard
            if (!shortHandle)
            {
                y = handleArea.Y1;
                for (int i = handleArea.X0; i <= handleArea.X1; i++)
                {
                    Func.PlaceSingleTile(i, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordHandlePaint].id, actuated: actuated);
                }
            }

            lastPartEnd = (handleArea.X0, handleArea.Y1);

            #endregion



            #region sword crossguard

            int xMin = area.X0 + 1; // leave 1 space to the sides
            int xMax = area.X1 - 1; // leave 1 space to the sides
            int yMin = lastPartEnd.y - 1; // an 18 xTiles giant sword would have the crossguard stand out 1 yTile more than the connection point
            if (area.XTiles - 2 <= 16) yMin = lastPartEnd.y; // 16 xTiles and less giant swords would have no outstanding crossguard
            int yMax = lastPartEnd.y + 4;
            if (smallCrossGuard) yMax--;

            Rectangle2P crossguardArea = new(xMin, yMin, xMax, yMax, "dummy");
            Rectangle2P crossguardCenterArea = new(lastPartEnd.x - 1, lastPartEnd.y + 1, lastPartEnd.x + 4, lastPartEnd.y + 4, "dummy");

            #region center part

            // going from top to bottom
            x1 = crossguardCenterArea.X0;
            x2 = crossguardCenterArea.X1;
            y = crossguardCenterArea.Y0;

            Func.PlaceSingleTile(x1    , y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
            Func.PlaceSingleTile(x2    , y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);

            Func.PlaceSingleTile(x1 + 1, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
            Func.PlaceSingleTile(x2 - 1, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

            if (shortHandle)
            {
                Func.PlaceSingleTile(x1 + 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);
                Func.PlaceSingleTile(x2 - 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
            }

            y++;
            Func.PlaceSingleTile(x1    , y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
            Func.PlaceSingleTile(x2    , y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

            Func.PlaceSingleTile(x1 + 1, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotRight, actuated: actuated);
            Func.PlaceSingleTile(x2 - 1, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);

            checkPoints.Add((x2, y, Deco[S.SwordBrick].id));

            if (smallCrossGuard)
            {
                Func.PlaceSingleTile(x1 + 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
                Func.PlaceSingleTile(x2 - 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);
            }

            if (!smallCrossGuard)
            {
                y++;
                Func.PlaceSingleTile(x1, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                Func.PlaceSingleTile(x2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

                Func.PlaceSingleTile(x1 + 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
                Func.PlaceSingleTile(x2 - 2, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);
            }

            y++;
            for (int i = crossguardCenterArea.X0; i <= crossguardCenterArea.X1; i++)
            {
                Func.PlaceSingleTile(i, y, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
            }

            // put energy flow backwall
            Func.ReplaceWallArea(new(crossguardCenterArea.X0 + 1, crossguardCenterArea.Y0, crossguardCenterArea.X1 - 1, crossguardCenterArea.Y1, "dummy"), Deco[S.SwordEnergyFlowWall].id);

            #endregion


            #region left part

            x = crossguardCenterArea.X0 - 1;
            y = crossguardCenterArea.Y0 + 1;

            Func.PlaceSingleTile(x, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
            Func.PlaceSingleTile(x, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

            checkPoints.Add((x, y, Deco[S.SwordBrick].id));

            if (crossguardArea.XTiles <= 12) // special case: no 3 yTiles part of the crossguard
            {
                Func.PlaceSingleTile(x - 1, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                Func.PlaceSingleTile(x - 1, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

                Func.PlaceSingleTile(x - 2, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
                Func.PlaceSingleTile(x - 2, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);
            }
            else
            {
                for (int i = x - 1; i >= crossguardArea.X0 + 1; i--) // the area where there are always 3 or 4 yTiles
                {
                    if (i > crossguardArea.X0 + 1) y--;

                    Func.PlaceSingleTile(i, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                    Func.PlaceSingleTile(i, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                    Func.PlaceSingleTile(i, y + 2, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

                    if (i == x - 1 ) Func.SlopeTile(i, y, (int)Func.SlopeVal.UpRight);
                    else if (i == crossguardArea.X0 + 1) Func.SlopeTile(i, y + 2, (int)Func.SlopeVal.BotLeft);
                    else
                    {
                        Func.SlopeTile(i, y, (int)Func.SlopeVal.UpRight);
                        Func.PlaceSingleTile(i, y + 3, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);
                    }
                }

                x = crossguardArea.X0;
                Func.PlaceSingleTile(x, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
                Func.PlaceSingleTile(x, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);
            }

            #endregion


            #region right part

            x = crossguardCenterArea.X1 + 1;
            y = crossguardCenterArea.Y0 + 1;

            Func.PlaceSingleTile(x, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
            Func.PlaceSingleTile(x, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

            checkPoints.Add((x, y, Deco[S.SwordBrick].id));

            if (crossguardArea.XTiles <= 12) // special case: no 3 yTiles part of the crossguard
            {
                Func.PlaceSingleTile(x + 1, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                Func.PlaceSingleTile(x + 1, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

                Func.PlaceSingleTile(x + 2, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpLeft, actuated: actuated);
                Func.PlaceSingleTile(x + 2, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotLeft, actuated: actuated);
            }
            else
            {
                for (int i = x + 1; i <= crossguardArea.X1 - 1; i++) // the area where there are always 3 or 4 yTiles
                {
                    if (i < crossguardArea.X1 - 1) y--;

                    Func.PlaceSingleTile(i, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                    Func.PlaceSingleTile(i, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);
                    Func.PlaceSingleTile(i, y + 2, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, actuated: actuated);

                    if (i == x + 1) Func.SlopeTile(i, y, (int)Func.SlopeVal.UpLeft);
                    else if (i == crossguardArea.X1 - 1) Func.SlopeTile(i, y + 2, (int)Func.SlopeVal.BotRight);
                    else
                    {
                        Func.SlopeTile(i, y, (int)Func.SlopeVal.UpLeft);
                        Func.PlaceSingleTile(i, y + 3, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotRight, actuated: actuated);
                    }
                }

                x = crossguardArea.X1;
                Func.PlaceSingleTile(x, y    , Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.UpRight, actuated: actuated);
                Func.PlaceSingleTile(x, y + 1, Deco[S.SwordBrick].id, paint: Deco[S.SwordCrossGPaint].id, slope: (int)Func.SlopeVal.BotRight, actuated: actuated);
            }
            #endregion

            lastPartEnd = (crossguardCenterArea.X0, crossguardCenterArea.Y1);

            #endregion



            #region sword blade

            Rectangle2P bladeArea = new(lastPartEnd.x, lastPartEnd.y, crossguardCenterArea.X1, area.Y1, "dummy");
            bool bladeEnergyFlowWallExist = bladeArea.XTiles > 2;
            bool bladeCenterWallExist = bladeArea.XTiles > 4;

            for (int j = bladeArea.Y0; j <= bladeArea.Y1; j++)
            {
                for (int i = bladeArea.X0; i <= bladeArea.X1; i++)
                {
                    if (i == bladeArea.X0 || i == bladeArea.X1)
                    {
                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, Deco[S.SwordBladeEdgeWall].id);
                    }
                    if (bladeEnergyFlowWallExist)
                    {
                        if (i == bladeArea.X0 + 1 || i == bladeArea.X1 - 1)
                        {
                            WorldGen.KillWall(i, j);
                            WorldGen.PlaceWall(i, j, Deco[S.SwordEnergyFlowWall].id);
                        }
                    }
                    if (bladeCenterWallExist)
                    {
                        if (i > bladeArea.X0 + 1 && i < bladeArea.X1 - 1)
                        {
                            WorldGen.KillWall(i, j);
                            WorldGen.PlaceWall(i, j, Deco[S.SwordBladeWall].id);
                        }
                    }
                }
            }

            // hide wall on the sides of the blade so the cutting edge wall of the blade stands out more
            for (int j = bladeArea.Y0 - 1; j <= bladeArea.Y1 + 1; j++)
            {
                WorldGen.paintCoatWall(bladeArea.X0 - 1, j, PaintCoatingID.Echo);
                WorldGen.paintCoatWall(bladeArea.X1 + 1, j, PaintCoatingID.Echo);
            }

            #endregion


            return (true, pommelArea, handleArea, crossguardArea, crossguardCenterArea, bladeArea, checkPoints);
        }


        //TODO: put and fill chest
        public void FillChest(Chest chest, int style)
        {
            List<int> mainItem = [];
            mainItem.Add(ItemID.Vilethorn);
            mainItem.Add(ItemID.Musket);
            mainItem.Add(ItemID.BandofStarpower);
            mainItem.Add(ItemID.BallOHurt);
            mainItem.Add(ItemID.ShadowOrb);

            List<int> potionItem = [];
            if (style == 1)
            {
                potionItem.Add(ItemID.RagePotion);
                potionItem.Add(ItemID.WrathPotion);
                potionItem.Add(ItemID.LifeforcePotion);
                potionItem.Add(ItemID.SummoningPotion);
            }
            else
            {
                potionItem.Add(ItemID.SwiftnessPotion);
                potionItem.Add(ItemID.IronskinPotion);
                potionItem.Add(ItemID.RegenerationPotion);
                potionItem.Add(ItemID.SummoningPotion);
            }

            List<int> lightItem =
            [
                !WorldGen.crimson ? ItemID.CrimsonTorch : ItemID.CorruptTorch,
                ItemID.Glowstick,
                ItemID.FairyGlowstick,
                ItemID.SpelunkerGlowstick
            ];

            List<int> materialItem = [];
            if (WorldGen.crimson)
            {
                materialItem.Add(ItemID.TissueSample);
                materialItem.Add(ItemID.CrimtaneBar);
                materialItem.Add(ItemID.CrimsonSeeds);
                materialItem.Add(ItemID.Vertebrae);
            }
            else
            {
                materialItem.Add(ItemID.ShadowScale);
                materialItem.Add(ItemID.DemoniteBar);
                materialItem.Add(ItemID.CorruptSeeds);
                materialItem.Add(ItemID.RottenChunk);
            }


            int nextItem = 0; //init

            chest.item[nextItem].SetDefaults(mainItem[WorldGen.genRand.Next(mainItem.Count)]);
            chest.item[nextItem].stack = 1;
            nextItem++;

            chest.item[nextItem].SetDefaults(potionItem[WorldGen.genRand.Next(potionItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 4);
            nextItem++;

            chest.item[nextItem].SetDefaults(lightItem[WorldGen.genRand.Next(lightItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(6, 13);
            nextItem++;

            chest.item[nextItem].SetDefaults(materialItem[WorldGen.genRand.Next(materialItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(5, 10);
            nextItem++;

            chest.item[nextItem].SetDefaults(ItemID.GoldCoin);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 3);
            if (style == 4) chest.item[nextItem].stack = WorldGen.genRand.Next(6, 20);
        }





        /// <summary>
        /// Tries to place a painting in the given area. It tries to place paintings from tall to flat (6x4 -> 3x3 -> 2x3 -> 3x2)
        /// <br/>
        /// <br/> ATTENTION: does not pre-check if the final placement position is empty. Best make sure that the whole area is free.
        /// </summary>
        /// <param name="area">The area where the painting can be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeMode">The placement method: 0 = centered in x and y, 1 = random x and centered y, 2 = centered x and random y, 3 = random x and y</param>
        /// <param name="allowType">Allow types of paintings: (binary selection) 0= no painting, 1=3x2, 2=2x3, 4=3x3, 8=6x4, 15=all types</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        /// <param name="centerErrorX">If x-placeMode is "centered" and the painting placement results in an impossible symmetrical centering do: -1 = force left position, 0 = random, 1 = force right position, -88 = abort function</param>
        /// <param name="centerErrorX">If y-placeMode is "centered" and the painting placement results in an impossible symmetrical centering do: -1 = force upper position, 0 = random, 1 = force lower position, -88 = abort function</param>

        /// <returns><br/>Tupel item1 <b>success</b>: true if placement was successful
        ///          <br/>Tupel item2 <b>paintingArea</b>: if success = true, the covered area of the painting, else Rectangle2P.Empty
        ///          <br/>Tupel item3 <b>paintingType</b>: contains the placed / attempted to place painting type (1 = 3x2, 2 = 2x3, 4 = 3x3, 8 = 6x4), else 0
        ///          <br/>Tupel item4 <b>failReason</b>: if success = false, contains the reason for failing 
        ///          <br/> --> (1 = WorldGen.PlaceTile failed(), 2 = aborted because of centerErrorX, 3 = aborted because of centerErrorY, 4 = every single painting Chance roll failed), else 0</returns>
        ///          
        public (bool success, Rectangle2P paintingArea, int paintingType, int failReason) PlacePainting(Rectangle2P area, int style, int placeMode = 0, byte allowType = 15, bool placeWall = false, int centerErrorX = 0, int centerErrorY = -1)
        {
            bool allow3x2 = ((allowType & 1) != 0) && (area.XTiles >= 3) && (area.YTiles >= 2);
            bool allow2x3 = ((allowType & 2) != 0) && (area.XTiles >= 2) && (area.YTiles >= 3);
            bool allow3x3 = ((allowType & 4) != 0) && (area.XTiles >= 3) && (area.YTiles >= 3);
            bool allow6x4 = ((allowType & 8) != 0) && (area.XTiles >= 6) && (area.YTiles >= 4);

            bool centX = (placeMode == 0) || (placeMode == 2); // painting centered in x direction
            bool centY = (placeMode == 0) || (placeMode == 1); // painting centered in y direction

            bool roomEvenX = (area.XTiles % 2) == 0;
            bool roomEvenY = (area.YTiles % 2) == 0;

            int randAddX, randAddY;// 3 XTiles cannot be put centered symmetrically in an even XTiles room, and 2 XTiles cannot in an uneven XTiles room,
                                   // so these values are for alternating betweend the two "out-center" positions

            // prepare random positioning values
            if (centerErrorX == -1) randAddX = 0; // force left position
            else if (centerErrorX == 1) randAddX = 1; // force right position
            else randAddX = WorldGen.genRand.Next(2);
            bool abortCenterX = centerErrorX == -88;

            if (centerErrorY == -1) randAddY = 0; // force upper position
            else if (centerErrorY == 1) randAddY = 1; // force lower position
            else randAddY = WorldGen.genRand.Next(2);
            bool abortCenterY = centerErrorY == -88;

            // prepare local output variables
            bool success = false;
            Rectangle2P paintingArea = Rectangle2P.Empty;
            int paintingType = 0;
            int failReason = 0;

            //painting
            int x = area.X0, y = area.Y0; // init

            if (allow6x4 && Chance.Simple())
            {
                paintingType = 8;

                if (!roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                if (!roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                if (centX)
                {
                    x = area.XCenter - 2; // even room
                    if (!roomEvenX) x -= (1 - randAddX);
                }
                else x = area.X0 + WorldGen.genRand.Next((area.XTiles - 6) + 1);

                if (centY)
                {
                    y = area.YCenter - 1; // even room
                    if (!roomEvenY) y -= (1 - randAddY);
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 4) + 1);

                paintingArea = new(x, y, 6, 4);
                success = Place6x4PaintingByStyle(paintingArea, style, placeWall);

                if (!success) failReason = 1;
            }

            else if (allow3x3 && Chance.Simple())
            {
                paintingType = 4;

                if (roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                if (roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                if (centX)
                {
                    x = area.XCenter - 1; // uneven room
                    if (roomEvenX) x += randAddX;
                }
                else x = area.X0 + WorldGen.genRand.Next((area.XTiles - 3) + 1);

                if (centY)
                {
                    y = area.YCenter - 1; // uneven room
                    if (roomEvenY) y += randAddY;
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 3) + 1);

                paintingArea = new(x, y, 3, 3);
                success = Place3x3PaintingByStyle(paintingArea, style, placeWall);

                if (!success) failReason = 1;
            }

            else if (allow2x3 && Chance.Simple())
            {
                paintingType = 2;

                if (!roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                if (roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                if (centX)
                {
                    x = area.XCenter; // even room
                    if (!roomEvenX) x -= (1 - randAddX);
                }
                else x = area.X0 + WorldGen.genRand.Next((area.XTiles - 2) + 1);

                if (centY)
                {
                    y = area.YCenter - 1; // uneven room
                    if (roomEvenY) y += randAddY;
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 3) + 1);

                paintingArea = new(x, y, 2, 3);
                success = Place2x3PaintingByStyle(paintingArea, style, placeWall);

                if (!success) failReason = 1;
            }

            else if (allow3x2 && Chance.Simple())
            {
                paintingType = 1;

                if (roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                if (!roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                if (centX)
                {
                    x = area.XCenter - 1; // uneven room
                    if (roomEvenX) x += randAddX;
                }
                else x = area.X0 + WorldGen.genRand.Next((area.XTiles - 3) + 1);

                if (centY)
                {
                    y = area.YCenter; // even room
                    if (!roomEvenY) y -= (1 - randAddY);
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 2) + 1);

                paintingArea = new(x, y, 3, 2);
                success = Place3x2PaintingByStyle(paintingArea, style, placeWall);

                if (!success) failReason = 1;
            }

            if (!success && paintingType == 0) failReason = 4;

            return (success, paintingArea, paintingType, failReason);
        }

        /// <summary>
        /// Places a random 6x4 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 6x4 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the chastised church</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place6x4PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall].id);

            List<int> paintings = [];
            if (style == S.StyleHellstone)
            {
                paintings.Add(0); // The Eye Sees the End
                paintings.Add(3); // The Screamer
                paintings.Add(8); // The Destroyer
                paintings.Add(13); // Facing the Cerebral Mastermind
                paintings.Add(14); // Lake of Fire
                paintings.Add(21); // Morbid Curiosity
                paintings.Add(52); // Ocular Resonance
                paintings.Add(53); // Wings of Evil
                paintings.Add(56); // Dread of the Red Sea
            }
            else if (style == S.StyleTitanstone)
            {
                paintings.Add(0); // The Eye Sees the End
                paintings.Add(8); // The Destroyer
                paintings.Add(14); // Lake of Fire
                paintings.Add(21); // Morbid Curiosity
                paintings.Add(36); // Not a Kid, nor a Squid
                paintings.Add(44); // Graveyard (Painting)
                paintings.Add(50); // Remnants of Devotion
                paintings.Add(55); // Eyezorhead
                paintings.Add(56); // Dread of the Red Sea

            }
            else if (style == S.StyleBlueBrick)
            {
                paintings.Add(17); // Jacking Skeletron
                paintings.Add(18); // Bitter Harvest
                paintings.Add(19); // Blood Moon Countess
                paintings.Add(21); // Morbid Curiosity
                paintings.Add(29); // The Truth Is Up There
                paintings.Add(44); // Graveyard (Painting)
                paintings.Add(50); // Remnants of Devotion
                paintings.Add(52); // Ocular Resonance
                paintings.Add(55); // Eyezorhead
                paintings.Add(59); // Moonman & Company
            }
            else
            {
                paintings.Add(23); // Leopard Skin...should never occur or I called the method wrong...so just to be sure
            }

            bool success = WorldGen.PlaceTile(area.X0 + 2, area.Y0 + 2, TileID.Painting6X4, style: paintings[WorldGen.genRand.Next(paintings.Count)]);

            return success;
        }

        /// <summary>
        /// Places a random 3x3 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 3x3 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the chastised church</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place3x3PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall].id);

            List<int> paintings = [];
            if (style == S.StyleHellstone)
            {
                paintings.Add(22); // Guide Picasso
                paintings.Add(24); // Father of Someone
                paintings.Add(26); // Discover
                paintings.Add(76); // Outcast
                paintings.Add(77); // Fairy Guides
                paintings.Add(79); // Morning Hunt
                paintings.Add(82); // Cat Sword
            }
            else if (style == S.StyleTitanstone)
            {
                paintings.Add(13); // The Hanged Man
                paintings.Add(19); // The Cursed Man
                paintings.Add(20); // Sunflowers
                paintings.Add(22); // Guide Picasso
                paintings.Add(23); // The Guardian's Gaze
                paintings.Add(25); // Nurse Lisa
                paintings.Add(28); // Old Miner
                paintings.Add(33); // The Merchant
                paintings.Add(34); // Crowno Devours His Lunch
                paintings.Add(70); // Nevermore
                paintings.Add(78); // A Horrible Night for Alchemy
            }
            else if (style == S.StyleBlueBrick)
            {
                paintings.Add(70); // Nevermore
                paintings.Add(71); // Reborn
                paintings.Add(68); // Snakes, I Hate Snakes
                paintings.Add(65); // Burning Spirit
                paintings.Add(35); // Rare Enchantment
                paintings.Add(34); // Crowno Devours His Lunch
                paintings.Add(12); // Blood Moon Rising
                paintings.Add(13); // The Hanged Man
                paintings.Add(15); // Bone Warp
                paintings.Add(18); // Skellington J Skellingsworth
                paintings.Add(23); // The Guardian's Gaze
                paintings.Add(30); // Imp Face
            }
            else
            {
                paintings.Add(48); // Compass Rose...should never occur or I called the method wrong...so just to be sure
            }

            bool success = WorldGen.PlaceTile(area.X0 + 1, area.Y0 + 1, TileID.Painting3X3, style: paintings[WorldGen.genRand.Next(paintings.Count)]);

            return success;
        }

        /// <summary>
        /// Places a random 2x3 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 2x3 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the chastised church</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place2x3PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall].id);

            List<int> paintings = [];
            if (style == S.StyleHellstone)
            {
                paintings.Add(0); // Waldo
                paintings.Add(10); // Ghost Manifestation
                paintings.Add(15); // Strange Growth #1
                paintings.Add(19); // Happy Little Tree
                paintings.Add(26); // Love is in the Trash Slot
            }
            else if (style == S.StyleTitanstone)
            {
                paintings.Add(1); // Darkness
                paintings.Add(2); // Dark Soul Reaper
                paintings.Add(3); // Land
                paintings.Add(4); // Trapped Ghost
                paintings.Add(6); // Glorious Night
                paintings.Add(7); // Bandage Boy
                paintings.Add(17); // Strange Growth #3
                paintings.Add(18); // Strange Growth #4
                paintings.Add(21); // Secrets
                paintings.Add(22); // Thunderbolt
                paintings.Add(24); // The Werewolf
            }
            else if (style == S.StyleBlueBrick)
            {
                paintings.Add(4); // Trapped Ghost
                paintings.Add(11); // Wicked Undead
                paintings.Add(12); // Bloody Goblet
                paintings.Add(20); // Strange Dead Fellows
                paintings.Add(22); // Thunderbolt
                paintings.Add(24); // The Werewolf
            }
            else
            {
                paintings.Add(18); // Strange Growth #4...should never occur or I called the method wrong...so just to be sure
            }

            bool success = WorldGen.PlaceTile(area.X0, area.Y0 + 1, TileID.Painting2X3, style: paintings[WorldGen.genRand.Next(paintings.Count)]);

            return success;
        }

        /// <summary>
        /// Places a random 3x2 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 3x2 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the chastised church</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place3x2PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall].id);

            List<int> paintings = [];
            if (style == S.StyleHellstone)
            {
                paintings.Add(6); // Place Above the Clouds
                paintings.Add(8); // Cold Waters in the White Land
                paintings.Add(15); // Sky Guardian
                paintings.Add(32); // Viking Voyage
                paintings.Add(35); // Forest Troll
            }
            else if (style == S.StyleTitanstone)
            {
                paintings.Add(1); // Finding Gold
                paintings.Add(5); // Through the Window
                paintings.Add(7); // Do Not Step on the Grass
                paintings.Add(11); // Daylight
                paintings.Add(20); // Still Life
                paintings.Add(33); // Bifrost
            }
            else if (style == S.StyleBlueBrick)
            {
                paintings.Add(0); // Demon's Eye
                paintings.Add(1); // Finding Gold
                paintings.Add(9); // Lightless Chasms
                paintings.Add(20); // Still Life
                paintings.Add(36); // Aurora Borealis
            }
            else
            {
                paintings.Add(4); // Underground Reward...should never occur or I called the method wrong...so just to be sure
            }

            bool success = WorldGen.PlaceTile(area.X0 + 1, area.Y0, TileID.Painting3X2, style: paintings[WorldGen.genRand.Next(paintings.Count)]);

            return success;
        }


    }

    internal class S //Style
    {
        public const String StyleSave = "Style";
        public const String SubStyleSave = "SubStyleSave";
        public const String Brick = "Brick";
        public const String BrickPaint = "BrickPaint";
        public const String RoofBrick = "RoofBrick";
        public const String RoofBrickPaint = "RoofBrickPaint";
        public const String Floor = "Floor";
        public const String FloorPaint = "FloorPaint";
        public const String BelowRoomFloor = "BelowRoomFloor";
        public const String BelowRoomFloorPaint = "BelowRoomFloorPaint";
        public const String EvilTile = "EvilTile";
        public const String BackWall = "BackWall";
        public const String BackWallPaint = "BackWallPaint";
        public const String CrookedWall = "CrookedWall";
        public const String WindowWall = "WindowWall";
        public const String WindowPaint = "WindowPaint";
        public const String DoorWall = "DoorWall";
        public const String DoorPlat = "DoorPlatform";
        public const String DoorPlatPaint = "DoorPlatformPaint";
        public const String Door = "Door";
        public const String DoorPaint = "DoorPaint";
        public const String Chest = "Chest";
        public const String Campfire = "Campfire";
        public const String CampfirePaint = "CampfirePaint";
        public const String Table = "Table";
        public const String Workbench = "Workbench";
        public const String Chair = "Chair";
        public const String MainPainting = "MainPainting";
        public const String Chandelier = "Chandelier";
        public const String Candelabra = "Candelabra";
        public const String Candle = "Candle";
        public const String Lamp = "Lamp";
        public const String Torch = "Torch";
        public const String Lantern = "Lantern";
        public const String Banner = "Banner";
        public const String DecoPlat = "DecoPlatform";
        public const String StylePaint = "StylePaint";
        public const String HangingPot = "HangingPot";
        public const String Bookcase = "Bookcase";
        public const String Sofa = "Sofa";
        public const String Clock = "Clock";
        public const String PaintingWallpaper = "PaintingWallpaper";
        public const String Dresser = "Dresser";
        public const String Piano = "Piano";
        public const String Column = "Column";
        public const String ColumnPaint = "ColumnPaint";


        // altar
        public const String MiddleWall = "MiddleWall";
        public const String MiddleWallPaint = "MiddleWallPaint";
        public const String AltarSteps = "AltarSteps";
        public const String AltarStepsPaint = "AltarStepsPaint";
        public const String AltarDeco = "AltarDeco";
        public const String AltarDecoPaint = "AltarDecoPaint";
        public const String AltarWall = "AltarWall";
        public const String AltarWallPaint = "AltarWallPaint";

        public const String RunicWallPaint = "RunicWallPaint";

        // giant sword
        public const String SwordBrick = "SwordBrick";
        public const String SwordHandleGemItem = "SwordHandleGemItem";
        public const String SwordHandlePaint = "SwordHandlePaint";
        public const String SwordCrossGPaint = "SwordCrossGPaint";
        public const String SwordEnergyFlowWall = "SwordEnergyFlowWall";
        public const String SwordBladeEdgeWall = "SwordBladeEdgeWall";
        public const String SwordBladeWall = "SwordBladeWall";

        // tree
        public const String TreePaint = "TreePaint";
        public const String BannerHangPlat = "BannerHangPlat";

        // inner temple

        public const String TempleBrick = "TempleBrick";
        public const String TempleBrickBottomPaint = "TempleBrickBottomPaint";
        public const String TempleBrickAltarPaint = "TempleBrickAltarPaint";
        public const String TempleStreamerPaint = "TempleStreamerPaint";
        public const String TempleSteps = "TempleSteps";
        public const String TempleStepsPaint = "TempleStepsPaint";
        public const String TempleColumnPaint = "TempleColumnPaint";
        public const String TempleCeilingPlat = "TempleCeilingPlat";



        public const int StyleHellstone = 0;
        public const int StyleTitanstone = 1;
        public const int StyleBlueBrick = 2;
    }

    internal class RoomID
    {
        public const short MainRoom = 0;
        public const short SideRight = 1;
        public const short SideLeft = -1;
        public const short AboveSide = 2;
        public const short BelowSide = -2;
    }

    internal class Door //Door
    {
        public const short Left = 0;
        public const short Right = 1;
        public const short Up = 2;
        public const short Down = 3;
    }

    internal class BP //Breaking Point
    {
        public const short Left = 0; // the left or the only backwall breaking point in the room
        public const short Right = 1; // the right backwall breaking point in the room
    }
}
