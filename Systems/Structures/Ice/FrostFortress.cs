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

//TODO: - on small maps sometime the FrostFortress creates extreme slow - unknown reason

namespace WorldGenMod.Systems.Structures.Caverns
{
    class FrostFortress : ModSystem
    {
        List<Vector2> fortresses = new List<Vector2>();

        public override void PreWorldGen()
        {
            fortresses.Clear(); // in case of more than 1 world generated during a game
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateFrostFortresses)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Lakes")); //used to be "Buried Chests", moved 1 step ahead of "Dungeon" because it was sometimes overlapping the dungeon
                tasks.Insert(genIndex + 1, new PassLegacy("WorldGenMod: Frost Fortress", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Building a snow fortress";

                    GenerateFortresses();
                }));
            }

        }

        public void GenerateFortresses()
        {
            int amount = (int)(Main.maxTilesX * 0.0004f);
            int amountGenerated = 0;

            while (amountGenerated < amount)
            {
                Vector2 position = new Vector2(WorldGen.genRand.Next(200, Main.maxTilesX - 200), WorldGen.genRand.Next((int)Terraria.WorldBuilding.GenVars.rockLayer, Main.maxTilesY - 200));

                List<int> allowedTiles = new List<int>()
                {
                    TileID.SnowBlock, TileID.IceBlock, TileID.CorruptIce, TileID.FleshIce
                };

                bool tooClose = true;
                while (Main.tile[(int)position.X, (int)position.Y] == null || !allowedTiles.Contains(Main.tile[(int)position.X, (int)position.Y].TileType) || tooClose)
                {
                    position = new Vector2(WorldGen.genRand.Next(200, Main.maxTilesX - 200), WorldGen.genRand.Next((int)Terraria.WorldBuilding.GenVars.rockLayer, Main.maxTilesY - 200));

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
            nextItem++;
        }

        List<Point16> traps = new List<Point16>();
        public void GenerateRoom(Rectangle room, bool leftDoor = false, bool rightDoor = false, bool upDoor = false, bool downDoor = false)
        {
            Rectangle hollowRect = room;
            hollowRect.Width -= 4;
            hollowRect.Height -= 4;
            hollowRect.X += 2;
            hollowRect.Y += 2;

            bool noBreakPoint = WorldGen.genRand.NextBool();
            Vector2 wallBreakPoint = new Vector2(room.X + WorldGen.genRand.Next(room.Width), room.Y + WorldGen.genRand.Next(room.Height));

            List<Rectangle> doors = new List<Rectangle>();
            if (leftDoor) doors.Add(new Rectangle(room.X, room.Y + room.Height - 5, 2, 3));
            if (rightDoor) doors.Add(new Rectangle(room.X + room.Width - 2, room.Y + room.Height - 5, 2, 3));
            if (upDoor) doors.Add(new Rectangle(room.X + room.Width / 2 - 2, room.Y, 4, 2));
            if (downDoor) doors.Add(new Rectangle(room.X + room.Width / 2 - 2, room.Y + room.Height - 2, 4, 2));

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                for (int j = room.Y; j < room.Y + room.Height; j++)
                {
                    WorldGen.KillWall(i, j);
                    if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f) || noBreakPoint) WorldGen.PlaceWall(i, j, wallType);

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
                            if (Vector2.Distance(new Vector2(i, j), wallBreakPoint) > WorldGen.genRand.NextFloat(1f, 7f) || noBreakPoint) WorldGen.PlaceWall(i, j, doorWallType);
                        }
                    }
                }
            }

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                int j = room.Y + room.Height - 2;
                WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 35);
            }

            for (int i = room.X; i < room.X + room.Width; i++)
            {
                int j = room.Y;
                WorldGen.PlaceTile(i, j, TileID.Platforms, true, false, style: 35);
            }

            int decoration = WorldGen.genRand.Next(4);
            int chest = -1;
            switch (decoration)
            {
                default:
                    break;
                case 0:
                    if (WorldGen.genRand.NextBool())
                    {
                        chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: 4);
                        if (chest != -1) FillChest(Main.chest[chest], 4);
                    }
                    else
                    {
                        chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: defaultChestType);
                        if (chest != -1) FillChest(Main.chest[chest], defaultChestType);
                    }
                    break;
                case 1:
                    chest = WorldGen.PlaceChest(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, style: defaultChestType);
                    if (chest != -1) FillChest(Main.chest[chest], defaultChestType);
                    break;
                case 2:
                    WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Campfire, style: defaultCampfireType);
                    break;
                case 3:
                    WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Tables, style: defaultTableType);
                    break;
            }

            if (WorldGen.genRand.NextBool())
            {
                int statue = WorldGen.genRand.Next(6);
                switch (statue)
                {
                    case 0:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 27);
                        break;
                    case 1:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 32);
                        break;
                    case 2:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 33);
                        break;
                    case 3:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 35);
                        break;
                    case 4:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 37);
                        break;
                    case 5:
                        WorldGen.PlaceTile(room.X + WorldGen.genRand.Next(room.Width), room.Y + room.Height - 3, TileID.Statues, style: 68);
                        break;
                }
            }

            traps.Add(new Point16(room.X + WorldGen.genRand.Next(room.Width), room.Y + WorldGen.genRand.Next(room.Height)));
        }

        int brickType = TileID.SnowBrick;
        int floorType = TileID.IceBrick;
        int wallType = WallID.SnowBrick;
        int doorWallType = WallID.IceBrick;
        int defaultChestType = 11;
        int defaultCampfireType = 3;
        int defaultTableType = 24;
        public void GenerateFortress(Point16 position)
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
                    wallType = WallID.SnowBrick;
                    doorWallType = WallID.IceBrick;
                    defaultChestType = 11;
                    defaultCampfireType = 3;
                    defaultTableType = 24;
                    break;
                case 1:
                    brickType = TileID.BorealWood;
                    floorType = TileID.TungstenBrick;
                    if (WorldGen.genRand.NextBool()) floorType = TileID.SilverBrick;
                    wallType = WallID.BorealWood;
                    doorWallType = WallID.BorealWoodFence;
                    defaultChestType = 33;
                    defaultCampfireType = 0;
                    defaultTableType = 28;
                    break;
            }

            GenerateRoom(new Rectangle(position.X - initialRoomSizeX / 2, position.Y - initialRoomSizeY, initialRoomSizeX, initialRoomSizeY), true, true);

            int currentPlusX = initialRoomSizeX / 2;
            int amount = WorldGen.genRand.Next(3, 7);
            for (int i = 0; i < amount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool();
                bool generateDown = WorldGen.genRand.NextBool();

                GenerateRoom(new Rectangle(position.X + currentPlusX, position.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight), true, i != amount - 1, generateUp, generateDown);

                int currentPlusY = -currentRoomHeight + 2;
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/;
                        GenerateRoom(new Rectangle(position.X + currentPlusX, position.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight), false, false, j != vertAmount - 1, true);
                        currentPlusY -= vertRoomHeight - 2;
                    }
                }

                currentPlusY = currentRoomHeight - 2;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        GenerateRoom(new Rectangle(position.X + currentPlusX, position.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight), false, false, true, j != vertAmount - 1);
                        currentPlusY += vertRoomHeight - 2;
                    }
                }

                currentPlusX += currentRoomWidth;
            }

            currentPlusX = -initialRoomSizeX / 2;
            amount = WorldGen.genRand.Next(3, 7);
            for (int i = 0; i < amount; i++)
            {
                int currentRoomWidth = WorldGen.genRand.Next(15, 20);
                int currentRoomHeight = (int)(currentRoomWidth * WorldGen.genRand.NextFloat(0.6f, 1f));

                bool generateUp = WorldGen.genRand.NextBool();
                bool generateDown = WorldGen.genRand.NextBool();

                GenerateRoom(new Rectangle(position.X + currentPlusX - currentRoomWidth, position.Y - currentRoomHeight, currentRoomWidth, currentRoomHeight), i != amount - 1, true, generateUp, generateDown);

                int currentPlusY = -currentRoomHeight + 2;
                if (generateUp)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight/* + WorldGen.genRand.Next(-3, 4)*/;
                        GenerateRoom(new Rectangle(position.X + currentPlusX - currentRoomWidth, position.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight), false, false, j != vertAmount - 1, true);
                        currentPlusY -= vertRoomHeight - 2;
                    }
                }

                currentPlusY = currentRoomHeight - 2;
                if (generateDown)
                {
                    int vertAmount = WorldGen.genRand.Next(1, 4);
                    for (int j = 0; j < vertAmount; j++)
                    {
                        int vertRoomHeight = currentRoomHeight;
                        GenerateRoom(new Rectangle(position.X + currentPlusX - currentRoomWidth, position.Y - vertRoomHeight + currentPlusY, currentRoomWidth, currentRoomHeight), false, false, true, j != vertAmount - 1);
                        currentPlusY += vertRoomHeight - 2;
                    }
                }

                currentPlusX -= currentRoomWidth;
            }

            foreach (Point16 trap in traps)
            {
                WorldGen.placeTrap(trap.X, trap.Y, WorldGen.genRand.Next(3));
            }
        }
    }
}
