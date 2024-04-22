using Terraria;
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

//TODO: sometimes the FrostFortress creates extremely slow - supposedly because of the frequent PlaceTile calls...what to do?

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

            while (amountGenerated == 0) // reduced to only 1 Fortress per map. was: (amountGenerated < amount)
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
                    Deco[S.Dresser] = 30;  // Tile ID 88 (Dressers) -> Type 30=Frozen
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
                    Deco[S.Dresser] = 18;  // Tile ID 88 (Dressers) -> Type 18=Boreal
                    break;

                case S.StyleDarkLead: // Dark Lead
                    Deco[S.StyleSave] = S.StyleDarkLead;
                    Deco[S.Brick] = TileID.LeadBrick;
                    Deco[S.Floor] = TileID.EbonstoneBrick;
                    //TODO: find something (Platinum Brick?)     if (Chance.Simple())   Deco[Style.Floor] = TileID.AncientSilverBrick;
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
                    Deco[S.Lantern] = 0;   // Tile ID 42 (Lanterns) -> Type 0=Chain Lantern
                    Deco[S.Banner] = 0;    // Tile ID 91 (Banners) -> Type 0=Red
                    Deco[S.DecoPlat] = 19; // Tile ID 19 (Plattforms) -> Type 19=Boreal
                    Deco[S.StylePaint] = PaintID.GrayPaint;
                    Deco[S.HangingPot] = 6; // Tile ID 591 (PotsSuspended) -> Type 6=Corrupt Deathweed
                    Deco[S.Bookcase] = 7;  // Tile ID 101 (Bookcases) -> Type 7=Ebonwood
                    Deco[S.Sofa] = 2;      // Tile ID 89 (Sofas) -> Type 2=Ebonwood
                    Deco[S.Clock] = 10;    // Tile ID 104 (GrandfatherClocks) -> Type 10=Ebonwood
                    Deco[S.Bed] = 1;      // Tile ID 79 (Beds) -> Type 1=Ebonwood
                    Deco[S.BedWallpaper] = WallID.StarlitHeavenWallpaper;
                    Deco[S.Dresser] = 1;  // Tile ID 88 (Dressers) -> Type 1=Ebonwood
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
            if (!WorldGenMod.generateFrostFortresses)
            {
                return;
            }

            Deco.Clear();
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
            int forceEvenRoom = 1; // 1 = force all side rooms to have an even XTiles count; 0 = force all side rooms to have an odd XTiles count
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
            // the "free" room.... e.g. the rooms free inside (not the wall bricks)
            Rectangle2P freeR = new Rectangle2P(room.X0 + wThick, room.Y0 + wThick, room.X1 - wThick, room.Y1 - wThick, "dummyString");

            int x; //temp variable for later calculations;
            int y; //temp variable for later calculations;

            bool noBreakPoint = Chance.Simple(); //force the background wall of the room to have no holes
            Vector2 wallBreakPoint = new(room.X0 + WorldGen.genRand.Next(room.XDiff), room.Y0 + WorldGen.genRand.Next(room.YDiff));



            // create door rectangles
            #region door rectangles
            IDictionary<int, (bool doorExist ,Rectangle2P doorRect)> doors = new Dictionary<int, (bool, Rectangle2P)>(); // a dictionary for working and sending the doors in a compact way

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
                    continue; // these door are already created by their previous room, no need to to
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
                WorldGen.SlopeTile(leftDoorRect.X1, leftDoorRect.Y0 - 1, 3); // door right corner
            }
            if (rightDoor)
            {
                WorldGen.SlopeTile(rightDoorRect.X0, rightDoorRect.Y0 - 1, 4); // door left corner
            }
            if (upDoor && !downRoom)
            {
                WorldGen.SlopeTile(upDoorRect.X0 - 1, upDoorRect.Y1, 3); // updoor left corner
                WorldGen.SlopeTile(upDoorRect.X1 + 1, upDoorRect.Y1, 4); // updoor right corner
            }
            if (downDoor && !upRoom)
            {
                WorldGen.SlopeTile(downDoorRect.X0 - 1, downDoorRect.Y1, 3); // updoor left corner
                WorldGen.SlopeTile(downDoorRect.X1 + 1, downDoorRect.Y1, 4); // updoor right corner
            }
            #endregion

            DecorateRoom(room: room,
                         roomType: roomType,
                         doors: doors);

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
                PlaceCobWeb(freeR, 1, 25);

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

            if (roomType == RoomID.AboveSide)
            {
                //TODO?
            }

            if (roomType == RoomID.BelowSide)
            {
                //TODO?
            }

            // init variables
            bool placed;
            bool stinkbugPlaced = false; //stinkbug for preventing NPCs to move into rooms
            (bool success, int x, int y) placeResult, placeResult2;
            Rectangle2P area1, area2, area3, noBlock = Rectangle2P.Empty; // for creating areas for random placement
            List<(int x, int y)> rememberPos = new List<(int, int)>(); // for remembering positions
            List<(ushort TileID, int style, byte chance)> randomItems = new List<(ushort, int, byte)>(); // for random item placement
            int chestID;

            //choose room decoration at random
            //int roomDeco = WorldGen.genRand.Next(7); //TODO: don't forget to put the correct values in the end
            int roomDeco = 4;
            switch (roomDeco)
            {
                case 0: // two tables, two lamps, a beam line, high rooms get another beam line and a painting

                    // table left
                    x = freeR.XCenter - WorldGen.genRand.Next(3, freeR.XDiff / 2 - 1);
                    y = freeR.Y1;
                    placed = false;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.Tables, style: Deco[S.Table]); // Table
                    else if (Chance.Simple()) Func.PlaceLargePile(x, y, 22, 0, 186, paint: (byte)Deco[S.StylePaint]); //Broken Table covered in CobWeb

                    // stuff on the left table
                    if (placed)
                    {
                        if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.FoodPlatter); // food plate
                        if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.Bottles, style: 4); // mug
                    }


                    // table right
                    x = freeR.XCenter + WorldGen.genRand.Next(3, freeR.XDiff / 2 - 1);
                    y = freeR.Y1;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.Tables, style: Deco[S.Table]); // Table
                    else if (Chance.Simple()) Func.PlaceLargePile(x, y, 22, 0, 186, paint: (byte)Deco[S.StylePaint]); //Broken Table covered in CobWeb

                    // stuff on the right table
                    if (placed)
                    {
                        if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.FoodPlatter); // food plate
                        if (Chance.Simple()) WorldGen.PlaceTile(x + WorldGen.genRand.Next(-1, 2), y - 2, TileID.Bottles, style: 4); // mug
                    }


                    // wooden beam
                    if (freeR.YTiles >= 8) // if less than 8 tiles, there won't be enough space for the lanterns to look good
                    {
                        y = freeR.Y1 - 4;

                        for (x = freeR.X0; x <= freeR.X1; x++)
                        {
                            if (!(Main.tile[x, y].WallType == 0))
                            {
                                WorldGen.PlaceTile(x, y, TileID.BorealBeam);
                                WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                            }
                        }

                    }

                    // if room is too high, there will be a lot of unused space...fill it
                    if (freeR.YTiles >= 12)
                    {
                        int lastBeam = y;
                        y = freeR.Y0 + 3;

                        // wooden beam
                        for (x = freeR.X0; x <= freeR.X1; x++)
                        {
                            if (!(Main.tile[x, y].WallType == 0))
                            {
                                WorldGen.PlaceTile(x, y, TileID.BorealBeam);
                                WorldGen.paintTile(x, y, (byte)Deco[S.StylePaint]);
                            }
                        }

                        //painting
                        PlacePainting(new Rectangle2P(freeR.X0, y + 1, freeR.X1, lastBeam - 1, "dummyString"), Deco[S.StyleSave]);
                    }


                    // lantern left
                    x = freeR.XCenter - WorldGen.genRand.Next(3, freeR.XDiff / 2);
                    y = freeR.Y0;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: Deco[S.Lantern]); // Table
                    if (placed) Func.UnlightLantern(x, y);

                    // lantern right
                    x = freeR.XCenter + 1 + WorldGen.genRand.Next(3, freeR.XDiff / 2);
                    y = freeR.Y0;
                    if (Chance.Simple()) placed = WorldGen.PlaceTile(x, y, TileID.HangingLanterns, style: Deco[S.Lantern]); // Table
                    if (placed) Func.UnlightLantern(x, y);

                    break;

                case 1: // kitchen with shelves

                    // wooden beam arch at floor
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

                    // chains hanging down
                    if (((freeR.Y1 - 5) - freeR.Y0) >= 1) // at least two tiles or it looks weird
                    {
                        for (y = freeR.Y0; y <= freeR.Y1 - 5; y++)
                        {
                            WorldGen.PlaceTile(doors[Door.Down].doorRect.X0 - 1, y, TileID.Chain);
                            WorldGen.PlaceTile(doors[Door.Down].doorRect.X1 + 1, y, TileID.Chain);
                        }
                    }


                    // cooking pot and bar table or fireplace
                    area1 = new Rectangle2P(freeR.X0 + 1, freeR.Y1, freeR.X1 - 1, freeR.Y1, "dummyString");
                    area2 = doors[Door.Down].doorRect.CloneAndMove(0, -1);

                    Func.TryPlaceTile(area1, area2, TileID.CookingPots, style: 0); // cooking pot

                    placeResult = Func.TryPlaceTile(area1, area2, TileID.Tables, style: 17, chance: 70); // wooden bar (table)
                    if (placeResult.success)
                    {
                        // put deco on bar
                        area1 = new Rectangle2P(placeResult.x - 1, placeResult.y - 2, placeResult.x + 1, placeResult.y - 2, "dummyString");
                        Func.TryPlaceTile(area1, noBlock, TileID.FoodPlatter, style: 17, chance: 50); // food plate
                        placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Candles, style: 0, chance: 50); // normal candle (blends better with the wooden bar)
                        if (placeResult.success) Func.Unlight1x1(placeResult.x, placeResult.y);
                        Func.TryPlaceTile(area1, noBlock, TileID.Bottles, style: 4, chance: 50); // Mug

                    }

                    placeResult = Func.TryPlaceTile(area1, area2, TileID.Fireplace, chance: 70); // Fireplace
                    if (placeResult.success) Func.UnlightFireplace(placeResult.x, placeResult.y);

                    Func.TryPlaceTile(area1, area2, TileID.Sinks, style: 0, chance: 70); // wooden sink

                    // first shelf 
                    if (freeR.YTiles >= 6)
                    {
                        for (x = freeR.X0; x <= doors[Door.Down].doorRect.X0 - 1; x++)
                        {
                            WorldGen.PlaceTile(x, freeR.Y1 - 4, TileID.Platforms, style: 19); // Boreal wood platform
                            WorldGen.paintTile(x, freeR.Y1 - 4, (byte)Deco[S.StylePaint]);
                        }
                        for (x = doors[Door.Down].doorRect.X1 + 1; x <= freeR.X1; x++)
                        {
                            WorldGen.PlaceTile(x, freeR.Y1 - 4, TileID.Platforms, style: 19); // Boreal wood platform
                            WorldGen.paintTile(x, freeR.Y1 - 4, (byte)Deco[S.StylePaint]);
                        }

                        //put deco on first shelf
                        area1 = new Rectangle2P(freeR.X0, freeR.Y1 - 5, freeR.X1, freeR.Y1 - 5, "dummyString");
                        area2 = doors[Door.Down].doorRect.CloneAndMove(0, -6);

                        randomItems.Add((TileID.Bowls, 0, 50)); // bowl
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

                        if (freeR.YTiles >= 7)
                        {
                            Func.TryPlaceTile(area1, area2, TileID.Kegs, style: 0, chance: 50); // wooden keg

                            placeResult = Func.TryPlaceTile(area1, area2, TileID.FishingCrate, style: 0, chance: 50); // wooden fishing crate
                            if (placeResult.success) rememberPos.Add((placeResult.x, placeResult.y)); // remember placement position for later

                            Func.TryPlaceTile(area1, area2, TileID.Bottles, style: WorldGen.genRand.Next(9), chance: 50); // any 1x1 dish
                            Func.TryPlaceTile(area1, area2, TileID.FoodPlatter, style: 17, chance: 50); // food plate

                            placeResult = Func.TryPlaceTile(area1, area2, TileID.FishingCrate, style: 0, chance: 50); // wooden fishing crate
                            if (placeResult.success) rememberPos.Add((placeResult.x, placeResult.y)); // remember placement position for later


                            for (int i = 1; i <= 9; i++)
                            {
                                int num = WorldGen.genRand.Next(randomItems.Count);
                                Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            }
                        }
                        else
                        {
                            for (int i = 1; i <= 15; i++)
                            {
                                int num = WorldGen.genRand.Next(randomItems.Count);
                                Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            }
                        }
                    }

                    // second shelf 
                    if (freeR.YTiles >= 9)
                    {
                        if (Chance.Simple())
                        {
                            for (x = freeR.X0; x <= doors[Door.Down].doorRect.X0 - 2; x++)
                            {
                                WorldGen.PlaceTile(x, freeR.Y1 - 7, TileID.Platforms, style: 19); // Boreal wood platform
                                WorldGen.paintTile(x, freeR.Y1 - 7, (byte)Deco[S.StylePaint]);
                            }
                        }

                        if (Chance.Simple())
                        {
                            for (x = doors[Door.Down].doorRect.X1 + 2; x <= freeR.X1; x++)
                            {
                                WorldGen.PlaceTile(x, freeR.Y1 - 7, TileID.Platforms, style: 19); // Boreal wood platform
                                WorldGen.paintTile(x, freeR.Y1 - 7, (byte)Deco[S.StylePaint]);
                            }
                        }

                        // try stacking wooden crates
                        if (rememberPos.Count > 0) // wooden crates have been placed succesfully on the lower shelf
                        {
                            for (int posNum = 0; posNum <= rememberPos.Count - 1; posNum++) // for every placed crate
                            {
                                if (Chance.Simple()) WorldGen.Place2x2(rememberPos[posNum].x + 1, rememberPos[posNum].y - 2, TileID.FishingCrate, style: 0); // try stack another
                                // Explanation: PlaceTile didn't work at all and looks like Place2x2 doesn't use the same anchor point as PlaceTile...that's why the +1 is necessary
                            }
                        }

                        //put deco on second shelf
                        area1 = new Rectangle2P(freeR.X0, freeR.Y1 - 8, freeR.X1, freeR.Y1 - 8, "dummyString");
                        area2 = doors[Door.Down].doorRect.CloneAndMove(0, -9);
                        rememberPos.Clear();
                        if (freeR.YTiles >= 10)
                        {
                            Func.TryPlaceTile(area1, area2, TileID.Kegs, style: 0, chance: 50); // wooden keg

                            placeResult = Func.TryPlaceTile(area1, area2, TileID.FishingCrate, style: 0, chance: 50); // wooden fishing crate
                            if (placeResult.success) rememberPos.Add((placeResult.x, placeResult.y)); // remember placement position for later

                            Func.TryPlaceTile(area1, area2, TileID.Bottles, style: WorldGen.genRand.Next(9), chance: 50); // any 1x1 dish
                            Func.TryPlaceTile(area1, area2, TileID.FoodPlatter, style: 17, chance: 50); // food plate

                            placeResult = Func.TryPlaceTile(area1, area2, TileID.FishingCrate, style: 0, chance: 50); // wooden fishing crate
                            if (placeResult.success) rememberPos.Add((placeResult.x, placeResult.y)); // remember placement position for later

                            for (int i = 1; i <= 9; i++)
                            {
                                int num = WorldGen.genRand.Next(randomItems.Count);
                                Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            }
                        }
                        else
                        {
                            for (int i = 1; i <= 15; i++)
                            {
                                int num = WorldGen.genRand.Next(randomItems.Count);
                                Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            }
                        }

                        // try stacking wooden crates
                        if (rememberPos.Count > 0) // wooden crates have been placed succesfully on the lower shelf
                        {
                            for (int posNum = 0; posNum <= rememberPos.Count - 1; posNum++) // for every placed crate
                            {
                                if (Chance.Simple()) WorldGen.Place2x2(rememberPos[posNum].x + 1, rememberPos[posNum].y - 2, TileID.FishingCrate, style: 0); // try stack another
                                // Explanation: PlaceTile didn't work at all and looks like Place2x2 doesn't use the same anchor point as PlaceTile...that's why the +1 is necessary
                            }
                        }

                    }

                    // try placing some hanging pots at the ceiling
                    area1 = new Rectangle2P(freeR.X0, freeR.Y0, freeR.X1, freeR.Y0, "dummyString");
                    area2 = doors[Door.Up].doorRect.CloneAndMove(0, 1);
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: Deco[S.HangingPot], chance: 50); // hanging pot with herb
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: 0, chance: 50); // hanging empty pot
                    Func.TryPlaceTile(area1, area2, TileID.PotsSuspended, style: Deco[S.HangingPot], chance: 50); // hanging pot with herb

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


                    // deco in the middle of the room
                    // statue at floor
                    if (Chance.Simple())
                    {
                        x = freeR.XCenter;
                        if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of statue (2 xTiles) in an odd xTiles rooms
                        y = freeR.Y1;
                        if (Chance.Perc(60)) WorldGen.PlaceTile(x, y, TileID.Statues, style: 0); // armor statue
                        else if (Chance.Perc(60)) WorldGen.PlaceTile(x, y, TileID.GrandfatherClocks, style: Deco[S.Clock]); // angel statue
                        else if (Chance.Perc(60)) WorldGen.PlaceTile(x, y, TileID.Statues, style: 1); // angel statue
                    }

                    // item frames hanging on 2/3's room height
                    if ((freeR.YTiles >= 12))
                    {
                        if (Chance.Simple())
                        {
                            // frame 1
                            x = doors[Door.Up].doorRect.X0;
                            y = freeR.Y0 + freeR.YTiles / 3 - 1;
                            Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);

                            // frame 2
                            x = doors[Door.Up].doorRect.X1 - 1;
                            Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);


                            // frame 3
                            x = freeR.XCenter;
                            if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of item frame (2 xTiles) in an odd xTiles rooms
                            y = freeR.Y0 + freeR.YTiles / 3 + 1;
                            Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);
                        }
                    }
                    else if ((freeR.YTiles >= 8))
                    {
                        if (Chance.Simple())
                        {
                            // frame 1
                            x = freeR.XCenter;
                            if (!freeR.IsEvenX()) x -= WorldGen.genRand.Next(2); // to variate position of item frame (2 xTiles) in an odd xTiles rooms
                            y = freeR.Y0 + freeR.YTiles / 3;
                            Func.PlaceItemFrame(x, y, paint: Deco[S.StylePaint]);
                        }
                    }

                    //choose side for bookcase + candelabra and for working bench + books
                    area1 = new Rectangle2P(freeR.X0, freeR.Y1, doors[Door.Down].doorRect.X0 - 2, freeR.Y1, "dummyString"); // left side
                    area2 = new Rectangle2P(doors[Door.Down].doorRect.X1 + 2, freeR.Y1, freeR.X1, freeR.Y1, "dummyString"); // right side

                    if (Chance.Simple()) // randomize side
                    {
                        area3 = area1; //change area1 and 2 by using area3
                        area1 = area2;
                        area2 = area3;
                    }

                    // side 1: bookcase and candelabra
                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Bookcases, style: Deco[S.Bookcase]); // Bookcase
                    if (placeResult.success) placeResult = Func.TryPlaceTile(area1.CloneAndMove(0, -4), noBlock, TileID.Candelabras, style: Deco[S.Candelabra], chance: 75); // Try put candelabra on bookcase
                    if (placeResult.success) Func.UnlightCandelabra(placeResult.x, placeResult.y); // unlight candelabra

                    // side 2: chest, workbench and candle and chair
                    rememberPos.Clear(); //init
                    placeResult = Func.TryPlaceTile(area2, noBlock, TileID.WorkBenches, style: Deco[S.Workbench], chance: 70); // Workbench
                    if (placeResult.success)
                    {
                        rememberPos.Add((placeResult.x, placeResult.y));
                        placeResult = Func.TryPlaceTile(area2.CloneAndMove(0, -1), noBlock, TileID.Candles, style: Deco[S.Candle], chance: 75); // Try put candle on workbench
                        if (placeResult.success) Func.Unlight1x1(placeResult.x, placeResult.y); // unlight candle

                        if ( !Main.tile[rememberPos[0].x - 1, rememberPos[0].y].HasTile) // left of workbench is free
                        {
                            x = rememberPos[0].x - 1;
                            y = rememberPos[0].y;
                            if (Chance.Simple()) WorldGen.PlaceTile(x, y, TileID.Chairs, style: Deco[S.Chair]); // try place chair
                            if (Main.tile[x, y].HasTile) Func.ChairTurnRight(x, y); // if placed, change the facing direction of the chair
                        }
                        if (!Main.tile[rememberPos[0].x + 2, rememberPos[0].y].HasTile) // right of workbench is free
                        {
                            x = rememberPos[0].x + 2;
                            y = rememberPos[0].y;
                            if (Chance.Simple()) WorldGen.PlaceTile(x, y, TileID.Chairs, style: Deco[S.Chair]); // try place chair
                        }
                    }
                    else
                    {
                        placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Tables, style: Deco[S.Table], chance: 85); // Table
                        if (placeResult.success)
                        {
                            x = placeResult.x - WorldGen.genRand.Next(2);
                            y = placeResult.y - 2;
                            WorldGen.PlaceTile(x, y, TileID.MusicBoxes, style: 0); // music box
                            WorldGen.paintTile(x, y - 1,     (byte)Deco[S.StylePaint]);
                            WorldGen.paintTile(x, y,         (byte)Deco[S.StylePaint]);
                            WorldGen.paintTile(x + 1, y - 1, (byte)Deco[S.StylePaint]);
                            WorldGen.paintTile(x + 1, y,     (byte)Deco[S.StylePaint]);

                        }
                        placeResult = Func.TryPlaceTile(area2, noBlock, TileID.Containers, style: Deco[S.Chest], chance: 70); // Chest
                        if (placeResult.success)
                        {
                            chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                            if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2));
                        }
                    }

                    // still side 2: shelf with books above workbench and chair
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

                    List<(int, int, int)> armorPool = new List<(int, int, int)>() // the possible styles of armor
                    {
                        (ArmorIDs.Head.CopperHelmet, ArmorIDs.Body.CopperChainmail, ArmorIDs.Legs.CopperGreaves),
                        (ArmorIDs.Head.TinHelmet, ArmorIDs.Body.TinChainmail, ArmorIDs.Legs.TinGreaves),
                        (ArmorIDs.Head.IronHelmet, ArmorIDs.Body.IronChainmail, ArmorIDs.Legs.IronGreaves),
                        (ArmorIDs.Head.LeadHelmet, ArmorIDs.Body.LeadChainmail, ArmorIDs.Legs.LeadGreaves),
                        (ArmorIDs.Head.NinjaHood, ArmorIDs.Body.NinjaShirt, ArmorIDs.Legs.NinjaPants),
                        (ArmorIDs.Head.FossilHelmet, ArmorIDs.Body.FossilPlate, ArmorIDs.Legs.FossilGreaves),

                        (ArmorIDs.Head.WoodHelmet, ArmorIDs.Body.WoodBreastplate, ArmorIDs.Legs.WoodGreaves),
                        (ArmorIDs.Head.BorealWoodHelmet, ArmorIDs.Body.BorealWoodBreastplate, ArmorIDs.Legs.BorealWoodGreaves),
                        (ArmorIDs.Head.ShadewoodHelmet, ArmorIDs.Body.ShadewoodBreastplate, ArmorIDs.Legs.ShadewoodGreaves),
                        (ArmorIDs.Head.AshWoodHelmet, ArmorIDs.Body.AshWoodBreastplate, ArmorIDs.Legs.AshWoodGreaves),
                    };

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
                                        int num = WorldGen.genRand.Next(randomItems.Count);
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
                                        int num = WorldGen.genRand.Next(randomItems.Count);
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
                        randomItems.Add((TileID.Containers, Deco[S.Chest], 65)); // Chest
                        randomItems.Add((TileID.FishingCrate, 0, 65)); // wooden crate

                        area1 = new Rectangle2P(freeR.X0, y, freeR.X1 - 1, y, "dummyString");
                        if (doors[Door.Up].doorExist)   area2 = new Rectangle2P(doors[Door.Up].doorRect.X0 - 3, y, doors[Door.Up].doorRect.X1 + 2, y, "dummyString");
                        else                            area2 = new Rectangle2P(doors[Door.Up].doorRect.X0    , y, doors[Door.Up].doorRect.X1 - 1, y, "dummyString");

                        for (int i = 1; i <= 6; i++)
                        {
                            int num = WorldGen.genRand.Next(randomItems.Count);
                            placeResult = Func.TryPlaceTile(area1, area2, randomItems[num].TileID, style: randomItems[num].style, chance: randomItems[num].chance); // one random item of the list
                            if (placeResult.success && randomItems[num].TileID == TileID.FishingCrate)
                            {
                                area1 = new Rectangle2P(placeResult.x, placeResult.y - 1, 2, 2);
                                Func.PaintArea(area1, (byte)Deco[S.StylePaint]);
                            }
                        }

                    }

                    // finalization
                    Func.PlaceStinkbug(freeR);
                    
                    break;

                case 5: // armory

                    // floor
                    area1 = new Rectangle2P(freeR.X0, freeR.Y1, freeR.X1, freeR.Y1, "dummyString");

                    placeResult = Func.TryPlaceTile(area1, noBlock, TileID.Containers, style: Deco[S.Chest], chance: 70); // Chest
                    if (placeResult.success)
                    {
                        chestID = Chest.FindChest(placeResult.x, placeResult.y - 1);
                        if (chestID != -1) FillChest(Main.chest[chestID], WorldGen.genRand.Next(2));
                    }

                    placeResult = Func.TryPlaceTile(area1, Rectangle2P.Empty, TileID.WorkBenches, style: Deco[S.Workbench], chance: 50); // workbench
                    if (placeResult.success)
                    {
                        if (Chance.Perc(65))
                        {
                            WorldGen.PlaceTile(placeResult.x + 1, placeResult.y - 1, TileID.Candelabras, style: Deco[S.Candelabra]);
                            Func.UnlightCandelabra(placeResult.x + 1, placeResult.y - 1);
                        }
                    }

                    List<(ushort TileID, int style)> floorItems = new List<(ushort, int)>()
                    {
                        (TileID.FishingCrate, 0),  // wooden crate
                        (TileID.FishingCrate, 1),  // iron crate
                        (TileID.Containers, 5),  // wooden barrel
                        (TileID.Statues, 3),  // sword statue
                        (TileID.Statues, 6),  // shield statue
                        (TileID.Statues, 21),  // spear statue
                    };

                    for (int i = 1; i <= 6; i++)
                    {
                        int num = WorldGen.genRand.Next(floorItems.Count);
                        Func.TryPlaceTile(area1, Rectangle2P.Empty, floorItems[num].TileID, style: floorItems[num].style, chance: 50); // one random item of the list
                    }
                    

                    if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X0, freeR.Y1 - 4, TileID.Painting3X3, style: 45); // sword rack
                    if (Chance.Perc(75)) WorldGen.PlaceTile(doors[Door.Down].doorRect.X1, freeR.Y1 - 4, TileID.Painting3X3, style: 45); // sword rack

                    if (freeR.YTiles >= 9)
                    {
                        if (Chance.Perc(75)) WorldGen.PlaceTile(freeR.X0 + 2, freeR.Y1 - 7, TileID.Painting3X3, style: 42); // carpentry rack
                        if (Chance.Perc(75)) WorldGen.PlaceTile(freeR.X1 - 3, freeR.Y1 - 7, TileID.Painting3X3, style: 42); // carpentry rack
                    }

                    if (freeR.YTiles >= 12)
                    {
                        if (Chance.Perc(75)) WorldGen.PlaceTile(freeR.X0 + 2, freeR.Y1 - 10, TileID.Painting3X3, style: 43); // helmet rack
                        if (Chance.Perc(75)) WorldGen.PlaceTile(freeR.X1 - 3, freeR.Y1 - 10, TileID.Painting3X3, style: 43); // helmet rack
                    }

                    break;

                case 6: //empty room because I don't have enough room templates and the other rooms repeat too much!

                    break;

                case 100: // empty room for display
                    break;

            }

            PlaceCobWeb(freeR, 1, WorldGenMod.configFrostFortressCobwebFilling);
        }

        public void FillChest(Chest chest, int style)
        {
            List<int> mainItem = new List<int>();
            if (style == 1)
            {
                mainItem.Add(ItemID.Frostbrand);
                mainItem.Add(ItemID.IceBow);
                mainItem.Add(ItemID.IceBoomerang);
                mainItem.Add(ItemID.FlowerofFrost);
                mainItem.Add(ItemID.IceRod);
                mainItem.Add(ItemID.IceSkates);
            }
            else
            {
                mainItem.Add(ItemID.IceBlade);
                mainItem.Add(ItemID.SnowballCannon);
                mainItem.Add(ItemID.IceBoomerang);
                mainItem.Add(ItemID.SapphireStaff);
                mainItem.Add(ItemID.FlinxStaff);
                mainItem.Add(ItemID.FlurryBoots);
            }
            mainItem.Add(ItemID.BlizzardinaBottle);


            List<int> potionItem = new List<int>();
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


            List<int> lightItem = new List<int>()
            {
                ItemID.IceTorch,
                ItemID.Glowstick,
                ItemID.FairyGlowstick,
                ItemID.SpelunkerGlowstick
            };


            List<int> ammoItem = new List<int>()
            {
                ItemID.FrostburnArrow,
                ItemID.FrostDaggerfish,
                ItemID.Snowball,
                ItemID.SpelunkerGlowstick
            };

            int nextItem = 0;

            chest.item[nextItem].SetDefaults(mainItem[WorldGen.genRand.Next(mainItem.Count)]);
            chest.item[nextItem].stack = 1;
            nextItem++;

            chest.item[nextItem].SetDefaults(potionItem[WorldGen.genRand.Next(potionItem.Count)]);
            chest.item[nextItem].stack = WorldGen.genRand.Next(1, 3);
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
        /// Places patches of CobWeb in an rectangular space and adds some randomness on the edges.
        /// <br/>CobWebs are only placed on "free" tiles, where there are no other tiles present.
        /// </summary>
        /// <param name="area">The rectangle where CobWeb shall be placed</param>
        /// <param name="randomize">Whether a CobWeb shall be placed by chance (0=no; 1=with the chance stated in "percChance"; 2=the further away from the rectangle center point, the less likely)</param>
        /// <param name="percChance">The percentual chance to place a CobWeb tile for randomize = 1</param>
        public void PlaceCobWeb(Rectangle2P area, int randomize = 0, int percChance = 50)
        {
            for (int x = area.X0; x <= area.X1; x++)
            {
                for (int y = area.Y0; y <= area.Y1; y++)
                {
                    if (!Main.tile[x, y].HasTile) //first the fast query
                    {
                        //then the more compute-heavy check
                        switch (randomize)
                        {
                            case 0:
                                WorldGen.PlaceTile(x, y, TileID.Cobweb);
                                break;

                            case 1:
                                if (WorldGen.genRand.Next(1, 101) <= percChance)   WorldGen.PlaceTile(x, y, TileID.Cobweb);
                                break;

                            case 2:
                                
                                break;
                        }
                        
                        //TODO: overthink case 2...putting it as described would more or less create an ellipse...and I already have one :-/
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
        /// <param name="includeBorder">Whether the border of the ellipse shall get CobWeb placed or not</param>
        /// <param name="randomize">Whether CobWeb shall be placed by chance (the further away from the ellipse center point, the less likely)</param>
        public void PlaceCobWeb(int x0, int y0, int xRadius, int yRadius, bool includeBorder = false, bool randomize = true)
        {
            Ellipse CobWebs = new Ellipse(xCenter: x0, yCenter: y0, xRadius: xRadius, yRadius: yRadius);
            Rectangle2P overall = new Rectangle2P(x0 - xRadius, y0 - yRadius, x0 + xRadius, y0 + yRadius, "dummy"); // the rectangle exactly covering the ellipse
            
            for (int x = overall.X0; x <= overall.X1; x++) 
            {
                for(int y = overall.Y0; y <= overall.Y1; y++) 
                {
                    if ( !Main.tile[x, y].HasTile ) //first the fast query
                    {
                        bool contains;
                        float distance;
                        (distance, contains) = CobWebs.Distance_Contains(x, y, includeBorder); //then the more compute-heavy check

                        if (contains && (WorldGen.genRand.NextFloat() > distance || !randomize)) //make the outer cobwebs less likely to appear
                        {
                            WorldGen.PlaceTile(x, y, TileID.Cobweb);
                        }
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
        /// <param name="room">The rectangular room where the CobWeb is allowed to be placed</param>
        /// <param name="includeBorder">Whether the border of the ellipse shall get CobWeb placed or not</param>
        /// <param name="randomize">Whether CobWeb shall be placed by chance (the further away from the ellipse center point, the less likely)</param>
        public void PlaceCobWeb(int x0, int y0, int xRadius, int yRadius, Rectangle2P room, bool includeBorder = false, bool randomize = true)
        {
            Ellipse CobWebs = new Ellipse(xCenter: x0, yCenter: y0, xRadius: xRadius, yRadius: yRadius);

            for (int x = room.X0; x <= room.X1; x++)
            {
                for (int y = room.Y0; y <= room.Y1; y++)
                {
                    if (!Main.tile[x, y].HasTile) //first the fast query
                    {
                        bool contains;
                        float distance;
                        (distance, contains) = CobWebs.Distance_Contains(x, y, includeBorder); //then the more compute-heavy check

                        if (contains && (WorldGen.genRand.NextFloat() > distance || !randomize)) //make the outer cobwebs less likely to appear
                        {
                            WorldGen.PlaceTile(x, y, TileID.Cobweb);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tries to place a painting in the given area. It tries to place paintings from tall to flat (6x4 -> 3x3 -> 2x3 -> 3x2)
        /// <br/>
        /// <br/> ATTENTION: does not check if the final placement position is empty. Best make sure that the whole area is free.
        /// </summary>
        /// <param name="area">The area where the painting can be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        /// <param name="placeMode">The placement method: 0 = centered in x and y, 1 = random x and centered y</param>
        /// <param name="limitType">Allow types of paintings: (binary selection) 0= no painting, 1=3x2, 2=2x3, 4=3x3, 8=6x4, 15=all types</param>
        public void PlacePainting(Rectangle2P area, int style, int placeMode = 0, byte limitType = 15)
        {
            bool allow3x2 = (limitType & 1) != 0;
            bool allow2x3 = (limitType & 2) != 0;
            bool allow3x3 = (limitType & 4) != 0;
            bool allow6x4 = (limitType & 8) != 0;

            //painting
            int x, y;
            if (area.YTiles >= 4 && allow6x4 && Chance.Simple())
            {
                if (placeMode == 0)
                {
                    x = area.XCenter - 2;
                    y = area.Y0 + ((area.YTiles - 4) / 2);
                    Place6x4PaintingByStyle(new Rectangle2P(x, y, 6, 4), style);
                } 
                else if (placeMode == 1)
                {
                    x = area.X0 + WorldGen.genRand.Next(area.XTiles - (6 - 1));
                    y = area.Y0 + ((area.YTiles - 4) / 2);
                    Place6x4PaintingByStyle(new Rectangle2P(x, y, 6, 4), style);
                }
            }

            else if (area.YTiles >= 3 && allow3x3 && Chance.Simple())
            {
                if (placeMode == 0)
                {
                    x = area.XCenter - 1 + WorldGen.genRand.Next(2); //3 XTiles cannot be centered in an even room, so alternate betweend the two "out-center" positions..that's why the Next(2)
                    y = area.Y0 + ((area.YTiles - 3) / 2);
                    Place3x3PaintingByStyle(new Rectangle2P(x, y, 3, 3), style);
                }
                else if (placeMode == 1)
                {
                    x = area.X0 + WorldGen.genRand.Next(area.XTiles - (3 - 1));
                    y = area.Y0 + ((area.YTiles - 3) / 2);
                    Place3x3PaintingByStyle(new Rectangle2P(x, y, 3, 3), style);
                }
            }

            else if (area.YTiles >= 3 && allow2x3 && Chance.Simple())
            {
                if (placeMode == 0)
                {
                    x = area.XCenter;
                    y = area.Y0 + ((area.YTiles - 3) / 2);
                    Place2x3PaintingByStyle(new Rectangle2P(x, y, 2,  3),  style);
                }
                else if (placeMode == 1)
                {
                    x = area.X0 + WorldGen.genRand.Next(area.XTiles - (2 - 1));
                    y = area.Y0 + ((area.YTiles - 3) / 2);
                    Place2x3PaintingByStyle(new Rectangle2P(x, y, 2, 3), style);
                }
            }

            else if (area.YTiles >= 2 && allow3x2 && Chance.Simple())
            {
                if (placeMode == 0)
                {
                    x = area.XCenter - 1 + WorldGen.genRand.Next(2);
                    y = area.Y0 + ((area.YTiles - 2) / 2);
                    Place3x2PaintingByStyle(new Rectangle2P(x, y, 3, 2), style);
                }
                else if (placeMode == 1)
                {
                    x = area.X0 + WorldGen.genRand.Next(area.XTiles - (3 - 1));
                    y = area.Y0 + ((area.YTiles - 2) / 2);
                    Place3x2PaintingByStyle(new Rectangle2P(x, y, 3, 2), style);
                }
            }
        }

        /// <summary>
        /// Places a random 4x6 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 4x6 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        public void Place6x4PaintingByStyle(Rectangle2P area, int style)
        {
            for (int x = area.X0; x <= area.X1; x++) 
            {
                for (int y = area.Y0; x <= area.Y1; y++)
                {
                    WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // place background just in case it got deleted by the "cracked" background design
                }
            }

            List<int> paintings = new List<int>();
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
                paintings.Add(49); // Remnants of Devotion
            }
            else
            {
                paintings.Add(23); // Leopard Skin...should never occur or I called the method wrong...so just to be sure
            }

            WorldGen.PlaceTile(area.X0 + 2, area.Y0 + 2, TileID.Painting6X4, style: paintings[WorldGen.genRand.Next(paintings.Count)] );
        }

        /// <summary>
        /// Places a random 3x3 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 3x3 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        public void Place3x3PaintingByStyle(Rectangle2P area, int style)
        {
            for (int x = area.X0; x <= area.X1; x++)
            {
                for (int y = area.Y0; x <= area.Y1; y++)
                {
                    WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // place background just in case it got deleted by the "cracked" background design
                }
            }

            List<int> paintings = new List<int>();
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

            WorldGen.PlaceTile(area.X0 + 1, area.Y0 + 1, TileID.Painting3X3, style: paintings[WorldGen.genRand.Next(paintings.Count)]);
        }

        /// <summary>
        /// Places a random 2x3 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 2x3 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        public void Place2x3PaintingByStyle(Rectangle2P area, int style)
        {
            for (int x = area.X0; x <= area.X1; x++)
            {
                for (int y = area.Y0; x <= area.Y1; y++)
                {
                    WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // place background just in case it got deleted by the "cracked" background design
                }
            }

            List<int> paintings = new List<int>();
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

            WorldGen.PlaceTile(area.X0, area.Y0 + 1, TileID.Painting2X3, style: paintings[WorldGen.genRand.Next(paintings.Count)]);
        }

        /// <summary>
        /// Places a random 3x2 painting of a pre-selected variety for the given decoration style 
        /// </summary>
        /// <param name="area">The 3x2 area where the painting shall be placed</param>
        /// <param name="style">The decoration style of the frost fortress</param>
        public void Place3x2PaintingByStyle(Rectangle2P area, int style)
        {
            for (int x = area.X0; x <= area.X1; x++)
            {
                for (int y = area.Y0; x <= area.Y1; y++)
                {
                    WorldGen.PlaceWall(x, y, Deco[S.BackWall]); // place background just in case it got deleted by the "cracked" background design
                }
            }

            List<int> paintings = new List<int>();
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

            WorldGen.PlaceTile(area.X0 + 1, area.Y0, TileID.Painting3X2, style: paintings[WorldGen.genRand.Next(paintings.Count)]);
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
        public const String Dresser = "Dresser";

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
