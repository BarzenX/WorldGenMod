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
using Terraria.GameContent.UI.States;
using Steamworks;
using System.Security.Policy;
using static Humanizer.In;
using MonoMod.Utils;

//TODO: - on small maps sometime the FrostFortress creates extreme slow - unknown reason

namespace WorldGenMod.Structures.Ice
{
    class FrostFortress : ModSystem
    {
        List<Vector2> fortresses = new();
        List<Point16> traps = new();
        readonly int gap = -1; // the horizontal gap between two side room columns
        readonly int wThick = 2; // the tickness of the outer walls and ceilings in code

        IDictionary<string, int> Deco = new Dictionary<string, int>(); // the dictionary where the styles of tiles are stored

        public override void PreWorldGen()
        {
            // in case of more than 1 world generated during a game
            fortresses.Clear();
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateFrostFortresses)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Lakes")); //used to be "Buried Chests", moved 1 step ahead of "Dungeon" because it was sometimes overlapping the dungeon
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Frost Fortress", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Building a snow fortress";

                    LocateAndGenerateFortresses();
                }));
            }

        }

        public void LocateAndGenerateFortresses()
        {
            int amount = (int)(Main.maxTilesX * 0.0004f);
            int amountGenerated = 0;

            while (amountGenerated < amount)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)GenVars.rockLayer, Main.maxTilesY - 200);
                Vector2 position = new(x, y); //init for later position search iteration

                List<int> allowedTiles = new()
                {
                    TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce, TileID.FleshIce
                };

                bool tooClose = true;
                while (Main.tile[(int)position.X, (int)position.Y] == null || !allowedTiles.Contains(Main.tile[(int)position.X, (int)position.Y].TileType) || tooClose)
                {
                    x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                    y = WorldGen.genRand.Next((int)GenVars.rockLayer, Main.maxTilesY - 200);
                    position = new Vector2(x, y);

                    tooClose = false;
                    foreach (Vector2 fort in fortresses)
                    {
                        if (fort.Distance(position) <= 125)
                        {
                            tooClose = true;
                        }
                    }
                }

                amountGenerated++;
                fortresses.Add(position);
                GenerateFortress(position.ToPoint16());
            }
        }

        public void FillAndChooseStyle()
        {
            // create dictionary entries
            Deco.Add(S.Brick, 0);
            Deco.Add(S.Floor, 0);
            Deco.Add(S.BackWall, 0);
            Deco.Add(S.DoorWall, 0);
            Deco.Add(S.DoorPlat, 0);
            Deco.Add(S.Door, 0);
            Deco.Add(S.Chest, 0);
            Deco.Add(S.Campfire, 0);
            Deco.Add(S.Table, 0);
            Deco.Add(S.Chandelier, 0);
            Deco.Add(S.Lamp, 0);
            Deco.Add(S.Torch, 0);
            Deco.Add(S.Banner, 0);

            //choose a random style and define it's types
            int chooseStyle = WorldGen.genRand.Next(3);
            switch (chooseStyle)
            {
                case 0:
                    Deco[S.Brick] = TileID.SnowBrick;
                    Deco[S.Floor] = TileID.IceBrick;
                    if (WorldGen.genRand.NextBool())   Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.SnowBrick;
                    Deco[S.DoorWall] = WallID.IceBrick;
                    Deco[S.DoorPlat] = 35; // Tile ID 19 (Plattforms) -> Type 35=Frozen
                    Deco[S.Door] = 27;     // Tile ID 10 (Doors) -> Type 27=Frozen (Closed)
                    Deco[S.Chest] = 11;    // Tile ID 21 (Cests) -> Type 11=Frozen
                    Deco[S.Campfire] = 3;  // Tile ID 215 (Campfire) -> Type 3=Frozen
                    Deco[S.Table] = 24;    // Tile ID 14 (Tables) -> Type 24=Frozen
                    Deco[S.Chandelier] = 11;// Tile ID 34 (Chandeliers) -> Type 11=Frozen
                    Deco[S.Lamp] = 5;      // Tile ID 93 (Lamps) -> Type 5=Frozen
                    Deco[S.Torch] = 9;     // Tile ID 93 (Torches) -> Type 9=Ice
                    Deco[S.Banner] = 2;    // Tile ID 91 (Banners) -> Type 2=Blue
                    break;

                case 1:
                    Deco[S.Brick] = TileID.BorealWood;
                    Deco[S.Floor] = TileID.GrayBrick;
                    if (WorldGen.genRand.NextBool())   Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.BorealWood;
                    Deco[S.DoorWall] = WallID.BorealWoodFence;
                    Deco[S.DoorPlat] = 28; // Tile ID 19 (Plattforms) -> Type 28=Granite
                    Deco[S.Door] = 15;     // Tile ID 10 (Doors) -> Type 15=Iron (Closed)
                    Deco[S.Chest] = 33;    // Tile ID 21 (Cests) -> Type 33=Boreal
                    Deco[S.Campfire] = 0;  // Tile ID 215 (Campfire) -> Type 0=Normal
                    Deco[S.Table] = 28;    // Tile ID 14 (Tables) -> Type 33=Boreal
                    Deco[S.Chandelier] = 25;// Tile ID 34 (Chandeliers) -> Type 25=Boreal
                    Deco[S.Lamp] = 20;     // Tile ID 93 (Lamps) -> Type 20=Boreal
                    Deco[S.Torch] = 9;     // Tile ID 93 (Torches) -> Type 9=Ice
                    Deco[S.Banner] = 2;    // Tile ID 91 (Banners) -> Type 2=Blue
                    break;

                case 2:
                    Deco[S.Brick] = TileID.LeadBrick;
                    Deco[S.Floor] = TileID.EbonstoneBrick;
                    //TODO: find something     if (WorldGen.genRand.NextBool())   Deco[Style.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.BlueDungeonSlab;
                    Deco[S.DoorWall] = WallID.Bone;
                    Deco[S.DoorPlat] = 43; // Tile ID 19 (Plattforms) -> Type 43=Stone
                    Deco[S.Door] = 16;     // Tile ID 10 (Doors) -> Type 16=Blue Dungeon (Closed)
                    Deco[S.Chest] = 3;     // Tile ID 21 (Cests) -> Type 33=Shadow
                    Deco[S.Campfire] = 7;  // Tile ID 215 (Campfire) -> Type 0=Bone
                    Deco[S.Table] = 1;     // Tile ID 14 (Tables) -> Type 33=Ebonwood Table
                    Deco[S.Chandelier] = 32;// Tile ID 34 (Chandeliers) -> Type 32=Obsidian
                    Deco[S.Lamp] = 23;     // Tile ID 93 (Lamps) -> Type 23=Obsidian
                    Deco[S.Torch] = 7;     // Tile ID 93 (Torches) -> Type 7=Demon
                    Deco[S.Banner] = 0;    // Tile ID 91 (Banners) -> Type 0=Red
                    break;
            }
            
            //more collection:
            // Tile ID 93 (Lamps) -> Type 20=Boreal
            // Tile ID 91 (Banners) -> Type 2=Blue
            // Tile ID 34 (Chandeliers) -> Type 4=Tungsten
            // Tile ID 240 (Paintings) -> Type 35=Crowno Devours His Lunch
            // Tile ID 574 -> Boreal Beam
            // Tile ID 51 -> Cob web
        }

        public void GenerateFortress(Point16 MainRoomPos)
        {
            if (!WorldGenMod.generateFrostFortresses)
            {
                return;
            }

            Deco.Clear();
            traps.Clear();

            FillAndChooseStyle();
            
            //init for variables for later filling the gap between the rooms with bricks
            Rectangle2P mainRoom;
            Rectangle2P previousRoom;
            Rectangle2P actualSideRoom;
            Rectangle2P actualVerticalRoom;
            int previousHighestY;
            int previousLowestY;
            int actualHighestY = 0;
            int actualLowestY = 0;


            // generate the main room
            int initialRoomSizeX = 31; //an odd number, so there will be a middle tile
            int initialRoomSizeY = 20;
            mainRoom = GenerateRoom(room: new Rectangle2P(MainRoomPos.X - initialRoomSizeX / 2, MainRoomPos.Y - initialRoomSizeY, initialRoomSizeX, initialRoomSizeY),
                                                            roomType: RoomID.MainRoom,
                                                            leftDoor: true,
                                                            rightDoor: true,
                                                            upDoor: false,
                                                            downDoor: false);

            previousRoom = mainRoom;
            previousHighestY = MainRoomPos.Y - initialRoomSizeY; //for later filling the gap between the rooms with bricks
            previousLowestY = MainRoomPos.Y; //for later filling the gap between the rooms with bricks



            




            // generate rooms to the right of the main room
            //TODO: after each GenerateRoom do the room decoration?
            int currentPlusX = (initialRoomSizeX / 2) + (initialRoomSizeX % 2) + gap;
            int sideRoomCount = WorldGen.genRand.Next(2, 6); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 0; i <= sideRoomCount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                if (currentRoomWidth % 2 == 1) currentRoomWidth++; //make room width always even, so the up/down doors are centered in the room
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool(); // if rooms above this main-column-room shall be generated
                bool generateDown = WorldGen.genRand.NextBool(); // if rooms below this main-column-room shall be generated

                // create main room of this column
                actualSideRoom = GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX, MainRoomPos.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight),
                                              roomType: RoomID.SideRight,
                                              leftDoor: true,
                                              rightDoor: i != sideRoomCount,
                                              upDoor: generateUp,
                                              downDoor: generateDown);

                int currentPlusY = -currentRoomHeight + wThick; //init
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(3); //number of rooms above this main-column room
                    for (int j = 0; j <= vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/; //TODO: what happens if the room height gets randomized?
                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                                          roomType: RoomID.AboveSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: j != vertAmount,
                                                          downDoor: true);

                        actualHighestY = actualVerticalRoom.Y0;
                        currentPlusY -= vertRoomHeight - wThick;
                    }
                }
                else actualHighestY = actualSideRoom.Y0;

                currentPlusY = currentRoomHeight - wThick;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(3); //number of rooms below this main-column room
                    for (int j = 0; j <= vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                                          roomType: RoomID.BelowSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: true,
                                                          downDoor: j != vertAmount);

                        actualLowestY = actualVerticalRoom.Y1;
                        currentPlusY += vertRoomHeight - wThick;
                    }
                }
                else actualLowestY = actualSideRoom.Y1;


                currentPlusX += currentRoomWidth + gap;

                if (gap > 0) FillGap(previousRoom, actualSideRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);
                previousRoom = actualSideRoom;
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }







            // generate rooms to the left of the main room
            previousRoom = mainRoom;
            previousHighestY = MainRoomPos.Y - initialRoomSizeY; //for later filling the gap between the rooms with bricks
            previousLowestY = MainRoomPos.Y; //for later filling the gap between the rooms with bricks

            currentPlusX = -(initialRoomSizeX / 2) - gap;
            sideRoomCount = WorldGen.genRand.Next(2, 6); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 0; i <= sideRoomCount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                if (currentRoomWidth % 2 == 1) currentRoomWidth++; //make room width always even, so the up/down doors are centered in the room
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool(); // if rooms above this main-column-room shall be generated
                bool generateDown = WorldGen.genRand.NextBool(); // if rooms below this main-column-room shall be generated

                actualSideRoom = GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight),
                                              roomType: RoomID.SideLeft,
                                              leftDoor: i != sideRoomCount,
                                              rightDoor: true,
                                              upDoor: generateUp,
                                              downDoor: generateDown);

                int currentPlusY = -currentRoomHeight + wThick;
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(3);
                    for (int j = 0; j <= vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/;
                        GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: RoomID.AboveSide,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: j != vertAmount,
                                     downDoor: true);

                        actualHighestY = MainRoomPos.Y - vertRoomHeight + currentPlusY;
                        currentPlusY -= vertRoomHeight - wThick;
                    }
                }
                else actualHighestY = actualSideRoom.Y0;

                currentPlusY = currentRoomHeight - wThick;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(3);
                    for (int j = 0; j <= vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        GenerateRoom(room: new Rectangle2P(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: RoomID.BelowSide,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: true,
                                     downDoor: j != vertAmount);

                        actualLowestY = MainRoomPos.Y + currentPlusY;
                        currentPlusY += vertRoomHeight - wThick;
                    }
                }
                else actualLowestY = actualSideRoom.Y1;

                currentPlusX -= currentRoomWidth + gap;

                if (gap > 0) FillGap(previousRoom, actualSideRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);
                previousRoom = actualSideRoom;
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }


            int[] allowedTraps = new int[]
                {
                    0, //darts
                    //1, //darts and a boulder? -> I don't want boulders!
                    //2, //dynamite -> I don't want it!
                    3  //geysirs
                };
            foreach (Point16 trap in traps)
            {
                WorldGen.placeTrap(trap.X, trap.Y, allowedTraps[WorldGen.genRand.Next(allowedTraps.Length)]);
            }
        }

        /// <summary>
        /// Creates a room with the given dimensions and places doors on the sides and platforms above / below if activated
        /// </summary>
        /// <param name="room">The dimensions of the room. X and Y define the top left corner of the room</param>
        /// <param name="roomType">The type of the to-be-created room for some specific generation needs: <br/> 0=main room, 1=side room right, -1=side room left, 2=room above side room, -2=room below side room   --> use "RoomID."</param>
        /// <param name="leftDoor">If a door to the left shall be created</param>
        /// <param name="rightDoor">If a door to the right shall be created</param>
        /// <param name="upDoor">If a platform in the ceiling shall be created</param>
        /// <param name="downDoor">If a platform in the floor shall be created</param>
        /// 
        /// <returns>Hands back the room dimensions input</returns>
        public Rectangle2P GenerateRoom(Rectangle2P room, int roomType, bool leftDoor = false, bool rightDoor = false, bool upDoor = false, bool downDoor = false)
        {
            Rectangle2P hollowRect = room; // the "hollow" room.... e.g. without the wall bricks
            hollowRect.X0 += wThick;
            hollowRect.Y0 += wThick;
            hollowRect.X1 -= wThick;
            hollowRect.Y1 -= wThick;

            int x; //temp variable for later calculations;
            int y; //temp variable for later calculations;

            bool noBreakPoint = WorldGen.genRand.NextBool(); //force the background wall of the room to have no holes
            Vector2 wallBreakPoint = new(room.X0 + WorldGen.genRand.Next(room.XDiff), room.Y0 + WorldGen.genRand.Next(room.YDiff));


            // create door rectangles
            List<Rectangle2P> doors = new();

            int leftRightDoorsYTiles = 3; //how many tiles the left and right doors are high
            y = hollowRect.Y1 - (leftRightDoorsYTiles - 1);
            Rectangle2P leftDoorRect = new(room.X0, y, wThick, leftRightDoorsYTiles);
            Rectangle2P rightDoorRect = new(hollowRect.X1 + 1, y, wThick, leftRightDoorsYTiles);

            int upDownDoorXTiles = 4; // how many tiles the up and down doors are wide
            if (hollowRect.XTiles % 2 == 1)   upDownDoorXTiles++; // an odd number of x-tiles in the room also requires an odd number of platforms so the door is symmetrical
            x = (hollowRect.X0 + hollowRect.X1) / 2 - (upDownDoorXTiles / 2 - 1);
            Rectangle2P upDoorRect = new(x, room.Y0, upDownDoorXTiles, wThick);
            Rectangle2P downDoorRect = new(x, hollowRect.Y1 + 1, upDownDoorXTiles, wThick);

            if (leftDoor) doors.Add(leftDoorRect);
            if (rightDoor) doors.Add(rightDoorRect);
            if (upDoor) doors.Add(upDoorRect);
            if (downDoor) doors.Add(downDoorRect);



            // Create room frame & background wall
            bool lastLeftSideRoom = (roomType == RoomID.SideLeft && !leftDoor);
            bool lastRightSideRoom = (roomType == RoomID.SideRight && !rightDoor);
            bool lastSideRoom = lastLeftSideRoom || lastRightSideRoom; //this side room is the last one on this side
            for (int i = room.X0; i <= room.X1; i++)
            {
                for (int j = room.Y0; j <= room.Y1; j++)
                {
                    WorldGen.EmptyLiquid(i, j);


                    if (i > room.X0 && i < room.X1 && j > room.Y0 && j < room.Y1 && // leave 1 tile distance from the sides (so the background won't overlap to the outside)
                         (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f) || noBreakPoint)) // make here and there some cracks in the background to let it look more "abandoned"
                    {
                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, Deco[S.BackWall]);
                    }

                    if ( (j == hollowRect.Y1 + 1) && // the height of this rooms floor
                          ( ((i >= hollowRect.X0 && i <= hollowRect.X1) || ((roomType == RoomID.SideLeft && !lastSideRoom) || roomType == RoomID.MainRoom || (roomType == RoomID.SideRight && !lastSideRoom)) ) ||  //main rooms and side rooms need the floor on the room frame, up/down rooms mustn't
                            (i >= hollowRect.X0 && lastLeftSideRoom) ||// the last side room mustn't have the floor on the outer wall
                            (i <= hollowRect.X1 && lastRightSideRoom) ) ) // the last side room mustn't have the floor on the outer wall
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Floor], true, true);
                    }
                    else if (j == room.Y0 && // the height of this rooms topmost ceiling row
                             roomType == RoomID.BelowSide) // down-rooms have the floor type of the above room laying at this height
                    {
                        continue; // don't override anything.
                    }
                    else if (i < hollowRect.X0 || i > hollowRect.X1 || j < hollowRect.Y0 || j > hollowRect.Y1 + 1)
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Brick], true, true); // place the outer wall bricks of the room
                    }
                    else WorldGen.KillTile(i, j); //carve out the inside of the room
                }
            }

            #region Doors
            //carve out doors
            if (doors.Count != 0)
            {
                foreach (Rectangle2P doorRect in doors)
                {
                    for (int i = doorRect.X0; i <= doorRect.X1; i++)
                    {
                        for (int j = doorRect.Y0; j <= doorRect.Y1; j++)
                        {
                            WorldGen.KillTile(i, j);
                            WorldGen.KillWall(i, j);
                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f) || noBreakPoint) WorldGen.PlaceWall(i, j, Deco[S.DoorWall]);
                        }
                    }
                }
            }

            // place every door according to it's roomy type
            if (leftDoor)
            {
                x = leftDoorRect.X1;
                y = leftDoorRect.Y0 - 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

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
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = rightDoorRect.X1;
                y = rightDoorRect.Y1 + 1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // There is a one background wall tile missing here as this coordinates used to be on the border of the room. Adding this tile is not a big deal in the end, but little things matter!
            }

            if (downDoor)
            {
                int j = downDoorRect.Y0;
                for (int i = downDoorRect.X0; i <= downDoorRect.X1; i++)
                {
                    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: Deco[S.DoorPlat]);
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

            if (upDoor)
            {
                int j = upDoorRect.Y0;
                for (int i = upDoorRect.X0; i <= upDoorRect.X1; i++)
                {
                    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: Deco[S.DoorPlat]);
                }

                x = upDoorRect.X0 - 1;
                y = upDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer

                x = upDoorRect.X1 + 1;
                y = upDoorRect.Y1;
                WorldGen.KillWall(x, y);
                WorldGen.PlaceWall(x, y, Deco[S.DoorWall]); // the corner of the door will get a slope. Put the doorWallType there so it looks nicer
            }
            #endregion

            #region Slopes
            // if one would form a rhombus: 0 is no slope, 1 is up-right corner, 2 is up-left corner, 3 is down-right corner, 4 is down-left corner.
            if (leftDoor)
            {
                WorldGen.SlopeTile(leftDoorRect.X1, leftDoorRect.Y0 - 1, 3); // door right corner
            }
            if (rightDoor)
            {
                WorldGen.SlopeTile(rightDoorRect.X0, rightDoorRect.Y0 - 1, 4); // door left corner
            }
            if (upDoor)
            {
                WorldGen.SlopeTile(upDoorRect.X0 - 1, upDoorRect.Y1, 3); // updoor left corner
                WorldGen.SlopeTile(upDoorRect.X1 + 1, upDoorRect.Y1, 4); // updoor right corner
            }
            if (downDoor)
            {
                WorldGen.SlopeTile(downDoorRect.X0 - 1, downDoorRect.Y1, 3); // updoor left corner
                WorldGen.SlopeTile(downDoorRect.X1 + 1, downDoorRect.Y1, 4); // updoor right corner
            }
            #endregion

            //TODO: do decoration in separate method
            DecorateRoom(room: room,
                         roomType: roomType,
                         leftDoor: leftDoor,
                         rightDoor: rightDoor,
                         upDoor: upDoor,
                         downDoor: downDoor);


            //int decoration = WorldGen.genRand.Next(4);
            //int chest = -1;
            //switch (decoration)
            //{
            //    default:
            //        break;
            //    case 0:
            //        if (WorldGen.genRand.NextBool())
            //        {
            //            chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: 4);
            //            if (chest != -1) FillChest(Main.chest[chest], 4);
            //        }
            //        else
            //        {
            //            chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: defaultChestType);
            //            if (chest != -1) FillChest(Main.chest[chest], defaultChestType);
            //        }
            //        break;
            //    case 1:
            //        chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: defaultChestType);
            //        if (chest != -1) FillChest(Main.chest[chest], defaultChestType);
            //        break;
            //    case 2:
            //        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Campfire, style: defaultCampfireType);
            //        break;
            //    case 3:
            //        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Tables, style: defaultTableType);
            //        break;
            //}

            //if (WorldGen.genRand.NextBool())
            //{
            //    int statue = WorldGen.genRand.Next(6);
            //    switch (statue)
            //    {
            //        case 0:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 27);
            //            break;
            //        case 1:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 32);
            //            break;
            //        case 2:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 33);
            //            break;
            //        case 3:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 35);
            //            break;
            //        case 4:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 37);
            //            break;
            //        case 5:
            //            WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 68);
            //            break;
            //    }
            //}

            traps.Add(new Point16(room.X0 + WorldGen.genRand.Next(room.XDiff), room.Y0 + WorldGen.genRand.Next(room.YDiff)));

            return room;
        }

        /// <summary>
        /// Shifting a side room of the main room some blocks, leaves a gap that has to be filled.
        /// This is also the location where the door should be placed (will be placed in the middle of the gap)
        /// </summary>
        /// <param name="previousRoom">The rectangle of the previous created side room</param>
        /// <param name="actualRoom">The rectangle of the just created side room</param>
        /// <param name="previousHighestY">The topmost Y coordinate of the previous room column</param>
        /// <param name="actualHighestY">The topmost Y coordinate of the actual room column</param>
        /// <param name="previousLowestY">The bottommost Y coordinate of the previous room column</param>
        /// <param name="actualLowestY">The bottommost Y coordinate of the actual room column</param>
        public void FillGap(Rectangle2P previousRoom, Rectangle2P actualRoom, int previousHighestY, int actualHighestY, int previousLowestY, int actualLowestY)
        {

            Rectangle2P gap = new(0, 0, 0, 0); //init

            //TODO: where to put PlaceWall at x-1 and x+1 of the gap?, as these will be left out when generating the room

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
            
            //second step: find out which room column reaches less farther up (to define Y of the gap)
            if (previousHighestY > actualHighestY)    gap.Y0 = previousHighestY;
            else    gap.Y0 = actualHighestY;

            //third step: find out which room column reaches less farther down (to define height of the gap)
            if (previousLowestY > actualLowestY)   gap.Y1 = actualLowestY;
            else   gap.Y1 = previousLowestY;



            //fill gap
            for (int i = gap.X0; i <= gap.X1; i++)
            {
                for (int j = gap.Y0; j <= gap.Y1; j++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.EmptyLiquid(i, j);

                    if (j == previousRoom.Y1 - (wThick - 1)) //doesn't matter if previousRoom or actualRoom, because the floor is at the same height
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Floor], true, true);
                        WorldGen.PlaceWall(i, j, Deco[S.BackWall]); //put the designated background wall
                    }
                    else if (j >= previousRoom.Y1 - wThick - 2 && j <= previousRoom.Y1 - wThick ) // a left or a right door
                    {
                        WorldGen.KillTile(i, j); //leave the "door" free

                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, Deco[S.DoorWall]); //put the designated background wall
                    }
                    else
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Brick], true, true); //fill gap with bricks
                    }
                }
            }
        }

        public void DecorateRoom(Rectangle2P room, int roomType, bool leftDoor = false, bool rightDoor = false, bool upDoor = false, bool downDoor = false)
        {
            Rectangle2P freeR = room; // the "free" room.... e.g. without the wall bricks
            freeR.X0 += wThick;
            freeR.Y0 += wThick;
            freeR.X1 -= wThick;
            freeR.Y1 -= wThick;

            int x = freeR.X0 + freeR.XDiff / 2 - freeR.XDiff % 2; // middle tile
            int y = freeR.Y1;

            #region Decorate Main Room
            if (roomType == RoomID.MainRoom)
            {
                //build throne podest
                for (int i = x - 4; i <= x + 4; i++)
                {
                    WorldGen.PlaceTile(i, y, Deco[S.Floor], true, true);
                }
                for (int i = x - 2; i <= x + 2; i++)
                {
                    WorldGen.PlaceTile(i, y - 1, Deco[S.Floor], true, true);
                }

                WorldGen.PlaceTile(x - 4, y - 1, TileID.Statues, style: 0); //Armor statue

                WorldGen.PlaceTile(x + 3, y - 1, TileID.Statues, style: 0); //Armor statue

                WorldGen.PlaceTile(x, y - 2, TileID.Thrones, style: 0); //Throne

                WorldGen.PlaceTile(x - 2, y - 2, TileID.GoldCoinPile, style: 0); //Gold Coins
                WorldGen.PlaceTile(x - 2, y - 3, TileID.SilverCoinPile, style: 0); //Silver Coins
                WorldGen.PlaceTile(x + 2, y - 2, TileID.GoldCoinPile, style: 0); //Gold Coins


                WorldGen.PlaceTile(x - 5, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                WorldGen.PlaceTile(x - 6, y, TileID.Lamps, style: Deco[S.Lamp]); //Boreal Wood Lamp
                WorldGen.PlaceTile(x + 6, y, TileID.Lamps, style: Deco[S.Lamp]); //Boreal Wood Lamp

                //left side coins
                if (WorldGen.genRand.NextBool())
                {
                    WorldGen.PlaceTile(x - 8, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                    WorldGen.PlaceTile(x - 9, y, TileID.GoldCoinPile, style: 0); //Silver Coins
                }
                else
                {
                    WorldGen.PlaceSmallPile(x - WorldGen.genRand.Next(7, 12), y, WorldGen.genRand.Next(16, 18), 1); //Copper or Silver coin stash
                }


                //right side coins
                if (WorldGen.genRand.NextBool())
                {
                    WorldGen.PlaceTile(x + 8, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                    WorldGen.PlaceTile(x + 9, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                    WorldGen.PlaceTile(x + 10, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                }
                else
                {
                    WorldGen.PlaceSmallPile(x + WorldGen.genRand.Next(7, 12), y, WorldGen.genRand.Next(16, 18), 1); //Copper or Silver coin stash
                }




                //beams
                for (x = freeR.X0; x <= freeR.X1; x++)
                {
                    if (x < freeR.X0 + 12 || x > freeR.X1 - 12) //leave space for the picture
                    {
                        WorldGen.PlaceTile(x, freeR.Y0 + 4, TileID.BorealBeam);
                        WorldGen.PlaceTile(x, freeR.Y0 + 6, TileID.BorealBeam);
                    }
                }

                //picture
                for (x = freeR.X0 + 12; x <= freeR.X1 - 12; x++)
                {
                    WorldGen.PlaceWall(x, freeR.Y0 + 4, Deco[S.BackWall]); //just in case it got deleted by the "cracked" background design
                    WorldGen.PlaceWall(x, freeR.Y0 + 5, Deco[S.BackWall]);
                    WorldGen.PlaceWall(x, freeR.Y0 + 6, Deco[S.BackWall]);
                }
                WorldGen.PlaceTile(freeR.X0 + 13, freeR.Y0 + 5, TileID.Painting3X3, style: 34);

                //banners
                y = freeR.Y0;
                WorldGen.PlaceTile(freeR.X0    , y, TileID.Banners, style: Deco[S.Banner]);
                WorldGen.PlaceTile(freeR.X0 + 9, y, TileID.Banners, style: Deco[S.Banner]);
                WorldGen.PlaceTile(freeR.X1    , y, TileID.Banners, style: Deco[S.Banner]);
                WorldGen.PlaceTile(freeR.X1 - 9, y, TileID.Banners, style: Deco[S.Banner]);

                //floating blocks in the room
                x = freeR.X0 + 5;
                y = freeR.Y0 + 7;
                WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true); //put a brick to place the banner to it
                WorldGen.PlaceTile(x, y+1, TileID.Banners, style: Deco[S.Banner]); //banner
                WorldGen.PlaceTile(x - 1, y, TileID.Torches, style: Deco[S.Torch]); //torch
                WorldGen.PlaceTile(x + 1, y, TileID.Torches, style: Deco[S.Torch]); //torch

                x = freeR.X1 - 5;
                WorldGen.PlaceTile(x, y, Deco[S.Brick], true, true); //put a brick to place the banner to it
                WorldGen.PlaceTile(x, y + 1, TileID.Banners, style: Deco[S.Banner]); // banner
                WorldGen.PlaceTile(x - 1, y, TileID.Torches, style: Deco[S.Torch]); //torch
                WorldGen.PlaceTile(x + 1, y, TileID.Torches, style: Deco[S.Torch]); //torch

                // lighting
                WorldGen.PlaceTile(freeR.X0 + 13, freeR.Y0, TileID.Chandeliers, style: Deco[S.Chandelier]); //boreal chandelier

                y = freeR.Y1 - 4;
                WorldGen.PlaceTile(freeR.X0, y, TileID.Torches, style: Deco[S.Torch]); //torch
                WorldGen.PlaceTile(freeR.X1, y, TileID.Torches, style: Deco[S.Torch]); //torch

                // Tile ID 93 (Lamps) -> Type 20=Boreal
                // Tile ID 10 (Doors) -> Type 15=Iron (Closed)
                // Tile ID 91 (Banners) -> Type 2=Blue
                // Tile ID 34 (Chandeliers) -> Type 4=Tungsten
                // Tile ID 240 (Paintings3x3) -> Type 34=Crowno Devours His Lunch
                // Tile ID 574 -> Boreal Beam
                // Tile ID 51 -> Cob web
                PlaceCobWeb(freeR);
            }
            #endregion

            if (roomType == RoomID.SideLeft)
            {
                x = room.X1; // left side rooms always have a right door
                y = freeR.Y1;
                WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); //Door

                PlaceCobWeb((freeR.X0 + freeR.X1)/2, (freeR.Y0 + freeR.Y1) / 2, 6, 3, false);
            }

            if (roomType == RoomID.SideRight)
            {
                x = room.X0; // right side rooms always have a left door
                y = freeR.Y1;
                WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); //Door
            }
        }

        public void FillChest(Chest chest, int style)
        {
            int nextItem = 0;

            int mainItem = 0;
            int potionItem = 0;
            int lightItem = 0;
            int ammoItem = 0;

            switch (WorldGen.genRand.Next(4))
            {
                case 0:
                    mainItem = ItemID.IceBlade;
                    if (style == 4) mainItem = ItemID.Frostbrand;
                    break;
                case 1:
                    mainItem = ItemID.SnowballCannon;
                    if (style == 4) mainItem = ItemID.IceBow;
                    break;
                case 2:
                    mainItem = ItemID.SapphireStaff;
                    if (style == 4) mainItem = ItemID.FlowerofFrost;
                    break;
                case 3:
                    mainItem = ItemID.FlinxStaff;
                    if (style == 4) mainItem = ItemID.IceRod;
                    break;
            }

            switch (WorldGen.genRand.Next(4))
            {
                case 0:
                    potionItem = ItemID.SwiftnessPotion;
                    if (style == 4) potionItem = ItemID.RagePotion;
                    break;
                case 1:
                    potionItem = ItemID.IronskinPotion;
                    if (style == 4) potionItem = ItemID.WrathPotion;
                    break;
                case 2:
                    potionItem = ItemID.RegenerationPotion;
                    if (style == 4) potionItem = ItemID.LifeforcePotion;
                    break;
                case 3:
                    potionItem = ItemID.SummoningPotion;
                    if (style == 4) potionItem = ItemID.SummoningPotion;
                    break;
            }

            switch (WorldGen.genRand.Next(4))
            {
                case 0:
                    lightItem = ItemID.IceTorch;
                    break;
                case 1:
                    lightItem = ItemID.Glowstick;
                    break;
                case 2:
                    lightItem = ItemID.FairyGlowstick;
                    break;
                case 3:
                    lightItem = ItemID.SpelunkerGlowstick;
                    break;
            }


            switch (WorldGen.genRand.Next(3))
            {
                case 0:
                    ammoItem = ItemID.FrostburnArrow;
                    break;
                case 1:
                    ammoItem = ItemID.FrostDaggerfish;
                    break;
                case 2:
                    ammoItem = ItemID.Snowball;
                    break;
            }

            chest.item[nextItem].SetDefaults(mainItem);
            chest.item[nextItem].stack = 1;
            nextItem++;

            chest.item[nextItem].SetDefaults(potionItem);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 3);
            nextItem++;

            chest.item[nextItem].SetDefaults(lightItem);
            chest.item[nextItem].stack = WorldGen.genRand.Next(6, 13);
            nextItem++;

            chest.item[nextItem].SetDefaults(ammoItem);
            chest.item[nextItem].stack = WorldGen.genRand.Next(25, 75);
            nextItem++;

            chest.item[nextItem].SetDefaults(ItemID.GoldCoin);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 3);
            if (style == 4) chest.item[nextItem].stack = WorldGen.genRand.Next(6, 20);
        }

        /// <summary>
        /// Places patches of CobWeb in an rectangular space and adds some randomness on the edges.
        /// <br/>CobWebs are only placed on "free" tiles, where there are no other tiles present.
        /// </summary>
        /// <param name="area">The rectangle where CobWeb shall be placed</param>
        public void PlaceCobWeb(Rectangle2P area)
        {
            for (int x = area.X0; x <= area.X1; x++)
            {
                for (int y = area.Y0; y <= area.Y1; y++)
                {
                    if (!Main.tile[x, y].HasTile) //first the fast query
                    {
                        WorldGen.PlaceTile(x, y, TileID.Cobweb); //TODO:Add randomness
                    }
                }
            }
        }

        /// <summary>
        /// Places patches of CobWeb in an ellipsoid space and adds some randomness on the edges.
        /// <br/>CobWebs are only placed on "free" tiles, where there are no other tiles present.
        /// </summary>
        /// <param name="x0">Center x-coordinate of the CobWeb patch</param>
        /// <param name="y0">Center y-coordinate of the CobWeb patch</param>
        /// <param name="xRadius">Radius (written in tiles) in x-direction of the CobWeb patch</param>
        /// <param name="yRadius">Radius (written in tiles) in y-direction of the CobWeb patch</param>
        /// <param name="includeBorder">whether the border of the ellipse shall get CobWeb placed or not</param>
        public void PlaceCobWeb(int x0, int y0, int xRadius, int yRadius, bool includeBorder)
        {
            Ellipse CobWebs = new Ellipse(xCenter: x0, yCenter: y0, xRadius: xRadius, yRadius: yRadius);
            Rectangle2P overall = new Rectangle2P(x0 - xRadius, y0 - yRadius, x0 + xRadius, y0 + yRadius, "dummy"); // the rectangle exactly covering the ellipse
            
            for (int x = overall.X0; x <= overall.X1; x++) 
            {
                for(int y = overall.Y0; y <= overall.Y1; y++) 
                {
                    if ( !Main.tile[x, y].HasTile ) //first the fast query
                    {
                        if ( CobWebs.Contains(x, y, includeBorder) ) //then the more compute-heavy check
                        {
                            WorldGen.PlaceTile(x, y, TileID.Cobweb); //TODO:Add randomness
                        }
                    }
                }
            }
        }
    }

    internal class RoomID
    {
        public const short MainRoom = 0;
        public const short SideRight = 1;
        public const short SideLeft = -1;
        public const short AboveSide = 2;
        public const short BelowSide = -2;

    }

    internal class S  //Style
    {
        public const String Brick = "Brick";
        public const String Floor = "Floor";
        public const String BackWall = "BackWall";
        public const String DoorWall = "DoorWall";
        public const String DoorPlat = "DoorPlattform";
        public const String Door = "Door";
        public const String Chest = "Chest";
        public const String Campfire = "Campfire";
        public const String Table = "Table";
        public const String Chandelier = "Chandelier";
        public const String Lamp = "Lamp";
        public const String Torch = "Torch";
        public const String Banner = "Banner";
    }
}
