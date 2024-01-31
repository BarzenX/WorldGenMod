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

//TODO: - on small maps sometime the FrostFortress creates extreme slow - unknown reason

namespace WorldGenMod.Systems.Structures.Caverns
{
    class FrostFortress : ModSystem
    {
        List<Vector2> fortresses = new();
        List<Point16> traps = new();

        public override void PreWorldGen()
        {
            // in case of more than 1 world generated during a game
            fortresses.Clear();
            traps.Clear();
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
                int y = WorldGen.genRand.Next((int)Terraria.WorldBuilding.GenVars.rockLayer, Main.maxTilesY - 200);
                Vector2 position = new(x, y); //init for later position search iteration

                List<int> allowedTiles = new()
                {
                    TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce, TileID.FleshIce
                };

                bool tooClose = true;
                while (Main.tile[(int)position.X, (int)position.Y] == null || !allowedTiles.Contains(Main.tile[(int)position.X, (int)position.Y].TileType) || tooClose)
                {
                    x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                    y = WorldGen.genRand.Next((int)Terraria.WorldBuilding.GenVars.rockLayer, Main.maxTilesY - 200);
                    position = new Vector2(x, y);

                    tooClose = false;
                    foreach(Vector2 fort in fortresses)
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

        int brickType;
        int floorType;
        int wallType;
        int doorWallType;
        int doorPlattformType;
        int defaultChestType;
        int defaultCampfireType;
        int defaultTableType;
        public void GenerateFortress(Point16 MainRoomPos)
        {
            if (!WorldGenMod.generateFrostFortresses)
            {
                return;
            }

            traps.Clear();
            int initialRoomSizeX = 30;
            int initialRoomSizeY = 20;

            int tileTypes = WorldGen.genRand.Next(2);
            switch (tileTypes)
            {
                case 0:
                    brickType = TileID.SnowBrick;
                    floorType = TileID.IceBrick;
                    if (WorldGen.genRand.NextBool()) floorType = TileID.AncientSilverBrick;
                    wallType = WallID.SnowBrick;
                    doorWallType = WallID.IceBrick;
                    doorPlattformType = 35; // 19 Plattforms -> 35=Frozen
                    defaultChestType = 11;
                    defaultCampfireType = 3;
                    defaultTableType = 24;
                    break;
                case 1:
                    brickType = TileID.BorealWood;
                    floorType = TileID.GrayBrick;
                    if (WorldGen.genRand.NextBool())   floorType = TileID.AncientSilverBrick;
                    wallType = WallID.BorealWood;
                    doorWallType = WallID.BorealWoodFence;
                    doorPlattformType = 28; // 19 Plattforms -> 28=Granite
                    defaultChestType = 33;
                    defaultCampfireType = 0;
                    defaultTableType = 28;
                    break;
            }

            //init
            int gap = 1; // the horizontal gap between two room columns
            Rectangle mainRoom; //for later filling the gap between the rooms with bricks
            Rectangle previousRoom; // same
            Rectangle actualRoom; // same
            int previousHighestY; // same
            int previousLowestY; // same
            int actualHighestY = 0; // same
            int actualLowestY = 0; // same


            // generate the main room
            mainRoom = GenerateRoom(room: new Rectangle(MainRoomPos.X - initialRoomSizeX / 2, MainRoomPos.Y - initialRoomSizeY, initialRoomSizeX, initialRoomSizeY),
                                                            roomType: 0,
                                                            leftDoor: true,
                                                            rightDoor: true,
                                                            upDoor: false,
                                                            downDoor: false);

            previousRoom = mainRoom;
            previousHighestY = MainRoomPos.Y - initialRoomSizeY; //for later filling the gap between the rooms with bricks
            previousLowestY = MainRoomPos.Y; //for later filling the gap between the rooms with bricks








            // generate rooms to the right of the main room
            //TODO: after each GenerateRoom do the room decoration?
            int currentPlusX = initialRoomSizeX / 2 + gap;
            int sideRoomCount = WorldGen.genRand.Next(3, 7); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 0; i < sideRoomCount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                if (currentRoomWidth % 2 == 1) currentRoomWidth++; //make room width always even, so the up/down doors are centered in the room
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool(); // if rooms above this main-column-room shall be generated
                bool generateDown = WorldGen.genRand.NextBool(); // if rooms below this main-column-room shall be generated

                // create main room of this column
                actualRoom = GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX, MainRoomPos.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight),
                                                    roomType: 1,
                                                    leftDoor: true,
                                                    rightDoor: i != (sideRoomCount - 1),
                                                    upDoor: generateUp,
                                                    downDoor: generateDown);

                int currentPlusY = -currentRoomHeight + 2; //init
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4); //number of rooms above this main-column room
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/; //TODO: what happens if the room height gets randomized?
                        GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: 2,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: j != (vertAmount - 1),
                                     downDoor: true);

                        actualHighestY = MainRoomPos.Y - vertRoomHeight + currentPlusY;
                        currentPlusY -= vertRoomHeight - 2;
                    }
                }
                else   actualHighestY = MainRoomPos.Y + currentPlusY - 2;

                currentPlusY = currentRoomHeight - 2;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4); //number of rooms below this main-column room
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: -2,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: true,
                                     downDoor: j != (vertAmount - 1));

                        actualLowestY = MainRoomPos.Y + currentPlusY;
                        currentPlusY += vertRoomHeight - 2;
                    }
                }
                else   actualLowestY = MainRoomPos.Y;


                currentPlusX += currentRoomWidth + gap;

                FillGapAndPutDoor(previousRoom, actualRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);
                previousRoom = actualRoom;
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }
            WorldGen.PlaceTile(MainRoomPos.X + currentPlusX - gap - 1, MainRoomPos.Y - 2, brickType, true, true); //there is some FloorTile on the outer wall of the fortress, clean it
            WorldGen.PlaceTile(MainRoomPos.X + currentPlusX - gap - 2, MainRoomPos.Y - 2, brickType, true, true); //there is some FloorTile on the outer wall of the fortress, clean it








            // generate rooms to the left of the main room
            previousRoom = mainRoom;
            previousHighestY = MainRoomPos.Y - initialRoomSizeY; //for later filling the gap between the rooms with bricks
            previousLowestY = MainRoomPos.Y; //for later filling the gap between the rooms with bricks

            currentPlusX = -initialRoomSizeX / 2 - gap;
            sideRoomCount = WorldGen.genRand.Next(3, 7); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 0; i < sideRoomCount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                if (currentRoomWidth % 2 == 1) currentRoomWidth++; //make room width always even, so the up/down doors are centered in the room
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool(); // if rooms above this main-column-room shall be generated
                bool generateDown = WorldGen.genRand.NextBool(); // if rooms below this main-column-room shall be generated

                actualRoom = GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight),
                                          roomType: -1,
                                          leftDoor: i != (sideRoomCount - 1),
                                          rightDoor: true,
                                          upDoor: generateUp,
                                          downDoor: generateDown);

                int currentPlusY = -currentRoomHeight + 2;
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/;
                        GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: 2,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: j != (vertAmount - 1),
                                     downDoor: true);

                        actualHighestY = MainRoomPos.Y - vertRoomHeight + currentPlusY;
                        currentPlusY -= vertRoomHeight - 2;
                    }
                }
                else actualHighestY = MainRoomPos.Y + currentPlusY - 2;

                currentPlusY = currentRoomHeight - 2;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        GenerateRoom(room: new Rectangle(MainRoomPos.X + currentPlusX - currentRoomWidth, MainRoomPos.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight),
                                     roomType: -2,
                                     leftDoor: false,
                                     rightDoor: false,
                                     upDoor: true,
                                     downDoor: j != (vertAmount - 1));

                        actualLowestY = MainRoomPos.Y + currentPlusY;
                        currentPlusY += vertRoomHeight - 2;
                    }
                }
                else actualLowestY = MainRoomPos.Y;

                currentPlusX -= currentRoomWidth + gap;

                FillGapAndPutDoor(previousRoom, actualRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);
                previousRoom = actualRoom;  
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }
            WorldGen.PlaceTile(MainRoomPos.X + currentPlusX + gap, MainRoomPos.Y - 2, brickType, true, true); //there is some FloorTile on the outer wall of the fortress, clean it
            WorldGen.PlaceTile(MainRoomPos.X + currentPlusX + gap + 1, MainRoomPos.Y - 2, brickType, true, true); //there is some FloorTile on the outer wall of the fortress, clean it


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
        /// <param name="roomType">The type of the to-be-created room for some specific generation needs: <br/> 0=main room, 1=side room right, -1=side room left, 2=room above side room, -2=room below side room</param>
        /// <param name="leftDoor">If a door to the left shall be created</param>
        /// <param name="rightDoor">If a door to the right shall be created</param>
        /// <param name="upDoor">If a platform in the ceiling shall be created</param>
        /// <param name="downDoor">If a platform in the floor shall be created</param>
        /// 
        /// <returns>Hands back the room dimensions input</returns>
        public Rectangle GenerateRoom(Rectangle room, int roomType, bool leftDoor = false, bool rightDoor = false, bool upDoor = false, bool downDoor = false)
        {
            Rectangle hollowRect = room; // the "hollow" room.... e.g. without the wall bricks
            hollowRect.Width -= 4;
            hollowRect.Height -= 4;
            hollowRect.X += 2;
            hollowRect.Y += 2;

            bool noBreakPoint = WorldGen.genRand.NextBool(); //force the background wall of the room to have no holes
            Vector2 wallBreakPoint = new(room.X + WorldGen.genRand.Next(room.Width), room.Y + WorldGen.genRand.Next(room.Height));

            List<Rectangle> doors = new();
            Rectangle leftDoorRect = new(room.X, room.Y + room.Height - 5, 2, 3);
            Rectangle rightDoorRect = new(room.X + room.Width - 2, room.Y + room.Height - 5, 2, 3);
            Rectangle upDoorRect = new(room.X + room.Width / 2 - 2, room.Y, 4, 2);
            Rectangle downDoorRect = new(room.X + room.Width / 2 - 2, room.Y + room.Height - 2, 4, 2);

            if (leftDoor) doors.Add(leftDoorRect);
            if (rightDoor) doors.Add(rightDoorRect);
            if (upDoor) doors.Add(upDoorRect);
            if (downDoor) doors.Add(downDoorRect);


            // Create room frame & background wall
            for (int i = room.X; i < room.X + room.Width; i++)
            {
                for (int j = room.Y; j < room.Y + room.Height; j++)
                {
                    WorldGen.EmptyLiquid(i, j);
                    WorldGen.KillWall(i, j);
                    

                    if ( (((i > room.X) && (i < room.X + room.Width - 1)) && ((j > room.Y) && (j < room.Y + room.Height - 1))) && // leave 1 tile distance from the sides (so the background won't overlap to the outside)
                         ((Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f)) || noBreakPoint)     ) // make here and there some cracks in the background to let it look more "abandoned"
                    {
                        WorldGen.PlaceWall(i, j, wallType);
                    } 

                    if ( ((j == room.Y + room.Height - 2) && // the height of this rooms floor
                          ((i >= hollowRect.X) && (i < hollowRect.X + hollowRect.Width) || (roomType >= -1 && roomType <= 1)))) //main rooms and side rooms need the floor on the room frame, up/down rooms mustn't
                    {
                        WorldGen.PlaceTile(i, j, floorType, true, true);
                    }
                    else if ((j == room.Y) && // the height of this rooms topmost ceiling row
                             (roomType == -2)) // down-rooms have the floor type of the above room laying at this height
                    {
                        continue; // don't override anything.
                    }
                    else if ( ((i < hollowRect.X) || (i >= hollowRect.X + hollowRect.Width)) || ((j < hollowRect.Y) || (j >= hollowRect.Y + hollowRect.Height)) )
                    {
                        WorldGen.PlaceTile(i, j, brickType, true, true);
                    }
                    else WorldGen.KillTile(i, j); //carve out the inside of the room

                    WorldGen.SlopeTile(i, j); //TODO: maybe first complete the room and then check the frame for slopes?
                }
            }

            // carve out the inside of the room
            //for (int i = hollowRect.X; i < hollowRect.X + hollowRect.Width; i++)
            //{
            //    for (int j = hollowRect.Y; j < hollowRect.Y + hollowRect.Height; j++)
            //    {
            //        WorldGen.KillTile(i, j);
            //    }
            //}

            #region Doors
            if (doors.Count != 0)
            {
                foreach (Rectangle doorRect in doors)
                {
                    for (int i = doorRect.X; i < doorRect.X + doorRect.Width; i++)
                    {
                        for (int j = doorRect.Y; j < doorRect.Y + doorRect.Height; j++)
                        {
                            WorldGen.KillTile(i, j);
                            WorldGen.KillWall(i, j);
                            if ( (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f)) || noBreakPoint) WorldGen.PlaceWall(i, j, doorWallType);
                        }
                    }
                }
            }

            if (downDoor)
            {
                for (int i = downDoorRect.X; i < downDoorRect.X + downDoorRect.Width; i++)
                {
                    int j = downDoorRect.Y + downDoorRect.Height - 2;
                    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: doorPlattformType);
                }
            }

            if ( upDoor)
            {
                for (int i = upDoorRect.X; i < upDoorRect.X + upDoorRect.Width; i++)
                {
                    int j = upDoorRect.Y;
                    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: doorPlattformType);
                }
            }

            #endregion

            //TODO: do decoration in separate method
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

            traps.Add(new Point16(room.X + WorldGen.genRand.Next(room.Width), room.Y + WorldGen.genRand.Next(room.Height)));

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
        public void FillGapAndPutDoor(Rectangle previousRoom, Rectangle actualRoom, int previousHighestY, int actualHighestY, int previousLowestY, int actualLowestY)
        {
            
            Rectangle gap = new(0, 0, 0, 0); //init
            //TODO: where fill wall -1 and +1 of the gap, as this will be left out when generating the room

            #region first step: derive the side to which the room expanded
            if (actualRoom.X > previousRoom.X) // the actual room is on the right side of the previous room
            {
                gap.X = previousRoom.X + previousRoom.Width;
                gap.Width = actualRoom.X - (previousRoom.X + previousRoom.Width);
            }
            else // the actual room is on the left side of the previous room
            {
                gap.X = actualRoom.X + actualRoom.Width;
                gap.Width = previousRoom.X - (actualRoom.X + actualRoom.Width);
            }
            #endregion

            #region second step: find out which room column reaches less farther up (to define Y of the gap)
            if (previousHighestY > actualHighestY)   gap.Y = previousHighestY;
            else                                     gap.Y = actualHighestY;
            #endregion

            #region third step: find out which room column reaches less farther down (to define height of the gap)
            if (previousLowestY > actualLowestY)   gap.Height = actualLowestY - gap.Y;
            else                                   gap.Height = previousLowestY - gap.Y;
            #endregion

            //fill gap
            for (int i = gap.X; i < gap.X + gap.Width; i++)
            {
                for (int j = gap.Y; j < gap.Y + gap.Height; j++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.EmptyLiquid(i, j);

                    if (j == previousRoom.Y + previousRoom.Height - 2) //doesn't matter if previousRoom or actualRoom, because the floor is at the same height
                    {
                        WorldGen.PlaceTile(i, j, floorType, true, true);
                    }
                    else if (j > previousRoom.Y + previousRoom.Height - 6 && j < previousRoom.Y + previousRoom.Height - 2)
                    {
                        WorldGen.KillTile(i, j); //leave the "door" free

                        WorldGen.KillWall(i, j);
                        WorldGen.PlaceWall(i, j, doorWallType); //put the designated background wall
                    }
                    else
                    {
                        WorldGen.PlaceTile(i, j, brickType, true, true); //fill gap with bricks
                    }
                }
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
    }
}
