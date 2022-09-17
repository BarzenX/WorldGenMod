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

namespace PenumbralsWorldgen.Systems.Structures.Overworld
{
    class Canyon : ModSystem
    {
        int previousCanyon = 0;
        List<Point16> extraOrePositions = new List<Point16>();
        public override void PreWorldGen()
        {
            extraOrePositions.Clear();
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle"));
            tasks.Insert(genIndex + 1, new PassLegacy("Canyon", delegate (GenerationProgress progress, GameConfiguration config)
            {
                progress.Message = "Earthquake!";

                //Generate small canyon
                GenerateCanyon(
                    sizeY: Main.maxTilesY / 3, 
                    sizeXTop: (int)(50 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                    sizeXBottom: (int)(15 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                    minDistanceFromSpawn: 75,
                    checkForPreviousCanyon: false);
                //Generate small canyon
                GenerateCanyon(
                    sizeY: Main.maxTilesY / 2, 
                    sizeXTop: (int)(50 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                    sizeXBottom: (int)(15 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                    minDistanceFromSpawn: 75,
                    checkForPreviousCanyon: false);
            }));
            
            genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));
            tasks.Insert(genIndex + 1, new PassLegacy("CanyonOres", delegate (GenerationProgress progress, GameConfiguration config)
            {
                progress.Message = "Heating up stones";

                //Generate small canyon
                foreach (Point16 pos in extraOrePositions)
                {
                    List<int> ores = new List<int>();
                    ores.Add(TileID.Hellstone);
                    if (WorldGen.crimson)
                    {
                        ores.Add(TileID.Crimtane);
                    }
                    else
                    {
                        ores.Add(TileID.Demonite);
                    }

                    WorldGen.TileRunner(pos.X, pos.Y, WorldGen.genRand.Next(6, 10), 5, WorldGen.genRand.Next(ores), false, 0, 0, false, true);
                }

            }));
        }

        public void GenerateCanyon(int sizeY, int sizeXTop, int sizeXBottom, int minDistanceFromSpawn, bool checkForPreviousCanyon = false)
        {
            int positionX = 0;
            int positionY = 0;
            bool canGenHere = false;

            FastNoiseLite noise = new FastNoiseLite(WorldGen.genRand.Next(1000, 9000)); //Noise for larger size fluctuations
            noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            noise.SetFrequency(1f);

            FastNoiseLite noise2 = new FastNoiseLite(WorldGen.genRand.Next(1000, 9000)); //Noise for more common size fluctuations
            noise2.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            noise2.SetFrequency(2f);

            //Find a position to generate the canyon
            while (!canGenHere)
            {
                int maxDistanceFromSpawnOnDungeonSide = Main.maxTilesX / 4;
                positionX = Main.rand.Next(450, Main.maxTilesX - 450);
                while (Math.Abs(positionX - Main.spawnTileX) < minDistanceFromSpawn || (Math.Sign(positionX - Main.spawnTileX) == WorldGen.dungeonSide && Math.Abs(positionX - Main.spawnTileX) > maxDistanceFromSpawnOnDungeonSide) || (checkForPreviousCanyon && Math.Abs(positionX - previousCanyon) < Main.maxTilesX / 4))
                {
                    positionX = Main.rand.Next(450, Main.maxTilesX - 450);
                }

                positionY = 200;

                while (Main.tile[positionX, positionY] == null || !Main.tile[positionX, positionY].HasTile || !Main.tileSolid[Main.tile[positionX, positionY].TileType])
                {
                    positionY++;
                }


                List<int> allowedTiles = new List<int>()
                {
                    TileID.Dirt, TileID.Grass
                };
                if (allowedTiles.Contains(Main.tile[positionX, positionY].TileType) && positionY > 200)
                {
                    canGenHere = true;
                    break;
                }
            }

            previousCanyon = positionX;

            //Various lists for after the hole is generated
            List<Point16> sideSpikesRight = new List<Point16>();
            int tilesUntilRightSpike = WorldGen.genRand.Next(10, 40);
            List<Point16> sideSpikesLeft = new List<Point16>();
            int tilesUntilLeftSpike = WorldGen.genRand.Next(10, 40);
            List<Point16> waterCaves = new List<Point16>();
            int tilesUntilWaterCave = WorldGen.genRand.Next(5);
            List<Point16> lavaCaves = new List<Point16>();
            int tilesUntilLavaCave = WorldGen.genRand.Next(15, 25);

            int tilesUntilOreSpot = WorldGen.genRand.Next(15, 25);

            for (int j = positionY - 30; j < positionY + sizeY; j++)
            {
                float progressDown = ((float)j - (float)positionY) / (float)sizeY;
                int sizeXCurrent = (int)MathHelper.Lerp(sizeXTop, sizeXBottom, progressDown);
                sizeXCurrent = (int)(sizeXCurrent * MathHelper.Lerp(0.65f, 1.35f, (noise.GetNoise(positionX * 10f, positionY * 10f) + 1f) / 2f));
                sizeXCurrent = (int)(sizeXCurrent * MathHelper.Lerp(0.85f, 1.15f, (noise2.GetNoise(positionX * 1000f, positionY * 1000f) + 1f) / 2f));
                sizeXCurrent += Main.rand.Next(-2, 3);

                tilesUntilRightSpike--;
                if (tilesUntilRightSpike <= 0)
                {
                    tilesUntilRightSpike = WorldGen.genRand.Next(10, 20);
                    sideSpikesRight.Add(new Point16(positionX + sizeXCurrent / 2 + 5, j));
                }

                tilesUntilLeftSpike--;
                if (tilesUntilLeftSpike <= 0)
                {
                    tilesUntilLeftSpike = WorldGen.genRand.Next(10, 20);
                    sideSpikesLeft.Add(new Point16(positionX - sizeXCurrent / 2 - 5, j));
                }

                tilesUntilWaterCave--;
                if (tilesUntilWaterCave <= 0)
                {
                    tilesUntilWaterCave = WorldGen.genRand.Next(20, 50);
                    int side = 1;
                    if (WorldGen.genRand.NextBool()) side = -1;
                    waterCaves.Add(new Point16(positionX + sizeXCurrent / 2 * side + WorldGen.genRand.Next(35) * side, j));
                }

                tilesUntilLavaCave--;
                if (tilesUntilLavaCave <= 0)
                {
                    tilesUntilLavaCave = WorldGen.genRand.Next(30, 80);
                    int side = 1;
                    if (WorldGen.genRand.NextBool()) side = -1;
                    lavaCaves.Add(new Point16(positionX + sizeXCurrent / 2 * side + WorldGen.genRand.Next(45) * side, j));
                }

                tilesUntilOreSpot--;
                if (tilesUntilOreSpot <= 0)
                {
                    tilesUntilOreSpot = WorldGen.genRand.Next(20, 55);
                    int side = 1;
                    if (WorldGen.genRand.NextBool()) side = -1;
                    extraOrePositions.Add(new Point16(positionX + sizeXCurrent / 2 * side + WorldGen.genRand.Next(30) * side, j));
                }

                for (int i = positionX - sizeXCurrent / 2; i < positionX + sizeXCurrent / 2; i++)
                {
                    WorldGen.KillTile(i, j, false, false, true);
                }
            }

            foreach (Point16 position in sideSpikesRight)
            {
                if (Main.tile[position.X, position.Y] != null && Main.tile[position.X, position.Y].HasTile && Main.tileSolid[Main.tile[position.X, position.Y].TileType])
                {
                    int type = Main.tile[position.X, position.Y].TileType;
                    int spikeSizeY = WorldGen.genRand.Next(4, 11);
                    int spikeSizeX = (int)WorldGen.genRand.NextFloat(sizeXTop * 0.15f, sizeXTop * 0.35f);

                    for (float j = -1f; j < 1f; j += 2f / (float)spikeSizeY)
                    {
                        for (int i = -(int)(spikeSizeX * (1f - Math.Abs(j)) + Main.rand.Next(spikeSizeX / 2)); i < 0; i++)
                        {
                            WorldGen.PlaceTile(position.X + i, (int)(position.Y + spikeSizeY * j * 0.5f), type, false, false);
                        }
                    }
                }
            }

            foreach (Point16 position in sideSpikesLeft)
            {
                if (Main.tile[position.X, position.Y] != null && Main.tile[position.X, position.Y].HasTile && Main.tileSolid[Main.tile[position.X, position.Y].TileType])
                {
                    int type = Main.tile[position.X, position.Y].TileType;
                    int spikeSizeY = WorldGen.genRand.Next(4, 11);
                    int spikeSizeX = (int)WorldGen.genRand.NextFloat(sizeXTop * 0.25f, sizeXTop * 0.6f);

                    for (float j = -1f; j < 1f; j += 2f / (float)spikeSizeY)
                    {
                        for (int i = (int)(spikeSizeX * (1f - Math.Abs(j)) + Main.rand.Next(spikeSizeX / 2)); i > 0; i--)
                        {
                            WorldGen.PlaceTile(position.X + i, (int)(position.Y + spikeSizeY * j * 0.5f), type, false, false);
                        }
                    }
                }
            }

            foreach (Point16 position in waterCaves)
            {
                float sizeMult = Main.rand.NextFloat(1f, 1.2f);
                float radiusX = Main.rand.NextFloat(5f, 9f) * sizeMult;
                float radiusY = 5f * sizeMult;
                float yMult = radiusX / radiusY;

                for (int i = position.X - (int)radiusX; i < position.X + (int)radiusX; i++)
                {
                    for (int j = position.Y - (int)radiusY; j < position.Y + (int)radiusY; j++)
                    {
                        if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX)
                        {
                            WorldGen.KillTile(i, j, noItem: true);
                            if (j > position.Y) WorldGen.PlaceLiquid(i, j, LiquidID.Water, 100);
                        }
                    }
                }
            }

            foreach (Point16 position in lavaCaves)
            {
                float sizeMult = Main.rand.NextFloat(1.3f, 1.55f);
                float radiusX = Main.rand.NextFloat(5f, 9f) * sizeMult;
                float radiusY = 5f * sizeMult;
                float yMult = radiusX / radiusY;

                for (int i = position.X - (int)radiusX; i < position.X + (int)radiusX; i++)
                {
                    for (int j = position.Y - (int)radiusY; j < position.Y + (int)radiusY; j++)
                    {
                        if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX)
                        {
                            WorldGen.KillTile(i, j, noItem: true);
                            if (j > position.Y) WorldGen.PlaceLiquid(i, j, LiquidID.Lava, 100);
                        }
                    }
                }
            }
        }
    }
}
