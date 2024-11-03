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
        readonly (int xMin, int xMax, int yMin, int yMax) belowRoomSizes = (35, 60, 14, 16); // possible below room dimensions
        List<Rectangle2P> belowRoomsAndStairs = []; // list of all created below rooms and staircases to check and prevent overlappings
        List<(int x, int y, int pounds, int type, int style, byte paint, bool echoCoating)> PoundAfterSmoothWorld = []; // as the worldgen step "Smooth World" destroys the stairs of below rooms, they get stored here to create the stairs after that step
        

        Dictionary<string, (int id, int style)> Deco = []; // the dictionary where the styles of tiles are stored

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

                    int side; //init
                    if (WorldGenMod.chastisedChurchGenerationSide == "Left") side = -1;
                    else if (WorldGenMod.chastisedChurchGenerationSide == "Right") side = 1;
                    else if (WorldGenMod.chastisedChurchGenerationSide == "Both") side = -1; // start on the left
                    else side = WorldGen.genRand.NextBool() ? 1 : -1; // "Random"

                    //TODO: deactivate debug mode!
                    side = -1;
                    int startPosX = 0; // start on the left world side

                    //GenerateChastisedChurch(side);  <-- this was used before debug mode
                    for (int i = 1; i <= 10; i++)
                    {
                        startPosX = GenerateChastisedChurch(side, startPosX);
                    }


                    if (WorldGenMod.chastisedChurchGenerationSide == "Both")
                    {
                        GenerateChastisedChurch(1); // do the right side
                    }

                }));

                genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Smooth World"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: repair CC stairs", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Repairing the stairs that got deleted during the previous worldgen step...";

                    CreateStairsFromData();
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

        public void FillAndChooseStyle()
        {
            Deco.Clear(); // init

            #region create dictionary entries
            Deco.Add(S.StyleSave, (0,0));
            Deco.Add(S.Brick, (0, 0));
            Deco.Add(S.BrickPaint, (0, 0));
            Deco.Add(S.RoofBrick, (0, 0));
            Deco.Add(S.RoofBrickPaint, (0, 0));
            Deco.Add(S.Floor, (0, 0));
            Deco.Add(S.FloorPaint, (0, 0));
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

            //altar
            Deco.Add(S.MiddleWall, (0, 0));
            Deco.Add(S.MiddleWallPaint, (0, 0));
            Deco.Add(S.AltarSteps, (0, 0));
            Deco.Add(S.AltarStepsPaint, (0, 0));
            Deco.Add(S.AltarDeco, (0, 0));
            Deco.Add(S.AltarDecoPaint, (0, 0));
            Deco.Add(S.AltarWall, (0, 0));
            Deco.Add(S.AltarWallPaint, (0, 0));
            Deco.Add(S.RunicWallPaint, (0, 0));
        #endregion

        //choose a random style and define it's types
        int chooseStyle = WorldGen.genRand.Next(3);
            bool subStyle = false;
            switch (chooseStyle)
            {
                case S.StyleHellstone: // Hellstone

                    subStyle = Chance.Simple();

                    Deco[S.StyleSave] = (0, S.StyleHellstone);

                    Deco[S.Brick] = (TileID.AncientHellstoneBrick, 0);
                    Deco[S.BrickPaint] = (0, 0);
                    Deco[S.RoofBrick] = (TileID.AncientHellstoneBrick, 0);
                    Deco[S.RoofBrickPaint] = (0, 0);
                    Deco[S.Floor] = (TileID.CrimtaneBrick, 0);
                    Deco[S.FloorPaint] = (PaintID.RedPaint, 0);

                    if (subStyle)
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
                    Deco[S.Campfire] = (TileID.Campfire, 3);  //* Frozen
                    Deco[S.Table] = (TileID.Tables2, 11);  //AshWood
                    Deco[S.Workbench] = (TileID.WorkBenches, 43); //AshWood
                    Deco[S.Chair] = (TileID.Chairs, 11); // Shadewood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 26); //* "Discover"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 19); // Shadewood
                    Deco[S.Candelabra] = (TileID.Candelabras, 14); // Shadewood
                    if (subStyle) Deco[S.Candelabra] = (TileID.Candelabras, 12); // Spooky
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
                    Deco[S.PaintingWallpaper] = (WallID.SparkleStoneWallpaper, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 30); //* Frozen
                    Deco[S.Piano] = (TileID.Pianos, 7); //* Frozen

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
                    break;

                case S.StyleTitanstone: // Titanstone

                    subStyle = Chance.Simple();

                    Deco[S.StyleSave] = (S.StyleTitanstone, 0);
                    Deco[S.Brick] = (TileID.Titanstone, 0);
                    Deco[S.BrickPaint] = (0, 0);
                    Deco[S.RoofBrick] = (TileID.Titanstone, 0);
                    Deco[S.RoofBrickPaint] = (0, 0);

                    Deco[S.Floor] = (TileID.CrimtaneBrick, 0);
                    Deco[S.FloorPaint] = (0, 0);
                    if (subStyle)
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
                    Deco[S.Campfire] = (TileID.Campfire, 0); //* Normal
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
                    if (subStyle) Deco[S.Clock] = (TileID.GrandfatherClocks, 43); // AshWood
                    Deco[S.PaintingWallpaper] = (WallID.LivingWood, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 18); //* Boreal
                    Deco[S.Piano] = (TileID.Pianos, 23); //* Boreal

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
                    break;

                case S.StyleBlueBrick: //TODO: look for another third design. It was recommended to use EbonstoneBrick on Steam, maybe also just red brick?

                    subStyle = Chance.Simple();

                    Deco[S.StyleSave] = (S.StyleBlueBrick, 0);
                    Deco[S.Brick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.RoofBrick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.Floor] = (TileID.EbonstoneBrick, 0);
                    if (subStyle) Deco[S.Floor] = (TileID.MeteoriteBrick, 0);
                    Deco[S.FloorPaint] = (0, 0);
                    Deco[S.EvilTile] = (TileID.Ebonstone, 0);
                    Deco[S.BackWall] = (WallID.Shadewood, 0);
                    Deco[S.BackWallPaint] = (PaintID.None, 0);
                    Deco[S.CrookedWall] = (WallID.Corruption3Echo, 0);
                    Deco[S.WindowWall] = (WallID.BlueStainedGlass, 0);
                    Deco[S.WindowPaint] = (PaintID.BluePaint, 0);
                    Deco[S.DoorWall] = (WallID.SpookyWood, 0);

                    Deco[S.DoorPlat] = (TileID.Platforms, 16); // Spooky
                    Deco[S.DoorPlatPaint] = (PaintID.BluePaint, 0);
                    if (subStyle)
                    {
                        Deco[S.DoorPlat] = (TileID.Platforms, 27); // Meteorite
                        Deco[S.DoorPlatPaint] = (0, 0);
                    }

                    Deco[S.Door] = (TileID.TallGateClosed, 0);
                    Deco[S.DoorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Chest] = (TileID.Containers, 3); // Shadow
                    Deco[S.Campfire] = (TileID.Campfire, 7); //* Bone
                    Deco[S.Table] = (TileID.Tables, 1); // Ebonwood
                    Deco[S.Workbench] = (TileID.WorkBenches, 1); //* Ebonwood
                    Deco[S.Chair] = (TileID.Chairs, 2); // Ebonwood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 35); //* "Rare Enchantment"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 32); // Obsidian
                    Deco[S.Candelabra] = (TileID.Candelabras, 2); // Ebonwood
                    if (subStyle) Deco[S.Candelabra] = (TileID.PlatinumCandelabra, 0); // PlatinumCandelabra
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
                    Deco[S.PaintingWallpaper] = (WallID.BluegreenWallpaper, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 1); //* Ebonwood
                    Deco[S.Piano] = (TileID.Pianos, 1); //* Ebonwood
                    //TODO: decide if everything obsidian / demon or ebonwood!

                    //altar
                    Deco[S.MiddleWall] = (WallID.Bone, 0);
                    Deco[S.MiddleWallPaint] = (PaintID.RedPaint, 0);
                    Deco[S.AltarSteps] = (TileID.Platforms, 10); //Brass Shelf
                    if (subStyle) Deco[S.AltarSteps] = (TileID.Platforms, 22); //Skyware
                    Deco[S.AltarStepsPaint] = (0, 0);
                    Deco[S.AltarDeco] = (TileID.Platforms, 10); //Brass Shelf
                    if (subStyle) Deco[S.AltarDeco] = (TileID.Platforms, 22); //Skyware
                    Deco[S.AltarDecoPaint] = (0, 0);
                    Deco[S.AltarWall] = (WallID.DemoniteBrick, 0);
                    Deco[S.AltarWallPaint] = (0, 0);
                    Deco[S.RunicWallPaint] = (0, 0);
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
                    if (isStairCase && y < freeR.Y0) continue; // cellars overlap with the above laying room (they share the same floor / ceiling), don't touch that!

                    WorldGen.KillWall(x, y);
                    WorldGen.KillTile(x, y);
                    WorldGen.EmptyLiquid(x, y);

                    if (y == freeR.Y1 + 1) // the floor height of this room
                    {
                        if ((!doors[Door.Left].doorExist && x < freeR.X0))       { WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true); WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id); }
                        else if ((!doors[Door.Right].doorExist && x > freeR.X1)) { WorldGen.PlaceTile(x, y, Deco[S.Brick].id, true, true); WorldGen.paintTile(x, y, (byte)Deco[S.BrickPaint].id); }
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
        /// <param name="doors">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
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

                    #region modify windows shape
                    if (windowsExist)
                    {
                        bool alternativeShape = Chance.Simple();

                        List<System.Drawing.Point> windowPoints = [];
                        Tile windowTile;
                        foreach (Rectangle2P windowRect in windowsPairs)
                        {
                            windowPoints.Clear();
                            windowPoints.Add(new System.Drawing.Point(x: windowRect.X0, y: windowRect.Y0)); // upper left corner
                            windowPoints.Add(new System.Drawing.Point(x: windowRect.X1, y: windowRect.Y0)); // upper right corner

                            if (alternativeShape && (( freeR.YTiles - 2*windowYMargin ) >= 6) ) //window higher than 6 tiles
                            {
                                windowPoints.Add(new System.Drawing.Point(x: windowRect.X0, y: windowRect.Y0 + 3)); // point for cross shaped form
                                windowPoints.Add(new System.Drawing.Point(x: windowRect.X1, y: windowRect.Y0 + 3)); // point for cross shaped form
                            }

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


                            // check if the pedestral can be big
                            bool bigPedestral = ((windowRect.XCenter - 1 >= windowRect.X0) && (Main.tile[windowRect.XCenter - 1, y].TileType != Deco[S.DoorPlat].id)) &&
                                                ((windowRect.XCenter + 2 <= windowRect.X1) && (Main.tile[windowRect.XCenter + 2, y].TileType != Deco[S.DoorPlat].id));


                            // put pedestral in the middle of the window (XCenter and XCenter++)
                            y = freeR.Y1;

                            
                            x = windowRect.XCenter - 1;
                            if (bigPedestral)
                            {
                                WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                                WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);
                            }

                            x = windowRect.XCenter;
                            WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                            WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);

                            x = windowRect.XCenter + 1;
                            WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                            WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);

                            x = windowRect.XCenter + 2;
                            if (bigPedestral)
                            {
                                WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                                WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);
                            }


                            // put statue
                            randomItem = randomItems.PopAt(WorldGen.genRand.Next(randomItems.Count));
                            if (Chance.Perc(randomItem.chance))    WorldGen.PlaceTile(windowRect.XCenter, y - 1, randomItem.id, style: randomItem.style);
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

                                            ItemID.WandofSparking, ItemID.WandofFrosting, ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.SapphireStaff, ItemID.EmeraldStaff,

                                            ItemID.FlintlockPistol, ItemID.FlareGun, ItemID.ChainKnife, ItemID.Mace, ItemID.FlamingMace, ItemID.Spear, ItemID.Trident, ItemID.WoodenBoomerang,
                                            ItemID.EnchantedBoomerang, ItemID.BlandWhip
                                        ];

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

                    // Stuff on ceiling: HangingPots, Chandelier, 
                    #endregion

                    #region fill middle space
                    if(middleSpaceExist)
                    {
                        if (middleSpace.XTiles <= windowXTiles) { } // do nothing

                        else if (!doors[Door.Down].doorExist) // middle space not on a door
                        {
                            #region middleSpace.XTiles <= 8
                            if (middleSpace.XTiles <= 8)
                            {
                                # region create cascade with S.MiddleWall
                                int xStart = middleSpace.X0; //init value
                                int xEnd   = middleSpace.X1; //init value
                                for (int j = freeR.Y0; j <= freeR.Y1 + 1; j++)
                                {
                                    for (int i = xStart; i <= xEnd; i++)
                                    {
                                        if ((Main.tile[i, j].WallType != Deco[S.CrookedWall].id) || Chance.Perc(60))
                                        {
                                            WorldGen.KillWall(i, j);
                                            WorldGen.PlaceWall(i, j, Deco[S.MiddleWall].id);
                                            WorldGen.paintWall(i, j, (byte)Deco[S.MiddleWallPaint].id);
                                        }
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

                            #region middleSpace.XTiles <= 10
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

                            #region middleSpace.XTiles <= 18
                            if (middleSpace.XTiles <= 18)
                            {
                                //CreateAltar(middleSpace.X0 + 1, middleSpace.X1 - 1, freeR.Y1, 8);
                            }
                            #endregion
                        }
                    }
                    #endregion

                    Func.PlaceStinkbug(freeR);

                    break;

                case 100: // empty room for display
                          //windows blueprint for copying
                    #region windows
                    windowsPairs.Clear();

                    // create window rectangles
                    if (freeR.YTiles > (windowXTiles + 2 * windowYMargin) && freeR.XTiles >= (windowXTiles + 2 * 2)) //minimal window size: windowXTiles * windowXTiles
                    {
                        if (freeR.XTiles <= 12) // narrow room, place window in the middle
                        {
                            int windowCenterOffset = (windowXTiles / 2) - 1 + (windowXTiles % 2); // to center the window at a specified x position

                            windowsPairs.Add(new Rectangle2P(freeR.XCenter - windowCenterOffset, windowY0, windowXTiles, windowYTiles));
                        }

                        else // symmetrical window pairs with spaces in between
                        {
                            int windowXMargin = 2; // how many tiles the outer windows-pair shall be away from the left / right wall
                            int windowDistanceXTiles = 4; // XTiles between two windows

                            int windowLeftX0 = freeR.X0 + windowXMargin; // init
                            int windowRightX0 = freeR.X1 - windowXMargin - (windowXTiles - 1); // init

                            while (windowLeftX0 + windowXTiles < freeR.XCenter)
                            {
                                windowsPairs.Add(new Rectangle2P(windowLeftX0, windowY0, windowXTiles, windowYTiles)); // left room side
                                windowsPairs.Add(new Rectangle2P(windowRightX0, windowY0, windowXTiles, windowYTiles)); // right room side

                                windowLeftX0 += (windowXTiles + windowDistanceXTiles);
                                windowRightX0 -= (windowXTiles + windowDistanceXTiles);
                            }
                        }
                    }

                    // put windows
                    windowPairsExist = windowsPairs.Count > 0;
                    if (windowPairsExist)
                    {
                        foreach (Rectangle2P windowRect in windowsPairs)
                        {
                            for (int i = windowRect.X0; i <= windowRect.X1; i++)
                            {
                                for (int j = windowRect.Y0; j <= windowRect.Y1; j++)
                                {
                                    WorldGen.KillWall(i, j);

                                    if (!wallBreak[BP.Left].exist) awayEnough1 = true;
                                    else awayEnough1 = Vector2.Distance(new Vector2(i, j), wallBreak[BP.Left].point) > WorldGen.genRand.NextFloat(4f, 12f);

                                    if (!wallBreak[BP.Right].exist) awayEnough2 = true;
                                    else awayEnough2 = Vector2.Distance(new Vector2(i, j), wallBreak[BP.Right].point) > WorldGen.genRand.NextFloat(4f, 12f);


                                    if (awayEnough1 && awayEnough2)
                                    {
                                        WorldGen.PlaceWall(i, j, Deco[S.WindowWall].id);
                                        WorldGen.paintWall(i, j, (byte)Deco[S.WindowPaint].id);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

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
        /// <param name="doors">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
        public void DecorateStairCase(Rectangle2P room, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors, IDictionary<int, (bool exist, Vector2 point)> wallBreak)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");


            // init variables
            int x, y;
            
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




            // pound platform tile of the already existing down door
            (int x, int y) stairsStart = (startX - creationDir, startY - 1);
            stairs.Add((stairsStart.x, stairsStart.y), (1, false));

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

            Func.PlaceCobWeb(freeR, 1, WorldGenMod.configChastisedChurchCobwebFilling);
        }

        /// <summary>
        /// The main method for choosing and running a below rooms decoration
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        /// <param name="doors">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
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


            // for window placement
            List<Rectangle2P> windowsPairs = []; // ascending indexes refer to windows in the room like this: 6 windows (0 2 4 5 3 1), 8 windows (0 2 4 6 7 5 3 1) etc.
            List<Rectangle2P> windowsOrder = []; // ascending indexes refer to windows in the room like this: 6 windows (0 1 2 3 4 5), 8 windows (0 1 2 3 4 5 6 7) etc.
            List<Rectangle2P> spacesOrder = []; // ascending indexes refer to the spaces between windows in the room like this: 2 spaces (4 windows) (W 0 W | W 1 W), 4 spaces (6 windows) (W 0 W 1 W | W 2 W 3 W) etc.
            Rectangle2P middleSpace; // the middle space if the room has pairs of windows

            int windowXTiles = 4;

            int windowYMargin = 2; // how many Tiles the window shall be away from the ceiling / floor
            int windowY0 = freeR.Y0 + windowYMargin; // height where the window starts
            int windowYTiles = freeR.YTiles - (2 * windowYMargin); // the YTiles height of a window

            bool awayEnough1, awayEnough2, windowsExist, windowPairsExist, middleSpaceExist = false, windowDistanceXTilesOdd = false;




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
    }

    internal class S //Style
    {
        public const String StyleSave = "Style";
        public const String Brick = "Brick";
        public const String BrickPaint = "BrickPaint";
        public const String RoofBrick = "RoofBrick";
        public const String RoofBrickPaint = "RoofBrickPaint";
        public const String Floor = "Floor";
        public const String FloorPaint = "FloorPaint";
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
