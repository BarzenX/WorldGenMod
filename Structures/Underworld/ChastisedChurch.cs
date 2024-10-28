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

namespace WorldGenMod.Structures.Underworld
{
    class ChastisedChurch : ModSystem
    {
        readonly int gap = -1; // the horizontal gap between two side room columns
        readonly int wThick = 2; // the thickness of the outer walls and ceilings in code
        readonly int doorHeight = 5; // the height of a connection between two
        readonly int forceEvenRoom = 1; // 1 = force all rooms to have an even XTiles count; 0 = force all side rooms to have an odd XTiles count
        readonly int maxChurchLength = 500; // maximum tile length of the ChastisedChurch
        readonly (int xmin, int xmax, int ymin, int ymax) maxRoom = (12, 80, 12, 30); // possible room dimensions


        Dictionary<string, (int id, int style)> Deco = []; // the dictionary where the styles of tiles are stored

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateChastisedChurch)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Underworld"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Chastised Church", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Chastising the crooked church...";

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
            Deco.Add(S.Bed, (0, 0));
            Deco.Add(S.BedWallpaper, (0, 0));
            Deco.Add(S.PaintingWallpaper, (0, 0));
            Deco.Add(S.Dresser, (0, 0));
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
                    Deco[S.Table] = (TileID.Tables2, 11);  //Ash Wood
                    Deco[S.Workbench] = (TileID.WorkBenches, 20); //* Frozen
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
                    Deco[S.DecoPlat] = (TileID.Platforms, 19); //* Boreal
                    Deco[S.StylePaint] = (PaintID.RedPaint, 0);
                    Deco[S.HangingPot] = (TileID.PotsSuspended, 4); //* Shiverthorn
                    Deco[S.Bookcase] = (TileID.Bookcases, 43); // Ash Wood
                    Deco[S.Sofa] = (TileID.Benches, 5); // Shade Wood
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 11); //* Frozen
                    Deco[S.Bed] = (TileID.Beds, 15); //* Frozen
                    Deco[S.BedWallpaper] = (WallID.StarsWallpaper, 0); //*
                    Deco[S.PaintingWallpaper] = (WallID.SparkleStoneWallpaper, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 30); //* Frozen
                    Deco[S.Piano] = (TileID.Pianos, 7); //* Frozen
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
                    Deco[S.Workbench] = (TileID.WorkBenches, 23); //* Boreal
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
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 6); //* Boreal
                    Deco[S.Bed] = (TileID.Beds, 24); //* Boreal
                    Deco[S.BedWallpaper] = (WallID.StarlitHeavenWallpaper, 0); //*
                    Deco[S.PaintingWallpaper] = (WallID.LivingWood, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 18); //* Boreal
                    Deco[S.Piano] = (TileID.Pianos, 23); //* Boreal
                    break;

                case S.StyleBlueBrick: //TODO: look for another third design. It was recommended to use EbonstoneBrick on Steam, maybe also just red brick?

                    subStyle = Chance.Simple();

                    Deco[S.StyleSave] = (S.StyleBlueBrick, 0);
                    Deco[S.Brick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.RoofBrick] = (TileID.BlueDungeonBrick, 0);
                    Deco[S.Floor] = (TileID.EbonstoneBrick, 0);
                    Deco[S.FloorPaint] = (0, 0);
                    if (subStyle) Deco[S.Floor] = (TileID.MeteoriteBrick, 0);
                    Deco[S.EvilTile] = (TileID.Ebonstone, 0);
                    Deco[S.BackWall] = (WallID.Shadewood, 0);
                    Deco[S.BackWallPaint] = (PaintID.None, 0);
                    Deco[S.CrookedWall] = (WallID.Corruption3Echo, 0);
                    Deco[S.WindowWall] = (WallID.BlueStainedGlass, 0);
                    Deco[S.WindowPaint] = (PaintID.BluePaint, 0);
                    Deco[S.DoorWall] = (WallID.SpookyWood, 0);

                    Deco[S.DoorPlat] = (TileID.Platforms, 16); // Spooky
                    Deco[S.DoorPlatPaint] = (PaintID.DeepBluePaint, 0);
                    Deco[S.Door] = (TileID.TallGateClosed, 0);
                    Deco[S.DoorPaint] = (PaintID.RedPaint, 0);
                    Deco[S.Chest] = (TileID.Containers, 3); // Shadow
                    Deco[S.Campfire] = (TileID.Campfire, 7); //* Bone
                    Deco[S.Table] = (TileID.Tables, 1); // Ebonwood
                    Deco[S.Workbench] = (TileID.WorkBenches, 1); //* Ebonwood
                    Deco[S.Chair] = (TileID.Chairs, 2); // Ebonwood
                    Deco[S.MainPainting] = (TileID.Painting3X3, 35); //* "Rare Enchantment"
                    Deco[S.Chandelier] = (TileID.Chandeliers, 32); // Obsidian
                    Deco[S.Candelabra] = (TileID.Candelabras, 2); //* Ebonwood
                    if (subStyle) Deco[S.Candelabra] = (TileID.PlatinumCandelabra, 0); // PlatinumCandelabra
                    Deco[S.Candle] = (TileID.Candles, 5); // Ebonwood
                    Deco[S.Lamp] = (TileID.Lamps, 23); // Obsidian
                    Deco[S.Torch] = (TileID.Torches, 7); //* Demon
                    Deco[S.Lantern] = (TileID.HangingLanterns, 2); //* Caged Lantern
                    Deco[S.Banner] = (TileID.Banners, 0); //* Red
                    Deco[S.DecoPlat] = (TileID.Platforms, 19); //* Boreal
                    Deco[S.StylePaint] = (PaintID.GrayPaint, 0);
                    Deco[S.HangingPot] = (TileID.PotsSuspended, 6); //* Corrupt Deathweed
                    Deco[S.Bookcase] = (TileID.Bookcases, 7); // Ebonwood
                    Deco[S.Sofa] = (TileID.Benches, 2); // Ebonwood
                    Deco[S.Clock] = (TileID.GrandfatherClocks, 10); //* Ebonwood
                    Deco[S.Bed] = (TileID.Beds, 1); //* Ebonwood
                    Deco[S.BedWallpaper] = (WallID.StarlitHeavenWallpaper, 0); //*
                    Deco[S.PaintingWallpaper] = (WallID.BluegreenWallpaper, 0); //*
                    Deco[S.Dresser] = (TileID.Dressers, 1); //* Ebonwood
                    Deco[S.Piano] = (TileID.Pianos, 1); //* Ebonwood
                    //TODO: decide if everything obsidian / demon or ebonwood!
                    break;
            }
        }

        public int GenerateChastisedChurch(int generationSide, int startX = 0)
        {
            if (!WorldGenMod.generateChastisedChurch) return 0;

            FillAndChooseStyle();

            int startPosX, startPosY;

            if      (generationSide == -1) startPosX =                  WorldGen.genRand.Next(50, 100); // left world side
            else if (generationSide ==  1) startPosX = Main.maxTilesX - WorldGen.genRand.Next(50, 100); // right world side
            else                           startPosX = 0;

            if (generationSide == -1 && startX > 0) startPosX = startX + 10;

            startPosY = Main.maxTilesY - 100;


            int totalTiles = 0;
            int maxTiles = Math.Min(Main.maxTilesX / 8, maxChurchLength);
            int actX = 0, actY = 0;
            int roomWidth = 0, roomHeight = 0;
            bool leftDoor, rightDoor;
            Rectangle2P actRoom, lastRoom = Rectangle2P.Empty; // Rectangle2P for later filling a possible gap between the rooms

            while (totalTiles < maxTiles)
            {
                roomWidth = WorldGen.genRand.Next(maxRoom.xmin, maxRoom.xmax + 1);
                if      (forceEvenRoom == 1) roomWidth -= (roomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) roomWidth -= (roomWidth % 2) + 1; //make room always uneven

                roomHeight = WorldGen.genRand.Next(maxRoom.ymin, maxRoom.ymax + 1);

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
        /// <param name="belowCount">Stating how many rooms below the main line this particular room is. 0 = main line</param>
        /// 
        /// <returns>Hands back the room dimensions input or an empty room if the creation failed</returns>
        public Rectangle2P GenerateRoom(Rectangle2P room, Rectangle2P previousRoom, int roofHeight = 0, bool leftDoor = false, bool rightDoor = false, int belowCount = 0)
        {
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");

            int x, y; //temp variables for later calculations;

            if (room.Y1 >= Main.maxTilesY || room.X1 >= Main.maxTilesX || room.X0 <= 0) return Rectangle2P.Empty;


            // calculate if this room will have a "cellar".... is needed now for creating this rooms doors properly
            #region cellar calculation
            int nextCellarYTiles = (int)(room.YTiles * WorldGen.genRand.NextFloat(0.8f, 1.0f));
            bool downRoomPossible = WorldGen.genRand.NextBool(2 + belowCount) && belowCount <= 3 && room.Y1 + nextCellarYTiles < Main.maxTilesY - wThick - 2;

            bool downRoomExist = false;
            Rectangle2P belowRoom = Rectangle2P.Empty;
            if (downRoomPossible)
            {
                int cellarWidth = (int)(room.XTiles * WorldGen.genRand.NextFloat(0.5f, 1f));
                if (forceEvenRoom == 1) cellarWidth -= (cellarWidth % 2); //make room always even
                else if (forceEvenRoom == 0) cellarWidth -= (cellarWidth % 2) + 1; //make room always uneven

                belowRoom = new(room.X0 + (room.XTiles - cellarWidth) / 2, freeR.Y1 + 1, cellarWidth, nextCellarYTiles);

                downRoomExist = belowRoom.XTiles >= maxRoom.xmin && belowRoom.YTiles >= maxRoom.ymin;
            }
            #endregion


            #region door rectangles
            Dictionary<int, (bool doorExist, Rectangle2P doorRect)> doors = []; // a dictionary for working and sending the doors in a compact way

            int leftRightDoorsYTiles = 5; // how many tiles the left and right doors are high
            y = freeR.Y1 - (leftRightDoorsYTiles - 1);
            Rectangle2P leftDoorRect  = new(room.X0     , y, wThick, leftRightDoorsYTiles);
            Rectangle2P rightDoorRect = new(freeR.X1 + 1, y, wThick, leftRightDoorsYTiles);

            int upDownDoorXTiles = 6; // how many tiles the up and down doors are wide
            int adjustX = 0; // init
            if (freeR.XTiles % 2 == 1 && upDownDoorXTiles % 2 == 0) upDownDoorXTiles++; // an odd number of x-tiles in the room also requires an odd number of platforms so the door is symmetrical
            else adjustX = -1; //in even XTile rooms there is a 2-tile-center and XCenter will be the left tile of the two. To center an even-numberd door in this room, you have to subtract 1. Odd XTile rooms are fine
            x = (freeR.XCenter) - (upDownDoorXTiles / 2 + adjustX);
            Rectangle2P upDoorRect   = new(x, room.Y0     , upDownDoorXTiles, wThick);
            Rectangle2P downDoorRect = new(x, freeR.Y1 + 1, upDownDoorXTiles, wThick);

            doors.Add(Door.Left, (leftDoor, leftDoorRect));
            doors.Add(Door.Right, (rightDoor, rightDoorRect));
            doors.Add(Door.Up, (belowCount > 0, upDoorRect));
            doors.Add(Door.Down, (downRoomExist, downDoorRect));
            #endregion


            #region carve out room and place bricks
            for (x = room.X0; x <= room.X1; x++)
            {
                for (y = room.Y0; y <= room.Y1; y++)
                {
                    if (belowCount > 0 && y < freeR.Y0) continue; // cellars overlap with the above laying room (they share the same floor / ceiling), don't touch that!

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
                if (belowCount > 0 && doorNum == Door.Up) continue; // this door was already created by the previous room, no need to do it again

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
                    WorldGen.PlaceTile(i, j, TileID.Platforms, mute: true, forced: true, style: Deco[S.DoorPlat].id);
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
                //    WorldGen.PlaceTile(i, j, TileID.Platforms, mute: true, forced: true, style: Deco[S.DoorPlat]);
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

            // put actual doors
            if (belowCount == 0) // only the main line has left/right doors
            {
                if (previousRoom.X0 < room.X0 ) // rooms advancing from left to right: put left door
                {
                    bool placed;

                    x = room.X0; // right side rooms always have a left door
                    y = freeR.Y1;//;
                    placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // left gate

                    if (placed )//&& Deco[S.DoorPaint] > PaintID.None)
                    {
                        Func.GateTurn(x, y);
                        for (int i = 0; i < doorHeight; i++)
                        {
                            WorldGen.paintTile(x, y - i, (byte)Deco[S.DoorPaint].id);
                        }
                    }



                    if (gap > 0 && doors[Door.Right].doorExist) // in case there is a gap between side rooms and this right side room also has a right door
                    {
                        x = room.X1;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed && Deco[S.DoorPaint].id > PaintID.None)
                        {
                            for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint].id);
                        }
                    }
                }

                else // rooms advancing from right to left: put right door
                {
                    bool placed;

                    x = room.X1; // left side rooms always have a right door
                    y = freeR.Y1 - (doorHeight - 1);
                    placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // right gate

                    if (placed && Deco[S.DoorPaint].id > PaintID.None)
                    {
                        for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint].id);
                    }



                    if (gap > 0 && doors[Door.Left].doorExist) // in case there is a gap between side rooms and this left side room also has a left door
                    {
                        x = room.X0;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed && Deco[S.DoorPaint].id > PaintID.None)
                        {
                            Func.GateTurn(x, y);
                            for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint].id);
                        }
                    }
                }
            }
            #endregion


            #region put roof
            if (belowCount == 0) //only the main line rooms have a roof
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
                WorldGen.SlopeTile(leftDoorRect.X1, leftDoorRect.Y0 - 1, (int)Func.SlopeVal.BotRight); // door right corner
            }
            if (rightDoor)
            {
                WorldGen.SlopeTile(rightDoorRect.X0, rightDoorRect.Y0 - 1, (int)Func.SlopeVal.BotLeft); // door left corner
            }
            if (belowCount > 0)
            {
                WorldGen.SlopeTile(upDoorRect.X0 - 1, upDoorRect.Y1, (int)Func.SlopeVal.BotRight); // updoor left corner
                WorldGen.SlopeTile(upDoorRect.X1 + 1, upDoorRect.Y1, (int)Func.SlopeVal.BotLeft); // updoor right corner
            }
            #endregion


            if (downRoomExist)
            {
                GenerateRoom(belowRoom, Rectangle2P.Empty, belowCount: belowCount + 1);
            }

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


            DecorateRoom(room, doors, wallBreak, belowCount);

            //TODO: chest style
            if (Chance.Perc(20))
            {
                (bool success, int x, int y) placeResult;
                Rectangle2P area1 = new Rectangle2P(freeR.X0, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");
                placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.Containers, style: Deco[S.Chest].style, chance: 75); // Chest...
                if (placeResult.success)
                {
                    int chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                    if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2)); // ...with loot
                }
            }

            return room;
        }


        /// <summary>
        /// The main method for choosing and running the a rooms decoration
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        /// <param name="doors">The points of the possible backwall breaks in the room and a bool stating if it actually exists (use class "BP" to refer to a specific breaking point)</param>
        /// <param name="belowCount">Stating how many rooms below the main line this particular room is. 0 = main line</param>
        public void DecorateRoom(Rectangle2P room, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors, IDictionary<int, (bool exist, Vector2 point)> wallBreak, int belowCount = 0)
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
            int x, y, chestID, unusedXTiles, num;


            // for window placement
            List<Rectangle2P> windowsPairs = []; // ascending indexes refer to windows in the room like this: 6 windows (0 2 4 5 3 1), 8 windows (0 2 4 6 7 5 3 1) etc.
            List<Rectangle2P> windowsOrder = []; // ascending indexes refer to windows in the room like this: 6 windows (0 1 2 3 4 5), 8 windows (0 1 2 3 4 5 6 7) etc.
            List<Rectangle2P> spacesOrder = []; // ascending indexes refer to the spaces between windows in the room like this: 2 spaces (4 windows) (W 0 W | W 1 W), 4 spaces (6 windows) (W 0 W 1 W | W 2 W 3 W) etc.
            Rectangle2P middleSpace; // the middle space if the room has pairs of windows
            
            int windowXTiles = 4;

            int windowYMargin = 2; // how many Tiles the window shall be away from the ceiling / floor
            int windowY0 = freeR.Y0 + windowYMargin; // height where the window starts
            int windowYTiles = freeR.YTiles - (2 * windowYMargin); // the YTiles height of a window

            bool awayEnough1, awayEnough2, windowsExist, middleSpaceExist = false, windowDistanceXTilesOdd = false;

            int roomDeco;
            if (belowCount == 0)
            {
                //choose the upper room decoration at random
                roomDeco = WorldGen.genRand.Next(1); //TODO: don't forget to put the correct values in the end!
            }
            else
            {
                //choose the below room decoration at random
                roomDeco = WorldGen.genRand.Next(50,50); //TODO: don't forget to put the correct values in the end!

            }

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
                    randomItems.Add((TileID.Statues, 11, 95));//Reaper
                    randomItems.Add((TileID.Statues, 13, 95));//Imp
                    randomItems.Add((TileID.Statues, 14, 95));//Gargoyle
                    randomItems.Add((TileID.Statues, 15, 95));//Gloom
                    randomItems.Add((TileID.Statues, 22, 95));//Cross
                    randomItems.Add((TileID.Statues, 30, 95));//Corrupt
                    randomItems.Add((TileID.Statues, 35, 95));//Eyeball
                    randomItems.Add((TileID.Statues, 63, 95));//Wall Creeper
                    randomItems.Add((TileID.Statues, 65, 95));//Drippler
                    randomItems.Add((TileID.Statues, 71, 95));//Pigron
                    randomItems.Add((TileID.Statues, 74, 95));//Armed Zombie
                    randomItems.Add((TileID.Statues, 75, 95));//Blood Zombie

                    if (windowsExist)
                    {
                        foreach (Rectangle2P windowRect in windowsPairs)
                        {
                            y = freeR.Y1;

                            // put pedestral
                            x = windowRect.XCenter;
                            WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                            WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);

                            x = windowRect.XCenter + 1;
                            WorldGen.PlaceTile(x, y, Deco[S.Floor].id, true, true);
                            WorldGen.paintTile(x, y, (byte)Deco[S.FloorPaint].id);

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
                            if (Chance.Perc(5)) continue;

                            if (windowDistanceXTilesOdd) // preferably place odd-x-tiles-object so they come out centered
                            {
                                switch (WorldGen.genRand.Next(5))
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

                                    default:
                                        break;
                                }

                            }
                            else // preferably place even-x-tiles-object so they come out centered
                            {
                                randomItems.Clear();
                                //randomItems.Add((TileID.Candelabras, 0, 95)); // candelabra on a platform
                                //randomItems.Add((TileID.GrandfatherClocks, 0, 95));
                                randomItems.Add((TileID.PottedLavaPlants, 0, 95));
                                randomItems.Add((TileID.PottedLavaPlantTendrils, 0, 95));
                                randomItems.Add((TileID.PottedPlants2, 0, 95));
                                randomItems.Add((TileID.ItemFrame, 0, 95));
                                randomItems.Add((TileID.Tombstones, 0, 95)); //golden Tombstones
                                //randomItems.Add((TileID.WorkBenches, 0, 95)); //workbench with stuff on it

                                switch (WorldGen.genRand.Next(5))
                                {
                                    case 0: // candelabra on a platform

                                        y = freeR.Y1 - (windowYMargin - 1);
                                        WorldGen.PlaceTile(windowRect.XCenter    , y, Deco[S.DecoPlat].id, style: Deco[S.DecoPlat].style);
                                        WorldGen.PlaceTile(windowRect.XCenter + 1, y, Deco[S.DecoPlat].id, style: Deco[S.DecoPlat].style);

                                        placed = WorldGen.PlaceTile(windowRect.XCenter, y - 1, Deco[S.Candelabra].id, style: Deco[S.Candelabra].style);
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

                                    case 3: // Sofa
                                        WorldGen.PlaceTile(windowRect.XCenter, freeR.Y1, Deco[S.Sofa].id, style: Deco[S.Sofa].style);
                                        break;

                                    case 4: //ItemFrame

                                        List<int> itemFrameItems =
                                        [
                                            ItemID.Amber, ItemID.Amethyst, ItemID.Diamond, ItemID.Emerald, ItemID.Ruby, ItemID.Sapphire, ItemID.Topaz
                                        ];

                                        Func.PlaceItemFrame(windowRect.XCenter, freeR.Y1 - 4, paint: Deco[S.StylePaint].id,
                                                                                               item: itemFrameItems.PopAt(WorldGen.genRand.Next(itemFrameItems.Count)) );
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    // Stuff on ceiling: HangingPots, Chandelier, 
                    #endregion

                    Func.PlaceStinkbug(freeR);

                    break;

                case 50: // Below room #1

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
                    windowsExist = windowsPairs.Count > 0;
                    if (windowsExist)
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
        public const String Bed = "Bed";
        public const String BedWallpaper = "BedWallpaper";
        public const String PaintingWallpaper = "PaintingWallpaper";
        public const String Dresser = "Dresser";
        public const String Piano = "Piano";

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
