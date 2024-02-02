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

namespace WorldGenMod.Structures.Caverns
{
    class AbandonedHellevator : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateHellevators)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));  //used to be "Micro Biomes", moved 1 step ahead of "Dungeon" because it was sometimes overlapping the dungeon
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Abandoned Hellevator", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Digging out some express shafts";

                    for (int i = 1; i <= WorldGenMod.hellevatorCount; i++)
                    {
                        GenerateHellevator();
                    }
                }));
            }

        }

        public void GenerateHellevator()
        {
            if (!WorldGenMod.generateHellevators)
            {
                return;
            }

            Point16 position = new(WorldGen.genRand.Next(200, Main.maxTilesX), Main.maxTilesY - WorldGen.genRand.Next(125, 700));

            int tunnelHeight = WorldGen.genRand.Next(Main.maxTilesY / 6, Main.maxTilesY / 3);
            for (int i = -3 + position.X; i <= 3 + position.X; i++)
            {
                int timeUntilPlacable = WorldGen.genRand.Next(10);
                for (int j = 0 + position.Y; j > -tunnelHeight - WorldGen.genRand.Next(10) + position.Y; j--)
                {
                    if (Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.LihzahrdBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.BlueDungeonBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.GreenDungeonBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.PinkDungeonBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.CrackedBlueDungeonBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.CrackedGreenDungeonBrick ||
                         Main.tile[i, j] != null && Main.tile[i, j].TileType == TileID.CrackedPinkDungeonBrick ||

                         Main.tile[i, j].WallType == WallID.BlueDungeon ||
                         Main.tile[i, j].WallType == WallID.BlueDungeonSlab ||
                         Main.tile[i, j].WallType == WallID.BlueDungeonSlabUnsafe ||
                         Main.tile[i, j].WallType == WallID.BlueDungeonTile ||
                         Main.tile[i, j].WallType == WallID.BlueDungeonTileUnsafe ||
                         Main.tile[i, j].WallType == WallID.BlueDungeonUnsafe ||

                         Main.tile[i, j].WallType == WallID.GreenDungeon ||
                         Main.tile[i, j].WallType == WallID.GreenDungeonSlab ||
                         Main.tile[i, j].WallType == WallID.GreenDungeonSlabUnsafe ||
                         Main.tile[i, j].WallType == WallID.GreenDungeonTile ||
                         Main.tile[i, j].WallType == WallID.GreenDungeonTileUnsafe ||
                         Main.tile[i, j].WallType == WallID.GreenDungeonUnsafe ||

                         Main.tile[i, j].WallType == WallID.PinkDungeon ||
                         Main.tile[i, j].WallType == WallID.PinkDungeonSlab ||
                         Main.tile[i, j].WallType == WallID.PinkDungeonSlabUnsafe ||
                         Main.tile[i, j].WallType == WallID.PinkDungeonTile ||
                         Main.tile[i, j].WallType == WallID.PinkDungeonTileUnsafe ||
                         Main.tile[i, j].WallType == WallID.PinkDungeonUnsafe)
                    {

                    }
                    else
                    {
                        WorldGen.KillTile(i, j, noItem: true);
                        WorldGen.KillWall(i, j);

                        if (timeUntilPlacable > 0)
                        {
                            timeUntilPlacable--;
                        }
                        else
                        {
                            if (i - position.X == -3 || i - position.X == 3)
                            {
                                WorldGen.PlaceTile(i, j, TileID.IronBrick, true, true);
                                WorldGen.PlaceWall(i, j, WallID.IronBrick, true);
                            }

                            if (i - position.X == 0)
                            {
                                WorldGen.PlaceTile(i, j, TileID.Chain, true, true);
                                WorldGen.PlaceWall(i, j, WallID.IronBrick, true);
                            }

                            if (i - position.X == -1 || i - position.X == 1)
                            {
                                WorldGen.PlaceWall(i, j, WallID.LeadBrick, true);
                            }

                            if (i - position.X == -2 || i - position.X == 2)
                            {
                                int type = TileID.LeadBrick;
                                if (j % 4 < 2) type = TileID.DiamondGemspark;

                                WorldGen.PlaceTile(i, j, type, true, true);
                                WorldGen.PlaceWall(i, j, WallID.IronBrick, true);
                            }
                        }
                    }
                }
            }
        }
    }
}
