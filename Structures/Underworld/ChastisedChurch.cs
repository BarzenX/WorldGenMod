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

namespace WorldGenMod.Structures.Underworld
{
    class ChastisedChurch : ModSystem
    {
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

        public int floorType = TileID.ObsidianBrick;
        public int wallType = WallID.HellstoneBrickUnsafe;
        public int brickType = TileID.HellstoneBrick;
        public int doorWallType = WallID.ObsidianBrickUnsafe;
        public int towerBrickType = TileID.HellstoneBrick;
        public int crookedWallType = WallID.Flesh;
        public int evilTile = TileID.Crimstone;
        public int chestStyle = 43;

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
                    if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(4f, 12f) || noBreakPoint) WorldGen.PlaceWall(i, j, wallType);
                    else if (!noBreakPoint) WorldGen.PlaceWall(i, j, crookedWallType);

                    if (j == room.Y + room.Height - 2)
                    {
                        WorldGen.PlaceTile(i, j, floorType, true, true);
                    }
                    else
                    {
                        WorldGen.PlaceTile(i, j, brickType, true, true);
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
                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(4f, 12f) || noBreakPoint) WorldGen.PlaceWall(i, j, doorWallType);
                            else if (!noBreakPoint) WorldGen.PlaceWall(i, j, crookedWallType);
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
                    WorldGen.PlaceTile(i, j, towerBrickType, true, true);
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
                WorldGen.TileRunner(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 2, WorldGen.genRand.NextFloat(3f, 7f), 3, evilTile, true);
            }

            int chest = -1;
            if (WorldGen.genRand.NextBool(3))
            {
                chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: chestStyle);
                if (chest != -1) FillChest(Main.chest[chest], chestStyle);
            }

            //for (int i = room.X; i < room.X + room.Width; i++)
            //{
            //    int j = room.Y;
            //    WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 35);
            //}

            
        }

        public void GenerateChastisedChurch(int generationSide)
        {
            if (!WorldGenMod.generateChastisedChurch)
            {
                return;
            }



            Point16 position = new(Main.maxTilesX / 2 + (Main.maxTilesX / 2 - 45) * generationSide, Main.maxTilesY - 100);

            if (!WorldGen.crimson)
            {
                floorType = TileID.ObsidianBrick;
                wallType = WallID.HellstoneBrickUnsafe;
                brickType = TileID.HellstoneBrick;
                doorWallType = WallID.ObsidianBrickUnsafe;
                towerBrickType = TileID.HellstoneBrick;
                crookedWallType = WallID.Flesh;
                evilTile = TileID.Crimstone;
                chestStyle = 43;
            }
            else
            {
                floorType = TileID.HellstoneBrick;
                wallType = WallID.ObsidianBrickUnsafe;
                brickType = TileID.ObsidianBrick;
                doorWallType = WallID.HellstoneBrickUnsafe;
                towerBrickType = TileID.ObsidianBrick;
                crookedWallType = WallID.CorruptionUnsafe2;
                evilTile = TileID.Ebonstone;
                chestStyle = 46;
            }

            int totalWidth = 0;
            int maxWidth = Main.maxTilesX / 8;
            while (totalWidth < maxWidth)
            {
                int width = WorldGen.genRand.Next(12, 80);
                int height = WorldGen.genRand.Next(12, 30);
                float ratio = height / width;
                int towerHeight = WorldGen.genRand.Next(5, 10);
                if (ratio > 1.2f) towerHeight = WorldGen.genRand.Next(10, 20);

                if (generationSide == -1)
                {
                    bool leftDoor = totalWidth != 0;
                    bool rightDoor = totalWidth + width < maxWidth;

                    GenerateRoom(new Rectangle(position.X + totalWidth, position.Y - height, width, height), towerHeight, leftDoor, rightDoor);
                    totalWidth += width;
                }
                else
                {
                    bool rightDoor = totalWidth != 0;
                    bool leftDoor = totalWidth + width < maxWidth;

                    GenerateRoom(new Rectangle(position.X - totalWidth - width, position.Y - height, width, height), towerHeight, leftDoor, rightDoor);
                    totalWidth += width;
                }
            }
        }


        public void FillChest(Chest chest, int style)
        {
            int nextItem = 0;

            int mainItem = 0;
            int potionItem = 0;
            int lightItem;
            int materialItem = 0;

            switch (WorldGen.genRand.Next(5))
            {
                case 0:
                    mainItem = ItemID.Vilethorn;
                    if (!WorldGen.crimson) mainItem = ItemID.CrimsonRod;
                    break;
                case 1:
                    mainItem = ItemID.Musket;
                    if (!WorldGen.crimson) mainItem = ItemID.TheUndertaker;
                    break;
                case 2:
                    mainItem = ItemID.BandofStarpower;
                    if (!WorldGen.crimson) mainItem = ItemID.PanicNecklace;
                    break;
                case 3:
                    mainItem = ItemID.BallOHurt;
                    if (!WorldGen.crimson) mainItem = ItemID.TheMeatball;
                    break;
                case 4:
                    mainItem = ItemID.ShadowOrb;
                    if (!WorldGen.crimson) mainItem = ItemID.CrimsonHeart;
                    break;
            }

            switch (WorldGen.genRand.Next(4))
            {
                case 0:
                    potionItem = ItemID.RagePotion;
                    break;
                case 1:
                    potionItem = ItemID.WrathPotion;
                    break;
                case 2:
                    potionItem = ItemID.LifeforcePotion;
                    break;
                case 3:
                    potionItem = ItemID.SummoningPotion;
                    break;
            }

            lightItem = !WorldGen.crimson ? ItemID.CrimsonTorch : ItemID.CorruptTorch;


            switch (WorldGen.genRand.Next(4))
            {
                case 0:
                    materialItem = ItemID.ShadowScale;
                    if (!WorldGen.crimson) materialItem = ItemID.TissueSample;
                    break;
                case 1:
                    materialItem = ItemID.DemoniteBar;
                    if (!WorldGen.crimson) materialItem = ItemID.CrimtaneBar;
                    break;
                case 2:
                    materialItem = ItemID.CorruptSeeds;
                    if (!WorldGen.crimson) materialItem = ItemID.CrimsonSeeds;
                    break;
                case 3:
                    materialItem = ItemID.RottenChunk;
                    if (!WorldGen.crimson) materialItem = ItemID.Vertebrae;
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

            chest.item[nextItem].SetDefaults(materialItem);
            chest.item[nextItem].stack = WorldGen.genRand.Next(5, 10);
            nextItem++;

            chest.item[nextItem].SetDefaults(ItemID.GoldCoin);
            chest.item[nextItem].stack = WorldGen.genRand.Next(5, 13);
        }
    }
}
