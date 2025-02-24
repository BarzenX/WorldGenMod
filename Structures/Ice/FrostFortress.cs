﻿using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.GameContent.Generation;
using Terraria.IO;
using Terraria.DataStructures;
using System;
using Terraria.ObjectData;
using System.Diagnostics;
using Terraria.UI;
using rail;
using Terraria.GameContent;
using System.Collections;
using log4net.Core;
using System.IO.Pipelines;
using static WorldGenMod.LineAutomat;
using System.Drawing;
using System.Threading.Channels;
using Microsoft.CodeAnalysis.CSharp.Syntax;

//TODO: sometimes the FrostFortress creates extremely slow - supposedly because of the frequent PlaceTile calls...what to do?

namespace WorldGenMod.Structures.Ice
{
    class FrostFortress : ModSystem
    {
        List<Vector2> fortresses = [];
        List<Point16> traps = [];
        readonly int gap = -1; // the horizontal gap between two side room columns
        readonly int wThick = 2; // the thickness of the outer walls and ceilings in code
        readonly int forceEvenRoom = 1; // 1 = force all rooms to have an even XTiles count; 0 = force all side rooms to have an odd XTiles count

        Dictionary<string, int> Deco = []; // the dictionary where the styles of tiles are stored

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

            while (amountGenerated == 0) // reduced to only 1 Fortress per map. was: (amountGenerated < amount)
            {
                int x = WorldGen.genRand.Next(200, Main.maxTilesX - 200);
                int y = WorldGen.genRand.Next((int)GenVars.rockLayer, Main.maxTilesY - 200);
                Vector2 position = new(x, y); //init for later position search iteration

                List<int> allowedTiles =
                [
                    TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce, TileID.FleshIce
                ];

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
            Deco.Clear(); // init

            // create dictionary entries
            Deco.Add(S.StyleSave, 0);
            Deco.Add(S.Brick, 0);
            Deco.Add(S.Floor, 0);
            Deco.Add(S.BackWall, 0);
            Deco.Add(S.DoorWall, 0);
            Deco.Add(S.DoorPlat, 0);
            Deco.Add(S.Door, 0);
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
                case S.StyleSnow: // Snow
                    Deco[S.StyleSave] = S.StyleSnow;
                    Deco[S.Brick] = TileID.SnowBrick;
                    Deco[S.Floor] = TileID.IceBrick;
                    if (Chance.Simple())   Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.SnowBrick;
                    Deco[S.DoorWall] = WallID.IceBrick;
                    Deco[S.DoorPlat] = 35; // Tile ID 19 (Plattforms) -> Type 35=Frozen
                    Deco[S.Door] = 27;     // Tile ID 10 (Doors) -> Type 27=Frozen (Closed)
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
                    Deco[S.Sofa] = 27    ; // Tile ID 89 (Sofas) -> Type 27=Frozen
                    Deco[S.Clock] = 11;    // Tile ID 104 (GrandfatherClocks) -> Type 11=Frozen
                    Deco[S.Bed] = 15;      // Tile ID 79 (Beds) -> Type 15=Frozen
                    Deco[S.BedWallpaper] = WallID.StarsWallpaper;
                    Deco[S.PaintingWallpaper] = WallID.SparkleStoneWallpaper;
                    Deco[S.Dresser] = 30;  // Tile ID 88 (Dressers) -> Type 30=Frozen
                    Deco[S.Piano] = 7;     // Tile ID 87 (Pianos) -> Type 7=Frozen
                    break;

                case S.StyleBoreal: // Boreal
                    Deco[S.StyleSave] = S.StyleBoreal;
                    Deco[S.Brick] = TileID.BorealWood;
                    Deco[S.Floor] = TileID.GrayBrick;
                    if (Chance.Simple())   Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.BorealWood;
                    Deco[S.DoorWall] = WallID.BorealWoodFence;
                    Deco[S.DoorPlat] = 28; // Tile ID 19 (Plattforms) -> Type 28=Granite
                    Deco[S.Door] = 15;     // Tile ID 10 (Doors) -> Type 15=Iron (Closed)
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

                case S.StyleDarkLead: // Dark Lead
                    Deco[S.StyleSave] = S.StyleDarkLead;
                    Deco[S.Brick] = TileID.LeadBrick;
                    Deco[S.Floor] = TileID.EbonstoneBrick;
                    //TODO: find something (Platinum Brick meh, Titanstone block meh)     if (Chance.Simple())   Deco[Style.Floor] = TileID.AncientSilverBrick;
                    Deco[S.BackWall] = WallID.BlueDungeonSlab;
                    Deco[S.DoorWall] = WallID.Bone;
                    Deco[S.DoorPlat] = 43; // Tile ID 19 (Plattforms) -> Type 43=Stone
                    Deco[S.Door] = 16;     // Tile ID 10 (Doors) -> Type 16=Blue Dungeon (Closed)
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
            if (!WorldGenMod.generateFrostFortresses)  return;

            traps.Clear();

            FillAndChooseStyle();
            
            //init for variables for later filling the gap between the rooms with bricks
            Rectangle2P mainRoom;
            Rectangle2P previousSideRoom;
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
            
            previousSideRoom = mainRoom;
            previousHighestY = mainRoom.Y0; //for later filling the gap between the rooms with bricks
            previousLowestY = mainRoom.Y1; //for later filling the gap between the rooms with bricks




            // generate all other rooms
            int sideRoomWidthMin = 16; // attention: smaller than 16 will break most room decoration
            int sideRoomWidthMax = 22;
            int sideRoomX0, sideRoomY0, sideRoomX1, sideRoomY1; //create variables
            int verticalRoomX0, verticalRoomY0, verticalRoomX1, verticalRoomY1; //create variables


            // generate rooms to the right of the main room
            sideRoomX0 = mainRoom.X1 + 1 + gap; // init value for first iteration
            sideRoomY1 = mainRoom.Y1; // this value is constant
            int sideRoomCount = WorldGen.genRand.Next(3, 6); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 1; i <= sideRoomCount; i++)
            {
                int sideRoomXTiles = WorldGen.genRand.Next(sideRoomWidthMin, sideRoomWidthMax);
                if (sideRoomXTiles % 2 == forceEvenRoom) sideRoomXTiles++; //forces all rooms to be even or odd 
                sideRoomX1 = sideRoomX0 + (sideRoomXTiles - 1);

                int sideRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                sideRoomY0 = sideRoomY1 - (sideRoomYTiles - 1);


                bool generateUp = Chance.Simple(); // if rooms above this main-column-room shall be generated
                bool generateDown = Chance.Simple(); // if rooms below this main-column-room shall be generated

                // create main room of this column
                actualSideRoom = GenerateRoom(room: new Rectangle2P(sideRoomX0, sideRoomY0, sideRoomX1, sideRoomY1, "dummyText"),
                                              roomType: RoomID.SideRight,
                                              leftDoor: true,
                                              rightDoor: i != sideRoomCount,
                                              upDoor: generateUp,
                                              downDoor: generateDown);

                //create rooms above this side room
                if (generateUp)
                {
                    verticalRoomX0 = sideRoomX0; //this value is constant
                    verticalRoomX1 = sideRoomX1; //this value is constant
                    verticalRoomY1 = sideRoomY0 + (wThick - 1); // init value for first iteration

                    int vertAmount = WorldGen.genRand.Next(1, 4); //number of rooms above this main-column room
                    for (int j = 1; j <= vertAmount; j++)
                    {
                        int vertRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                        verticalRoomY0 = verticalRoomY1 - (vertRoomYTiles - 1);

                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(verticalRoomX0, verticalRoomY0, verticalRoomX1, verticalRoomY1, "dummyText"),
                                                          roomType: RoomID.AboveSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: j != vertAmount,
                                                          downDoor: true);

                        actualHighestY = actualVerticalRoom.Y0;
                        verticalRoomY1 = verticalRoomY0 + (wThick - 1); //The ceiling of this room will be the floor of the next higher room
                    }
                }
                else actualHighestY = actualSideRoom.Y0;

                //create rooms below this side room
                if (generateDown)
                {
                    verticalRoomX0 = sideRoomX0; //this value is constant
                    verticalRoomX1 = sideRoomX1; //this value is constant
                    verticalRoomY0 = sideRoomY1 - (wThick - 1); // init value for first iteration

                    int vertAmount = WorldGen.genRand.Next(1, 4); //number of rooms below this main-column room
                    for (int j = 1; j <= vertAmount; j++)
                    {
                        int vertRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                        verticalRoomY1 = verticalRoomY0 + (vertRoomYTiles - 1);

                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(verticalRoomX0, verticalRoomY0, verticalRoomX1, verticalRoomY1, "dummyText"),
                                                          roomType: RoomID.BelowSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: true,
                                                          downDoor: j != vertAmount);

                        actualLowestY = actualVerticalRoom.Y1;
                        verticalRoomY0 = verticalRoomY1 - (wThick - 1); //The floor of this room will be the ceiling of the next lower room
                    }
                }
                else actualLowestY = actualSideRoom.Y1;



                if (gap > 0) FillGap(previousSideRoom, actualSideRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);

                // actualize values for next side room iteration
                sideRoomX0 = sideRoomX1 + 1 + gap;
                previousSideRoom = actualSideRoom;
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }







            // generate rooms to the left of the main room
            previousSideRoom = mainRoom;
            previousHighestY = mainRoom.Y0; //for later filling the gap between the rooms with bricks
            previousLowestY = mainRoom.Y1; //for later filling the gap between the rooms with bricks

            sideRoomX1 = mainRoom.X0 - 1 - gap; // init value for first iteration
            sideRoomY1 = mainRoom.Y1; // this value is constant
            sideRoomCount = WorldGen.genRand.Next(3, 6); //the rooms are arranged in shape of columns and each column has a fixed width. This is the amount of columns on a side of the main room
            for (int i = 1; i <= sideRoomCount; i++)
            {
                int sideRoomXTiles = WorldGen.genRand.Next(sideRoomWidthMin, sideRoomWidthMax);
                if (sideRoomXTiles % 2 == forceEvenRoom) sideRoomXTiles++; //forces all rooms to be even or odd 
                sideRoomX0 = sideRoomX1 - (sideRoomXTiles - 1);

                int sideRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                sideRoomY0 = sideRoomY1 - (sideRoomYTiles - 1);

                bool generateUp = Chance.Simple(); // if rooms above this main-column-room shall be generated
                bool generateDown = Chance.Simple(); // if rooms below this main-column-room shall be generated

                actualSideRoom = GenerateRoom(room: new Rectangle2P(sideRoomX0, sideRoomY0, sideRoomX1, sideRoomY1, "dummyText"),
                                              roomType: RoomID.SideLeft,
                                              leftDoor: i != sideRoomCount,
                                              rightDoor: true,
                                              upDoor: generateUp,
                                              downDoor: generateDown);

                //create rooms above this side room
                if (generateUp)
                {
                    verticalRoomX0 = sideRoomX0; //this value is constant
                    verticalRoomX1 = sideRoomX1; //this value is constant
                    verticalRoomY1 = sideRoomY0 + (wThick - 1); // init value for first iteration

                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 1; j <= vertAmount; j++)
                    {
                        int vertRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                        verticalRoomY0 = verticalRoomY1 - (vertRoomYTiles - 1);

                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(verticalRoomX0, verticalRoomY0, verticalRoomX1, verticalRoomY1, "dummyText"),
                                                          roomType: RoomID.AboveSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: j != vertAmount,
                                                          downDoor: true);

                        actualHighestY = actualVerticalRoom.Y0;
                        verticalRoomY1 = verticalRoomY0 + (wThick - 1); //The ceiling of this room will be the floor of the next higher room
                    }
                }
                else actualHighestY = actualSideRoom.Y0;


                //create rooms below this side room
                if (generateDown)
                {
                    verticalRoomX0 = sideRoomX0; //this value is constant
                    verticalRoomX1 = sideRoomX1; //this value is constant
                    verticalRoomY0 = sideRoomY1 - (wThick - 1); // init value for first iteration

                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 1; j <= vertAmount; j++)
                    {
                        int vertRoomYTiles = (int)(sideRoomXTiles * WorldGen.genRand.NextFloat(0.6f, 1f));
                        verticalRoomY1 = verticalRoomY0 + (vertRoomYTiles - 1);

                        actualVerticalRoom = GenerateRoom(room: new Rectangle2P(verticalRoomX0, verticalRoomY0, verticalRoomX1, verticalRoomY1, "dummyText"),
                                                          roomType: RoomID.BelowSide,
                                                          leftDoor: false,
                                                          rightDoor: false,
                                                          upDoor: true,
                                                          downDoor: j != vertAmount);

                        actualLowestY = actualVerticalRoom.Y1;
                        verticalRoomY0 = verticalRoomY1 - (wThick - 1); //The floor of this room will be the ceiling of the next lower room
                    }
                }
                else actualLowestY = actualSideRoom.Y1;



                if (gap > 0) FillGap(previousSideRoom, actualSideRoom, previousHighestY, actualHighestY, previousLowestY, actualLowestY);

                // actualize values for next side room iteration
                sideRoomX1 = sideRoomX0 - 1 - gap;
                previousSideRoom = actualSideRoom;
                previousHighestY = actualHighestY;
                previousLowestY = actualLowestY;
            }


            int[] allowedTraps =
                [
                    0, //darts
                    //1, //darts and a boulder? -> I don't want boulders!
                    //2, //dynamite -> I don't want it!
                    3  //geysirs
                ];
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
            // the "free" room.... e.g. the rooms free inside ("room" without the wall bricks)
            Rectangle2P freeR = new(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");

            int x, y; //temp variable for later calculations;

            bool noBreakPoint = Chance.Simple(); //force the background wall of the room to have no holes
            Vector2 wallBreakPoint = new(room.X0 + WorldGen.genRand.Next(room.XDiff), room.Y0 + WorldGen.genRand.Next(room.YDiff));



            // create door rectangles
            #region door rectangles
            Dictionary<int, (bool doorExist, Rectangle2P doorRect)> doors = []; // a dictionary for working and sending the doors in a compact way

            int leftRightDoorsYTiles = 3; // how many tiles the left and right doors are high
            y = freeR.Y1 - (leftRightDoorsYTiles - 1);
            Rectangle2P leftDoorRect  = new(room.X0     , y, wThick, leftRightDoorsYTiles);
            Rectangle2P rightDoorRect = new(freeR.X1 + 1, y, wThick, leftRightDoorsYTiles);

            int upDownDoorXTiles = 4; // how many tiles the up and down doors are wide
            int adjustX = 0; // init
            if (freeR.XTiles % 2 == 1 && upDownDoorXTiles % 2 == 0) upDownDoorXTiles++; // an odd number of x-tiles in the room also requires an odd number of platforms so the door is symmetrical
            else adjustX = -1; //in even XTile rooms there is a 2-tile-center and XCenter will be the left tile of the two. To center an even-numberd door in this room, you have to subtract 1. Odd XTile rooms are fine
            x = (freeR.XCenter) - (upDownDoorXTiles / 2 + adjustX);
            Rectangle2P upDoorRect   = new(x, room.Y0     , upDownDoorXTiles, wThick);
            Rectangle2P downDoorRect = new(x, freeR.Y1 + 1, upDownDoorXTiles, wThick);

            doors.Add(Door.Left , (leftDoor , leftDoorRect));
            doors.Add(Door.Right, (rightDoor, rightDoorRect));
            doors.Add(Door.Up   , (upDoor   , upDoorRect));
            doors.Add(Door.Down , (downDoor , downDoorRect));
            #endregion


            // Create room frame, floor and background wall
            bool mainRoom = roomType == RoomID.MainRoom;

            bool leftRoom = roomType == RoomID.SideLeft;
            bool rightRoom = roomType == RoomID.SideRight;
            bool sideRoom = leftRoom || rightRoom;

            bool lastLeftRoom = leftRoom && !leftDoor;
            bool lastRightRoom = rightRoom && !rightDoor;

            bool upRoom = roomType == RoomID.AboveSide;
            bool downRoom = roomType == RoomID.BelowSide;

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

                    if ((upRoom && j > freeR.Y1) || (downRoom && j < freeR.Y0)) // the parts where above and below rooms overlap
                    {
                        continue; // do nothing, it has already been taken care of
                    }
                    else if (j == freeR.Y1 + 1) // the height of this rooms floor
                    {
                        if (((i >= freeR.X0 && i <= freeR.X1) || ((leftRoom && !lastLeftRoom) || mainRoom || (rightRoom && !lastRightRoom))) ||  //main rooms and side rooms need the floor on the room frame, up/down rooms mustn't
                             (i >= freeR.X0 && lastLeftRoom) || // the last left side room has the floor only on its right side (on the left there is the outer wall)
                             (i <= freeR.X1 && lastRightRoom)) // the last right side room has the floor only on its left side (on the right there is the outer wall)
                        {
                            WorldGen.PlaceTile(i, j, Deco[S.Floor], mute: true, forced: true);
                        }
                        else WorldGen.PlaceTile(i, j, Deco[S.Brick], mute: true, forced: true); // normal brick
                    }
                    else if (j == room.Y0) // the height of this rooms topmost ceiling row
                    {
                        if ((sideRoom || upRoom) && upDoor) // prepare the floor for any above laying room
                        {
                            if (i >= freeR.X0 && i <= freeR.X1)
                            {
                                WorldGen.PlaceTile(i, j, Deco[S.Floor], mute: true, forced: true);
                            }
                            else WorldGen.PlaceTile(i, j, Deco[S.Brick], mute: true, forced: true); // normal brick
                        }
                        else WorldGen.PlaceTile(i, j, Deco[S.Brick], mute: true, forced: true); // normal brick
                    }
                    else if (i < freeR.X0 || i > freeR.X1 || j < freeR.Y0 || j > freeR.Y1 + 1)
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Brick], mute: true, forced: true); // place the outer wall bricks of the room
                    }
                    else WorldGen.KillTile(i, j); //carve out the inside of the room
                }
            }

            #region Doors
            //carve out doors
            for (int doorNum = 0; doorNum <= doors.Count - 1; doorNum++)
            {
                if ((upRoom && doorNum == Door.Down) || (downRoom && doorNum == Door.Up))
                {
                    continue; // these doors are already created by their previous room, no need to do it again
                }

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
            for (int doorNum = 0; doorNum <= doors.Count - 1; doorNum++)
            {
                if (doors[doorNum].doorExist)
                {
                    for (int i = doors[doorNum].doorRect.X0; i <= doors[doorNum].doorRect.X1; i++)
                    {
                        for (int j = doors[doorNum].doorRect.Y0; j <= doors[doorNum].doorRect.Y1; j++)
                        {
                            WorldGen.KillWall(i, j);

                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f) || noBreakPoint)
                            {
                                WorldGen.PlaceWall(i, j, Deco[S.DoorWall]);
                            }
                        }
                    }
                }
            }

            // put additional background walls at special positions
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

            if (downDoor)
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

            if (upDoor)
            {
                int j = upDoorRect.Y0;
                for (int i = upDoorRect.X0; i <= upDoorRect.X1; i++)
                {
                    WorldGen.PlaceTile(i, j, TileID.Platforms, mute: true, forced: true, style: Deco[S.DoorPlat]);
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
                WorldGen.SlopeTile(leftDoorRect.X1, leftDoorRect.Y0 - 1, (int)Func.SlopeVal.BotRight); // door right corner
            }
            if (rightDoor)
            {
                WorldGen.SlopeTile(rightDoorRect.X0, rightDoorRect.Y0 - 1, (int)Func.SlopeVal.BotLeft); // door left corner
            }
            if (upDoor && !downRoom)
            {
                WorldGen.SlopeTile(upDoorRect.X0 - 1, upDoorRect.Y1, (int)Func.SlopeVal.BotRight); // updoor left corner
                WorldGen.SlopeTile(upDoorRect.X1 + 1, upDoorRect.Y1, (int)Func.SlopeVal.BotLeft); // updoor right corner
            }
            if (downDoor && !upRoom)
            {
                WorldGen.SlopeTile(downDoorRect.X0 - 1, downDoorRect.Y1, (int)Func.SlopeVal.BotRight); // downDoor left corner
                WorldGen.SlopeTile(downDoorRect.X1 + 1, downDoorRect.Y1, (int)Func.SlopeVal.BotLeft); // downDoor right corner
            }
            #endregion

            DecorateRoom(room: room,
                         roomType: roomType,
                         doors: doors);

            traps.Add(new Point16(room.X0 + WorldGen.genRand.Next(room.XDiff), room.Y0 + WorldGen.genRand.Next(room.YDiff)));

            return room;
        }

        /// <summary>
        /// Shifting a side room of the main room some blocks away, leaves a gap that has to be filled.
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
                    else if (j >= previousRoom.Y1 - wThick - 2 && j <= previousRoom.Y1 - wThick) // the door between the rooms
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

        /// <summary>
        /// The main method for choosing and running the a rooms decoration
        /// </summary>
        /// <param name="room">The rectangular area of the room, including the outer walls</param>
        /// <param name="roomType">Stating which type the room is of (see class "RoomID")</param>
        /// <param name="doors">The rectangular areas of the possible doors in the room and a bool stating if it actually exists (use class "Door" to refer to a specific door)</param>
        public void DecorateRoom(Rectangle2P room, int roomType, IDictionary<int, (bool doorExist, Rectangle2P doorRect)> doors)
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
                // extra doors only if there is a gap
                if (gap > 0)
                {
                    x = room.X0;
                    y = freeR.Y1;
                    WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); // put another door (resulting in double doors)

                    x = room.X1;
                    y = freeR.Y1;
                    WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); // put another door (resulting in double doors)
                }


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
                if (Chance.Simple())
                {
                    WorldGen.PlaceTile(x - 8, y, TileID.SilverCoinPile, style: 0); //Silver Coins
                    WorldGen.PlaceTile(x - 9, y, TileID.GoldCoinPile, style: 0); //Silver Coins
                }
                else
                {
                    WorldGen.PlaceSmallPile(x - WorldGen.genRand.Next(7, 12), y, WorldGen.genRand.Next(16, 18), 1); //Copper or Silver coin stash
                }


                //right side coins
                if (Chance.Simple())
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
                        WorldGen.paintTile(x, freeR.Y0 + 4, (byte)Deco[S.StylePaint]);

                        WorldGen.PlaceTile(x, freeR.Y0 + 6, TileID.BorealBeam);
                        WorldGen.paintTile(x, freeR.Y0 + 6, (byte)Deco[S.StylePaint]);
                    }
                }

                //painting
                for (x = freeR.X0 + 12; x <= freeR.X1 - 12; x++)
                {
                    WorldGen.PlaceWall(x, freeR.Y0 + 4, Deco[S.BackWall]); //just in case it got deleted by the "cracked" background design
                    WorldGen.PlaceWall(x, freeR.Y0 + 5, Deco[S.BackWall]);
                    WorldGen.PlaceWall(x, freeR.Y0 + 6, Deco[S.BackWall]);
                }
                WorldGen.PlaceTile(freeR.X0 + 13, freeR.Y0 + 5, TileID.Painting3X3, style: Deco[S.MainPainting]);

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
                Func.PlaceCobWeb(freeR, 1, 25);

                return;
            }
            #endregion

            
            if (roomType == RoomID.SideLeft)
            {
                // doors
                x = room.X1; // left side rooms always have a right door
                y = freeR.Y1;
                WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); //Door

                if (gap > 0 && doors[Door.Left].doorExist) // in case there is a gap between side rooms and this left side room also has a left door
                {
                    x = room.X0;
                    y = freeR.Y1;
                    WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); // put another door (resulting in double doors)
                }
            }

            if (roomType == RoomID.SideRight)
            {
                // doors
                x = room.X0; // right side rooms always have a left door
                y = freeR.Y1;
                WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); //Door

                if (gap > 0 && doors[Door.Right].doorExist) // in case there is a gap between side rooms and this right side room also has a right door
                {
                    x = room.X1;
                    y = freeR.Y1;
                    WorldGen.PlaceTile(x, y, TileID.ClosedDoor, style: Deco[S.Door]); // put another door (resulting in double doors)
                }
            }

            // init variables
            bool placed, placed2;
            (bool success, int x, int y) placeResult, placeResult2;
            Rectangle2P area1, area2, area3, noBlock = Rectangle2P.Empty; // for creating areas for random placement
            List<(int x, int y)> rememberPos = []; // for remembering positions
            List<(ushort TileID, int style, byte chance)> randomItems = [], randomItems2 = []; // for random item placement
            int chestID, unusedXTiles, num;

            //choose room decoration at random
            bool valid;
            int roomDeco;
            do
            {
                valid = true;
                roomDeco = WorldGen.genRand.Next(8); //TODO: don't forget to put the correct values in the end!
                if (roomDeco == 6 && (roomType != RoomID.BelowSide))   valid = false; // room 6 may only appear as a "below" room

            } while (!valid);

            //roomDeco = 6;

            switch (roomDeco)
            {
                case 0: // corridor: two tables, two lamps, a beam line, high rooms get another beam line and a painting

                    // clock / potted tree in the middle
                    if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.XCenter, y, TileID.GrandfatherClocks, style: Deco[S.Clock]); // Clock
                    else if (Chance.Perc(65))
                    {
                        placed = WorldGen.PlaceObject(freeR.XCenter + 1, y, TileID.PottedPlants1, style: 3); // potted forest tree
                        if(placed) Func.PaintArea(new Rectangle2P(freeR.XCenter, y - 4, freeR.XCenter + 1, y, "dummyString"), Deco[S.StylePaint]);
                    }

                    #region left and right side

                    for (int i = 0; i <= 1; i++)
                    {
                        int leftEnd, rightEnd;
                        if (i == 0) //left side
                        {
                            leftEnd = freeR.X0;
                            rightEnd = doors[Door.Down].doorRect.X0;
                            x = rightEnd - WorldGen.genRand.Next(3, rightEnd - leftEnd);
                            y = freeR.Y1;
                            area1 = new Rectangle2P(leftEnd, y, rightEnd, y, "dummyString");
                        }
                        else // right side
                        {
                            leftEnd = doors[Door.Down].doorRect.X1;
                            rightEnd = freeR.X1;
                            x = leftEnd + WorldGen.genRand.Next(3, rightEnd - leftEnd);
                            y = freeR.Y1;
                            area1 = new Rectangle2P(leftEnd, y, rightEnd, y, "dummyString");
                        }
                        
                        placed = false;
                        placed2 = false;

                        // table / broken table
                        if (Chance.Simple())
                        {
                            placed = WorldGen.PlaceTile(x, y, TileID.Tables, style: Deco[S.Table]); // Table

                            if (placed) // stuff on the left table
                            {
                                if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.FoodPlatter); // food plate
                                if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.Bottles, style: 4); // mug
                                if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.Bottles, style: 0); // bottle
                                if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.Bottles, style: 8); // Chalice
                            }
                        }
                        else if (Chance.Simple())
                        {
                            placed2 = Func.PlaceLargePile(x, y, 22, 0, 186, paint: (byte)Deco[S.StylePaint]); // Broken Table covered in CobWeb 
                        }


                        // statue
                        randomItems.Clear();
                        randomItems.Add((TileID.Statues, 0, 50)); // Armor Statue
                        randomItems.Add((TileID.Statues, 14, 50)); // Gargoyle Statue
                        randomItems.Add((TileID.Statues, 11, 50)); // Reaper Statue
                        randomItems.Add((TileID.Statues, 12, 50)); // Woman Statue
                        num = WorldGen.genRand.Next(randomItems.Count);
                        if (Chance.Simple()) Func.TryPlaceTile(area1, Rectangle2P.Empty, randomItems[num].TileID, style: randomItems[num].style, add: new() { { "CheckArea", [0, 1, 1, 0] } }); // Statue


                        // Stools or broken chairs left and right of the table / broken table
                        if (placed || placed2)
                        {
                            if (Chance.Simple())
                            {
                                if (Chance.Perc(75) && (x - 2) >= leftEnd)
                                {
                                    placed2 = WorldGen.PlaceTile(x - 2, y, TileID.Chairs, style: 21); // left bar stool
                                    if (placed2) Func.PaintArea(new Rectangle2P(x - 2, y - 1, x - 2, y, "dummyString"), Deco[S.StylePaint]);
                                }
                            }
                            else
                            {
                                if (Chance.Perc(75) && (x - 3) >= leftEnd)
                                {
                                    placed2 = WorldGen.PlaceSmallPile(x - 3, y, 31, 1); // left broken chair
                                    if (placed2) Func.PaintArea(new Rectangle2P(x - 3, y, x - 2, y, "dummyString"), Deco[S.StylePaint]);
                                }
                            }

                            if (Chance.Simple())
                            {
                                if (Chance.Perc(75) && (x + 3) <= rightEnd)
                                {
                                    placed2 = WorldGen.PlaceTile(x + 2, y, TileID.Chairs, style: 21); // right bar stool
                                    if (placed2) Func.PaintArea(new Rectangle2P(x + 2, y - 1, x + 2, y, "dummyString"), Deco[S.StylePaint]);
                                }
                            }
                            else
                            {
                                if (Chance.Perc(75) && (x + 2) <= rightEnd)
                                {
                                    placed2 = WorldGen.PlaceSmallPile(x + 2, y, 31, 1); // right broken chair
                                    if (placed2) Func.PaintArea(new Rectangle2P(x + 2, y, x + 3, y, "dummyString"), Deco[S.StylePaint]);
                                }
                            }
                        }
                        else
                        {
                            placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.Benches, style: 0, chance: 80, add: new() { { "CheckArea", [1, 1, 1, 0] } }); // normal Bench
                            if (placeResult.success) Func.PaintArea(new Rectangle2P(placeResult.x - 1, y - 1, placeResult.x + 1, y, "dummyString"), Deco[S.StylePaint]);

                            placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.Dressers, style: Deco[S.Dresser], chance: 80, add: new() { { "CheckArea", [1, 1, 1, 0] } }); // Dresser
                        }
                        placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.Containers, style: Deco[S.Chest], chance: 80, add: new() { { "CheckArea", [0, 1, 1, 0] } }); // Chest
                    }
                    
                    #endregion


                    #region top room: fancy wallpaper, with paintings and windows

                    // wooden beam
                    if (freeR.YTiles >= 8) // if less than 8 tiles, there won't be enough space for the lanterns to look good
                    {
                        int lowerStuco, upperStuco; // room height where the fancy wallpaper starts and ends

                        y = freeR.Y1 - 4;
                        Tile tile;

                        for (x = freeR.X0; x <= freeR.X1; x++)
                        {
                            tile = Main.tile[x, y];
                            if (!tile.HasTile && !(tile.WallType == 0))
                            {
                                WorldGen.PlaceTile(x, y, TileID.BorealBeam);
                                WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                            }
                            if ((tile.TileType == TileID.GrandfatherClocks || tile.TileType == TileID.PottedPlants1) && !(tile.WallType == 0))
                            {
                                WorldGen.KillWall(x, y);
                                WorldGen.PlaceWall(x, y, WallID.BorealWood);
                                WorldGen.paintWall(x, y, (byte)Deco[S.StylePaint]);
                            }
                        }

                        lowerStuco = y - 1;
                        upperStuco = freeR.Y0 + 2;

                        // if room is too high, put another beam to make more use of the room height
                        if (freeR.YTiles >= 12)
                        {
                            lowerStuco = y - 1;
                            upperStuco = freeR.Y0 + 4;

                            // wooden beam above the fancy wallpaper
                            for (x = freeR.X0; x <= freeR.X1; x++)
                            {
                                if (!(Main.tile[x, upperStuco - 1].WallType == 0))
                                {
                                    WorldGen.PlaceTile(x, upperStuco - 1, TileID.BorealBeam);
                                    WorldGen.paintTile(x, upperStuco - 1, (byte)Deco[S.StylePaint]);
                                }
                            }
                        }

                        // put fancy wallpaper
                        Func.ReplaceWallArea(new Rectangle2P(freeR.X0, upperStuco, freeR.X1, lowerStuco, "dummyString"), Deco[S.PaintingWallpaper]);

                        if (lowerStuco == upperStuco)
                        {
                            // fancy wallpaper below the one wooden beam so it looks nicer
                            Func.ReplaceWallArea(new Rectangle2P(freeR.X0, lowerStuco + 2, freeR.X1, lowerStuco + 2, "dummyString"), Deco[S.PaintingWallpaper]);
                        }

                        //painting
                        (bool success, Rectangle2P paintingArea, int paintingType, int failReason) paintingResult =  PlacePainting(new Rectangle2P(freeR.X0, upperStuco, freeR.X1, lowerStuco, "dummyString"), Deco[S.StyleSave], centerErrorX: -88);
                        Rectangle2P windowLeft = Rectangle2P.Empty;
                        Rectangle2P windowRight = Rectangle2P.Empty;

                        if (paintingResult.success)
                        {
                            if ((lowerStuco - upperStuco >= 3) && (paintingResult.paintingArea.X0 - freeR.X0) >= 4) // check for a reasonable size of the window space
                            {
                                windowLeft  = new(freeR.X0 + 1, upperStuco + 1, paintingResult.paintingArea.X0 - 2, lowerStuco - 1, "dummyString");
                                windowRight = new(paintingResult.paintingArea.X1 + 2, upperStuco + 1, freeR.X1 - 1, lowerStuco - 1, "dummyString");
                            }
                        }
                        else
                        {
                            if ((paintingResult.failReason == 1 || paintingResult.failReason == 4) && (lowerStuco - upperStuco >= 3) && Chance.Perc(75)) // put windows without a painting
                            {
                                if (Chance.Simple())
                                {
                                    windowLeft = new(freeR.X0 + 1, upperStuco + 1, doors[Door.Up].doorRect.X0 - 1, lowerStuco - 1, "dummyString");
                                    windowRight = new(doors[Door.Up].doorRect.X1 + 1, upperStuco + 1, freeR.X1 - 1, lowerStuco - 1, "dummyString");
                                }
                                else
                                {
                                    windowLeft = new(freeR.X0 + 1, upperStuco + 1, doors[Door.Up].doorRect.X0, lowerStuco - 1, "dummyString");
                                    windowRight = new(doors[Door.Up].doorRect.X1 + 1, upperStuco + 1, freeR.X1, lowerStuco - 1, "dummyString");
                                }
                            }
                            else if (paintingResult.failReason == 2)
                            {
                                ((bool, bool) success, (Rectangle2P, Rectangle2P) paintingArea, (int, int) paintingType, (int, int) failReason) paintingsResult = Place2Paintings(area: new Rectangle2P(freeR.X0, upperStuco, freeR.X1, lowerStuco, "dummyString"),
                                                                                                                                                                                  style: Deco[S.StyleSave],
                                                                                                                                                                                  placeMode: WorldGen.genRand.Next(11,13),
                                                                                                                                                                                  allowType: (byte)paintingResult.paintingType);
                                
                                if ((lowerStuco - upperStuco >= 3) && (paintingsResult.paintingArea.Item2.X0 - paintingsResult.paintingArea.Item1.X1) >= 4) // check for a reasonable size of the window space
                                {
                                    windowLeft = new(paintingsResult.paintingArea.Item1.X1 + 2, upperStuco + 1, paintingsResult.paintingArea.Item2.X0 - 2, lowerStuco - 1, "dummyString");
                                    windowRight = Rectangle2P.Empty;
                                }
                            }
                        }

                        // put windows
                        if (!windowLeft.IsEmpty())
                        {
                            Func.ReplaceWallArea(windowLeft, WallID.Glass);
                            Func.ReplaceWallArea(windowRight, WallID.Glass);

                            rememberPos.Clear();
                            y = windowLeft.Y1;
                            for (int i = 0; i < windowLeft.XTiles; i++)
                            {
                                x = windowLeft.X0 + i;

                                if (!(Main.tile[x, y].WallType == 0))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                    WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                    rememberPos.Add((x, y));
                                }
                            }
                            foreach (var item in rememberPos)
                            {
                                // make the platform be a "half brick"... e.g. make it appear at the bottom of the tile, to make it look like a window board
                                // has to be done in a separate loop or there are some pixel gaps in between the platform tiles...don't know why
                                tile = Main.tile[item.x, item.y];
                                tile.IsHalfBlock = true;

                                // same effect can be realized by pounding the tile 3 times with a hammer:
                                //WorldGen.PoundPlatform(item.x, item.y);
                                //WorldGen.PoundPlatform(item.x, item.y);
                                //WorldGen.PoundPlatform(item.x, item.y);
                            }

                            rememberPos.Clear();
                            y = windowRight.Y1;
                            for (int i = 0; i < windowRight.XTiles; i++)
                            {
                                x = windowRight.X0 + i;

                                if (!(Main.tile[x, y].WallType == 0))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                    WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                    rememberPos.Add((x, y));
                                }
                            }
                            foreach (var item in rememberPos)
                            {
                                tile = Main.tile[item.x, item.y];
                                tile.IsHalfBlock = true;
                            }
                        }
                    }
                    #endregion

                    // lantern left
                    placed = false;
                    x = freeR.XCenter - WorldGen.genRand.Next(3, freeR.XDiff / 2);
                    y = freeR.Y0;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: Deco[S.Lantern]); // Lantern
                    if (placed) Func.UnlightLantern(x, y);

                    // lantern right
                    placed = false;
                    x = freeR.XCenter + 1 + WorldGen.genRand.Next(3, freeR.XDiff / 2);
                    y = freeR.Y0;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: Deco[S.Lantern]); // Lantern
                    if (placed) Func.UnlightLantern(x, y);



                    Func.PlaceStinkbug(freeR);

                    break;

                case 1: // kitchen with shelves

                    #region wooden beam arch at floor and hanging chains
                    for (y = freeR.Y1 - 3; y <= freeR.Y1; y++)
                    {
                        WorldGen.PlaceTile(doors[Door.Down].doorRect.X0, y, TileID.BorealBeam);
                        WorldGen.paintTile(doors[Door.Down].doorRect.X0, y, (byte)Deco[S.StylePaint]);

                        WorldGen.PlaceTile(doors[Door.Down].doorRect.X1, y, TileID.BorealBeam);
                        WorldGen.paintTile(doors[Door.Down].doorRect.X1, y, (byte)Deco[S.StylePaint]);
                    }
                    for (x = doors[Door.Down].doorRect.X0; x <= doors[Door.Down].doorRect.X1; x++)
                    {
                        WorldGen.PlaceTile(x, freeR.Y1 - 4, TileID.BorealBeam);
                        WorldGen.paintTile(x, freeR.Y1 - 4, (byte)Deco[S.StylePaint]);
                    }

                    if (Chance.Perc(85))
                    {
                        x = doors[Door.Down].doorRect.X0 - 1;
                        y = freeR.Y1 - 3;
                        placed = WorldGen.PlaceTile(x, y, TileID.Torches, style: 0); // normal torch
                        if (placed) Func.Unlight1x1(x, y);
                    }
                    if (Chance.Perc(85))
                    {
                        x = doors[Door.Down].doorRect.X1 + 1;
                        y = freeR.Y1 - 3;
                        placed = WorldGen.PlaceTile(x, y, TileID.Torches, style: 0); // normal torch
                        if (placed) Func.Unlight1x1(x, y);
                    }

                    // chains hanging down
                    if (((freeR.Y1 - 5) - freeR.Y0) >= 1) // at least two tiles or it looks weird
                    {
                        for (y = freeR.Y0; y <= freeR.Y1 - 5; y++)
                        {
                            WorldGen.PlaceTile(doors[Door.Down].doorRect.X0 - 1, y, TileID.Chain);
                            WorldGen.PlaceTile(doors[Door.Down].doorRect.X1 + 1, y, TileID.Chain);
                        }
                    }
                    #endregion

                    #region floor deco: cooking pot, bar table, fireplace, sink
                    area1 = new Rectangle2P(freeR.X0 + 1, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");
                    area2 = doors[Door.Down].doorRect.CloneAndMove(0, -1);

                    Func.TryPlaceTile(area1, area2, TileID.CookingPots, style: 0); // cooking pot

                    placeResult = Func.TryPlaceTile(area1, area2, TileID.Tables, style: 17, chance: 85); // wooden bar (table)
                    if (placeResult.success)
                    {
                        // put deco on bar
                        area3 = new Rectangle2P(placeResult.x - 1, placeResult.y - 2, placeResult.x + 1, placeResult.y - 2, "dummyString");
                        Func.TryPlaceTile(area3, noBlock, TileID.FoodPlatter, style: 17, chance: 50); // food plate
                        placeResult = Func.TryPlaceTile(area3, noBlock, TileID.Candles, style: 0, chance: 50); // normal candle (blends better with the wooden bar)
                        if (placeResult.success) Func.Unlight1x1(placeResult.x, placeResult.y);
                        Func.TryPlaceTile(area3, noBlock, TileID.Bottles, style: 4, chance: 50); // Mug
                        Func.TryPlaceTile(area3, noBlock, TileID.Bottles, style: 3, chance: 50); // Pink Vase
                        Func.TryPlaceTile(area3, noBlock, TileID.Bottles, style: 8, chance: 50); // Chalice
                    }

                    placeResult = Func.TryPlaceTile(area1, area2, TileID.Fireplace, chance: 85); // Fireplace
                    if (placeResult.success) Func.UnlightFireplace(placeResult.x, placeResult.y);

                    Func.TryPlaceTile(area1, area2, TileID.Sinks, style: 0, chance: 85); // wooden sink

                    Func.TryPlaceTile(area1, area2, TileID.TrashCan, style: 0, chance: 85); // trash can

                    Func.TryPlaceTile(area1, area2, TileID.Chairs, style: 21, chance: 85); // bar stool

                    Func.TryPlaceTile(area1, area2, TileID.WorkBenches, style: 0, chance: 85); // wooden work bench
                    #endregion

                    #region shelves

                    randomItems.Clear();
                    randomItems.Add((TileID.Bowls, 0, 50)); // bowl
                    randomItems.Add((TileID.Bowls, 2, 50)); // fancy dishes
                    randomItems.Add((TileID.FoodPlatter, style: 17, 50)); // food plate
                    randomItems.Add((TileID.Bottles, 0, 50)); // Bottle
                    randomItems.Add((TileID.Bottles, 1, 50)); // Lesser Healing Potion
                    randomItems.Add((TileID.Bottles, 2, 50)); // Lesser Mana Potion
                    randomItems.Add((TileID.Bottles, 3, 50)); // Pink Vase
                    randomItems.Add((TileID.Bottles, 4, 50)); // Mug
                    randomItems.Add((TileID.Bottles, 5, 50)); // Dynasty Cup
                    randomItems.Add((TileID.Bottles, 6, 50)); // Wine Glass
                    randomItems.Add((TileID.Bottles, 7, 50)); // Honey Cup
                    randomItems.Add((TileID.Bottles, 8, 50)); // Chalice
                    randomItems.Add((TileID.TeaKettle, 0, 50)); // Teapot
                    randomItems.Add((TileID.ClayPot, 0, 50)); // ClayPot

                    randomItems2.Clear();
                    randomItems2.Add((TileID.TeaKettle, 0, 50)); // Teapot
                    randomItems2.Add((TileID.FishingCrate, 0, 50)); // wooden fishing crate
                    randomItems2.Add((TileID.Containers, 5, 50)); // wooden barrel
                    randomItems2.Add((TileID.Kegs, 0, 50)); // wooden keg

                    List<short> food =
                    [
                        ItemID.Apple,
                        ItemID.Apricot,
                        ItemID.Cherry,
                        ItemID.Plum,
                        ItemID.RoastedDuck,
                        ItemID.Bass,
                        ItemID.Trout,
                        ItemID.TundraTrout,
                    ];


                    // define blocked area for random placement
                    area2 = new Rectangle2P(doors[Door.Up].doorRect.X0, doors[Door.Up].doorRect.Y1, doors[Door.Up].doorRect.X1, doors[Door.Down].doorRect.Y0, "dummyString");

                    // init dynamic var
                    int kitchenShelfHeight = freeR.Y1 - 4;
                    int kitchenShelfMinTiles = 6; // 4 for floor deco, 5 for shelf, 6 for 1-tile items
                    bool first = true;
                    rememberPos.Clear();

                    while (freeR.YTiles >= kitchenShelfMinTiles)
                    {
                        // create shelves
                        if (Chance.Perc(80) || first)
                        {
                            for (x = freeR.X0; x <= doors[Door.Down].doorRect.X0 - 1; x++)
                            {
                                placed = WorldGen.PlaceTile(x, kitchenShelfHeight, TileID.Platforms, style: 19); // Boreal wood platform
                                if(placed) WorldGen.paintTile(x, kitchenShelfHeight, (byte)Deco[S.StylePaint]);
                            }
                        }

                        if (Chance.Perc(80) || first)
                        {
                            for (x = doors[Door.Down].doorRect.X1 + 1; x <= freeR.X1; x++)
                            {
                                placed = WorldGen.PlaceTile(x, kitchenShelfHeight, TileID.Platforms, style: 19); // Boreal wood platform
                                if (placed) WorldGen.paintTile(x, kitchenShelfHeight, (byte)Deco[S.StylePaint]);
                            }
                        }

                        // try stacking wooden crates from last shelf
                        if (rememberPos.Count > 0) // wooden crates have been placed succesfully on the lower shelf
                        {
                            for (int posNum = 0; posNum <= rememberPos.Count - 1; posNum++) // for every placed crate
                            {
                                if (Chance.Simple()) WorldGen.Place2x2(rememberPos[posNum].x + 1, rememberPos[posNum].y - 2, TileID.FishingCrate, style: 0); // try stack another
                                // Explanation: PlaceTile didn't work at all and looks like Place2x2 doesn't use the same anchor point as PlaceTile...that's why the +1 is necessary
                            }
                        }
                        rememberPos.Clear();

                        //put deco on shelf
                        area1 = new Rectangle2P(freeR.X0, kitchenShelfHeight - 1, freeR.X1, kitchenShelfHeight - 1, "dummyString");
                        unusedXTiles = freeR.XTiles - doors[Door.Down].doorRect.XTiles;

                        if (freeR.YTiles >= 7) // high enought for 2 YTile stuff?
                        {
                            for (int i = 1; i <= 3; i++) // just 3 is enough
                            {
                                num = WorldGen.genRand.Next(randomItems2.Count);
                                placeResult = Func.TryPlaceTile(area1, area2, randomItems2[num].TileID, style: randomItems2[num].style, chance: randomItems2[num].chance); // one random item of the 2x2-tiles list

                                if (placeResult.success)
                                {
                                    unusedXTiles -= 2;
                                    if(randomItems2[num].TileID == TileID.FishingCrate) rememberPos.Add((placeResult.x, placeResult.y)); // remember placement position for later stacking try
                                }
                            }
                        }

                        for (int i = 1; i <= unusedXTiles + 3; i++) // unusedXTiles + a little more to compensate Chance fails
                        {
                            num = WorldGen.genRand.Next(randomItems.Count);
                            Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the 1-tile list
                        }

                        // ItemFrame with food in the middle
                        if (Chance.Perc(75) && (freeR.Y0 <= kitchenShelfHeight - 2))
                        {
                            Func.PlaceItemFrame(freeR.XCenter, kitchenShelfHeight - 2, Deco[S.BackWall], Deco[S.StylePaint], item: food[WorldGen.genRand.Next(food.Count)]);
                        }

                        kitchenShelfMinTiles += 3;
                        kitchenShelfHeight -= 3;
                        first = false;
                    }
                    #endregion

                    // try placing some hanging pots at the ceiling
                    area1 = new Rectangle2P(freeR.X0, freeR.Y0, freeR.X1, freeR.Y0, "dummyString");
                    if (doors[Door.Up].doorExist) area2 = doors[Door.Up].doorRect.CloneAndMove(0, 1);
                    else                          area2 = Rectangle2P.Empty;

                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: Deco[S.HangingPot], chance: 60); // hanging pot with herb
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: 0, chance: 60); // hanging empty pot
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: Deco[S.HangingPot], chance: 60); // hanging pot with herb
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: 5, chance: 60); // hanging pot with blink rot


                    Func.PlaceStinkbug(freeR);

                    break;

                case 2: // library style 1

                    // wooden beams left and right
                    for (y = freeR.Y0; y <= freeR.Y1; y++)
                    {
                        WorldGen.PlaceTile(doors[Door.Down].doorRect.X0 - 1, y, TileID.BorealBeam);
                        WorldGen.paintTile(doors[Door.Down].doorRect.X0 - 1, y, (byte)Deco[S.StylePaint]);

                        WorldGen.PlaceTile(doors[Door.Down].doorRect.X1 + 1, y, TileID.BorealBeam);
                        WorldGen.paintTile(doors[Door.Down].doorRect.X1 + 1, y, (byte)Deco[S.StylePaint]);
                    }
                    WorldGen.SlopeTile(doors[Door.Up].doorRect.X0 - 1, doors[Door.Up].doorRect.Y1, 0); // undo possible slope of the updoor so the beams blend better
                    WorldGen.SlopeTile(doors[Door.Up].doorRect.X1 + 1, doors[Door.Up].doorRect.Y1, 0); // undo possible slope of the updoor so the beams blend better


                    # region deco in the middle of the room

                    randomItems.Clear();
                    randomItems.Add((TileID.Statues, 0, 60)); // armor statue
                    randomItems.Add((TileID.Statues, 1, 60)); // angel statue
                    randomItems.Add((TileID.GrandfatherClocks, Deco[S.Clock], 60)); // Clock
                    randomItems.Add((TileID.PottedPlants1, 3, 60)); // potted forest tree

                    // statue/clock at floor
                    if (Chance.Perc(75))
                    {
                        x = freeR.XCenter;
                        if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of statue (2 xTiles) in an odd xTiles rooms
                        y = freeR.Y1;
                        
                        num = WorldGen.genRand.Next(randomItems.Count);
                        if (randomItems[num].TileID == TileID.PottedPlants1)
                        {
                            placed = WorldGen.PlaceObject(x + 1, y, TileID.PottedPlants1, style: 3); // potted forest tree
                            if (placed) Func.PaintArea(new Rectangle2P(x, y - 4, x + 1, y, "dummyString"), Deco[S.StylePaint]);
                        }
                        else WorldGen.PlaceTile(x, y, randomItems[num].TileID, style: randomItems[num].style); // random item from the list
                    }


                    // item frames hanging on 2/3's room height
                    num = WorldGen.genRand.Next(4);
                    switch (num)
                    {
                        case 0: // 3 ItemFrames in a triangle shape
                            if (Chance.Perc(75))
                            {
                                // frame 1
                                x = freeR.XCenter - 1;
                                y = freeR.Y0 + freeR.YTiles / 3 - 1;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);

                                // frame 2
                                x = freeR.XCenter + 1;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);

                                // frame 3
                                x = freeR.XCenter;
                                if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of item frame (2 xTiles) in an odd xTiles rooms
                                y = freeR.Y0 + freeR.YTiles / 3 + 1;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);
                            }
                            break;

                        case 1: // 2 ItemFrames next to each other
                            if (Chance.Perc(75))
                            {
                                // frame 1
                                x = freeR.XCenter - 1;
                                y = freeR.Y0 + freeR.YTiles / 3;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);

                                // frame 2
                                x = freeR.XCenter + 1;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);
                            }
                            break;


                        case 2: // 1 item frame
                            if (Chance.Perc(75))
                            {
                                x = freeR.XCenter;
                                if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of item frame (2 xTiles) in an odd xTiles rooms
                                y = freeR.Y0 + freeR.YTiles / 3;
                                Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);
                            }
                            break;

                        case 3: // 1 painting
                            if (Chance.Perc(75))
                            {
                                if(freeR.IsEvenX())
                                {
                                    x = freeR.XCenter;
                                    y = freeR.Y0 + freeR.YTiles / 3;
                                    area1 = new Rectangle2P(x, y - 1, x + 1, y + 1, "dummyString");
                                    Place2x3PaintingByStyle(area1, Deco[S.StyleSave], true);
                                }
                                else
                                {
                                    x = freeR.XCenter;
                                    y = freeR.Y0 + freeR.YTiles / 3;
                                    area1 = new Rectangle2P(x - 1, y - 1, x + 1, y + 1, "dummyString");
                                    Place3x3PaintingByStyle(area1, Deco[S.StyleSave], true);
                                }
                            }
                            break;

                        default:
                            break;
                    }

                    if ((freeR.YTiles >= 12))
                    {
                        
                    }
                    else if ((freeR.YTiles >= 8))
                    {
                        
                    }
                    #endregion

                    //create sides
                    area1 = new Rectangle2P(freeR.X0, freeR.Y1, doors[Door.Down].doorRect.X0 - 2, freeR.Y1, "dummyString"); // left side
                    area2 = new Rectangle2P(doors[Door.Down].doorRect.X1 + 2, freeR.Y1, freeR.X1, freeR.Y1, "dummyString"); // right side

                    if (Chance.Simple()) // randomize side
                    {
                        area3 = area1; //change area1 and 2 by using area3
                        area1 = area2;
                        area2 = area3;
                    }

                    // side 1: bookcase or piano and candelabra on it
                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Bookcases, style: Deco[S.Bookcase], chance: 75);  // Bookcase
                    if (placeResult.success)
                    {
                        placeResult = Func.TryPlaceTile(area1.CloneAndMove(0, -4), noBlock, TileID.Candelabras, style: Deco[S.Candelabra], chance: 75); // Try put candelabra on bookcase
                        if (placeResult.success) Func.UnlightCandelabra(placeResult.x, placeResult.y); // unlight candelabra
                    }
                    else if( Chance.Perc(75))
                    {
                        placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Pianos, style: Deco[S.Piano], add: new() { { "CheckArea", [1, 1, 1, 0] } }); // piano
                        if (placeResult.success)
                        {
                            placeResult = Func.TryPlaceTile(area1.CloneAndMove(0, -2), noBlock, TileID.Candelabras, style: Deco[S.Candelabra], chance: 75); // Try put candelabra on piano
                            if (placeResult.success) Func.UnlightCandelabra(placeResult.x, placeResult.y); // unlight candelabra
                        }
                    }
                        

                    // side 2: chest, workbench and candle and chair
                    rememberPos.Clear(); //init
                    placeResult = Func.TryPlaceTile(area2, noBlock, TileID.WorkBenches, style: Deco[S.Workbench], chance: 70, add: new() { { "CheckArea", [0, 1, 0, 0] } }); // Workbench
                    if (placeResult.success)
                    {
                        rememberPos.Add((placeResult.x, placeResult.y));
                        placeResult = Func.TryPlaceTile(area2.CloneAndMove(0, -1), noBlock, TileID.Candles, style: Deco[S.Candle], chance: 75); // Try put candle on workbench
                        if (placeResult.success) Func.Unlight1x1(placeResult.x, placeResult.y); // unlight candle

                        x = rememberPos[0].x - 1;
                        y = rememberPos[0].y;
                        if ( !Main.tile[x, y].HasTile && freeR.Contains(x,y)) // left of workbench is free and not on a side door
                        {
                            if (Chance.Simple()) WorldGen.PlaceTile(x, y, TileID.Chairs, style: Deco[S.Chair]); // try place chair
                            if (Main.tile[x, y].HasTile) Func.ChairTurnRight(x, y); // if placed, change the facing direction of the chair
                        }

                        x = rememberPos[0].x + 2;
                        y = rememberPos[0].y;
                        if (!Main.tile[x, y].HasTile && freeR.Contains(x, y)) // right of workbench is free and not on a side door
                        {
                            if (Chance.Simple()) WorldGen.PlaceTile(x, y, TileID.Chairs, style: Deco[S.Chair]); // try place chair
                        }
                    }
                    else
                    {
                        placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Tables, style: Deco[S.Table], chance: 85, add: new() { { "CheckArea", [1, 1, 1, 0] } }); // Table
                        if (placeResult.success)
                        {
                            x = placeResult.x - WorldGen.genRand.Next(2);
                            y = placeResult.y - 2;
                            WorldGen.PlaceTile(x, y, TileID.MusicBoxes, style: 0); // music box
                            Func.PaintArea(new Rectangle2P(x, y - 1, x + 1, y, "dummyString"), Deco[S.StylePaint]);

                        }
                        placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Containers, style: Deco[S.Chest], chance: 70, add: new() { { "CheckArea", [0, 1, 1, 0] } }); // Chest
                        if (placeResult.success)
                        {
                            chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                            if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2));
                        }
                    }

                    // still side 2: shelf with books
                    if (freeR.YTiles >= 6)
                    {
                        area2.Move(0, -4);
                        for (x = area2.X0; x <= area2.X1; x++)
                        {
                            WorldGen.PlaceTile(x, area2.Y0, TileID.Platforms, style: 19); // Boreal wood platform
                            WorldGen.paintTile(x, area2.Y0, (byte)Deco[S.StylePaint]);

                            if(Chance.Simple()) WorldGen.PlaceTile(x, area2.Y0 - 1, TileID.Books, style: WorldGen.genRand.Next(5)); // normal book
                        }
                    }

                    //__________________________________________________________________________________________________________________________________
                    // second part of the room...basically the same but sides randomized again
                    area1 = new Rectangle2P(freeR.X0, freeR.Y1 - 7, doors[Door.Down].doorRect.X0 - 2, freeR.Y1 - 7, "dummyString"); // left side
                    area2 = new Rectangle2P(doors[Door.Down].doorRect.X1 + 2, freeR.Y1 - 7, freeR.X1, freeR.Y1 - 7, "dummyString"); // right side
                    if (freeR.YTiles >= 9)
                    {
                        // platform left
                        for (x = area1.X0; x <= area1.X1; x++)
                        {
                            WorldGen.PlaceTile(x, area1.Y0, TileID.Platforms, style: 19); // Boreal wood platform
                            WorldGen.paintTile(x, area1.Y0, (byte)Deco[S.StylePaint]);
                        }

                        // platform right
                        for (x = area2.X0; x <= area2.X1; x++)
                        {
                            WorldGen.PlaceTile(x, area2.Y0, TileID.Platforms, style: 19); // Boreal wood platform
                            WorldGen.paintTile(x, area2.Y0, (byte)Deco[S.StylePaint]);
                        }

                        area1.Move(0, -1); // 1 tile above platform
                        area2.Move(0, -1); //

                        if (freeR.YTiles >= 12)
                        {
                            if (Chance.Simple()) // randomize side
                            {
                                area3 = area1; //change area1 and 2 by using area3
                                area1 = area2;
                                area2 = area3;
                            }

                            // side 1: bookcase and lamp
                            Func.TryPlaceTile(area1, noBlock, TileID.Bookcases, style: Deco[S.Bookcase]); // bookcase
                            placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Lamps, style: Deco[S.Lamp], chance: 75); // lamp
                            if (placeResult.success) Func.UnlightLamp(placeResult.x, placeResult.y);

                            // side 2: sofa and books
                            placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Benches, style: Deco[S.Sofa], chance: 80); // Sofa
                            if (!placeResult.success)
                            {
                                // place books on shelf
                                for (x = area2.X0; x <= area2.X1; x++)
                                {
                                    if (Chance.Simple()) WorldGen.PlaceTile(x, area2.Y0, TileID.Books, style: WorldGen.genRand.Next(5)); // normal book
                                }
                            }
                            placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Candelabras, style: Deco[S.Candelabra], chance: 80); // Candelabra
                            if (placeResult.success) Func.UnlightCandelabra(placeResult.x, placeResult.y);
                            else
                            {
                                placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Candles, style: Deco[S.Candle]); // Candle
                                if (placeResult.success) Func.Unlight1x1(placeResult.x, placeResult.y);
                            }

                            area2.Move(0, -2); // shift area to next platform height
                            // another book shelf
                            for (x = area2.X0; x <= area2.X1; x++)
                            {
                                WorldGen.PlaceTile(x, area2.Y0, TileID.Platforms, style: 19); // Boreal wood platform
                                WorldGen.paintTile(x, area2.Y0, (byte)Deco[S.StylePaint]);

                                if (Chance.Simple()) WorldGen.PlaceTile(x, area2.Y0 - 1, TileID.Books, style: WorldGen.genRand.Next(5)); // normal book
                            }
                        }
                        else // too few YTiles for the bookcase
                        {
                            // place books on left shelf
                            for (x = area1.X0; x <= area1.X1; x++)
                            {
                                if (Chance.Simple()) WorldGen.PlaceTile(x, area1.Y0, TileID.Books, style: WorldGen.genRand.Next(5)); // normal book
                            }

                            // place books on right shelf
                            for (x = area2.X0; x <= area2.X1; x++)
                            {
                                if (Chance.Simple()) WorldGen.PlaceTile(x, area2.Y0, TileID.Books, style: WorldGen.genRand.Next(5)); // normal book
                            }
                        }
                    }

                    // ceiling: banners
                    if (freeR.YTiles >= 15)
                    {
                        if(!doors[Door.Up].doorExist)
                        {
                            if (freeR.XTiles % 2 == 1) WorldGen.PlaceTile(freeR.XCenter, freeR.Y0, TileID.Banners, style: Deco[S.Banner]); // banner
                            else
                            {
                                WorldGen.PlaceTile(freeR.XCenter, freeR.Y0, TileID.Banners, style: Deco[S.Banner]); // banner
                                WorldGen.PlaceTile(freeR.XCenter + 1, freeR.Y0, TileID.Banners, style: Deco[S.Banner]); // banner
                            }
                        }

                        area1 = new Rectangle2P(freeR.X0, freeR.Y0, doors[Door.Down].doorRect.X0 - 2, freeR.Y0, "dummyString"); // left side
                        area2 = new Rectangle2P(doors[Door.Down].doorRect.X1 + 2, freeR.Y0, freeR.X1, freeR.Y0, "dummyString"); // right side

                        Func.TryPlaceTile(area1, noBlock, TileID.Banners, style: Deco[S.Banner], chance: 75); // banner
                        Func.TryPlaceTile(area1, noBlock, TileID.Banners, style: Deco[S.Banner], chance: 75); // banner
                        Func.TryPlaceTile(area2, noBlock, TileID.Banners, style: Deco[S.Banner], chance: 75); // banner
                        Func.TryPlaceTile(area2, noBlock, TileID.Banners, style: Deco[S.Banner], chance: 75); // banner
                    }



                    // finalization
                    Func.PlaceStinkbug(freeR);
                    
                    break;

                case 3: // armory showroom

                    List<(int, int, int)> armorPool =    // the possible styles of armor
                    [
                        (ArmorIDs.Head.CopperHelmet, ArmorIDs.Body.CopperChainmail, ArmorIDs.Legs.CopperGreaves),
                        (ArmorIDs.Head.TinHelmet, ArmorIDs.Body.TinChainmail, ArmorIDs.Legs.TinGreaves),
                        (ArmorIDs.Head.IronHelmet, ArmorIDs.Body.IronChainmail, ArmorIDs.Legs.IronGreaves),
                        (ArmorIDs.Head.LeadHelmet, ArmorIDs.Body.LeadChainmail, ArmorIDs.Legs.LeadGreaves),
                        (ArmorIDs.Head.NinjaHood, ArmorIDs.Body.NinjaShirt, ArmorIDs.Legs.NinjaPants),

                        (ArmorIDs.Head.WoodHelmet, ArmorIDs.Body.WoodBreastplate, ArmorIDs.Legs.WoodGreaves),
                        (ArmorIDs.Head.BorealWoodHelmet, ArmorIDs.Body.BorealWoodBreastplate, ArmorIDs.Legs.BorealWoodGreaves),
                        (ArmorIDs.Head.ShadewoodHelmet, ArmorIDs.Body.ShadewoodBreastplate, ArmorIDs.Legs.ShadewoodGreaves),
                        (ArmorIDs.Head.AshWoodHelmet, ArmorIDs.Body.AshWoodBreastplate, ArmorIDs.Legs.AshWoodGreaves),
                    ];

                    // left Mannequin #1
                    if (Chance.Perc(75))
                    {
                        int setNum = WorldGen.genRand.Next(armorPool.Count);
                        (bool successs, int dollID) = Func.PlaceMannequin(freeR.X0 + 1, freeR.Y1, armorPool[setNum], female: Chance.Simple(), direction: 1);
                        if (successs)
                        {
                            armorPool.RemoveAt(setNum); // make sure that this amor set won't appear again in this room

                            //put vitrine
                            int moveCloser = 0; // init
                            if (!doors[Door.Left].doorExist)
                            {
                                for (int j = freeR.Y1 - 3; j <= freeR.Y1; j++)
                                {
                                    WorldGen.PlaceTile(freeR.X0, j, TileID.Glass);
                                    WorldGen.PlaceTile(freeR.X0 + 3, j, TileID.Glass);
                                }
                                WorldGen.PlaceTile(freeR.X0 + 1, freeR.Y1 - 3, TileID.Glass);
                                WorldGen.PlaceTile(freeR.X0 + 2, freeR.Y1 - 3, TileID.Glass);
                            }
                            else moveCloser = 1; // no vitrine, move lamps closer to Mannequin

                            // lamp before vitrine
                            if (freeR.X0 + 4 - moveCloser < doors[Door.Down].doorRect.X0)
                            {
                                WorldGen.PlaceTile(freeR.X0 + 4 - moveCloser, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]);
                                Func.UnlightLamp(freeR.X0 + 4 - moveCloser, freeR.Y1);
                            }
                            else // put a torch on the Mannequin vitrine
                            {
                                WorldGen.PlaceTile(freeR.X0 + 4, freeR.Y1 - 3, TileID.Torches, style: Deco[S.Torch]);
                                Func.Unlight1x1(freeR.X0 + 4, freeR.Y1 - 3);
                            }
                        }
                    }
                    else
                    {
                        if (Chance.Simple()) Func.PlaceLargePile(freeR.X0 + 2, freeR.Y1, 6, 0);
                    }

                    // right Mannequin #1
                    if (Chance.Perc(75))
                    {
                        int setNum = WorldGen.genRand.Next(armorPool.Count);
                        (bool successs, int dollID) = Func.PlaceMannequin(freeR.X1 - 2, freeR.Y1, armorPool[setNum], female: Chance.Simple(), direction: -1);
                        if (successs)
                        {
                            armorPool.RemoveAt(setNum); // make sure that this amor set won't appear again in this room

                            //put vitrine
                            int moveCloser = 0; // init
                            if (!doors[Door.Right].doorExist)
                            {
                                for (int j = freeR.Y1 - 3; j <= freeR.Y1; j++)
                                {
                                    WorldGen.PlaceTile(freeR.X1, j, TileID.Glass);
                                    WorldGen.PlaceTile(freeR.X1 - 3, j, TileID.Glass);
                                }
                                WorldGen.PlaceTile(freeR.X1 - 1, freeR.Y1 - 3, TileID.Glass);
                                WorldGen.PlaceTile(freeR.X1 - 2, freeR.Y1 - 3, TileID.Glass);
                            }
                            else moveCloser = 1; // no vitrine, move lamps closer to Mannequin

                            // lamp before vitrine
                            if (freeR.X1 - 4 + moveCloser > doors[Door.Down].doorRect.X1) // don't put lamp on platform
                            {
                                WorldGen.PlaceTile(freeR.X1 - 4 + moveCloser, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]);
                                Func.UnlightLamp(freeR.X1 - 4 + moveCloser, freeR.Y1);
                            }
                            else // put a torch on the Mannequin vitrine
                            {
                                WorldGen.PlaceTile(freeR.X1 - 4, freeR.Y1 - 3, TileID.Torches, style: Deco[S.Torch]);
                                Func.Unlight1x1(freeR.X1 - 4, freeR.Y1 - 3);
                            }
                        }
                    }
                    else
                    {
                        if (Chance.Perc(80)) Func.PlaceLargePile(freeR.X1 - 2, freeR.Y1, 6, 0);
                    }

                    // helmet rack between the #1 Mannequins
                    if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X0, freeR.Y1 - 2, TileID.Painting3X3, style: 43);
                    if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X1, freeR.Y1 - 2, TileID.Painting3X3, style: 43);



                    //__________________________________________________________________________________________________________________________________
                    // second floor of the room...basically the same again

                    if (freeR.YTiles >= 9)
                    {
                        // left Mannequin #2

                        // put platform
                        for (int i = freeR.X0; i < doors[Door.Down].doorRect.X0; i++)
                        {
                            WorldGen.PlaceTile(i, freeR.Y1 - 4, TileID.Platforms, style: Deco[S.DecoPlat]);
                            WorldGen.paintTile(i, freeR.Y1 - 4, (byte)Deco[S.StylePaint]);
                        }

                        if (Chance.Perc(75))
                        {
                            int setNum = WorldGen.genRand.Next(armorPool.Count);
                            (bool successs, int dollID) = Func.PlaceMannequin(freeR.X0 + 1, freeR.Y1 - 5, armorPool[setNum], female: Chance.Simple(), direction: 1);
                            if (successs)
                            {
                                armorPool.RemoveAt(setNum); // make sure that this amor set won't appear again in this room

                                //put vitrine
                                for (int j = freeR.Y1 - 8; j <= freeR.Y1 - 5; j++)
                                {
                                    WorldGen.PlaceTile(freeR.X0, j, TileID.Glass);
                                    WorldGen.PlaceTile(freeR.X0 + 3, j, TileID.Glass);
                                }
                                WorldGen.PlaceTile(freeR.X0 + 1, freeR.Y1 - 8, TileID.Glass);
                                WorldGen.PlaceTile(freeR.X0 + 2, freeR.Y1 - 8, TileID.Glass);

                                // lamp before vitrine
                                if (freeR.X0 + 4 < doors[Door.Down].doorRect.X0)
                                {
                                    WorldGen.PlaceTile(freeR.X0 + 4, freeR.Y1 - 5, TileID.Lamps, style: Deco[S.Lamp]);
                                    Func.UnlightLamp(freeR.X0 + 4, freeR.Y1 - 5);
                                }
                                else // put a torch on the Mannequin vitrine
                                {
                                    WorldGen.PlaceTile(freeR.X0 + 4, freeR.Y1 - 8, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X0 + 4, freeR.Y1 - 8);
                                }
                            }
                        }
                        else
                        {
                            //put broken vitrine with "Mannequin remains"
                            for (int j = freeR.Y1 - 8; j <= freeR.Y1 - 5; j++)
                            {
                                if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X0, j, TileID.Glass);
                                if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X0 + 3, j, TileID.Glass);
                            }
                            if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X0 + 1, freeR.Y1 - 8, TileID.Glass);
                            if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X0 + 2, freeR.Y1 - 8, TileID.Glass);
                            if (Chance.Perc(80)) WorldGen.PlaceSmallPile(freeR.X0 + 1, freeR.Y1 - 5, 6, 1); //Small bones with skull
                        }

                        // right Mannequin #2

                        // put platform
                        for (int i = doors[Door.Down].doorRect.X1 + 1; i <= freeR.X1; i++)
                        {
                            WorldGen.PlaceTile(i, freeR.Y1 - 4, TileID.Platforms, style: Deco[S.DecoPlat]);
                            WorldGen.paintTile(i, freeR.Y1 - 4, (byte)Deco[S.StylePaint]);
                        }

                        if (Chance.Perc(75))
                        {
                            int setNum = WorldGen.genRand.Next(armorPool.Count);
                            (bool successs, int dollID) = Func.PlaceMannequin(freeR.X1 - 2, freeR.Y1 - 5, armorPool[setNum], female: Chance.Simple(), direction: -1);
                            if (successs)
                            {
                                armorPool.RemoveAt(setNum); // make sure that this amor set won't appear again in this room

                                //put vitrine
                                for (int j = freeR.Y1 - 8; j <= freeR.Y1 - 5; j++)
                                {
                                    WorldGen.PlaceTile(freeR.X1, j, TileID.Glass);
                                    WorldGen.PlaceTile(freeR.X1 - 3, j, TileID.Glass);
                                }
                                WorldGen.PlaceTile(freeR.X1 - 1, freeR.Y1 - 8, TileID.Glass);
                                WorldGen.PlaceTile(freeR.X1 - 2, freeR.Y1 - 8, TileID.Glass);

                                // lamp before vitrine
                                if (freeR.X1 - 4 > doors[Door.Down].doorRect.X1) // don't put lamp on platform
                                {
                                    WorldGen.PlaceTile(freeR.X1 - 4, freeR.Y1 - 5, TileID.Lamps, style: Deco[S.Lamp]);
                                    Func.UnlightLamp(freeR.X1 - 4, freeR.Y1 - 5);
                                }
                                else // put a torch the Mannequin vitrine
                                {
                                    WorldGen.PlaceTile(freeR.X1 - 4, freeR.Y1 - 8, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X1 - 4, freeR.Y1 - 8);
                                }
                            }
                        }
                        else
                        {
                            //put broken vitrine with "Mannequin remains"
                            for (int j = freeR.Y1 - 8; j <= freeR.Y1 - 5; j++)
                            {
                                if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X1, j, TileID.Glass);
                                if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X1 - 3, j, TileID.Glass);
                            }
                            if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X1 - 1, freeR.Y1 - 8, TileID.Glass);
                            if (Chance.Perc(65)) WorldGen.PlaceTile(freeR.X1 - 2, freeR.Y1 - 8, TileID.Glass);
                            if (Chance.Perc(80)) WorldGen.PlaceSmallPile(freeR.X1 - 2, freeR.Y1 - 5, 6, 1); //Small bones with skull
                        }

                        // sword rack between the #2 Mannequins
                        if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X0, freeR.Y1 - 7, TileID.Painting3X3, style: 45);
                        if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X1, freeR.Y1 - 7, TileID.Painting3X3, style: 45);

                        if (!Main.tile[doors[Door.Down].doorRect.X0 - 1, freeR.Y1 - 5].HasTile && !Main.tile[doors[Door.Down].doorRect.X1 + 1, freeR.Y1 - 5].HasTile) // bot first platforms tiles are free
                        {
                            WorldGen.KillTile(doors[Door.Down].doorRect.X0 - 1, freeR.Y1 - 4); // kill platform so the sword racks look nicer
                            WorldGen.KillTile(doors[Door.Down].doorRect.X1 + 1, freeR.Y1 - 4); // kill platform so the sword racks look nicer
                        }
                    }

                    if (freeR.YTiles >= 14)
                    {
                        if (Chance.Perc(75)) Func.PlaceWeaponRack(freeR.X0 + 2, freeR.Y0 + 2, paint: Deco[S.StylePaint], item: ItemID.TungstenBroadsword);
                        if (Chance.Perc(75)) Func.PlaceWeaponRack(freeR.X1 - 2, freeR.Y0 + 2, paint: Deco[S.StylePaint], item: ItemID.TungstenBroadsword, direction: 1);
                    }

                    // place banners at ceiling
                    // (checks only for left side, but rooms are symmetrical...so I can place on both sides)
                    if (freeR.X0 + 5 == doors[Door.Up].doorRect.X0 - 1) // 1 space + 3 tiles WeaponRack + 1 space = at corner of updoor
                    {
                        // left banner
                        if (Chance.Perc(70))
                        {
                            x = doors[Door.Up].doorRect.X0 - 1;
                            y = freeR.Y0;
                            if (Func.CheckFree(new Rectangle2P(x, y, x, y + 2, "dummyString")))
                            {
                                WorldGen.SlopeTile(x, y - 1, 0); // undo slope
                                WorldGen.PlaceTile(x, y, TileID.Banners, style: Deco[S.Banner]); // banner
                            } 
                        }

                        // right banner
                        if (Chance.Perc(70))
                        {
                            x = doors[Door.Up].doorRect.X1 + 1;
                            y = freeR.Y0;
                            if (Func.CheckFree(new Rectangle2P(x, y, x, y + 2, "dummyString")))
                            {
                                WorldGen.SlopeTile(x, y - 1, 0); // undo slope
                                WorldGen.PlaceTile(x, y, TileID.Banners, style: Deco[S.Banner]); // banner
                            }
                        }
                    }
                    else if (freeR.X0 + 5 < doors[Door.Up].doorRect.X0 - 1) // 1 space + 3 tiles WeaponRack + 1 space = before corner of updoor
                    {
                        // left banner
                        if (Chance.Perc(70))
                        {
                            x = freeR.X0 + 5;
                            y = freeR.Y0;
                            if (Func.CheckFree(new Rectangle2P(x, y, x, y + 2, "dummyString")))
                            {
                                WorldGen.PlaceTile(x, y, TileID.Banners, style: Deco[S.Banner]); // banner
                            } 
                        }

                        // right banner
                        if (Chance.Perc(70))
                        {
                            x = freeR.X1 - 5;
                            y = freeR.Y0;
                            if (Func.CheckFree(new Rectangle2P(x, y, x, y + 2, "dummyString")))
                            {
                                WorldGen.PlaceTile(x, y, TileID.Banners, style: Deco[S.Banner]); // banner
                            }
                        }
                    }
                    //else { }// 1 space + 3 tiles WeaponRack + 1 space = already inside updoor -> too few space to look good...do nothing

                    
                    break;

                case 4: // dormitory

                    // place the platforms for the "bunk beds" on the down door or there will be no place for the banners beside the bed in small rooms
                    // also to have more space to place chests
                    int PlatformLeftStart = doors[Door.Down].doorRect.X0;
                    int PlatformRightStart = doors[Door.Down].doorRect.X1;
                    int lastFloorHeight; // for later calculating remaining YTiles for the ceiling stuff

                    //__________________________________________________________________________________________________________________________________
                    // ground floor: dressers and lanterns

                    lastFloorHeight = freeR.Y1 + 1;

                    // left dresser
                    area1 = new Rectangle2P(freeR.X0 + 1, freeR.Y1, doors[Door.Down].doorRect.X0 - 2, freeR.Y1, "dummyString");
                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Dressers, style: Deco[S.Dresser], chance: 70); // Dresser
                    if (placeResult.success)
                    {
                        if (Chance.Perc(80))
                        {
                            area1 = new Rectangle2P(placeResult.x - 1, placeResult.y - 2, placeResult.x + 1, placeResult.y - 2, "dummyString");
                            placeResult2 = Func.TryPlaceTile(area1, noBlock, TileID.Candles, style: Deco[S.Candle], chance: 50); // Candle on dresser
                            if (placeResult2.success) Func.Unlight1x1(placeResult2.x, placeResult2.y);
                        }
                    }
                    else
                    {
                        placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Benches, style: Deco[S.Sofa], chance: 70); // Sofa
                    }

                    if (placeResult.success) // dresser or sofa
                    {
                        if (Chance.Perc(80))
                        {
                            if (placeResult.x - 2 >= freeR.X0)
                            {
                                WorldGen.PlaceTile(placeResult.x - 2, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]); // Lamp on the left
                                Func.UnlightLamp(placeResult.x - 2, freeR.Y1);
                            }
                            else if (placeResult.x + 2 <= doors[Door.Down].doorRect.X0)
                            {
                                WorldGen.PlaceTile(placeResult.x + 2, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]); // Lamp on the right
                                Func.UnlightLamp(placeResult.x + 2, freeR.Y1);
                            }
                        }

                        if (Chance.Perc(80))
                        {
                            if (placeResult.x + 2 < doors[Door.Down].doorRect.X0)
                            {
                                WorldGen.PlaceTile(placeResult.x + 2, freeR.Y1, TileID.Chairs, style: Deco[S.Chair]); // Chair on the right
                                Func.ChairTurnRight(placeResult.x + 2, freeR.Y1);
                            }
                            else if (placeResult.x - 2 >= freeR.X0)
                            {
                                WorldGen.PlaceTile(placeResult.x - 2, freeR.Y1, TileID.Chairs, style: Deco[S.Chair]); // Chair on the left
                                Func.UnlightLamp(placeResult.x - 2, freeR.Y1);
                            }
                        }
                    }

                    if ( (freeR.YTiles >= 4) && (Chance.Perc(75)) )
                    {
                        Func.PlaceTileAndBanner(PlatformLeftStart, freeR.Y1 - 2, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                    }

                    // right dresser
                    area1 = new Rectangle2P(doors[Door.Down].doorRect.X1 + 2, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");
                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Dressers, style: Deco[S.Dresser], chance: 45); // Dresser
                    if (placeResult.success)
                    {
                        if (Chance.Perc(80))
                        {
                            area1 = new Rectangle2P(placeResult.x - 1, placeResult.y - 2, placeResult.x + 1, placeResult.y - 2, "dummyString");
                            placeResult2 = Func.TryPlaceTile(area1, noBlock, TileID.Candles, style: Deco[S.Candle], chance: 50); // Candle on dresser
                            if (placeResult2.success) Func.Unlight1x1(placeResult2.x, placeResult2.y);
                        }
                    }
                    else
                    {
                        placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Benches, style: Deco[S.Sofa], chance: 75); // Sofa
                    }

                    if (placeResult.success) // dresser or sofa
                    {
                        if (Chance.Perc(80))
                        {
                            if (placeResult.x + 2 <= freeR.X1)
                            {
                                WorldGen.PlaceTile(placeResult.x + 2, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]); // Lamp on the right
                                Func.UnlightLamp(placeResult.x + 2, freeR.Y1);
                            }
                            else if (placeResult.x - 2 >= doors[Door.Down].doorRect.X1)
                            {
                                WorldGen.PlaceTile(placeResult.x - 2, freeR.Y1, TileID.Lamps, style: Deco[S.Lamp]); // Lamp on the left
                                Func.UnlightLamp(placeResult.x - 2, freeR.Y1);
                            }
                        }

                        if (Chance.Perc(80))
                        {
                            if (placeResult.x - 2 > doors[Door.Down].doorRect.X1)
                            {
                                WorldGen.PlaceTile(placeResult.x - 2, freeR.Y1, TileID.Chairs, style: Deco[S.Chair]); // Chair on the left
                            }
                            else if (placeResult.x + 2 <= freeR.X1)
                            {
                                WorldGen.PlaceTile(placeResult.x + 2, freeR.Y1, TileID.Chairs, style: Deco[S.Chair]); // chair on the right
                                Func.ChairTurnRight(placeResult.x + 2, freeR.Y1);
                            }
                        }
                    }

                    if ((freeR.YTiles >= 4) && (Chance.Perc(75)))
                    {
                        Func.PlaceTileAndBanner(PlatformRightStart, freeR.Y1 - 2, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                    }

                    if (Chance.Perc(40))
                    {
                        Func.PlaceItemFrame(freeR.XCenter, freeR.Y1 - 2, Deco[S.BackWall], Deco[S.StylePaint]); // Item Frame
                    }

                    //__________________________________________________________________________________________________________________________________
                    // second ... fourth floor of the room: beds

                    int floorStart = freeR.Y1 - 3; // init: floor 2 platform
                    for (int f = 1; f <= 3; f++)
                    {
                        if (freeR.YTiles >= 3 + f*4)
                        {
                            lastFloorHeight = floorStart;

                            // left bed
                            if (Chance.Perc(70))
                            {
                                for (x = freeR.X0; x <= PlatformLeftStart; x++)
                                {
                                    y = floorStart;

                                    WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                    WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                }

                                WorldGen.PlaceTile(freeR.X0 + 1, floorStart - 1, TileID.Beds, style: Deco[S.Bed]);
                                Func.PlaceWallArea(new Rectangle2P(freeR.X0, floorStart - 3, 4, 2), Deco[S.BedWallpaper]);

                                if (Chance.Perc(75))
                                {
                                    WorldGen.PlaceTile(freeR.X0, floorStart - 3, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X0, floorStart - 3);
                                }

                                if ((freeR.YTiles >= 4 + f*4) && (Chance.Perc(75)))
                                {
                                    Func.PlaceTileAndBanner(PlatformLeftStart, floorStart - 3, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                                }


                                x = freeR.X0 + 4;
                                y = floorStart - 1;
                                if (Chance.Perc(50))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.Containers, style: Deco[S.Chest]); // Chest in front of bed
                                }
                                else if (Chance.Perc(50))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.WorkBenches, style: Deco[S.Workbench]); // Workbench in front of bed

                                    // and a drink on it
                                    randomItems.Clear();
                                    randomItems.Add((TileID.Bottles, 0, 50)); // Bottle
                                    randomItems.Add((TileID.Bottles, 1, 50)); // Lesser Healing Potion
                                    randomItems.Add((TileID.Bottles, 2, 50)); // Lesser Mana Potion

                                    area1 = new Rectangle2P(x, y - 1, x + 1, y - 1, "dummyString");

                                    for (int i = 1; i <= 3; i++)
                                    {
                                        num = WorldGen.genRand.Next(randomItems.Count);
                                        Func.TryPlaceTile(area1, noBlock, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                                    }
                                }
                            }
                            else
                            {
                                // put only damaged platform
                                for (x = freeR.X0; x <= PlatformLeftStart; x++)
                                {
                                    if (Chance.Perc(65))
                                    {
                                        y = floorStart;

                                        WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                        WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                    }
                                }

                                if (Chance.Perc(40))
                                {
                                    WorldGen.PlaceTile(freeR.X0, floorStart - 3, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X0, floorStart - 3);
                                }

                                if ((freeR.YTiles >= 4 + f*4) && (Chance.Perc(40)))
                                {
                                    Func.PlaceTileAndBanner(PlatformLeftStart, floorStart - 3, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                                }
                            }

                            // right platform and bed
                            if (Chance.Perc(70))
                            {
                                for (x = PlatformRightStart; x <= freeR.X1; x++)
                                {
                                    y = floorStart;

                                    WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                    WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                }

                                WorldGen.PlaceTile(freeR.X1 - 2, floorStart - 1, TileID.Beds, style: Deco[S.Bed]);
                                Func.BedTurnLeft(freeR.X1 - 2, floorStart - 1);
                                Func.PlaceWallArea(new Rectangle2P(freeR.X1 - 3, floorStart - 3, 4, 2), Deco[S.BedWallpaper]);

                                if (Chance.Perc(75))
                                {
                                    WorldGen.PlaceTile(freeR.X1, floorStart - 3, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X1, floorStart - 3);
                                }

                                if ((freeR.YTiles >= 4 + f*4) && (Chance.Perc(75)))
                                {
                                    Func.PlaceTileAndBanner(PlatformRightStart, floorStart - 3, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                                }

                                x = freeR.X1 - 5;
                                y = floorStart - 2;
                                if (Chance.Perc(50))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.Containers, style: Deco[S.Chest]); // Chest in front of bed
                                }
                                else if (Chance.Perc(50))
                                {
                                    WorldGen.PlaceTile(x, y, TileID.WorkBenches, style: Deco[S.Workbench]); // Workbench in front of bed

                                    // and a drink on it
                                    randomItems.Clear();
                                    randomItems.Add((TileID.Bottles, 0, 50)); // Bottle
                                    randomItems.Add((TileID.Bottles, 1, 50)); // Lesser Healing Potion
                                    randomItems.Add((TileID.Bottles, 2, 50)); // Lesser Mana Potion

                                    area1 = new Rectangle2P(x, y - 1, x + 1, y - 1, "dummyString");

                                    for (int i = 1; i <= 3; i++)
                                    {
                                        num = WorldGen.genRand.Next(randomItems.Count);
                                        Func.TryPlaceTile(area1, noBlock, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                                    }
                                }
                            }
                            else
                            {
                                // put only damaged platform
                                for (x = PlatformRightStart; x <= freeR.X1; x++)
                                {
                                    if (Chance.Perc(65))
                                    {
                                        y = floorStart;

                                        WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                                        WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                                    }
                                }

                                if (Chance.Perc(40))
                                {
                                    WorldGen.PlaceTile(freeR.X1, floorStart - 3, TileID.Torches, style: Deco[S.Torch]);
                                    Func.Unlight1x1(freeR.X1, floorStart - 3);
                                }

                                if ((freeR.YTiles >= 4 + f*4) && (Chance.Perc(40)))
                                {
                                    Func.PlaceTileAndBanner(PlatformRightStart, floorStart - 3, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                                }
                            }

                            if (Chance.Perc(40))
                            {
                                Func.PlaceItemFrame(freeR.XCenter, floorStart - 3, Deco[S.BackWall], Deco[S.StylePaint]); // Item Frame
                            }
                        }

                        floorStart -= 4; // next floor
                    }

                    //__________________________________________________________________________________________________________________________________
                    // ceiling stuff

                    int leftSpace = (lastFloorHeight - 4) - freeR.Y0; // calculate how much space there is left

                    if (leftSpace >= 0)
                    {
                        // at least one line: put platforms
                        y = lastFloorHeight - 4;
                        for (x = freeR.X0; x <= PlatformLeftStart; x++)
                        {
                            WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                            WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                        }

                        for (x = PlatformRightStart; x <= freeR.X1; x++)
                        {
                            WorldGen.PlaceTile(x, y, TileID.Platforms, style: Deco[S.DecoPlat]);
                            WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                        }
                    }

                    if (leftSpace == 1)
                    {
                        // exactly two lines: put chain deco "on" the platforms
                        y = lastFloorHeight - 5;

                        if (doors[Door.Up].doorExist)
                        {
                            for (x = freeR.X0; x <= doors[Door.Up].doorRect.X0 - 1; x++)
                            {
                                WorldGen.PlaceTile(x, y, TileID.Chain);
                            }

                            for (x = doors[Door.Up].doorRect.X1 + 1; x <= freeR.X1; x++)
                            {
                                WorldGen.PlaceTile(x, y, TileID.Chain);
                            }
                        }
                        else
                        {
                            for (x = freeR.X0; x <= freeR.X1; x++)
                            {
                                WorldGen.PlaceTile(x, y, TileID.Chain);
                            }
                        }
                    }

                    if (leftSpace == 2)
                    {
                        // exactly three lines: chests and wooden crates on the platform
                        y = lastFloorHeight - 5;

                        randomItems.Clear();
                        randomItems.Add((TileID.Containers, Deco[S.Chest], 70)); // Chest
                        randomItems.Add((TileID.FishingCrate, 0, 70)); // wooden crate

                        area1 = new Rectangle2P(freeR.X0, y, freeR.X1 - 1, y, "dummyString");
                        if (doors[Door.Up].doorExist)   area2 = new Rectangle2P(doors[Door.Up].doorRect.X0 - 2, y, doors[Door.Up].doorRect.X1 + 1, y, "dummyString");
                        else                            area2 = new Rectangle2P(doors[Door.Up].doorRect.X0    , y, doors[Door.Up].doorRect.X1 - 1, y, "dummyString");

                        for (int i = 1; i <= 8; i++)
                        {
                            num = WorldGen.genRand.Next(randomItems.Count);
                            placeResult = Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            
                            if (placeResult.success && randomItems[num].TileID == TileID.FishingCrate)
                            {
                                area3 = new Rectangle2P(placeResult.x, placeResult.y - 1, 2, 2);
                                Func.PaintArea(area3, (byte)Deco[S.StylePaint]);
                            }
                        }

                    }

                    // finalization
                    Func.PlaceStinkbug(freeR);
                    
                    break;

                case 5: // armory

                    //__________________________________________________________________________________________________________________________________
                    // floor
                    area1 = new Rectangle2P(freeR.X0, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");

                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Containers, style: Deco[S.Chest], chance: 75); // Chest
                    if (placeResult.success)
                    {
                        chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                        if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2)); // with loot
                    }

                    placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.WorkBenches, style: Deco[S.Workbench], chance: 50); // workbench
                    if (placeResult.success)
                    {
                        if (Chance.Perc(65))
                        {
                            WorldGen.PlaceTile(placeResult.x + 1, placeResult.y - 1, TileID.Candelabras, style: Deco[S.Candelabra]); // with candelabra
                            Func.UnlightCandelabra(placeResult.x + 1, placeResult.y - 1);
                        }
                    }

                    List<(ushort TileID, int style)> floorItems =
                    [
                        (TileID.FishingCrate, 0),  // wooden crate
                        (TileID.FishingCrate, 1),  // iron crate
                        (TileID.Containers, 5),  // wooden barrel
                        (TileID.Statues, 3),  // sword statue
                        (TileID.Statues, 6),  // shield statue
                        (TileID.Statues, 17),  // bomb statue
                        (TileID.Statues, 21),  // spear statue
                        (TileID.Statues, 24),  // bow statue
                        (TileID.Statues, 25),  // boomerang statue
                        (TileID.TargetDummy, 0),  // Target Dummy
                        (TileID.WorkBenches, 0),  // Placeholder for: Mannequin with armor
                        (TileID.WorkBenches, 0)  // Placeholder for: Mannequin with armor
                    ];

                    List<(int, int, int)> armorPool2 =    // the possible styles of armor for the Mannequin
                    [
                        (ArmorIDs.Head.CopperHelmet, ArmorIDs.Body.CopperChainmail, ArmorIDs.Legs.CopperGreaves),
                        (ArmorIDs.Head.TinHelmet, ArmorIDs.Body.TinChainmail, ArmorIDs.Legs.TinGreaves),
                        (ArmorIDs.Head.IronHelmet, ArmorIDs.Body.IronChainmail, ArmorIDs.Legs.IronGreaves),
                        (ArmorIDs.Head.LeadHelmet, ArmorIDs.Body.LeadChainmail, ArmorIDs.Legs.LeadGreaves),
                        (ArmorIDs.Head.NinjaHood, ArmorIDs.Body.NinjaShirt, ArmorIDs.Legs.NinjaPants),

                        (ArmorIDs.Head.WoodHelmet, ArmorIDs.Body.WoodBreastplate, ArmorIDs.Legs.WoodGreaves),
                        (ArmorIDs.Head.BorealWoodHelmet, ArmorIDs.Body.BorealWoodBreastplate, ArmorIDs.Legs.BorealWoodGreaves),
                        (ArmorIDs.Head.ShadewoodHelmet, ArmorIDs.Body.ShadewoodBreastplate, ArmorIDs.Legs.ShadewoodGreaves),
                        (ArmorIDs.Head.AshWoodHelmet, ArmorIDs.Body.AshWoodBreastplate, ArmorIDs.Legs.AshWoodGreaves),
                    ];
                    

                    for (int i = 1; i <= freeR.XTiles / 2; i++) // every item is 2 xTiles wide --> fill the room
                    {
                        num = WorldGen.genRand.Next(floorItems.Count);
                        placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, floorItems[num].TileID, style: floorItems[num].style, chance: 50); // one random item of the list


                        if (placeResult.success)
                        {
                            if (floorItems[num].TileID == TileID.TargetDummy && placeResult.x <= freeR.XCenter)
                            {
                                Func.TargetDummyTurnRight(placeResult.x, placeResult.y);
                            }
                            if (floorItems[num].TileID == TileID.Statues)
                            {
                                if (floorItems[num].style == 3 && placeResult.x > freeR.XCenter) // Sword statue
                                {
                                    Func.StatueTurn(placeResult.x, placeResult.y);
                                }
                                if (floorItems[num].style == 21 && placeResult.x > freeR.XCenter) // Spear statue
                                {
                                    Func.StatueTurn(placeResult.x, placeResult.y);
                                }
                                if (floorItems[num].style == 24 && placeResult.x > freeR.XCenter) // Bow statue
                                {
                                    Func.StatueTurn(placeResult.x, placeResult.y);
                                }
                                if (floorItems[num].style == 25 && placeResult.x <= freeR.XCenter) // Boomerang statue
                                {
                                    Func.StatueTurn(placeResult.x, placeResult.y);
                                }
                            }
                            if (floorItems[num].TileID == TileID.WorkBenches && placeResult.x <= freeR.XCenter)
                            {
                                WorldGen.KillTile(placeResult.x,   placeResult.y); // delete placeholder workbench
                                WorldGen.KillTile(placeResult.x+1, placeResult.y);

                                int num2 = WorldGen.genRand.Next(armorPool2.Count);
                                Func.PlaceMannequin(placeResult.x, placeResult.y, armorPool2[num2], female: Chance.Simple(), direction: (WorldGen.genRand.Next(2)*2)-1);
                                armorPool2.RemoveAt(num2); // don't repeat that armor
                            }
                        }

                        floorItems.RemoveAt(num); // don't repeat that style
                    }

                    //__________________________________________________________________________________________________________________________________
                    // hanging items styles

                    List<(int TileID, int style)> hangItems =
                    [
                        (TileID.Painting3X3, 41),  // blacksmith rack
                        (TileID.Painting3X3, 42),  // carpentry rack
                        (TileID.Painting3X3, 43),  // helmet rack
                        (TileID.Painting3X3, 44),  // spear rack
                        (TileID.Painting3X3, 45),  // sword rack
                    ];

                    List<List<int>> itemFrame_Styles =
                    [
                        [ // potions
                            ItemID.ArcheryPotion, ItemID.AmmoReservationPotion, ItemID.BattlePotion ,ItemID.EndurancePotion, ItemID.HealingPotion, ItemID.HunterPotion,
                            ItemID.IronskinPotion, ItemID.MagicPowerPotion, ItemID.ManaRegenerationPotion, ItemID.NightOwlPotion, ItemID.ObsidianSkinPotion,
                            ItemID.RagePotion, ItemID.RegenerationPotion, ItemID.SwiftnessPotion, ItemID.ThornsPotion, ItemID.TitanPotion, ItemID.WrathPotion
                        ],
                        [ // bombs
                            ItemID.Bomb, ItemID.Dynamite, ItemID.Grenade, ItemID.SmokeBomb, ItemID.StickyBomb, ItemID.StickyDynamite, ItemID.StickyGrenade, ItemID.Beenade, ItemID.ScarabBomb, ItemID.ExplosiveBunny, ItemID.Explosives, ItemID.MolotovCocktail
                        ],
                        [ // metals
                            ItemID.CopperBar, ItemID.TinBar, ItemID.IronBar, ItemID.LeadBar, ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar, ItemID.DemoniteBar, ItemID.CrimtaneBar, ItemID.Geode
                        ],
                        [ // ammo
                            ItemID.Bone, ItemID.Shuriken, ItemID.Snowball, ItemID.SpikyBall, ItemID.StarAnise, ItemID.RottenEgg, ItemID.ThrowingKnife, ItemID.PoisonedKnife, ItemID.BoneDagger, ItemID.BoneDagger, ItemID.FrostDaggerfish
                        ]
                    ];


                    List<List<int>> weaponRack_Styles = [];
                    // swords
                    weaponRack_Styles.Add([ItemID.CopperBroadsword, ItemID.TinBroadsword, ItemID.IronBroadsword, ItemID.LeadBroadsword, ItemID.SilverBroadsword, ItemID.TungstenBroadsword, ItemID.GoldBroadsword, ItemID.PlatinumBroadsword, ItemID.BoneSword, ItemID.IceBlade, ItemID.BorealWoodSword, ItemID.EbonwoodSword, ItemID.ShadewoodSword, ItemID.AshWoodSword]);
                    // bows
                    weaponRack_Styles.Add([ItemID.CopperBow, ItemID.TinBow, ItemID.IronBow, ItemID.LeadBow, ItemID.SilverBow, ItemID.TungstenBow, ItemID.GoldBow, ItemID.BorealWoodBow, ItemID.PalmWoodBow, ItemID.ShadewoodBow, ItemID.EbonwoodBow, ItemID.RichMahoganyBow]);
                    // magic
                    if (WorldGen.remixWorldGen) // "don't dig up" special worldgen seed
                    {
                        weaponRack_Styles.Add([ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.SapphireStaff, ItemID.EmeraldStaff]); // the WandofSparking and WandofFrosting are hardmode weapons in that game mode!
                    }
                    else
                    {
                        weaponRack_Styles.Add([ItemID.WandofSparking, ItemID.WandofFrosting, ItemID.AmethystStaff, ItemID.TopazStaff, ItemID.SapphireStaff, ItemID.EmeraldStaff]);
                    }
                    // randoms
                    if (WorldGen.remixWorldGen) // "don't dig up" special worldgen seed
                    {
                        weaponRack_Styles.Add([ItemID.FlintlockPistol, ItemID.FlareGun, ItemID.Mace, ItemID.Mace, ItemID.FlamingMace, ItemID.Spear, ItemID.Trident, ItemID.WoodenBoomerang, ItemID.EnchantedBoomerang, ItemID.BlandWhip]); // the ChainKnife is a hardmode weapon in that game mode!
                    }
                    else
                    {
                        weaponRack_Styles.Add([ItemID.FlintlockPistol, ItemID.FlareGun, ItemID.ChainKnife, ItemID.Mace, ItemID.FlamingMace, ItemID.Spear, ItemID.Trident, ItemID.WoodenBoomerang, ItemID.EnchantedBoomerang, ItemID.BlandWhip]);
                    }

                    //__________________________________________________________________________________________________________________________________
                    // init vars

                    LineAutomat automat;
                    Dictionary<int, List<int>> noAdd = [], Wall;
                    Dictionary<int, List<int>> WallAndPaint = new(){ {(int)LineAutomat.Adds.Wall,  [ Deco[S.BackWall],   0, -1, 0 ] },
                                                                     {(int)LineAutomat.Adds.Paint, [ Deco[S.StylePaint], 0, -1 ] }  };
                    int actX, actY;
                    List<int> weaponStyle, itemStyle;

                    //__________________________________________________________________________________________________________________________________
                    // topmost row: WeaponFrames with some Banners or ItemFrames to fill gaps

                    automat = new((freeR.X0, freeR.Y0 + 1), (int)LineAutomat.Dirs.xPlus);
                    unusedXTiles = freeR.XTiles % 3; // WeaponRacks are 3 tiles wide
                    actX = freeR.X0; // init
                    weaponStyle = weaponRack_Styles[WorldGen.genRand.Next(weaponRack_Styles.Count)]; // get a random style to later take items from it
                    itemStyle = itemFrame_Styles[WorldGen.genRand.Next(itemFrame_Styles.Count)]; // get a random style to later take items from it
                    int facingDir = -1; // starting left of the middle, swords will face from left to right

                    if (unusedXTiles == 0) // all WeaponRacks, no free spaces
                    {
                        for (int i = 1; i <= (freeR.XTiles / 3); i++)
                        {
                            if (actX + 1 > freeR.XCenter) facingDir = 1; // anchor point over the room middle

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                               size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                            actX += 3;
                        }
                    }
                    else if (unusedXTiles == 1)
                    {
                        if (Chance.Simple())  // Style 1: banner, WeaponRack, banner, only WeaponRacks, banner, WeaponRack, banner
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                               size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                            actX += 3;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            for (int i = 1; i <= (int)((freeR.XTiles - (5+5)) / 3); i++)
                            {
                                if (actX + 1 > freeR.XCenter) facingDir = 1; // anchor point over the room middle

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                   size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                                actX += 3;
                            }

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                               size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                            actX += 3;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;
                        }
                        else  // Style 2: WeaponRacks and 2 ItemFrames
                        {
                            if (Chance.Simple()) // WeaponRacks, 2x ItemFrame, WeaponRack2
                            {
                                for (int i = 1; i <= (int)(((freeR.XTiles - (2 + 2)) / 3) / 2); i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                       size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint));
                                    actX += 3;
                                }

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                   size: (2, 2), toAnchor: (0, -1), chance: 75, add: WallAndPaint));
                                actX += 2;

                                facingDir = 1;

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                   size: (2, 2), toAnchor: (0, -1), chance: 75, add: WallAndPaint));
                                actX += 2;

                                for (int i = 1; i <= (int)(((freeR.XTiles - (2 + 2)) / 3) / 2); i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                       size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint));
                                    actX += 3;
                                }
                            }
                            else // ItemFrame, WeaponRacks, ItemFrame
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                   size: (2, 2), toAnchor: (0, -1), chance: 75, add: WallAndPaint));
                                actX += 2;

                                for (int i = 1; i <= (int)((freeR.XTiles - (2+2)) /3); i++)
                                {
                                    if (actX + 1 > freeR.XCenter) facingDir = 1; // anchor point over the room middle

                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                       size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint));
                                    actX += 3;
                                }

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                   size: (2, 2), toAnchor: (0, -1), chance: 75, add: WallAndPaint));
                                actX += 2;
                            }
                        }
                    }
                    else if (unusedXTiles == 2)
                    {
                        if (Chance.Simple()) // Style 1: banner, WeaponRacks, banner
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            for (int i = 1; i <= (int)((freeR.XTiles - 2) / 3); i++)
                            {
                                if (actX + 1 > freeR.XCenter) facingDir = 1; // anchor point over the room middle

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                   size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                                actX += 3;
                            }

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;
                        }
                        else  // Style 2: WeaponRack, banner, only WeaponRacks, banner, WeaponRack
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                               size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                            actX += 3;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            for (int i = 1; i <= (int)((freeR.XTiles - 8) / 3); i++)
                            {
                                if (actX + 1 > freeR.XCenter) facingDir = 1; // anchor point over the room middle

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                                   size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                                actX += 3;
                            }

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, -1), chance: 75, add: noAdd));
                            actX++;

                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.WeaponRack, item: weaponStyle.PopAt(WorldGen.genRand.Next(weaponStyle.Count)), style: facingDir,
                                               size: (3, 3), toAnchor: (1, 0), chance: 75, add: WallAndPaint) );
                            actX += 3;
                        }
                    }
                    automat.Start();


                    //__________________________________________________________________________________________________________________________________
                    // second row from the top: ItemFrames with some Banners to fill gaps

                    automat = new((freeR.X0, freeR.Y0 + 3), (int)LineAutomat.Dirs.xPlus);
                    unusedXTiles = freeR.XTiles % 2; // ItemFrames are 2 tiles wide
                    actX = freeR.X0; // init
                    itemStyle = itemFrame_Styles[WorldGen.genRand.Next(itemFrame_Styles.Count)]; // get a random style to later take items from it
                    WallAndPaint = new(){ {(int)LineAutomat.Adds.Wall,  [ Deco[S.BackWall],   0, 0, 0 ] },
                                          {(int)LineAutomat.Adds.Paint, [ Deco[S.StylePaint], 0, 0 ] }  };

                    Dictionary<int, List<int>> WallPaintBanner = WallAndPaint;
                    WallPaintBanner.Add( (int)LineAutomat.Adds.Banner, [ Deco[S.Banner] ] );

                    if (freeR.YTiles >= 8) //WeaponRack + Floor + ItemFrame = 3+3+2
                    {
                        if (unusedXTiles == 0)
                        {
                            if (Chance.Perc(35))  // all ItemFrames, no free spaces
                            {
                                for (int i = 1; i <= (freeR.XTiles / 2); i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                       size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                    actX += 2;
                                }
                            }
                            else if (Chance.Perc(50)) // ItemFrame and a banner on each side
                            {
                                if (Chance.Perc(75))
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.BannerAndTile, item: TileID.Platforms, style: Deco[S.DecoPlat], size: (1, 4), toAnchor: (0, 1), chance: 100, add: WallPaintBanner));
                                }
                                else automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                actX++;

                                for (int i = 1; i <= (int)((freeR.XTiles - (1 + 1)) / 2); i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                       size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                    actX += 2;
                                }

                                if (Chance.Perc(75))
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.BannerAndTile, item: TileID.Platforms, style: Deco[S.DecoPlat], size: (1, 4), toAnchor: (0, 1), chance: 100, add: WallPaintBanner));
                                }
                                else automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                actX++;
                            }
                            else if (Chance.Perc(50)) // banners on the sides and a space between each ItemFrame
                            {
                                if (Chance.Perc(75))
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.BannerAndTile, item: TileID.Platforms, style: Deco[S.DecoPlat], size: (1, 4), toAnchor: (0, 1), chance: 100, add: WallPaintBanner));
                                }
                                else automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                actX += 2;

                                num = ((freeR.XTiles - (2 + 1)) / 3);
                                unusedXTiles = ((freeR.XTiles - (2 + 1)) % 3);
                                if (unusedXTiles == 0) // left "1 banner + 1 space" and right "1 banner" and the "ItemFrame + 1 space" can leave a 1 or 2 Tile gap!
                                { // no gap
                                    for (int i = 1; i <= num; i++)
                                    {
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                            size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                        actX += 3;
                                    }
                                }
                                else if (unusedXTiles == 1)
                                {  // do a double space in the middle OR put another ItemFrame to maintain symmetry
                                    for (int i = 1; i <= num / 2; i++)
                                    {
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                            size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                        actX += 3;
                                    }
                                    if (Chance.Simple()) // double space
                                    {
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                        actX++;
                                    }
                                    else // another ItemFrame without spaces
                                    {
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (-1, 0), (0, 0), 0, noAdd)); // go back 1 tile
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                            size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                    }
                                    
                                    for (int i = 1; i <= num / 2; i++)
                                    {
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                            size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                        actX += 3;
                                    }
                                }
                                else //(unusedXTiles == 2)
                                {
                                    if (num % 2 == 0) // an even number of ItemFrames to distribute
                                    { // do a triple space in the middle to maintain symmetry
                                        for (int i = 1; i <= num / 2; i++)
                                        {
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                            actX += 3;
                                        }
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (2, 0), (0, 0), 0, noAdd));
                                        actX +=2;
                                        for (int i = 1; i <= num / 2; i++)
                                        {
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                            actX += 3;
                                        }
                                    }
                                    else
                                    { // put two item Frames in the middle without space
                                        for (int i = 1; i <= num / 2; i++)
                                        {
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                            actX += 3;
                                        }

                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                        automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                        actX += 5;

                                        for (int i = 1; i <= num / 2; i++)
                                        {
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: itemStyle.PopAt(WorldGen.genRand.Next(itemStyle.Count)), style: 0,
                                                                size: (2, 2), toAnchor: (0, 0), chance: 75, add: WallAndPaint));
                                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                            actX += 3;
                                        }
                                    }
                                }

                                if (Chance.Perc(75))
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.BannerAndTile, item: TileID.Platforms, style: Deco[S.DecoPlat], size: (1, 4), toAnchor: (0, 1), chance: 100, add: WallPaintBanner));
                                }
                                else automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                actX += 1;
                            }
                        }
                        else { } // right now all rooms are always even XTiles wide...leave this one for later, maybe never
                        automat.Start();
                    }


                    //__________________________________________________________________________________________________________________________________
                    // check and place banners for the next rows

                    int startBanners = freeR.Y0 + 3; //where the platform of the ItemFrame row is at
                    if (Main.tile[freeR.X0, startBanners].TileType == TileID.Platforms || Main.tile[freeR.X1, startBanners].TileType == TileID.Platforms)   startBanners += 5;
                    else startBanners += 3;

                    while (freeR.YDiff >= (startBanners + 3) - freeR.Y0) // 3 for the floor items
                    {
                        if (Chance.Perc(50)) Func.PlaceTileAndBanner(freeR.X0, startBanners, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                        if (Chance.Perc(50)) Func.PlaceTileAndBanner(freeR.X1, startBanners, Deco[S.Banner], TileID.Platforms, Deco[S.DecoPlat], Deco[S.StylePaint]);
                        startBanners += 4;
                    }

                    //__________________________________________________________________________________________________________________________________
                    // next rows: hangItems

                    
                    int hangSpace, hangNum;
                    actY = freeR.Y0 + 6;
                    (int TileID, int style) hangItem;
                    Wall = new(){ {(int)LineAutomat.Adds.Wall,  [ Deco[S.BackWall],   0, -1, 0 ] } };

                    while (freeR.YDiff >= (actY + 3 + 1) - freeR.Y0) // floor and 1 from middle of racks to lowest end of tile
                    {
                        automat = new((freeR.X0, actY), (int)LineAutomat.Dirs.xPlus);
                        hangItem = hangItems[WorldGen.genRand.Next(hangItems.Count)]; // get a random style to later place it
                        
                        // check for "in between banners" placement
                        hangSpace = freeR.XTiles;
                        if ( !Func.CheckFree(new Rectangle2P(freeR.X0, actY - 1, 1, 3)) || !Func.CheckFree(new Rectangle2P(freeR.X1, actY - 1, 1, 3)) )
                        {
                            hangSpace = (freeR.XTiles - 2); // available space reduced by banners
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                        }
                        unusedXTiles = hangSpace % 3; // all hangItems are 3 tiles wide
                        hangNum = hangSpace / 3;

                        if (unusedXTiles == 0) // only hangItems, no free spaces
                        {
                            for (int i = 1; i <= hangNum; i++)
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                            }
                        }
                        else if (unusedXTiles == 1)
                        {
                            if (hangNum % 2 == 0) // even number of hangItems: space in the middle
                            {
                                for (int i = 1; i <= hangNum / 2; i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                }

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                for (int i = 1; i <= hangNum / 2; i++)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                }
                            }
                            else // uneven number of hangItems: take out 1 HangItem and distribute 4 spaces in symmetrical pairs
                            {
                                int firstSpacePos = WorldGen.genRand.Next((hangNum / 2) + 1) + 1; //e.g. 2 hangItems can have 3 positions where to put a pair of spaces
                                int secondSpacePos = WorldGen.genRand.Next((hangNum / 2) + 1) + 1;

                                for (int i = 1; i <= hangNum / 2; i++)
                                {
                                    if (i == firstSpacePos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                    if (i == secondSpacePos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                }

                                if (firstSpacePos > (hangNum / 2)) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (2, 0), (0, 0), 0, noAdd));
                                if (secondSpacePos > (hangNum / 2)) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (2, 0), (0, 0), 0, noAdd));

                                for (int i = hangNum / 2; i >= 1; i--)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));

                                    if (i == firstSpacePos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                    if (i == secondSpacePos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                }
                            }
                        }
                        else if (unusedXTiles == 2)
                        {
                            if (hangNum % 2 == 0) // even number of hangItems: distribute the spaces in a symmetrical pair
                            {
                                int SpacePairPos = WorldGen.genRand.Next((hangNum / 2) + 1) + 1; //e.g. 2 hangItems can have 3 positions where to put a pair of spaces

                                for (int i = 1; i <= hangNum / 2; i++)
                                {
                                    if (i == SpacePairPos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                }

                                if (SpacePairPos > (hangNum / 2)) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (2, 0), (0, 0), 0, noAdd));

                                for (int i = hangNum / 2; i >= 1; i--)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));

                                    if (i == SpacePairPos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                }
                            }
                            else // uneven number of hangItems: distribute the spaces in a symmetrical pair (except in the middle)
                            {
                                int SpacePairPos = WorldGen.genRand.Next((hangNum / 2) + 1) + 1; //e.g. 2 hangItems can have 3 positions where to put a pair of spaces

                                for (int i = 1; i <= hangNum / 2; i++)
                                {
                                    if (i == SpacePairPos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                }

                                if (SpacePairPos > (hangNum / 2)) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));
                                if (SpacePairPos > (hangNum / 2)) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));

                                for (int i = hangNum / 2; i >= 1; i--)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: hangItem.TileID, style: hangItem.style, size: (3, 3), toAnchor: (1, 0), chance: 75, add: Wall));

                                    if (i == SpacePairPos) automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, noAdd));
                                }
                            }
                        }

                        automat.Start();
                        actY += 3;
                    }
                    break;

                case 6: // prison / torture room with hanging skeletons

                    // preparing skelton lists
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

                    int brickHeight; // the height of the topmost row of iron bricks of a prison cell

                    // ground floor
                    if (!doors[Door.Down].doorExist)
                    {
                        brickHeight = freeR.Y1 - 4;

                        // make a big prison, covering the whole bottom part of the room
                        if (freeR.YTiles >= 5)
                        {
                            int bricksLeftEnd = freeR.XCenter - 1;
                            int bricksRightStart = freeR.XCenter + 2;

                            y = brickHeight;
                            for (int i = freeR.X0; i <= bricksLeftEnd; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.IronBrick);
                                WorldGen.paintTile(i, y, (byte)Deco[S.StylePaint]);
                            }
                            for (int i = bricksRightStart; i <= freeR.X1; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.IronBrick);
                                WorldGen.paintTile(i, y, (byte)Deco[S.StylePaint]);
                            }
                            WorldGen.PlaceObject(bricksLeftEnd + 1, y, TileID.TrapdoorClosed);
                            WorldGen.paintTile(bricksLeftEnd + 1, y, (byte)Deco[S.StylePaint]);
                            WorldGen.paintTile(bricksLeftEnd + 2, y, (byte)Deco[S.StylePaint]);

                            Func.PlaceWallArea(new Rectangle2P(freeR.X0, brickHeight + 1, freeR.X1, freeR.Y1, "dummyString"), WallID.WroughtIronFence, (byte)Deco[S.StylePaint]);

                            // fill the prison with skeletons!
                            automat = new((freeR.X0, freeR.Y1), (int)LineAutomat.Dirs.xPlus);

                            actX = freeR.X0;
                            int randNum;
                            (int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add) prisonItem;
                            while (actX <= freeR.X1)
                            {
                                randNum = WorldGen.genRand.Next(4); // prisonItem categories
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

                                if ((actX - 1) + prisonItem.size.x <= freeR.X1)
                                {
                                    automat.Steps.Add(((int)LineAutomat.Cmds.Tile, prisonItem.TileID, prisonItem.style, prisonItem.size, prisonItem.toAnchor, prisonItem.chance, prisonItem.add));
                                    actX += prisonItem.size.x;
                                }
                                else
                                {
                                    automat.Steps.Add(((int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, []));
                                    actX += 1;
                                }
                            }
                            automat.Start();

                            // put platform above the trap door
                            if (freeR.YTiles >= 6)
                            {
                                y = brickHeight - 1;
                                area1 = new Rectangle2P(bricksLeftEnd, y, bricksRightStart, y, "dummyString");

                                for (int i = area1.X0; i <= area1.X1; i++)
                                {
                                    WorldGen.PlaceTile(i, y, TileID.Platforms, style: 43); // stone platform
                                }
                                Func.SlopeTile(area1.X0, y, (int)Func.SlopeVal.UpLeft);
                                Func.SlopeTile(area1.X1, y, (int)Func.SlopeVal.UpRight);
                            }

                            brickHeight -= 5; // brick height of where the next "prison cells left and right" can start
                        }
                    }
                    else
                    {
                        brickHeight = freeR.Y1 - 4;
                    }

                    while (freeR.Y0 <= brickHeight)
                    {
                        // make prison cells on the left and the right
                        int bricksLeftEnd = doors[Door.Up].doorRect.X0; // lamps will hang in the door area
                        int bricksRightStart = doors[Door.Up].doorRect.X1; // lamps will hang in the door area

                        y = brickHeight;
                        for (int i = freeR.X0; i <= bricksLeftEnd; i++)
                        {
                            WorldGen.PlaceTile(i, y, TileID.IronBrick);
                            WorldGen.paintTile(i, y, (byte)Deco[S.StylePaint]);
                        }
                        for (int i = bricksRightStart; i <= freeR.X1; i++)
                        {
                            WorldGen.PlaceTile(i, y, TileID.IronBrick);
                            WorldGen.paintTile(i, y, (byte)Deco[S.StylePaint]);
                        }

                        // spikes "doors"
                        for (int j = brickHeight + 1; j <= brickHeight + 4; j++)
                        {
                            WorldGen.PlaceTile(bricksLeftEnd - 1, j, TileID.Spikes);
                            WorldGen.PlaceTile(bricksRightStart + 1, j, TileID.Spikes);
                        }

                        // place backwall
                        Func.PlaceWallArea(new Rectangle2P(freeR.X0, brickHeight + 1, bricksLeftEnd - 2, brickHeight + 4, "dummyString"), WallID.WroughtIronFence, (byte)Deco[S.StylePaint]);
                        Func.PlaceWallArea(new Rectangle2P(bricksRightStart + 2, brickHeight + 1, freeR.X1, brickHeight + 4, "dummyString"), WallID.WroughtIronFence, (byte)Deco[S.StylePaint]);

                        // put lanterns or banners on the ledge
                        bool placedLeft = false; //init
                        x = bricksLeftEnd;
                        y = brickHeight + 1;
                        if (Chance.Perc(50))
                        {
                            placedLeft = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: 2); // Caged Lantern
                            if (placedLeft) Func.UnlightLantern(x, y);
                        }
                        else if (Chance.Perc(50))
                        {
                            if (Chance.Simple()) placedLeft = WorldGen.PlaceObject(x, y, TileID.Banners, style: 12); // Rusted Company Standard
                            else placedLeft = WorldGen.PlaceObject(x, y, TileID.Banners, style: 19); // Lost Hopes of Man Banner
                        }

                        bool placedRight = false; //init
                        x = bricksRightStart;
                        y = brickHeight + 1;
                        if (Chance.Perc(50))
                        {
                            placedRight = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: 2); // Caged Lantern
                            if (placedRight) Func.UnlightLantern(x, y);
                        }
                        else if (Chance.Perc(50))
                        {
                            if (Chance.Simple()) placedRight = WorldGen.PlaceObject(x, y, TileID.Banners, style: 12); // Rusted Company Standard
                            else placedRight = WorldGen.PlaceObject(x, y, TileID.Banners, style: 19); // Lost Hopes of Man Banner
                        }

                        // slope ledges
                        Func.SlopeTile(bricksLeftEnd, brickHeight, (int)Func.SlopeVal.UpRight);
                        Func.SlopeTile(bricksRightStart, brickHeight, (int)Func.SlopeVal.UpLeft);

                        // put deco in between the cells
                        // first check if there would be enough space to hang a skeleton
                        x = 0;
                        if (!placedLeft && placedRight) x = bricksLeftEnd + 1;
                        else if (placedLeft && !placedRight) x = bricksLeftEnd + 2;
                        else if (!placedLeft && !placedRight) x = bricksLeftEnd + WorldGen.genRand.Next(1, 3);

                            
                        if (x > 0 && Chance.Perc(70))
                        {
                            y = brickHeight + WorldGen.genRand.Next(2, 4);
                            int skeletonStyle = WorldGen.genRand.Next(16, 18); // 16 = wall skeleton, 17 = hanging skeleton
                            placed = WorldGen.PlaceTile(x, y, TileID.Painting3X3, style: skeletonStyle);
                            if (placed)
                            {
                                for (int j = y - 2; j >= brickHeight; j--)
                                {
                                    if (skeletonStyle == 17)
                                    {
                                        if (!Main.tile[x, j].HasTile) WorldGen.PlaceTile(x, j, TileID.Chain);
                                    }
                                    if (skeletonStyle == 16)
                                    {
                                        if (!Main.tile[x - 1, j].HasTile) WorldGen.PlaceTile(x - 1, j, TileID.Chain);
                                        if (!Main.tile[x + 1, j].HasTile) WorldGen.PlaceTile(x + 1, j, TileID.Chain);
                                    }
                                }
                            }
                        }
                        else if (Chance.Perc(50))
                        {
                            x = bricksLeftEnd + 1;
                            y = brickHeight + 2;
                            placed = WorldGen.PlaceObject(x, y, TileID.TatteredWoodSign);
                            if (placed) Func.PaintArea(new Rectangle2P(x, y, 2, 2), Deco[S.StylePaint]);
                        }



                        //fill cell with bones
                        int randNum, limX;
                        (int TileID, int style, (int x, int y) size, (int x, int y) toAnchor, byte chance, Dictionary<int, List<int>> add) prisonItem;
                        for (int i = 1; i <= 2; i++)
                        {
                            if (i == 1)
                            {
                                actX = freeR.X0;
                                limX = bricksLeftEnd - 2;
                                automat = new((actX, brickHeight + 4), (int)LineAutomat.Dirs.xPlus);
                            }
                            else
                            {
                                actX = bricksRightStart + 2;
                                limX = freeR.X1;
                                automat = new((actX, brickHeight + 4), (int)LineAutomat.Dirs.xPlus);
                            }


                            while (actX <= limX)
                            {
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

                                if ((actX - 1) + prisonItem.size.x <= limX)
                                {
                                    automat.Steps.Add(((int)LineAutomat.Cmds.Tile, prisonItem.TileID, prisonItem.style, prisonItem.size, prisonItem.toAnchor, prisonItem.chance, prisonItem.add));
                                    actX += prisonItem.size.x;
                                }
                                else
                                {
                                    automat.Steps.Add(((int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, []));
                                    actX += 1;
                                }
                            }
                            automat.Start();
                        }

                        brickHeight -= 5;
                    }

                    //ceiling stuff
                    area1 = area2 = Rectangle2P.Empty;
                    int pileStart = 0, pileEnd = 4; // define which entries of prisonItems_all are going to be used for random item selection
                    switch (freeR.YTiles % 5) //every cell is 5 Tiles high, get the remaining space till the ceiling
                    {
                        case 1: // put spikes
                            y = freeR.Y0;
                            for (int i = freeR.X0; i <= doors[Door.Up].doorRect.X0 - 1; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }
                            for (int i = doors[Door.Up].doorRect.X1 + 1; i <= freeR.X1; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }
                            break;

                        case 2: // put skeletons
                            y = freeR.Y0 + 1;
                            area1 = new Rectangle2P(freeR.X0, y, freeR.X1, y, "dummyString");
                            area2 = new Rectangle2P(doors[Door.Up].doorRect.X0, y, doors[Door.Up].doorRect.X1, y, "dummyString");
                            pileStart = 1; // only large piles (wall skeletons don't fit) until..
                            pileEnd = 4;  // ..single piles 

                            break;

                        case 3: // put skeletons and spikes
                            y = freeR.Y0 + 2;
                            area1 = new Rectangle2P(freeR.X0 + 1, y, freeR.X1 - 1, y, "dummyString");
                            area2 = new Rectangle2P(doors[Door.Up].doorRect.X0, y, doors[Door.Up].doorRect.X1, y, "dummyString");
                            pileStart = 1; // large piles until..
                            pileEnd = 4;  // ..single piles

                            //Spikes
                            y = freeR.Y0;
                            for (int i = freeR.X0; i <= doors[Door.Up].doorRect.X0 - 1; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }
                            for (int i = doors[Door.Up].doorRect.X1 + 1; i <= freeR.X1; i++)
                            {
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }

                            for (int j = freeR.Y0; j <= freeR.Y0 + 2; j++)
                            {
                                WorldGen.PlaceTile(freeR.X0, j, TileID.Spikes);
                                WorldGen.PlaceTile(freeR.X1, j, TileID.Spikes);
                            }

                            break;

                        case 4: // put skeletons and spikes and banners
                            y = freeR.Y0 + 3;
                            area1 = new Rectangle2P(freeR.X0 + 1, y, freeR.X1 - 1, y, "dummyString");
                            //area2 = new Rectangle2P(doors[Door.Up].doorRect.X0, y, doors[Door.Up].doorRect.X1, y, "dummyString");
                            pileStart = 1; // large piles until..
                            pileEnd = 4;  // ..single piles

                            // banners
                            bool placedLeft = false; //init
                            x = doors[Door.Up].doorRect.X0 - 1;
                            y = freeR.Y0;
                            if (Chance.Perc(70))
                            {
                                Func.SlopeTile(x, y - 1, (int)Func.SlopeVal.Nope);
                                if (Chance.Simple()) placedLeft = WorldGen.PlaceObject(x, y, TileID.Banners, style: 12); // Rusted Company Standard
                                else placedLeft = WorldGen.PlaceObject(x, y, TileID.Banners, style: 19); // Lost Hopes of Man Banner

                                if (placedLeft) area2.X0 -= 1; //adjust blocked area
                            }

                            bool placedRight = false; //init
                            x = doors[Door.Up].doorRect.X1 + 1;
                            y = freeR.Y0;
                            if (Chance.Perc(70))
                            {
                                Func.SlopeTile(x, y - 1, (int)Func.SlopeVal.Nope);
                                if (Chance.Simple()) placedRight = WorldGen.PlaceObject(x, y, TileID.Banners, style: 12); // Rusted Company Standard
                                else placedRight = WorldGen.PlaceObject(x, y, TileID.Banners, style: 19); // Lost Hopes of Man Banner

                                if (placedRight) area2.X1 += 1; //adjust blocked area
                            }

                            //Spikes
                            y = freeR.Y0;
                            for (int i = freeR.X0; i <= doors[Door.Up].doorRect.X0 - 1; i++)
                            {
                                if (i == doors[Door.Up].doorRect.X0 - 1 && placedLeft) continue;
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }
                            for (int i = doors[Door.Up].doorRect.X1 + 1; i <= freeR.X1; i++)
                            {
                                if (i == doors[Door.Up].doorRect.X1 + 1 && placedRight) continue;
                                WorldGen.PlaceTile(i, y, TileID.Spikes);
                            }

                            for (int j = freeR.Y0; j <= freeR.Y0 + 3; j++)
                            {
                                WorldGen.PlaceTile(freeR.X0, j, TileID.Spikes);
                                WorldGen.PlaceTile(freeR.X1, j, TileID.Spikes);
                            }

                            break;
                    }

                    if (!area1.IsEmpty()) 
                    {
                        int availableX = area1.XTiles - area2.XTiles;
                        while (availableX > 0)
                        {
                            int pile = WorldGen.genRand.Next(pileStart, pileEnd); // get a pile variant at random
                            int item = WorldGen.genRand.Next(prisonItems_all[pile].Count); // get a specific pile item at random

                            int type = prisonItems_all[pile][item].TileID;
                            int style = prisonItems_all[pile][item].style;
                            int xSprite = prisonItems_all[pile][item].add[(int)LineAutomat.Adds.Piles][1];
                            int ySprite = prisonItems_all[pile][item].add[(int)LineAutomat.Adds.Piles][0];
                            List<int> checkAdd;
                            if (pile == 0) checkAdd = [1, 1, 1, 1]; // Wall skeletons
                            else if (pile == 1) checkAdd = [1, 1, 1, 0]; // LargePiles
                            else if (pile == 2) checkAdd = [0, 1, 1, 0]; // SmallPiles
                            else checkAdd = [0, 0, 0, 0];               // SinglePiles

                            Func.TryPlaceTile(area1, area2, (ushort)type, style: style, chance: 75, add: new() { { "Piles", [xSprite, ySprite] }, { "CheckFree", checkAdd } });

                            availableX -= prisonItems_all[pile][item].size.x;
                        }
                    }
                    break;

                case 7: // treasury

                    byte goldPaint = PaintID.DeepYellowPaint; // deep yellow is the color the most resembling a golden look...maybe in the future there will be something better?

                    # region entrance: tables, chests, piggy bank, some single decorative items

                    int treasuryEntranceYTiles = 4; // 4 YTiles height of where the "entrance" deco will be placed
                    bool highEnoughForStash = freeR.YTiles >= 9; // 8 YTiles would just leave 2 Tiles for standing in the "stash" (8 - 4 "entrance" - 1 wall - 1 wall)

                    int entranceDecoHeight; // height where the decorative items get placed
                    if (roomType == RoomID.BelowSide) // try creating the "entrance area" at the ceiling of the room
                    {
                        entranceDecoHeight = freeR.Y0 + (treasuryEntranceYTiles - 1);
                        if (!highEnoughForStash) entranceDecoHeight = freeR.Y1; // room too small, put the "entrance" to the bottom
                    }
                    else // all other rooms have their "entrance" always at the bottom of the room
                    {
                        entranceDecoHeight = freeR.Y1;
                    }
                    Rectangle2P entranceDeco = new(freeR.X0, entranceDecoHeight, freeR.X1, entranceDecoHeight, "dummy");
                    Rectangle2P stashDoor = new(freeR.XCenter, entranceDecoHeight + 1, 2, 1);


                    Func.ReplaceWallArea(new(freeR.X0, freeR.Y0, freeR.X1, entranceDecoHeight, "dummy"), WallID.AncientGoldBrickWall);

                    if (entranceDecoHeight != freeR.Y1) // put floor in case of "BelowSide" and sufficient high room
                    {
                        y = entranceDecoHeight + 1;
                        for (int i = entranceDeco.X0; i <= entranceDeco.X1; i++)
                        {
                            //if (i == freeR.XCenter || i == freeR.XCenter + 1) continue; // reserve space for the trap door
                            if (stashDoor.Contains(i, y)) continue; // reserve space for the trap door
                            WorldGen.PlaceTile(i, y, TileID.AncientGoldBrick);
                        }

                        // put door to access the "stash" below
                        WorldGen.PlaceObject(freeR.XCenter  , y, TileID.TrapdoorClosed);
                        WorldGen.paintTile(freeR.XCenter    , y, goldPaint);
                        WorldGen.paintTile(freeR.XCenter + 1, y, goldPaint);

                        stashDoor.Move(0, -1); // arrange the stash door rectangle so it can be used later as a blocker zone
                    }

                    
                    if (!(roomType == RoomID.BelowSide) && highEnoughForStash) // put ceiling of "entrance area" (a.k.a floor of the stash) for all other rooms if they're high enough
                    {
                        y = entranceDecoHeight - treasuryEntranceYTiles;
                        Rectangle2P marbleDoor = new(doors[Door.Down].doorRect.X0, y, doors[Door.Down].doorRect.XTiles, 1);
                        for (int i = freeR.X0; i <= freeR.X1; i++)
                        {
                            if (marbleDoor.Contains(i, y))  WorldGen.PlaceTile(i, y, TileID.Platforms, style: 29); // place the marble platform door
                            else                            WorldGen.PlaceTile(i, y, TileID.AncientGoldBrick);
                        }
                    }

                    List<int> decoCases = 
                    [
                        0, // Gold Chest
                        1, // Marble Table
                        2, // Marble Workbench
                        3, // Coin bags (small piles)
                        4, // Coins
                        5  // Gold Golf Trophy
                        // TileID: 85, Style: 8 -> Golden Grave Marker
                    ];

                    List<(ushort tileID, int style, int xTiles)> treasuryTableDeco =
                    [
                        (TileID.PiggyBank,0,2), // Piggy bank
                        (TileID.Candelabras,0,2),// Candelabra
                        (TileID.DjinnLamp,0,2),// Desert Spirit Lamp (Djinn Lamp)
                        (TileID.Bottles,8,1), // Chalice
                        (TileID.Candles,0,1), // Candle
                        (TileID.SilverCoinPile,0,1) // (Copper/Silver/Gold) Coins, "Silver" is just a placeholder
                    ];

                    // put deco as long as TryPlaceTile() doesn't fail
                    int failsInRow = 0;
                    int stashEntranceCase = -1;
                    int treasurySurfaceSpace; // how much xTiles space on the surface there is (table = 3, workbench = 2)
                    (ushort tileID, int style, int xTiles) treasuryTableDecoCase;
                    bool stashEntranceCaseInit = true;
                    do
                    {
                        if      (stashEntranceCaseInit && stashEntranceCase == -1)  stashEntranceCase = 1; // first loop: force Marble Table
                        else if (stashEntranceCaseInit && stashEntranceCase ==  1)  stashEntranceCase = 2; // next loop: force Marble Workbench
                        else                                                        stashEntranceCase = WorldGen.genRand.Next(decoCases.Count); // choose a decoCase at random

                        switch (stashEntranceCase)
                        {
                            case 0:
                                #region Gold Chest
                                placeResult = Func.TryPlaceTile(entranceDeco, stashDoor, TileID.Containers, style: 1, chance: 95,  // Gold Chest
                                                                add: new() { { "CheckFree", [0, 1, 1, 0] },
                                                                             { "CheckArea", [0, 1, 1, 0] } });
                                break;
                                #endregion

                            case 1:
                                #region Marble Table
                                placeResult = Func.TryPlaceTile(entranceDeco, stashDoor, TileID.Tables, style: 34, chance: 95, maxTry: 8,  // Marble Table
                                                                add: new() { { "CheckFree", [1, 1, 1, 0] },
                                                                             { "CheckArea", [1, 1, 1, 0] } });
                                if (placeResult.success)
                                {
                                    treasurySurfaceSpace = 3;
                                    area1 = new(placeResult.x - 1, placeResult.y - 2, placeResult.x + 1, placeResult.y - 2, "dummy");
                                    do
                                    {
                                        do treasuryTableDecoCase = treasuryTableDeco[WorldGen.genRand.Next(treasuryTableDeco.Count)]; // choose a treasuryTableDeco at random, that fits in the space
                                        while (treasuryTableDecoCase.xTiles > treasurySurfaceSpace);

                                        if (treasuryTableDecoCase.tileID == TileID.SilverCoinPile) if (Chance.Perc(10)) treasuryTableDecoCase.tileID = Func.CoinQuality(silverThreshold: 0); // define coin quality


                                        placeResult = Func.TryPlaceTile(area1, noBlock, type: treasuryTableDecoCase.tileID, style: treasuryTableDecoCase.style, chance: 95,
                                                                        add: new() { { "CheckFree", [0, treasuryTableDecoCase.xTiles - 1, 0, 0] },
                                                                                     { "CheckArea", [0, treasuryTableDecoCase.xTiles - 1, 0, 0] } });

                                        if (placeResult.success && treasuryTableDecoCase.tileID == TileID.Candelabras)   Func.UnlightCandelabra(placeResult.x, placeResult.y);
                                        else if (placeResult.success && treasuryTableDecoCase.tileID == TileID.Candles)   Func.Unlight1x1(placeResult.x, placeResult.y);
                                        else if (placeResult.success && (treasuryTableDecoCase.tileID == TileID.SilverCoinPile || treasuryTableDecoCase.tileID == TileID.GoldCoinPile))
                                              { if (Chance.Perc(50))  WorldGen.PlaceTile(placeResult.x, placeResult.y - 1, Func.CoinQuality(silverThreshold: 0)); } // stack coins

                                        treasurySurfaceSpace -= treasuryTableDecoCase.xTiles;
                                    } while (treasurySurfaceSpace > 0);
                                }
                                
                                break;
                                #endregion

                            case 2:
                                #region Marble Workbench
                                placeResult = Func.TryPlaceTile(entranceDeco, stashDoor, TileID.WorkBenches, style: 30, chance: 95,  // Marble Workbench
                                                                add: new() { { "CheckFree", [0, 1, 0, 0] },
                                                                             { "CheckArea", [0, 1, 0, 0] } });


                                if (placeResult.success)
                                {
                                    treasurySurfaceSpace = 2;
                                    area1 = new(placeResult.x, placeResult.y - 1, placeResult.x + 1, placeResult.y - 1, "dummy");
                                    do
                                    {
                                        do treasuryTableDecoCase = treasuryTableDeco[WorldGen.genRand.Next(treasuryTableDeco.Count)]; // choose a treasuryTableDeco at random, that fits in the space
                                        while (treasuryTableDecoCase.xTiles > treasurySurfaceSpace);

                                        if (treasuryTableDecoCase.tileID == TileID.SilverCoinPile) treasuryTableDecoCase.tileID = Func.CoinQuality(silverThreshold: 0); // define coin quality


                                        placeResult = Func.TryPlaceTile(area1, noBlock, type: treasuryTableDecoCase.tileID, style: treasuryTableDecoCase.style, chance: 95,
                                                                        add: new() { { "CheckFree", [0, treasuryTableDecoCase.xTiles - 1, 0, 0] },
                                                                                     { "CheckArea", [0, treasuryTableDecoCase.xTiles - 1, 0, 0] } });

                                        if (placeResult.success && treasuryTableDecoCase.tileID == TileID.Candelabras) Func.UnlightCandelabra(placeResult.x, placeResult.y);
                                        else if (placeResult.success && treasuryTableDecoCase.tileID == TileID.Candles) Func.Unlight1x1(placeResult.x, placeResult.y);
                                        else if (placeResult.success && (treasuryTableDecoCase.tileID == TileID.CopperCoinPile || treasuryTableDecoCase.tileID == TileID.SilverCoinPile || treasuryTableDecoCase.tileID == TileID.GoldCoinPile))
                                        { if (Chance.Perc(50)) WorldGen.PlaceTile(placeResult.x, placeResult.y - 1, Func.CoinQuality(silverThreshold: 0)); } // stack coins

                                        treasurySurfaceSpace -= treasuryTableDecoCase.xTiles;
                                    } while (treasurySurfaceSpace > 0);
                                }
                                break;
                                #endregion

                            case 3:
                                #region Coin stash
                                int stashType;
                                int stashQuality = WorldGen.genRand.Next(100);

                                if     (stashQuality >= 90) stashType = 18; // Gold coin stash
                                else if(stashQuality >= 30) stashType = 17; // Silver coin stash
                                else                        stashType = 16; // Copper coin stash

                                placeResult = Func.TryPlaceTile(entranceDeco, noBlock, TileID.SmallPiles, chance: 95,
                                                                add: new() { { "Piles", [stashType, 1]   },
                                                                             { "CheckFree", [0, 1, 0, 0] },
                                                                             { "CheckArea", [0, 1, 0, 0] } });
                                break;
                                #endregion

                            case 4:
                                #region Coins
                                ushort coinType = Func.CoinQuality(silverThreshold: 0);

                                placeResult = Func.TryPlaceTile(entranceDeco, noBlock, coinType, chance: 95);

                                if (placeResult.success)
                                {
                                    if (Chance.Perc(50))
                                    {
                                        placed =                       WorldGen.PlaceTile(placeResult.x, placeResult.y - 1, Func.CoinQuality(silverThreshold: 0)); // stack coins
                                        if (placed && Chance.Perc(30)) WorldGen.PlaceTile(placeResult.x, placeResult.y - 2, Func.CoinQuality(silverThreshold: 0)); // stack coins
                                    }

                                    if (!Main.tile[placeResult.x - 1, placeResult.y].HasTile)
                                    {
                                        if(Chance.Perc(15)) WorldGen.PlaceTile(placeResult.x - 1, placeResult.y, Func.CoinQuality(silverThreshold: 0));
                                    }
                                    if (!Main.tile[placeResult.x + 1, placeResult.y].HasTile)
                                    {
                                        if (Chance.Perc(15)) WorldGen.PlaceTile(placeResult.x + 1, placeResult.y, Func.CoinQuality(silverThreshold: 0));
                                    }

                                }
                                    
                                break;
                            #endregion

                            case 5:
                                #region Golf Trophy

                                placeResult = Func.TryPlaceTile(entranceDeco, stashDoor, TileID.GolfTrophies, style: WorldGen.genRand.Next(1,3), chance: 95,  // Silver / Gold Golf Trophy
                                                                add: new() { { "CheckFree", [1, 0, 1, 0] },
                                                                             { "CheckArea", [1, 0, 1, 0] } });
                                break;
                                #endregion
                            default:
                                placeResult = (false, 0, 0);
                                break;

                        }

                        if (stashEntranceCaseInit && stashEntranceCase == 2) stashEntranceCaseInit = false; // init done: stop forcing decos

                        if (!placeResult.success) failsInRow++;
                        else failsInRow = 0;

                    } while (failsInRow < 3);


                    #region Shelves
                    // left
                    List<bool> treasuryShelfList = [];
                    (int length, int start, int end) leftTreasuryShelf;
                    int maxTreasuryShelfLength = 4;
                    x = freeR.X0;
                    y = entranceDecoHeight - 2;

                    for (int i = x; i < freeR.XCenter; i++) treasuryShelfList.Add(!Main.tile[i, y].HasTile);
                    leftTreasuryShelf = Func.GetLongestQueue(treasuryShelfList);

                    if (leftTreasuryShelf.length >= 3)
                    {
                        if (leftTreasuryShelf.length > maxTreasuryShelfLength)
                        {
                            // limit shelf length and center it
                            leftTreasuryShelf.start = (leftTreasuryShelf.length -  maxTreasuryShelfLength) / 2 + (leftTreasuryShelf.length - maxTreasuryShelfLength) % 2;
                            leftTreasuryShelf.end = leftTreasuryShelf.start + (maxTreasuryShelfLength - 1);
                            leftTreasuryShelf.length = maxTreasuryShelfLength;
                        }
                        for (int i = leftTreasuryShelf.start; i <= leftTreasuryShelf.end; i++)
                        {
                            WorldGen.PlaceTile(x + i, y, TileID.Platforms, style: 29); // Marble Platform
                            if (Chance.Perc(75))
                            { if (Chance.Perc(90))  WorldGen.PlaceTile(x + i, y - 1, Func.CoinQuality(silverThreshold: 0)); }
                            else
                            { if (Chance.Perc(90))  WorldGen.PlaceTile(x + i, y - 1, TileID.Bottles, style: 8); } //Chalice
                        }
                    }

                    // right
                    treasuryShelfList.Clear();
                    (int length, int start, int end) rightTreasuryShelf;
                    x = freeR.XCenter + 2;
                    y = entranceDecoHeight - 2;

                    for (int i = x; i <= freeR.X1; i++) treasuryShelfList.Add(!Main.tile[i, y].HasTile);
                    rightTreasuryShelf = Func.GetLongestQueue(treasuryShelfList);

                    if (rightTreasuryShelf.length >= 3)
                    {
                        if (rightTreasuryShelf.length > maxTreasuryShelfLength)
                        {
                            // limit shelf length and center it
                            rightTreasuryShelf.start = (rightTreasuryShelf.length - maxTreasuryShelfLength) / 2 + (rightTreasuryShelf.length - maxTreasuryShelfLength) % 2;
                            rightTreasuryShelf.end = rightTreasuryShelf.start + (maxTreasuryShelfLength - 1);
                            rightTreasuryShelf.length = maxTreasuryShelfLength;
                        }
                        for (int i = rightTreasuryShelf.start; i <= rightTreasuryShelf.end; i++)
                        {
                            WorldGen.PlaceTile(x + i, y, TileID.Platforms, style: 29); // Marble Platform
                            if (Chance.Perc(75))
                            { if (Chance.Perc(90)) WorldGen.PlaceTile(x + i, y - 1, Func.CoinQuality(silverThreshold: 0)); }
                            else
                            { if (Chance.Perc(90)) WorldGen.PlaceTile(x + i, y - 1, TileID.Bottles, style: 8); } //Chalice
                        }
                    }
                    #endregion

                    // try hang some marble lanterns
                    if (highEnoughForStash)
                    {
                        y = -(treasuryEntranceYTiles - 1);

                        placeResult = Func.TryPlaceTile(entranceDeco.CloneAndMove(0, y), noBlock, TileID.HangingLanterns, style: 36, chance: 95,  // marble lantern
                                                        add: new() { { "CheckFree", [0, 0, 0, 1] },
                                                                     { "CheckArea", [0, 0, 0, 1] } });

                        if (placeResult.success) Func.UnlightLantern(placeResult.x, placeResult.y);


                        placeResult = Func.TryPlaceTile(entranceDeco.CloneAndMove(0, y), noBlock, TileID.HangingLanterns, style: 36, chance: 95,  // marble lantern
                                                        add: new() { { "CheckFree", [0, 0, 0, 1] },
                                                                     { "CheckArea", [0, 0, 0, 1] } });

                        if (placeResult.success) Func.UnlightLantern(placeResult.x, placeResult.y);
                    }

                    #endregion

                    #region Stash
                    List<int> StashDecoGemItems =
                        [
                            ItemID.Amber,
                            ItemID.Amethyst,
                            ItemID.Diamond,
                            ItemID.Emerald,
                            ItemID.Ruby,
                            ItemID.Sapphire,
                            ItemID.Topaz
                        ];

                    if (highEnoughForStash)
                    {
                        int stashCeiling, stashFloor, stashLeftWall, stashRightWall;
                        int miniChamberCeiling = 0, miniChamberFloor = 0, miniChamberLeftWall = 0, miniChamberRightWall = 0; // init to not get compile-complaints...

                        bool highEnoughForStashMiniChamber = freeR.YTiles >= 13; // 13 YTiles leave 3 Tiles to stand in the minichamber and 3 to stand in the stash
                        #region Stash walls
                        if (entranceDecoHeight == freeR.Y1) // stash is in the upper part of the room
                        {
                            stashCeiling = freeR.Y0;
                            stashFloor = freeR.Y1 - treasuryEntranceYTiles;
                            stashLeftWall = freeR.X0;
                            stashRightWall = freeR.X1;

                            for (int j = stashCeiling; j <= stashFloor; j++)
                            {
                                WorldGen.PlaceTile(stashLeftWall, j, TileID.AncientGoldBrick); // bricks left
                                WorldGen.PlaceTile(stashRightWall, j, TileID.AncientGoldBrick); // bricks right
                            }
                            for (int i = stashLeftWall + 1; i <= stashRightWall - 1; i++)
                            {
                                if (doors[Door.Up].doorExist && (i == freeR.XCenter || i == freeR.XCenter + 1)) continue; // leave space for the trap door to the next floor
                                WorldGen.PlaceTile(i, stashCeiling, TileID.AncientGoldBrick); // bricks ceiling
                            }

                            if (doors[Door.Up].doorExist)
                            {
                                // put trap door to access the "stash" from above
                                WorldGen.PlaceObject(stashDoor.X0, stashCeiling, TileID.TrapdoorClosed);
                                WorldGen.paintTile(stashDoor.X0, stashCeiling, goldPaint);
                                WorldGen.paintTile(stashDoor.X1, stashCeiling, goldPaint);
                            }

                            // put backwall
                            Func.PlaceWallArea(new(stashLeftWall, stashCeiling, stashRightWall, stashFloor, "dummyString"), WallID.GoldBrick);
                            
                            if (highEnoughForStashMiniChamber)
                            {
                                miniChamberCeiling = freeR.Y0 + 4;
                                miniChamberFloor = stashFloor;
                                miniChamberLeftWall = doors[Door.Up].doorRect.X0 - 1;
                                miniChamberRightWall = doors[Door.Up].doorRect.X1 + 1;
                                for (int j = miniChamberCeiling; j < miniChamberFloor; j++)
                                {
                                    WorldGen.PlaceTile(miniChamberLeftWall, j, TileID.AncientGoldBrick); // bricks minichamber left
                                    WorldGen.PlaceTile(miniChamberRightWall, j, TileID.AncientGoldBrick); // bricks minichamber right
                                }
                                for (int i = miniChamberLeftWall + 1; i <= miniChamberRightWall - 1; i++)
                                {
                                    if (i == freeR.XCenter || i == freeR.XCenter + 1) continue; // leave space for the trap door to stash
                                    WorldGen.PlaceTile(i, miniChamberCeiling, TileID.AncientGoldBrick); // bricks minichamber ceiling
                                }

                                // put trap door to access the "stash" from above
                                WorldGen.PlaceObject(stashDoor.X0, miniChamberCeiling, TileID.TrapdoorClosed);
                                WorldGen.paintTile(stashDoor.X0, miniChamberCeiling, goldPaint);
                                WorldGen.paintTile(stashDoor.X1, miniChamberCeiling, goldPaint);

                                // put backwall
                                Func.PlaceWallArea(new(miniChamberLeftWall, miniChamberCeiling, miniChamberRightWall, miniChamberFloor, "dummyString"), WallID.AncientGoldBrickWall);
                            }
                        }
                        else // stash is in the lower part of the room
                        {
                            stashCeiling = entranceDecoHeight + 1;
                            stashFloor = freeR.Y1;
                            stashLeftWall = freeR.X0;
                            stashRightWall = freeR.X1;

                            for (int j = stashCeiling + 1; j <= stashFloor; j++)
                            {
                                WorldGen.PlaceTile(stashLeftWall, j, TileID.AncientGoldBrick); // bricks left
                                WorldGen.PlaceTile(stashRightWall, j, TileID.AncientGoldBrick); // bricks right
                            }
                            for (int i = stashLeftWall + 1; i <= stashRightWall - 1; i++)
                            {
                                if (doors[Door.Down].doorExist) // leave space at the bottom for a door
                                {
                                    if ((i == freeR.XCenter || i == freeR.XCenter + 1)) continue; // leave space for the trap door to the next below room
                                    else if (highEnoughForStashMiniChamber && (i >= doors[Door.Up].doorRect.X0 && i <= doors[Door.Up].doorRect.X1)) continue; // leave this open, to resemble the upper stash marble platform
                                }
                                
                                WorldGen.PlaceTile(i, stashFloor, TileID.AncientGoldBrick); // bricks floor
                            }

                            if (doors[Door.Down].doorExist && !highEnoughForStashMiniChamber)
                            {
                                // put trap door to access the "stash" from the next below room
                                WorldGen.PlaceObject(stashDoor.X0, stashFloor, TileID.TrapdoorClosed);
                                WorldGen.paintTile(stashDoor.X0, stashFloor, goldPaint);
                                WorldGen.paintTile(stashDoor.X1, stashFloor, goldPaint);
                            }

                            // put backwall
                            Func.PlaceWallArea(new(stashLeftWall, stashCeiling, stashRightWall, stashFloor, "dummyString"), WallID.GoldBrick);

                            if (highEnoughForStashMiniChamber)
                            {
                                miniChamberCeiling = stashCeiling + 4;
                                miniChamberFloor = stashFloor;
                                miniChamberLeftWall = doors[Door.Up].doorRect.X0 - 1;
                                miniChamberRightWall = doors[Door.Up].doorRect.X1 + 1;
                                for (int j = miniChamberCeiling; j < miniChamberFloor; j++)
                                {
                                    WorldGen.PlaceTile(miniChamberLeftWall, j, TileID.AncientGoldBrick); // bricks minichamber left
                                    WorldGen.PlaceTile(miniChamberRightWall, j, TileID.AncientGoldBrick); // bricks minichamber right
                                }
                                for (int i = miniChamberLeftWall + 1; i <= miniChamberRightWall - 1; i++)
                                {
                                    if (i == freeR.XCenter || i == freeR.XCenter + 1) continue; // leave space for the trap door to stash
                                    WorldGen.PlaceTile(i, miniChamberCeiling, TileID.AncientGoldBrick); // bricks minichamber ceiling
                                }

                                // put trap door to access the "minichamer" from above
                                WorldGen.PlaceObject(stashDoor.X0, miniChamberCeiling, TileID.TrapdoorClosed);
                                WorldGen.paintTile(stashDoor.X0, miniChamberCeiling, goldPaint);
                                WorldGen.paintTile(stashDoor.X1, miniChamberCeiling, goldPaint);

                                // put backwall
                                Func.PlaceWallArea(new(miniChamberLeftWall, miniChamberCeiling, miniChamberRightWall, miniChamberFloor, "dummyString"), WallID.AncientGoldBrickWall);
                            }
                        }

                        #endregion

                        #region FillStashWithCoins
                        if (highEnoughForStashMiniChamber)
                        {
                            Rectangle2P leftStash = new Rectangle2P(stashLeftWall + 1, stashCeiling + 1, miniChamberLeftWall - 1, stashFloor - 1, "dummyString");
                            (bool success, int leftHeight, int rightHeight, bool[,] coins) leftCoins = Func.CoinPile(leftStash, 1, leftHeightRand: 25);

                            for (int i = 0; i <= leftStash.XDiff; i++)
                            {
                                for (int j = 0; j <= leftStash.YDiff; j++)
                                {
                                    if (leftCoins.coins[j, i])
                                    {
                                        x = leftStash.X0 + i;
                                        y = leftStash.Y1 - j; // starting down left in the stash
                                        WorldGen.PlaceTile(x, y, Func.CoinQuality(silverThreshold: 0));
                                    }
                                }
                            }

                            Rectangle2P rightStash = new Rectangle2P(miniChamberRightWall + 1, stashCeiling + 1, stashRightWall - 1, stashFloor - 1, "dummyString");
                            (bool success, int leftHeight, int rightHeight, bool[,] coins) rightCoins = Func.CoinPile(rightStash, 4, rightHeightRand: 25);

                            for (int i = 0; i <= rightStash.XDiff; i++)
                            {
                                for (int j = 0; j <= rightStash.YDiff; j++)
                                {
                                    if (rightCoins.coins[j, i])
                                    {
                                        x = rightStash.X0 + i;
                                        y = rightStash.Y1 - j; // starting down left in the stash
                                        WorldGen.PlaceTile(x, y, Func.CoinQuality(silverThreshold: 0));
                                    }
                                }
                            }
                        }

                        else // NOT highEnoughForStashMiniChamber
                        {
                            Rectangle2P leftStash = new Rectangle2P(stashLeftWall + 1, stashCeiling + 1, stashDoor.X0 - 2, stashFloor - 1, "dummyString");
                            (bool success, int leftHeight, int rightHeight, bool[,] coins) leftCoins = Func.CoinPile(leftStash, 1, leftHeightRand: 25);

                            for (int i = 0; i <= leftStash.XDiff; i++)
                            {
                                for (int j = 0; j <= leftStash.YDiff; j++)
                                {
                                    if (leftCoins.coins[j, i])
                                    {
                                        x = leftStash.X0 + i;
                                        y = leftStash.Y1 - j; // starting down left in the stash
                                        WorldGen.PlaceTile(x, y, Func.CoinQuality(silverThreshold: 0));
                                    }
                                }
                            }

                            Rectangle2P rightStash = new Rectangle2P(stashDoor.X1 + 2, stashCeiling + 1, stashRightWall - 1, stashFloor - 1, "dummyString");
                            (bool success, int leftHeight, int rightHeight, bool[,] coins) rightCoins = Func.CoinPile(rightStash, 4, rightHeightRand: 25);

                            for (int i = 0; i <= rightStash.XDiff; i++)
                            {
                                for (int j = 0; j <= rightStash.YDiff; j++)
                                {
                                    if (rightCoins.coins[j, i])
                                    {
                                        x = rightStash.X0 + i;
                                        y = rightStash.Y1 - j; // starting down left in the stash
                                        WorldGen.PlaceTile(x, y, Func.CoinQuality(silverThreshold: 0));
                                    }
                                }
                            }
                        }

                        #endregion

                        #region StashCeilingDeco

                        // search how much space there is for GemLocks (3x3 tiles) on both sides
                        int leftside3 = freeR.XCenter;
                        int rightside3 = freeR.XCenter;
                        y = stashCeiling + 3; // up until where a GemLock reaches
                        while (!Main.tile[leftside3 - 1, y].HasTile) leftside3--;
                        while (!Main.tile[rightside3 + 1, y].HasTile) rightside3++;

                        // search how much space there is for ItemFrames (2x2 tiles) on both sides
                        int leftside2 = leftside3;
                        int rightside2 = rightside3;
                        y = stashCeiling + 2; // up until where an ItemFrame reaches
                        while (!Main.tile[leftside2 - 1, y].HasTile) leftside2--;
                        while (!Main.tile[rightside2 + 1, y].HasTile) rightside2++;

                        int choice = 0; // init
                        if (leftside3 <= (freeR.XCenter - 3) && rightside3 >= (freeR.XCenter + 4)) // if there is space, prefer "GemLock, ItemFrame, GemLock"
                        {
                            choice = 1; // GemLock, ItemFrame, GemLock
                        }
                        else if (leftside3 <= (freeR.XCenter - 2) && rightside3 >= (freeR.XCenter + 3))
                        {
                            choice = 2; // two GemLocks next to each other
                        }
                        else if (leftside3 <= (freeR.XCenter - 1) && rightside3 >= (freeR.XCenter + 2))
                        {
                            choice = 3; // two ItemFrames next to each other
                        }
                        else // two ItemFrames in the middle
                        {
                            choice = 4;
                            
                        }

                        switch (choice)
                        {
                            case 1: // GemLock, ItemFrame, GemLock

                                if (Chance.Perc(90))
                                {
                                    placed = WorldGen.PlaceTile(freeR.XCenter - 2, stashCeiling + 2, TileID.GemLocks, style: WorldGen.genRand.Next(7));
                                    if (placed) WorldGen.ToggleGemLock(freeR.XCenter - 2, stashCeiling + 2, true);
                                }

                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter, stashCeiling + 1, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter, stashCeiling + 3, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);

                                if (Chance.Perc(90))
                                {
                                    placed = WorldGen.PlaceTile(freeR.XCenter + 3, stashCeiling + 2, TileID.GemLocks, style: WorldGen.genRand.Next(7));
                                    if (placed) WorldGen.ToggleGemLock(freeR.XCenter + 3, stashCeiling + 2, true);
                                }

                                break;

                            case 2: // two GemLocks next to each other

                                if (Chance.Perc(90))
                                {
                                    placed = WorldGen.PlaceTile(freeR.XCenter - 1, stashCeiling + 2, TileID.GemLocks, style: WorldGen.genRand.Next(7));
                                    if (placed) WorldGen.ToggleGemLock(freeR.XCenter - 1, stashCeiling + 2, true);
                                }

                                if (Chance.Perc(90))
                                {
                                    placed = WorldGen.PlaceTile(freeR.XCenter + 2, stashCeiling + 2, TileID.GemLocks, style: WorldGen.genRand.Next(7));
                                    if (placed) WorldGen.ToggleGemLock(freeR.XCenter + 2, stashCeiling + 2, true);
                                }
                                break;

                            case 3: // two ItemFrames next to each other

                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter - 1, stashCeiling + 1, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter + 1, stashCeiling + 1, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter    , stashCeiling + 3, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                break;

                            case 4: // two ItemFrames in the middle

                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter, stashCeiling + 1, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                if (Chance.Perc(90)) Func.PlaceItemFrame(freeR.XCenter, stashCeiling + 3, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                break;

                            default:
                                break;
                        }

                        #endregion

                        #region MiniChamberDeco
                        if (highEnoughForStashMiniChamber)
                        {
                            

                            int miniChamberItemFrameCount = (miniChamberFloor - miniChamberCeiling) / 2;
                            for (int i = 0; i < miniChamberItemFrameCount; i++)
                            {
                                if (Chance.Perc(75)) // two ItemFrames
                                {
                                    if (Chance.Perc(85)) Func.PlaceItemFrame(miniChamberLeftWall + 1, miniChamberCeiling + 1 + (i * 2), item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                    if (Chance.Perc(85)) Func.PlaceItemFrame(miniChamberLeftWall + 3, miniChamberCeiling + 1 + (i * 2), item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                }
                                else // one ItemFrame in the middle
                                {
                                    if (Chance.Perc(90)) Func.PlaceItemFrame(miniChamberLeftWall + 2, miniChamberCeiling + 1 + (i * 2), item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region Stashless Ceiling Deco
                        automat = new((freeR.X0, freeR.Y0), (int)LineAutomat.Dirs.xPlus);

                        // Wall / WallAndPaint for GemLocks (3x3, anchor is center)
                        Wall                             = new() { { (int)LineAutomat.Adds.Wall,  [ WallID.AncientGoldBrickWall, 0, -1, 0 ] } };
                        Dictionary<int, List<int>> Paint = new() { { (int)LineAutomat.Adds.Paint, [ PaintID.DeepYellowPaint    , 0, -1    ] } };
                        Dictionary<int, List<int>> GemLockFill = new() { { (int)LineAutomat.Adds.GemLockFill, [1] } };

                        // Wall / WallAndPaint for ItemFrames (2x2, anchor is top left)
                        Dictionary<int, List<int>> Wall2 =  new() { { (int)LineAutomat.Adds.Wall,  [ WallID.AncientGoldBrickWall, 0, 0, 0 ] } };
                        Dictionary<int, List<int>> Paint2 = new() { { (int)LineAutomat.Adds.Paint, [ PaintID.DeepYellowPaint    , 0, 0    ] } };

                        Dictionary<int, List<int>> LightOff = new() { { (int)LineAutomat.Adds.LightOff, [1] } };


                        hangSpace = freeR.XTiles;

                        int max3Tiles = hangSpace / 3; // how many 3-tile items ( GemLocks or (ItemFrame + 1 Space)) fit inside the room
                        //max3Tiles -= max3Tiles % 2; // make it an even count, to better fit the even-tiles-room
                        int pair3Tiles = WorldGen.genRand.Next((max3Tiles / 2)) + 1; // random amount of pairs, but at least one

                        int unusedXTilesItemFrames = hangSpace - ((pair3Tiles * 2) * 3); // remaining space for ItemFrames (always an even number in even-tiles-rooms)
                        int hangNumItemFrame = unusedXTilesItemFrames / 2;

                        actX = freeR.X0; // init

                        if (hangNumItemFrame > 0) // chance to turn an ItemFrame into two banners (1 on the left and 1 on right end)
                        {
                            if (Chance.Perc(50))
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, 0), chance: 90, add: []));
                                actX++;
                                hangNumItemFrame--;
                            }
                        }
                        else // chance to turn a GemLock-Pair into a banner + ItemFrame on both sides
                        {
                            if (Chance.Perc(50))
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.Banners, style: Deco[S.Banner], size: (1, 3), toAnchor: (0, 0), chance: 90, add: []));

                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], style: 0,
                                                                    size: (2, 2), toAnchor: (0, 0), chance: 90, add: Func.CombineDicts(Wall2, Paint2) ));
                                actX = actX + 3;
                                pair3Tiles--;
                            }
                        }

                        if (pair3Tiles > 0)
                        {
                            // chance to turn a GemLock-Pair into a marble lamp and an ItemFrame on both sides
                            if (Chance.Perc(50))
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.HangingLanterns, style: 36, size: (1, 2), toAnchor: (0, 0), chance: 90, add: LightOff));
                                
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], style: 0,
                                                                    size: (2, 2), toAnchor: (0, 0), chance: 90, add: Func.CombineDicts(Wall2, Paint2) ));
                                actX = actX + 3;
                                pair3Tiles--;
                            }
                        }

                        // put all remaining GemLock pairs
                        if (pair3Tiles > 0)
                        {
                            for (int i = 1; i <= pair3Tiles; i++)
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.GemLocks, style: WorldGen.genRand.Next(7),
                                                                    size: (3, 3), toAnchor: (1, 1), chance: 90, add: Func.CombineDicts(Wall, GemLockFill)));
                                actX = actX + 3;
                            }
                        }
                        
                        // put all remaining ItemFrame pairs
                        if ( ((int)hangNumItemFrame / 2) > 0 )
                        {
                            for (int i = 1; i <= (hangNumItemFrame / 2); i++)
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], style: 0,
                                                                    size: (2, 2), toAnchor: (0, 0), chance: 90, add: Func.CombineDicts(Wall2, Paint2)));
                                actX = actX + 2;
                            }
                        }

                        if((hangNumItemFrame % 2) > 0) // if there's an uneven number of ItemFrames then the one in the middle of the room needs to be placed manually. Add just a space here
                        {
                            automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Space, 0, 0, size: (1, 0), (0, 0), 0, []));
                        }


                        // the middle of the room is reached, mirror the steps, keeping the randomness
                        for (int i = automat.Steps.Count - 1; i >= 0; i--)
                        {
                            if      (automat.Steps[i].cmd == (int)LineAutomat.Cmds.Space) automat.Steps.Add(automat.Steps[i]); // just copy spaces
                            else if (automat.Steps[i].cmd == (int)LineAutomat.Cmds.ItemFrame)
                            {
                                automat.Steps.Add((cmd: (int)LineAutomat.Cmds.ItemFrame, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], style: 0,
                                                                    size: (2, 2), toAnchor: (0, 0), chance: 90, add: Func.CombineDicts(Wall2, Paint2)));
                            }
                            else if (automat.Steps[i].cmd == (int)LineAutomat.Cmds.Tile)
                            {
                                if (automat.Steps[i].item == TileID.GemLocks)
                                {
                                    automat.Steps.Add((cmd: (int)LineAutomat.Cmds.Tile, item: TileID.GemLocks, style: WorldGen.genRand.Next(7),
                                                                    size: (3, 3), toAnchor: (1, 1), chance: 90, add: Func.CombineDicts(Wall, GemLockFill)));
                                }
                                else automat.Steps.Add(automat.Steps[i]); // just copy other tiles
                            }
                        }

                        automat.Start();



                        if ((hangNumItemFrame % 2) > 0) // fill the possible hole in the middle, that the automat just reserved with spaces
                        {
                            if (Chance.Perc(85)) Func.PlaceItemFrame(freeR.XCenter, freeR.Y0, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                            if (Chance.Perc(85)) Func.PlaceItemFrame(freeR.XCenter, freeR.Y0 + 2, item: StashDecoGemItems[WorldGen.genRand.Next(StashDecoGemItems.Count)], paint: PaintID.DeepYellowPaint);
                        }
                        #endregion
                    }
                    #endregion
                    break;

                case 100: // empty room for display
                    break;

            }

            Func.PlaceCobWeb(freeR, 1, WorldGenMod.configFrostFortressCobwebFilling);
        }

        public void FillChest(Chest chest, int style)
        {
            List<int> mainItem = [];
            mainItem.Add(ItemID.IceBoomerang);
            mainItem.Add(ItemID.IceSkates);
            mainItem.Add(ItemID.IceBlade);
            mainItem.Add(ItemID.SnowballCannon);
            mainItem.Add(ItemID.IceBoomerang);
            mainItem.Add(ItemID.SapphireStaff);
            mainItem.Add(ItemID.FlinxStaff);
            mainItem.Add(ItemID.FlurryBoots);
            mainItem.Add(ItemID.BlizzardinaBottle);


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
                ItemID.IceTorch,
                ItemID.Glowstick,
                ItemID.FairyGlowstick,
                ItemID.SpelunkerGlowstick
            ];


            List<int> ammoItem =
            [
                ItemID.FrostburnArrow,
                ItemID.FrostDaggerfish,
                ItemID.Snowball,
                ItemID.ThrowingKnife,
                ItemID.JestersArrow,
                ItemID.Shuriken
            ];


            int nextItem = 0; //init

            chest.item[nextItem].SetDefaults(mainItem[WorldGen.genRand.Next(mainItem.Count)]);
            chest.item[nextItem].stack = 1;
            nextItem++;

            if (Chance.Perc(14.29f))
            {
                chest.item[nextItem].SetDefaults(ItemID.IceMachine);
                chest.item[nextItem].stack = 1;
                nextItem++;
            }

            if (Chance.Perc(20))
            {
                chest.item[nextItem].SetDefaults(ItemID.IceMirror);
                chest.item[nextItem].stack = 1;
                nextItem++;
            }

            chest.item[nextItem].SetDefaults(potionItem[WorldGen.genRand.Next(potionItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 4);
            nextItem++;

            chest.item[nextItem].SetDefaults(lightItem[WorldGen.genRand.Next(lightItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(6, 13);
            nextItem++;

            chest.item[nextItem].SetDefaults(ammoItem[WorldGen.genRand.Next(ammoItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(25, 75);
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
                    if (!roomEvenX)   x -= (1 - randAddX);
                }
                else   x = area.X0 + WorldGen.genRand.Next((area.XTiles - 6) + 1);

                if (centY)
                {
                    y = area.YCenter - 1; // even room
                    if (!roomEvenY) y -= (1 - randAddY);
                }
                else   y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 4) + 1);

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
                if ( roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

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

                if ( roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
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
        /// Tries to symmetrically place two paintings of the same type in the given area. It tries to place paintings from tall to flat (6x4 -> 3x3 -> 2x3 -> 3x2)
        /// <br/>
        /// <br/> ATTENTION: does not pre-check if the final placement position is empty. Best make sure that the whole area is free.
        /// </summary>
        /// <param name="area">The area where the painting can be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeMode">The placement method in the two halves of the room:
        /// <br/>                   --> 0 = centered in x and y, 1 = random x and centered y, 2 = centered x and random y, 3 = random x and y, 10..19 = force 0..9 x-tiles distance away from the edge of the given area and y centered</param>
        /// <param name="sameType">If the placed painting shall be of the same type or can be different</param>
        /// <param name="allowType">Allow types of paintings: (binary selection) 0= no painting, 1=3x2, 2=2x3, 4=3x3, 8=6x4, 15=all types</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        /// <param name="centerErrorX">If x-placeMode is "centered" and the painting placement results in an impossible symmetrical centering do: -1 = force left position, 0 = random, 1 = force right position, -88 = abort function</param>
        /// <param name="centerErrorX">If y-placeMode is "centered" and the painting placement results in an impossible symmetrical centering do: -1 = force upper position, 0 = random, 1 = force lower position, -88 = abort function</param>

        /// <returns><br/>Tupel item1 <b>success</b>: true if placement was successful
        ///          <br/>Tupel item2 <b>paintingArea</b>: if success = true, the covered area of the painting, else Rectangle2P.Empty
        ///          <br/>Tupel item3 <b>paintingType</b>: contains the placed / attempted to place painting type (1 = 6x4, 2 = 3x3, 3 = 2x3, 4 = 3x2), else 0
        ///          <br/>Tupel item4 <b>failReason</b>: if success = false, contains the reason for failing 
        ///          <br/> --> (1 = WorldGen.PlaceTile failed(), 2 = aborted because of centerErrorX, 3 = aborted because of centerErrorY, 4 = every single painting Chance roll failed), else 0</returns>
        ///          
        public ((bool, bool) success, (Rectangle2P, Rectangle2P) paintingArea, (int, int) paintingType, (int, int) failReason) Place2Paintings(
            Rectangle2P area, int style, int placeMode = 0, bool sameType = true, byte allowType = 15, bool placeWall = false, int centerErrorX = 0, int centerErrorY = -1)
        {
            bool allow3x2 = ((allowType & 1) != 0) && (area.XTiles >= 3) && (area.YTiles >= 2);
            bool allow2x3 = ((allowType & 2) != 0) && (area.XTiles >= 2) && (area.YTiles >= 3);
            bool allow3x3 = ((allowType & 4) != 0) && (area.XTiles >= 3) && (area.YTiles >= 3);
            bool allow6x4 = ((allowType & 8) != 0) && (area.XTiles >= 6) && (area.YTiles >= 4);

            bool centX = ((placeMode == 0) || (placeMode == 2)) && (placeMode < 10); // painting centered in x direction
            bool centY = ((placeMode == 0) || (placeMode == 1)) || ((placeMode >= 10) && (placeMode <= 19)); // painting centered in y direction

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
            bool success1 = false;
            bool success2 = false;
            Rectangle2P paintingArea1 = Rectangle2P.Empty;
            Rectangle2P paintingArea2 = Rectangle2P.Empty;
            int paintingType1 = 0;
            int paintingType2 = 0;
            int failReason1 = 0;
            int failReason2 = 0;

            //painting
            int x = area.X0, y = area.Y0; // init

            ////////////////////
            //// Disclaimer ////
            ////////////////////
            // --> This function is a WIP, I just implemented what I acutally needed for the use case!

            if (allow3x3 && sameType && centY && ((placeMode >= 10) && (placeMode <= 19)))
            {
                paintingType1 = paintingType2 = 2;

                //if (roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                //if (roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                x = area.X0 + (placeMode - 10);

                if (centY)
                {
                    y = area.YCenter - 1; // uneven room
                    if (roomEvenY) y += randAddY;
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 3) + 1);

                paintingArea1 = new(x, y, 3, 3);
                success1 = Place3x3PaintingByStyle(paintingArea1, style, placeWall);

                if (!success1) failReason1 = 1;

                ///////////////////////////////////////////////////////////

                x = area.X1 - 2 - (placeMode - 10);

                if (centY)
                {
                    y = area.YCenter - 1; // uneven room
                    if (roomEvenY) y += randAddY;
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 3) + 1);

                paintingArea2 = new(x, y, 3, 3);
                success2 = Place3x3PaintingByStyle(paintingArea2, style, placeWall);

                if (!success2) failReason2 = 1;
            }
            else if (allow3x2 && sameType && centY && ((placeMode >= 10) && (placeMode <= 19)))
            {
                paintingType1 = paintingType2 = 4;

                //if (roomEvenX && abortCenterX) return (success, paintingArea, paintingType, 2);
                //if (!roomEvenY && abortCenterY) return (success, paintingArea, paintingType, 3);

                x = area.X0 + (placeMode - 10);

                if (centY)
                {
                    y = area.YCenter; // even room
                    if (!roomEvenY) y -= (1 - randAddY);
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 2) + 1);

                paintingArea1 = new(x, y, 3, 2);
                success1 = Place3x2PaintingByStyle(paintingArea1, style, placeWall);

                if (!success1) failReason1 = 1;

                ///////////////////////////////////////////////////////////

                x = area.X1 - 2 - (placeMode - 10);

                if (centY)
                {
                    y = area.YCenter; // even room
                    if (!roomEvenY) y -= (1 - randAddY);
                }
                else y = area.Y0 + WorldGen.genRand.Next((area.YTiles - 2) + 1);

                paintingArea2 = new(x, y, 3, 2);
                success2 = Place3x2PaintingByStyle(paintingArea2, style, placeWall);

                if (!success2) failReason2 = 1;
            }

            if (!success1 && paintingType1 == 0) failReason1 = 4;
            if (!success2 && paintingType2 == 0) failReason2 = 4;

            return ((success1, success2), (paintingArea1, paintingArea2), (paintingType1, paintingType2), (failReason1, failReason2));
        }

            /// <summary>
            /// Places a random 6x4 painting of a pre-selected variety for the given decoration style 
            /// </summary>
            /// <param name="area">The 6x4 area where the painting shall be placed</param>
            /// <param name="style">The decoration style of the frost fortress</param>
            /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
            public bool Place6x4PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall]);

            List<int> paintings = [];
            if (style == S.StyleSnow)
            {
                paintings.Add(5); // Dryadisque
                paintings.Add(11); // Great Wave
                paintings.Add(12); // Starry Night
                paintings.Add(30); // Sparky
            }
            else if (style == S.StyleBoreal)
            {
                paintings.Add(0); // The Eye Sees the End
                paintings.Add(3); // The Screamer
                paintings.Add(4); // Goblins Playing Poker
                paintings.Add(9); // The Persistency of Eyes
                paintings.Add(16); // The Creation of the Guide
                paintings.Add(46); // The Gentleman Scientist
                paintings.Add(47); // The Firestarter
                paintings.Add(48); // The Bereaved
                paintings.Add(49); // The Strongman
            }
            else if (style == S.StyleDarkLead)
            {
                paintings.Add(1); // Something Evil is Watching You
                paintings.Add(2); // The Twins Have Awoken
                paintings.Add(8); // The Destroyer
                paintings.Add(17); // Jacking Skeletron
                paintings.Add(19); // Blood Moon Countess
                paintings.Add(21); // Morbid Curiosity
                paintings.Add(44); // Graveyard (Painting)
                paintings.Add(50); // Remnants of Devotion
            }
            else
            {
                paintings.Add(23); // Leopard Skin...should never occur or I called the method wrong...so just to be sure
            }

            bool success = WorldGen.PlaceTile(area.X0 + 2, area.Y0 + 2, TileID.Painting6X4, style: paintings[WorldGen.genRand.Next(paintings.Count)] );

            return success;
        }

        /// <summary>
        /// Places a random 3x3 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 3x3 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place3x3PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall]);

            List<int> paintings = [];
            if (style == S.StyleSnow)
            {
                paintings.Add(22); // Guide Picasso
                paintings.Add(24); // Father of Someone
                paintings.Add(26); // Discover
                paintings.Add(76); // Outcast
                paintings.Add(77); // Fairy Guides
                paintings.Add(79); // Morning Hunt
                paintings.Add(82); // Cat Sword
            }
            else if (style == S.StyleBoreal)
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
            else if (style == S.StyleDarkLead)
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
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place2x3PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall]);

            List<int> paintings = [];
            if (style == S.StyleSnow)
            {
                paintings.Add(0); // Waldo
                paintings.Add(10); // Ghost Manifestation
                paintings.Add(15); // Strange Growth #1
                paintings.Add(19); // Happy Little Tree
                paintings.Add(26); // Love is in the Trash Slot
            }
            else if (style == S.StyleBoreal)
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
            else if (style == S.StyleDarkLead)
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
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeWall">Forces backwall placement before trying to place the painting</param>
        public bool Place3x2PaintingByStyle(Rectangle2P area, int style, bool placeWall = false)
        {
            if (placeWall) Func.PlaceWallArea(area, Deco[S.BackWall]);

            List<int> paintings = [];
            if (style == S.StyleSnow)
            {
                paintings.Add(6); // Place Above the Clouds
                paintings.Add(8); // Cold Waters in the White Land
                paintings.Add(15); // Sky Guardian
                paintings.Add(32); // Viking Voyage
                paintings.Add(35); // Forest Troll
            }
            else if (style == S.StyleBoreal)
            {
                paintings.Add(1); // Finding Gold
                paintings.Add(5); // Through the Window
                paintings.Add(7); // Do Not Step on the Grass
                paintings.Add(11); // Daylight
                paintings.Add(20); // Still Life
                paintings.Add(33); // Bifrost
            }
            else if (style == S.StyleDarkLead)
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
        public const String Brick = "Brick";
        public const String Floor = "Floor";
        public const String BackWall = "BackWall";
        public const String DoorWall = "DoorWall";
        public const String DoorPlat = "DoorPlatform";
        public const String Door = "Door";
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

        public const int StyleSnow = 0;
        public const int StyleBoreal = 1;
        public const int StyleDarkLead = 2;
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


}
