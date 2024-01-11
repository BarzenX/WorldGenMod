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
using static Mono.CompilerServices.SymbolWriter.CodeBlockEntry;

namespace WorldGenMod.Systems.Structures.Caverns
{
    class UndergroundLakes : ModSystem
    {
        List<Vector2> lakes = new List<Vector2>();
        bool generatedGoldenLake = false;

        public override void PreWorldGen()
        {
            lakes.Clear(); // in case of more than 1 world generated during a game
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateLakes)
            {
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle"));
                tasks.Insert(genIndex + 1, new PassLegacy("WorldGenMod: Underground Lakes", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Filling some lakes in the Underground";

                    GenerateLakes();
                }));
            }

        }

        public void GenerateLakes()
        {
            bool canGen = false;
            int lakeX = 0;
            int lakeY = 0;
            Vector2 lakePosition;
            int breakCounter; // only for emergency, so worldgen doesn't freeze

            for (int i = 1; i <= WorldGenMod.lakeCount; i++)
            {
                breakCounter = 0; //init

                do
                {
                    lakeX = WorldGen.genRand.Next(300, Main.maxTilesX - 300);
                    lakeY = WorldGen.genRand.Next((int)Terraria.WorldBuilding.GenVars.rockLayer, Main.maxTilesY - 400); //everything between "Underground" and "Hell"
                    lakePosition = new Vector2(lakeX, lakeY);

                    canGen = true;
                    if (lakes.Count > 0)
                    {
                        foreach (Vector2 oldLake in lakes)
                        {
                            if (Vector2.Distance(lakePosition, oldLake) <= 400)
                            {
                                canGen = false;
                                breakCounter++;
                            }
                        }
                    }
                    if (breakCounter > 20) //if no suitable lake position can be found
                    {
                        canGen = false;
                        break;
                    }
                }
                while (!canGen);


                if (canGen)
                {
                    GenerateLake(
                        position: lakePosition.ToPoint16(),
                        ellipseX: 50 * WorldGen.genRand.NextFloat(0.8f, 1.2f),
                        ellipseY: 30 * WorldGen.genRand.NextFloat(0.8f, 1.2f) );
                }
            }
        }

        public void GenerateLake(Point16 position, float ellipseX, float ellipseY)
        {
            float radiusX = ellipseX;
            float radiusY = ellipseY;
            float yMult = radiusX / radiusY;

            #region define liquid filling & basin type

            int type = LiquidID.Water;
            if (position.Y > Terraria.WorldBuilding.GenVars.rockLayer + Main.maxTilesY / 3)
            {
                type = LiquidID.Lava;
            }


            int tileType = TileID.Mud;
            for (int i = position.X - 20; i < position.X + 20; i++)
            {
                for (int j = position.Y - 20; j < position.Y + 20; j++)
                {
                    int currentType = Main.tile[i, j].TileType;

                    if (currentType == TileID.SnowBlock || currentType == TileID.IceBlock)
                    {
                        tileType = TileID.IceBlock;
                        break;
                    }
                }
            }
            #endregion

            #region select golden lake
            if (!generatedGoldenLake)
            {
                generatedGoldenLake = true;
                type = LiquidID.Honey;
                tileType = TileID.Gold;
            }
            #endregion

            #region create lake
            for (int i = position.X - (int)radiusX - 20; i < position.X + (int)radiusX + 20; i++)
            {
                for (int j = position.Y - (int)radiusY - 20; j < position.Y + (int)radiusY + 20; j++)
                {
                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX + WorldGen.genRand.Next(12, 15))
                    {
                        WorldGen.KillTile(i, j, noItem: true);
                        WorldGen.EmptyLiquid(i, j);
                    }

                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX)
                    {
                        if (j > position.Y)
                        {
                            WorldGen.PlaceLiquid(i, j, (byte)type, 255);
                        }
                    }

                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() > radiusX && new Vector2(i - position.X, (j - position.Y) * yMult).Length() < radiusX + 15 + WorldGen.genRand.Next(6))
                    {
                        if (j > position.Y - 5)
                        {
                            if (type == LiquidID.Water || type == LiquidID.Honey)
                            {
                                WorldGen.PlaceTile(i, j, tileType, forced: true);
                            }
                            else
                            {
                                WorldGen.PlaceTile(i, j, TileID.Ash, forced: true);
                            }
                        } 
                    }
                }
            }
            #endregion

            lakes.Add(position.ToVector2());
        }
    }
}
