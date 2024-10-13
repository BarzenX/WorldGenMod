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


        Dictionary<string, int> Deco = []; // the dictionary where the styles of tiles are stored

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

                    GenerateChastisedChurch(side);


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
                        WorldGen.PlaceTile(x, y, Deco[S.Floor], true, true);
                        WorldGen.PlaceWall(x, y, Deco[S.BackWall]); //put the designated background wall
                    }
                    else if (y >= previousRoom.Y1 - (wThick - 1) - doorHeight && y <= previousRoom.Y1 - wThick) // the door between the rooms
                    {
                        // don't put bricks, leave the door "free"
                        WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); //put the designated background wall
                    }
                    else
                    {
                        WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true); // fill gap with bricks
                        WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // put the designated background wall
                    }
                }
            }

            // place backwall left and right of the gap (those were left out when the rooms were created)
            for (int y = gap.Y0; y <= gap.Y1; y++)
            {
                if (y >= previousRoom.Y1 - (wThick - 1) - doorHeight && y <= previousRoom.Y1 - wThick) continue; // doors already have that wall

                WorldGen.PlaceWall(gap.X0 - 1, y, Deco[S.BackWall]); //put the designated background wall
                WorldGen.PlaceWall(gap.X1 + 1, y, Deco[S.BackWall]); //put the designated background wall
            }
        }

        public void FillAndChooseStyle()
        {
            Deco.Clear(); // init

            // create dictionary entries
            Deco.Add(S.StyleSave, 0);
            Deco.Add(S.Brick, 0);
            Deco.Add(S.RoofBrick, 0);
            Deco.Add(S.Floor, 0);
            Deco.Add(S.EvilTile, 0);
            Deco.Add(S.BackWall, 0);
            Deco.Add(S.BackWallPaint, 0);
            Deco.Add(S.CrookedWall, 0);
            Deco.Add(S.WindowWall, 0);
            Deco.Add(S.WindowPaint, 0);
            Deco.Add(S.DoorWall, 0);
            Deco.Add(S.DoorPlat, 0);
            Deco.Add(S.DoorPlatPaint, 0);
            Deco.Add(S.Door, 0);
            Deco.Add(S.DoorPaint, 0);
            Deco.Add(S.Chest, 0);
            Deco.Add(S.Campfire, 0);
            Deco.Add(S.Table, 0);
            Deco.Add(S.Workbench, 0);
            Deco.Add(S.Chair, 0);
            Deco.Add(S.MainPainting, 0);
            Deco.Add(S.Chandelier, 0);
            Deco.Add(S.Candelabra, 0);
            Deco.Add(S.Candle, 0);
            Deco.Add(S.Lamp, 0);
            Deco.Add(S.Torch, 0);
            Deco.Add(S.Lantern, 0);
            Deco.Add(S.Banner, 0);
            Deco.Add(S.DecoPlat, 0);
            Deco.Add(S.StylePaint, 0);
            Deco.Add(S.HangingPot, 0);
            Deco.Add(S.Bookcase, 0);
            Deco.Add(S.Sofa, 0);
            Deco.Add(S.Clock, 0);
            Deco.Add(S.Bed, 0);
            Deco.Add(S.BedWallpaper, 0);
            Deco.Add(S.PaintingWallpaper, 0);
            Deco.Add(S.Dresser, 0);

            //choose a random style and define it's types
            int chooseStyle = WorldGen.genRand.Next(3);
            switch (chooseStyle)
            {
                case S.StyleHellstone: // Hellstone
                    Deco[S.StyleSave] = S.StyleHellstone;
                    Deco[S.Brick] = TileID.AncientHellstoneBrick;
                    Deco[S.RoofBrick] = TileID.AncientHellstoneBrick;
                    Deco[S.Floor] = TileID.ObsidianBrick;
                    if (Chance.Simple()) Deco[S.Floor] = TileID.CrimtaneBrick;
                    Deco[S.EvilTile] = TileID.Crimstone;
                    Deco[S.BackWall] = WallID.HellstoneBrickUnsafe;
                    Deco[S.BackWallPaint] = PaintID.None;
                    Deco[S.CrookedWall] = WallID.Crimson4Echo;
                    Deco[S.WindowWall] = WallID.RedStainedGlass;
                    Deco[S.WindowPaint] = PaintID.DeepRedPaint;
                    Deco[S.DoorWall] = WallID.CrimtaneBrick;

                    Deco[S.DoorPlat] = 10; // Tile ID 19 (Plattforms) -> Type 10=Brass Shelf
                    Deco[S.DoorPlatPaint] = PaintID.RedPaint;
                    Deco[S.Door] = TileID.TallGateClosed;
                    Deco[S.DoorPaint] = PaintID.RedPaint;
                    Deco[S.Chest] = 11;    // Tile ID 21 (Cests) -> Type 11=Frozen
                    Deco[S.Campfire] = 3;  // Tile ID 215 (Campfire) -> Type 3=Frozen
                    Deco[S.Table] = 24;    // Tile ID 14 (Tables) -> Type 24=Frozen
                    Deco[S.Workbench] = 20;// Tile ID 18 (Workbenches) -> Type 20=Frozen
                    Deco[S.Chair] = 28;    // Tile ID 15 (Chairs) -> Type 28=Frozen
                    Deco[S.MainPainting] = 26;// Tile ID 240 (Painting3X3) -> Type 26=Discover
                    Deco[S.Chandelier] = 11;// Tile ID 34 (Chandeliers) -> Type 11=Frozen
                    Deco[S.Candelabra] = 9;// Tile ID 100 (Candelabras) -> Type 9=Frozen
                    Deco[S.Candle] = 8;    // Tile ID 33 (Candles) -> Type 8=Frozen
                    Deco[S.Lamp] = 5;      // Tile ID 93 (Lamps) -> Type 5=Frozen
                    Deco[S.Torch] = 9;     // Tile ID 93 (Torches) -> Type 9=Ice
                    Deco[S.Lantern] = 18;  // Tile ID 42 (Lanterns) -> Type 18=Frozen
                    Deco[S.Banner] = 2;    // Tile ID 91 (Banners) -> Type 2=Blue
                    Deco[S.DecoPlat] = 19; // Tile ID 19 (Plattforms) -> Type 19=Boreal
                    Deco[S.StylePaint] = PaintID.WhitePaint;
                    Deco[S.HangingPot] = 4;// Tile ID 591 (PotsSuspended) -> Type 4=Shiverthorn
                    Deco[S.Bookcase] = 17; // Tile ID 101 (Bookcases) -> Type 17=Frozen
                    Deco[S.Sofa] = 27; // Tile ID 89 (Sofas) -> Type 27=Frozen
                    Deco[S.Clock] = 11;    // Tile ID 104 (GrandfatherClocks) -> Type 11=Frozen
                    Deco[S.Bed] = 15;      // Tile ID 79 (Beds) -> Type 15=Frozen
                    Deco[S.BedWallpaper] = WallID.StarsWallpaper;
                    Deco[S.PaintingWallpaper] = WallID.SparkleStoneWallpaper;
                    Deco[S.Dresser] = 30;  // Tile ID 88 (Dressers) -> Type 30=Frozen
                    Deco[S.Piano] = 7;     // Tile ID 87 (Pianos) -> Type 7=Frozen
                    break;

                case S.StyleTitanstone: // Titanstone
                    Deco[S.StyleSave] = S.StyleTitanstone;
                    Deco[S.Brick] = TileID.Titanstone;
                    Deco[S.RoofBrick] = TileID.Titanstone;
                    Deco[S.Floor] = TileID.CrimtaneBrick;
                    if (Chance.Simple()) Deco[S.Floor] = TileID.GrayBrick;
                    Deco[S.EvilTile] = TileID.Ebonstone;
                    Deco[S.BackWall] = WallID.GraniteBlock;
                    Deco[S.BackWallPaint] = PaintID.GrayPaint;
                    Deco[S.CrookedWall] = WallID.Lava3Echo;
                    Deco[S.WindowWall] = WallID.RedStainedGlass;
                    Deco[S.WindowPaint] = PaintID.DeepRedPaint;
                    Deco[S.DoorWall] = WallID.Shadewood;

                    Deco[S.DoorPlat] = 13; // Tile ID 19 (Plattforms) -> Type 13=Obsidian
                    Deco[S.DoorPlatPaint] = PaintID.None;
                    Deco[S.Door] = TileID.TallGateClosed;
                    Deco[S.DoorPaint] = PaintID.RedPaint;
                    Deco[S.Chest] = 33;    // Tile ID 21 (Cests) -> Type 33=Boreal
                    Deco[S.Campfire] = 0;  // Tile ID 215 (Campfire) -> Type 0=Normal
                    Deco[S.Table] = 28;    // Tile ID 14 (Tables) -> Type 33=Boreal
                    Deco[S.Workbench] = 23;// Tile ID 18 (Workbenches) -> Type 23=Boreal
                    Deco[S.Chair] = 30;    // Tile ID 15 (Chairs) -> Type 30=Boreal
                    Deco[S.MainPainting] = 34;// Tile ID 240 (Painting3X3) -> Type 34=Crowno Devours His Lunch
                    Deco[S.Chandelier] = 25;// Tile ID 34 (Chandeliers) -> Type 25=Boreal
                    Deco[S.Candelabra] = 20;// Tile ID 100 (Candelabras) -> Type 20=Boreal
                    Deco[S.Candle] = 20;   // Tile ID 33 (Candles) -> Type 20=Boreal
                    Deco[S.Lamp] = 20;     // Tile ID 93 (Lamps) -> Type 20=Boreal
                    Deco[S.Torch] = 9;     // Tile ID 93 (Torches) -> Type 9=Ice
                    Deco[S.Lantern] = 29;  // Tile ID 42 (Lanterns) -> Type 29=Boreal
                    Deco[S.Banner] = 2;    // Tile ID 91 (Banners) -> Type 2=Blue
                    Deco[S.DecoPlat] = 19; // Tile ID 19 (Plattforms) -> Type 19=Boreal
                    Deco[S.StylePaint] = 0;// no paint, leave boreal brown
                    Deco[S.HangingPot] = 5;// Tile ID 591 (PotsSuspended) -> Type 5=Blinkrot
                    Deco[S.Bookcase] = 25; // Tile ID 101 (Bookcases) -> Type 25=Boreal
                    Deco[S.Sofa] = 24;     // Tile ID 89 (Sofas) -> Type 24=Boreal
                    Deco[S.Clock] = 6;     // Tile ID 104 (GrandfatherClocks) -> Type 6=Boreal
                    Deco[S.Bed] = 24;      // Tile ID 79 (Beds) -> Type 24=Boreal
                    Deco[S.BedWallpaper] = WallID.StarlitHeavenWallpaper;
                    Deco[S.PaintingWallpaper] = WallID.LivingWood;
                    Deco[S.Dresser] = 18;  // Tile ID 88 (Dressers) -> Type 18=Boreal
                    Deco[S.Piano] = 23;    // Tile ID 87 (Pianos) -> Type 23=Boreal
                    break;

                case S.StyleBlueBrick: //TODO: look for another type of brick. It was recommended to use EbonstoneBrick on Steam, maybe also just red brick?
                    Deco[S.StyleSave] = S.StyleBlueBrick;
                    Deco[S.Brick] = TileID.BlueDungeonBrick;
                    Deco[S.RoofBrick] = TileID.BlueDungeonBrick;
                    Deco[S.Floor] = TileID.EbonstoneBrick;
                    if (Chance.Simple()) Deco[S.Floor] = TileID.MeteoriteBrick;
                    Deco[S.EvilTile] = TileID.Ebonstone;
                    Deco[S.BackWall] = WallID.Shadewood;
                    Deco[S.BackWallPaint] = PaintID.None;
                    Deco[S.CrookedWall] = WallID.Corruption3Echo;
                    Deco[S.WindowWall] = WallID.BlueStainedGlass;
                    Deco[S.WindowPaint] = PaintID.BluePaint;
                    Deco[S.DoorWall] = WallID.SpookyWood;

                    Deco[S.DoorPlat] = 16; // Tile ID 19 (Plattforms) -> Type 16=Spooky
                    Deco[S.DoorPlatPaint] = PaintID.DeepBluePaint;
                    Deco[S.Door] = TileID.TallGateClosed;
                    Deco[S.DoorPaint] = PaintID.RedPaint;
                    Deco[S.Chest] = 3;     // Tile ID 21 (Cests) -> Type 33=Shadow
                    Deco[S.Campfire] = 7;  // Tile ID 215 (Campfire) -> Type 0=Bone
                    Deco[S.Table] = 1;     // Tile ID 14 (Tables) -> Type 33=Ebonwood
                    Deco[S.Workbench] = 1; // Tile ID 18 (Workbenches) -> Type 1=Ebonwood
                    Deco[S.Chair] = 2;     // Tile ID 15 (Chairs) -> Type 2=Ebonwood
                    Deco[S.MainPainting] = 35;// Tile ID 240 (Painting3X3) -> Type 35=Rare Enchantment
                    Deco[S.Chandelier] = 32;// Tile ID 34 (Chandeliers) -> Type 32=Obsidian
                    Deco[S.Candelabra] = 2;// Tile ID 100 (Candelabras) -> Type 2=Ebonwood
                    Deco[S.Candle] = 5;    // Tile ID 33 (Candles) -> Type 5=Ebonwood
                    Deco[S.Lamp] = 23;     // Tile ID 93 (Lamps) -> Type 23=Obsidian
                    Deco[S.Torch] = 7;     // Tile ID 93 (Torches) -> Type 7=Demon
                    Deco[S.Lantern] = 2;   // Tile ID 42 (Lanterns) -> Type 2=Caged Lantern
                    Deco[S.Banner] = 0;    // Tile ID 91 (Banners) -> Type 0=Red
                    Deco[S.DecoPlat] = 19; // Tile ID 19 (Plattforms) -> Type 19=Boreal
                    Deco[S.StylePaint] = PaintID.GrayPaint;
                    Deco[S.HangingPot] = 6; // Tile ID 591 (PotsSuspended) -> Type 6=Corrupt Deathweed
                    Deco[S.Bookcase] = 7;  // Tile ID 101 (Bookcases) -> Type 7=Ebonwood
                    Deco[S.Sofa] = 2;      // Tile ID 89 (Sofas) -> Type 2=Ebonwood
                    Deco[S.Clock] = 10;    // Tile ID 104 (GrandfatherClocks) -> Type 10=Ebonwood
                    Deco[S.Bed] = 1;      // Tile ID 79 (Beds) -> Type 1=Ebonwood
                    Deco[S.BedWallpaper] = WallID.StarlitHeavenWallpaper;
                    Deco[S.PaintingWallpaper] = WallID.BluegreenWallpaper;
                    Deco[S.Dresser] = 1;  // Tile ID 88 (Dressers) -> Type 1=Ebonwood
                    Deco[S.Piano] = 1;    // Tile ID 87 (Pianos) -> Type 1=Ebonwood
                    //TODO: decide if everything obsidian / demon or ebonwood!
                    break;
            }
        }

        public void GenerateChastisedChurch(int generationSide)
        {
            if (!WorldGenMod.generateChastisedChurch) return;

            FillAndChooseStyle();

            int startPosX, startPosY;

            if      (generationSide == -1) startPosX =                  WorldGen.genRand.Next(50, 100); // left world side
            else if (generationSide ==  1) startPosX = Main.maxTilesX - WorldGen.genRand.Next(50, 100); // right world side
            else                           startPosX = 0;

            startPosY = Main.maxTilesY - 100;


            int totalTiles = 0;
            int maxTiles = Math.Min(Main.maxTilesX / 8, maxChurchLength);
            int actX, actY;
            bool leftDoor, rightDoor;
            Rectangle2P actRoom, lastRoom = Rectangle2P.Empty; // Rectangle2P for later filling a possible gap between the rooms

            while (totalTiles < maxTiles)
            {
                int roomWidth = WorldGen.genRand.Next(maxRoom.xmin, maxRoom.xmax + 1);
                if      (forceEvenRoom == 1) roomWidth -= (roomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) roomWidth -= (roomWidth % 2) + 1; //make room always uneven

                int roomHeight = WorldGen.genRand.Next(maxRoom.ymin, maxRoom.ymax + 1);

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
                        if      ((!doors[Door.Left].doorExist && x < freeR.X0))   WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true);
                        else if ((!doors[Door.Right].doorExist && x > freeR.X1))  WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true);
                        else                                                      WorldGen.PlaceTile(x, y, Deco[S.Floor], true, true);
                    }
                    else if (!freeR.Contains(x, y)) // x,y  are not in the free room? -> put outer wall bricks!
                    {
                        WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true);
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
                        WorldGen.PlaceWall(i, j, Deco[S.BackWall]);
                        if(Deco[S.BackWallPaint] > 0) WorldGen.paintWall(i, j, (byte)Deco[S.BackWallPaint]);
                    } 
                    else WorldGen.PlaceWall(i, j, Deco[S.CrookedWall]);
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
                            WorldGen.KillWall(i, j);

                            if (noBreakPoint1) awayEnough1 = true;
                            else               awayEnough1 = Vector2.Distance(new Vector2(i, j), wallBreakPoint1) > WorldGen.genRand.NextFloat(1f, 7f);

                            if (noBreakPoint2) awayEnough2 = true;
                            else               awayEnough2 = Vector2.Distance(new Vector2(i, j), wallBreakPoint2) > WorldGen.genRand.NextFloat(1f, 7f);


                            if ( awayEnough1 && awayEnough2) WorldGen.PlaceWall(i, j, Deco[S.DoorWall]);
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
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will later get a slope. Put the doorWallType there so it looks nicer

                x = leftDoorRect.X0;
                y = leftDoorRect.Y1 + 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // There is a one background wall tile missing here as this coordinates used to be on the border of the room. Adding this tile is not a big deal in the end, but little things matter!
            }

            if (rightDoor)
            {
                x = rightDoorRect.X0;
                y = rightDoorRect.Y0 - 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will later get a slope. Put the doorWallType there so it looks nicer

                x = rightDoorRect.X1;
                y = rightDoorRect.Y1 + 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // There is a one background wall tile missing here as this coordinates used to be on the border of the room. Adding this tile is not a big deal in the end, but little things matter!
            }

            if (doors[Door.Down].doorExist)
            {
                int j = downDoorRect.Y0;
                for (int i = downDoorRect.X0; i <= downDoorRect.X1; i++)
                {
                    WorldGen.PlaceTile(i, j, TileID.Platforms, mute: true, forced: true, style: Deco[S.DoorPlat]);
                }

                x = downDoorRect.X0 - 1;
                y = downDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = downDoorRect.X1 + 1;
                y = downDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer
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
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = upDoorRect.X1 + 1;
                y = upDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer
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
                            WorldGen.paintTile(x, y - i, (byte)Deco[S.DoorPaint]);
                        }
                    }



                    if (gap > 0 && doors[Door.Right].doorExist) // in case there is a gap between side rooms and this right side room also has a right door
                    {
                        x = room.X1;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed && Deco[S.DoorPaint] > PaintID.None)
                        {
                            for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint]);
                        }
                    }
                }

                else // rooms advancing from right to left: put right door
                {
                    bool placed;

                    x = room.X1; // left side rooms always have a right door
                    y = freeR.Y1 - (doorHeight - 1);
                    placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // right gate

                    if (placed && Deco[S.DoorPaint] > PaintID.None)
                    {
                        for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint]);
                    }



                    if (gap > 0 && doors[Door.Left].doorExist) // in case there is a gap between side rooms and this left side room also has a left door
                    {
                        x = room.X0;
                        placed = WorldGen.PlaceObject(x, y, TileID.TallGateClosed); // put another door (resulting in double doors)

                        if (placed && Deco[S.DoorPaint] > PaintID.None)
                        {
                            Func.GateTurn(x, y);
                            for (int i = 0; i < doorHeight; i++) WorldGen.paintTile(x, y + i, (byte)Deco[S.DoorPaint]);
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
                        WorldGen.PlaceTile(left, j, Deco[S.RoofBrick], true, true);
                    }

                    currentMultiplier = 1f - ((float)Math.Abs(right - rightHighest) / (float)rightDiff);
                    for (int j1 = 0; j1 < (int)(roofHeight * currentMultiplier); j1++)
                    {
                        int j = room.Y0 - 1 - j1;
                        WorldGen.PlaceTile(right, j, Deco[S.RoofBrick], true, true);
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


            //TODO: chest style
            if (Chance.Perc(20))
            {
                int chestStyle;
                if (!WorldGen.crimson) chestStyle = 43;
                else                   chestStyle = 46;
                //if (WorldGen.genRand.NextBool(3))
                //{
                //    chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: chestStyle);
                //    if (chest != -1) FillChest(Main.chest[chest], chestStyle);
                //}

                (bool success, int x, int y) placeResult;
                Rectangle2P area1 = new Rectangle2P(freeR.X0, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");
                placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.Containers, style: chestStyle, chance: 75); // Chest...
                if (placeResult.success)
                {
                    int chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                    if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2)); // ...with loot
                }
            }

            DecorateRoom(room, doors, wallBreak, belowCount);

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
            int x, y, chestID, unusedXTiles, num;


            // for window placement
            List<Rectangle2P> windows = new();
            int windowXTiles = 4;

            int windowYMargin = 2; // how many Tiles the window shall be away from the ceiling / floor
            int windowY0 = freeR.Y0 + windowYMargin; // height where the window starts
            int windowYTiles = freeR.YTiles - (2 * windowYMargin); // the YTiles height of a window

            bool awayEnough1, awayEnough2;


            //choose room decoration at random
            int roomDeco = WorldGen.genRand.Next(1); //TODO: don't forget to put the correct values in the end!

            roomDeco = 0;

            switch (roomDeco)
            {
                case 0: //
                    #region windows
                    windows.Clear();

                    // create window rectangles
                    if (freeR.YTiles > 8 && freeR.XTiles > 8)
                    {
                        if (freeR.XTiles <= 12) // narrow room, place window in the middle
                        {
                            int windowCenterOffset = (windowXTiles / 2) - 1 + (windowXTiles % 2); // to center the window at a specified x position

                            windows.Add(new Rectangle2P(freeR.XCenter - windowCenterOffset, windowY0, windowXTiles, windowYTiles));
                        }

                        else // symmetrical window pairs with spaces in between
                        {
                            int windowXMargin = 2; // how many tiles the outer windows-pair shall be away from the left / right wall
                            int windowDistanceXTiles = 4; // XTiles between two windows

                            int windowLeftX0 = freeR.X0 + windowXMargin; // init
                            int windowRightX0 = freeR.X1 - windowXMargin - (windowXTiles - 1); // init

                            while (windowLeftX0 + windowXTiles < freeR.XCenter)
                            {
                                windows.Add(new Rectangle2P(windowLeftX0, windowY0, windowXTiles, windowYTiles)); // left room side
                                windows.Add(new Rectangle2P(windowRightX0, windowY0, windowXTiles, windowYTiles)); // right room side

                                windowLeftX0  += (windowXTiles + windowDistanceXTiles);
                                windowRightX0 -= (windowXTiles + windowDistanceXTiles);
                            }
                        }
                    }

                    // put windows
                    if (windows.Count > 0 && belowCount == 0)
                    {
                        foreach (Rectangle2P windowRect in windows)
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
                                        WorldGen.PlaceWall(i, j, Deco[S.WindowWall]);
                                        WorldGen.paintWall(i, j, (byte)Deco[S.WindowPaint]);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    break;
                case 100: // empty room for display
                    //windows blueprint for copying
                    #region windows
                    windows.Clear();

                    // create window rectangles
                    if (freeR.YTiles > 8 && freeR.XTiles > 8)
                    {
                        if (freeR.XTiles <= 12) // narrow room, place window in the middle
                        {
                            int windowCenterOffset = (windowXTiles / 2) - 1 + (windowXTiles % 2); // to center the window at a specified x position

                            windows.Add(new Rectangle2P(freeR.XCenter - windowCenterOffset, windowY0, windowXTiles, windowYTiles));
                        }

                        else // symmetrical window pairs with spaces in between
                        {
                            int windowXMargin = 2; // how many tiles the outer windows-pair shall be away from the left / right wall
                            int windowDistanceXTiles = 4; // XTiles between two windows

                            int windowLeftX0 = freeR.X0 + windowXMargin; // init
                            int windowRightX0 = freeR.X1 - windowXMargin - (windowXTiles - 1); // init

                            while (windowLeftX0 + windowXTiles < freeR.XCenter)
                            {
                                windows.Add(new Rectangle2P(windowLeftX0, windowY0, windowXTiles, windowYTiles)); // left room side
                                windows.Add(new Rectangle2P(windowRightX0, windowY0, windowXTiles, windowYTiles)); // right room side

                                windowLeftX0 += (windowXTiles + windowDistanceXTiles);
                                windowRightX0 -= (windowXTiles + windowDistanceXTiles);
                            }
                        }
                    }

                    // put windows
                    if (windows.Count > 0 && belowCount == 0)
                    {
                        foreach (Rectangle2P windowRect in windows)
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
                                        WorldGen.PlaceWall(i, j, Deco[S.WindowWall]);
                                        WorldGen.paintWall(i, j, (byte)Deco[S.WindowPaint]);
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
        public const String RoofBrick = "RoofBrick";
        public const String Floor = "Floor";
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
