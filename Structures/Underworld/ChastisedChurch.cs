﻿using Terraria;
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

namespace WorldGenMod.Structures.Underworld
{
    class ChastisedChurch : ModSystem
    {
        readonly int gap = -1; // the horizontal gap between two side room columns
        readonly int wThick = 2; // the tickness of the outer walls and ceilings in code
        readonly int forceEvenRoom = 1; // 1 = force all rooms to have an even XTiles count; 0 = force all side rooms to have an odd XTiles count
        readonly int maxChurchLength = 500; // maximum length of the ChastisedChurch

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

        public void FillAndChooseStyle()
        {
            Deco.Clear(); // init

            // create dictionary entries
            Deco.Add(S.StyleSave, 0);
            Deco.Add(S.Brick, 0);
            Deco.Add(S.TowerBrick, 0);
            Deco.Add(S.Floor, 0);
            Deco.Add(S.EvilTile, 0);
            Deco.Add(S.BackWall, 0);
            Deco.Add(S.CrookedWall, 0);
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
                case S.StyleObsidian: // Obsidian
                    Deco[S.StyleSave] = S.StyleObsidian;
                    Deco[S.Brick] = TileID.HellstoneBrick;
                    Deco[S.TowerBrick] = TileID.HellstoneBrick;
                    Deco[S.Floor] = TileID.ObsidianBrick;
                    if (Chance.Simple()) Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.EvilTile] = TileID.Crimstone;
                    Deco[S.BackWall] = WallID.HellstoneBrickUnsafe;
                    Deco[S.CrookedWall] = WallID.Flesh;
                    Deco[S.DoorWall] = WallID.ObsidianBrickUnsafe;

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
                    Deco[S.Sofa] = 27; // Tile ID 89 (Sofas) -> Type 27=Frozen
                    Deco[S.Clock] = 11;    // Tile ID 104 (GrandfatherClocks) -> Type 11=Frozen
                    Deco[S.Bed] = 15;      // Tile ID 79 (Beds) -> Type 15=Frozen
                    Deco[S.BedWallpaper] = WallID.StarsWallpaper;
                    Deco[S.PaintingWallpaper] = WallID.SparkleStoneWallpaper;
                    Deco[S.Dresser] = 30;  // Tile ID 88 (Dressers) -> Type 30=Frozen
                    Deco[S.Piano] = 7;     // Tile ID 87 (Pianos) -> Type 7=Frozen
                    break;

                case S.StyleHellstone: // Hellstone
                    Deco[S.StyleSave] = S.StyleHellstone;
                    Deco[S.Brick] = TileID.ObsidianBrick;
                    Deco[S.TowerBrick] = TileID.ObsidianBrick;
                    Deco[S.Floor] = TileID.HellstoneBrick;
                    if (Chance.Simple()) Deco[S.Floor] = TileID.AncientSilverBrick;
                    Deco[S.EvilTile] = TileID.Ebonstone;
                    Deco[S.BackWall] = WallID.ObsidianBrickUnsafe;
                    Deco[S.CrookedWall] = WallID.CorruptionUnsafe2;
                    Deco[S.DoorWall] = WallID.HellstoneBrickUnsafe;
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

                case S.StyleSomething: //TODO: look for another type of brick. It was recommended to use EbonstoneBrick on Steam, maybe also just red brick?
                    Deco[S.StyleSave] = S.StyleSomething;
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
            while (totalTiles < maxTiles)
            {
                int roomWidth = WorldGen.genRand.Next(12, 80);
                if      (forceEvenRoom == 1) roomWidth -= (roomWidth % 2); //make room always even
                else if (forceEvenRoom == 0) roomWidth -= (roomWidth % 2) + 1; //make room always uneven

                int roomHeight = WorldGen.genRand.Next(12, 30);

                float ratio = roomHeight / roomWidth;
                int towerHeight;
                if (ratio > 1.2f) towerHeight = WorldGen.genRand.Next(10, 20);
                else              towerHeight = WorldGen.genRand.Next(5, 10);

                if (generationSide == -1) // left world side
                {
                    bool leftDoor = totalTiles != 0;
                    bool rightDoor = (totalTiles + roomWidth) < maxTiles;

                    GenerateRoom(new Rectangle(startPosX + totalTiles, startPosY - roomHeight, roomWidth, roomHeight), towerHeight, leftDoor, rightDoor);
                    totalTiles += roomWidth;
                }
                else if (generationSide == 1) // right world side
                {
                    bool rightDoor = totalTiles != 0;
                    bool leftDoor = (totalTiles + roomWidth) < maxTiles;

                    GenerateRoom(new Rectangle(startPosX - totalTiles - roomWidth, startPosY - roomHeight, roomWidth, roomHeight), towerHeight, leftDoor, rightDoor);
                    totalTiles += roomWidth;
                }
            }
        }


        public void GenerateRoom(Rectangle room, int towerHeight = 10, bool leftDoor = false, bool rightDoor = false, int extraCount = 0)
        {
            Rectangle hollowRect = room;
            hollowRect.Width -= 4;
            hollowRect.Height -= 4;
            hollowRect.X += 2;
            hollowRect.Y += 2;

            if (room.Y + room.Height >= Main.maxTilesY || room.X + room.Height >= Main.maxTilesX || room.X <= 0)
            {
                return;
            }

            bool noBreakPoint = WorldGen.genRand.NextBool();
            Vector2 wallBreakPoint = new(room.X + WorldGen.genRand.Next(room.Width), room.Y + WorldGen.genRand.Next(room.Height));

            List<Rectangle> doors = new();
            if (leftDoor) doors.Add(new Rectangle(room.X, room.Y + room.Height - 5, 2, 3));
            if (rightDoor) doors.Add(new Rectangle(room.X + room.Width - 2, room.Y + room.Height - 5, 2, 3));

            List<Rectangle> windows = new();
            if (room.Height > 12 && room.Width > 12)
            {
                if (room.Width <= 16)
                {
                    windows.Add(new Rectangle(room.Center.X - 2, room.Y + 4, 4, room.Height - 8));
                }
                else
                {
                    for (int i = 1; i < room.Width / 8 / 2; i++)
                    {
                        windows.Add(new Rectangle(room.X + i * 8, room.Y + 4, 4, room.Height - 8));
                        windows.Add(new Rectangle(room.X + room.Width - i * 8 - 4, room.Y + 4, 4, room.Height - 8));
                    }
                }
            }

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                for (int j = room.Y; j < room.Y + room.Height; j++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.EmptyLiquid(i, j);
                    if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(4f, 12f) || noBreakPoint) WorldGen.PlaceWall(i, j, Deco[S.BackWall]);
                    else if (!noBreakPoint) WorldGen.PlaceWall(i, j, Deco[S.CrookedWall]);

                    if (j == room.Y + room.Height - 2)
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Floor], true, true);
                    }
                    else
                    {
                        WorldGen.PlaceTile(i, j, Deco[S.Brick], true, true);
                    }
                    WorldGen.SlopeTile(i, j);
                }
            }

            for (int i = hollowRect.X; i < hollowRect.X + hollowRect.Width; i++)
            {
                for (int j = hollowRect.Y; j < hollowRect.Y + hollowRect.Height; j++)
                {
                    WorldGen.KillTile(i, j);
                }
            }

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
                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(4f, 12f) || noBreakPoint) WorldGen.PlaceWall(i, j, Deco[S.DoorWall]);
                            else if (!noBreakPoint) WorldGen.PlaceWall(i, j, Deco[S.CrookedWall]);
                        }
                    }
                }
            }

            if (windows.Count != 0 && extraCount == 0)
            {
                foreach (Rectangle windowRect in windows)
                {
                    for (int i = windowRect.X; i < windowRect.X + windowRect.Width; i++)
                    {
                        for (int j = windowRect.Y; j < windowRect.Y + windowRect.Height; j++)
                        {
                            WorldGen.KillWall(i, j);
                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(4f, 12f) || noBreakPoint)
                            {
                                WorldGen.PlaceWall(i, j, WallID.RedStainedGlass);
                                WorldGen.paintWall(i, j, PaintID.DeepRedPaint);
                            }
                        }
                    }
                }
            }

            for (int i = room.Center.X - room.Width / 2; i < room.Center.X + room.Width / 2; i++)
            {
                float currentMultiplier = 1f - Math.Abs(i - room.Center.X) / (room.Width / 2f);
                for (int j1 = 0; j1 < (int)(towerHeight * currentMultiplier); j1++)
                {
                    int j = room.Y - 1 - j1;
                    WorldGen.PlaceTile(i, j, Deco[S.TowerBrick], true, true);
                }
            }

            if (WorldGen.genRand.NextBool() && room.Height >= 12)
            {
                int j = room.Y + WorldGen.genRand.Next(4, room.Height - 6);
                for (int i = 0; i < WorldGen.genRand.Next(3, 7); i++)
                {
                    WorldGen.PlaceTile(i + room.X + 2, j, TileID.Platforms, true, false, style: 13);
                    WorldGen.PlaceTile(i + room.X + 2, j - 1, TileID.Books, true, false, style: WorldGen.genRand.Next(6));
                }
            }

            if (WorldGen.genRand.NextBool() && room.Height >= 12)
            {
                int j = room.Y + WorldGen.genRand.Next(4, room.Height - 6);
                for (int i = 0; i < WorldGen.genRand.Next(3, 7); i++)
                {
                    WorldGen.PlaceTile(-i + room.X + room.Width - 2, j, TileID.Platforms, true, false, style: 13);
                    WorldGen.PlaceTile(-i + room.X + room.Width - 2, j - 1, TileID.Books, true, false, style: WorldGen.genRand.Next(6));
                }
            }

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                int j = room.Y + 2;
                WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 13);
            }

            if (WorldGen.genRand.NextBool(2 + extraCount) && extraCount < 4 && room.Y + room.Height * 2 < Main.maxTilesY - 2)
            {
                int width = (int)(room.Width * WorldGen.genRand.NextFloat(0.5f, 1f));
                Rectangle nextRoom = new(room.X + WorldGen.genRand.Next(room.Width - width), room.Y + room.Height, width, room.Height);

                GenerateRoom(nextRoom, 0, false, false, extraCount + 1);

                for (int i = nextRoom.Center.X - 2; i <= nextRoom.Center.X + 2; i++)
                {
                    WorldGen.KillTile(i, room.Y + room.Height - 2);
                    WorldGen.KillTile(i, room.Y + room.Height - 1);
                    WorldGen.KillTile(i, room.Y + room.Height);
                    WorldGen.KillTile(i, room.Y + room.Height + 1);
                }
            }

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                int j = room.Y + room.Height - 2;
                WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 13);
            }

            if (extraCount > 0 && WorldGen.genRand.NextBool(3) || WorldGen.genRand.NextBool(6))
            {
                WorldGen.TileRunner(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 2, WorldGen.genRand.NextFloat(6f, 10f), 3, TileID.Hellstone, true);
            }
            else if (WorldGen.genRand.NextBool(4))
            {
                WorldGen.TileRunner(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 2, WorldGen.genRand.NextFloat(3f, 7f), 3, Deco[S.EvilTile], true);
            }



            //TODO: chest style
            //int chest = -1;
            //if (!WorldGen.crimson)
            //{
            //    chestStyle = 43;
            //}
            //else
            //{
            //    chestStyle = 46;
            //}
            //if (WorldGen.genRand.NextBool(3))
            //{
            //    chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: chestStyle);
            //    if (chest != -1) FillChest(Main.chest[chest], chestStyle);
            //}

            //for (int i = room.X; i < room.X + room.Width; i++)
            //{
            //    int j = room.Y;
            //    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 35);
            //}


        }


        //TODO: put and fill chest

        //public void FillChest(Chest chest, int style)
        //{
        //    int nextItem = 0;

        //    int mainItem = 0;
        //    int potionItem = 0;
        //    int lightItem;
        //    int materialItem = 0;

        //    switch (WorldGen.genRand.Next(5))
        //    {
        //        case 0:
        //            mainItem = ItemID.Vilethorn;
        //            if (!WorldGen.crimson) mainItem = ItemID.CrimsonRod;
        //            break;
        //        case 1:
        //            mainItem = ItemID.Musket;
        //            if (!WorldGen.crimson) mainItem = ItemID.TheUndertaker;
        //            break;
        //        case 2:
        //            mainItem = ItemID.BandofStarpower;
        //            if (!WorldGen.crimson) mainItem = ItemID.PanicNecklace;
        //            break;
        //        case 3:
        //            mainItem = ItemID.BallOHurt;
        //            if (!WorldGen.crimson) mainItem = ItemID.TheMeatball;
        //            break;
        //        case 4:
        //            mainItem = ItemID.ShadowOrb;
        //            if (!WorldGen.crimson) mainItem = ItemID.CrimsonHeart;
        //            break;
        //    }

        //    switch (WorldGen.genRand.Next(4))
        //    {
        //        case 0:
        //            potionItem = ItemID.RagePotion;
        //            break;
        //        case 1:
        //            potionItem = ItemID.WrathPotion;
        //            break;
        //        case 2:
        //            potionItem = ItemID.LifeforcePotion;
        //            break;
        //        case 3:
        //            potionItem = ItemID.SummoningPotion;
        //            break;
        //    }

        //    lightItem = !WorldGen.crimson ? ItemID.CrimsonTorch : ItemID.CorruptTorch;


        //    switch (WorldGen.genRand.Next(4))
        //    {
        //        case 0:
        //            materialItem = ItemID.ShadowScale;
        //            if (!WorldGen.crimson) materialItem = ItemID.TissueSample;
        //            break;
        //        case 1:
        //            materialItem = ItemID.DemoniteBar;
        //            if (!WorldGen.crimson) materialItem = ItemID.CrimtaneBar;
        //            break;
        //        case 2:
        //            materialItem = ItemID.CorruptSeeds;
        //            if (!WorldGen.crimson) materialItem = ItemID.CrimsonSeeds;
        //            break;
        //        case 3:
        //            materialItem = ItemID.RottenChunk;
        //            if (!WorldGen.crimson) materialItem = ItemID.Vertebrae;
        //            break;
        //    }

        //    chest.item[nextItem].SetDefaults(mainItem);
        //    chest.item[nextItem].stack = 1;
        //    nextItem++;

        //    chest.item[nextItem].SetDefaults(potionItem);
        //    chest.item[nextItem].stack = WorldGen.genRand.Next(1, 3);
        //    nextItem++;

        //    chest.item[nextItem].SetDefaults(lightItem);
        //    chest.item[nextItem].stack = WorldGen.genRand.Next(6, 13);
        //    nextItem++;

        //    chest.item[nextItem].SetDefaults(materialItem);
        //    chest.item[nextItem].stack = WorldGen.genRand.Next(5, 10);
        //    nextItem++;

        //    chest.item[nextItem].SetDefaults(ItemID.GoldCoin);
        //    chest.item[nextItem].stack = WorldGen.genRand.Next(5, 13);
        //}
    }

    internal class S //Style
    {
        public const String StyleSave = "Style";
        public const String Brick = "Brick";
        public const String TowerBrick = "TowerBrick";
        public const String Floor = "Floor";
        public const String EvilTile = "EvilTile";
        public const String BackWall = "BackWall";
        public const String CrookedWall = "CrookedWall";
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

        public const int StyleObsidian = 0;
        public const int StyleHellstone = 1;
        public const int StyleSomething = 2; //TODO: find a third style
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
