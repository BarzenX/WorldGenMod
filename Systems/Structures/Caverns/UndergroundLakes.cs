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

namespace PenumbralsWorldgen.Systems.Structures.Caverns
{
    class UndergroundLakes : ModSystem
    {
        List<Vector2> lakes = new List<Vector2>();
        bool generatedGoldenLake = false;

        public override void PreWorldGen()
        {
            lakes.Clear();
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle"));
            tasks.Insert(genIndex + 1, new PassLegacy("Canyon", delegate (GenerationProgress progress, GameConfiguration config)
            {
                progress.Message = "Underground lakes";

                GenerateLakes();
            }));
        }

        public void GenerateLakes()
        {
            if (!PenumbralsWorldgen.generateLakes)
            {
                return;
            }

            for (int i = 0; i < (int)Math.Round(Main.maxTilesX * 0.0035f); i++)
            {
                Vector2 position = new Vector2(WorldGen.genRand.Next(300, Main.maxTilesX - 300), WorldGen.genRand.Next((int)WorldGen.rockLayer, Main.maxTilesY - 400));

                bool canGen = true;
                foreach (Vector2 oldLake in lakes)
                {
                    if (Vector2.Distance(position, oldLake) <= 400)
                    {
                        canGen = false;
                    }
                }

                if (canGen)
                {
                    GenerateLake(position.ToPoint16(), WorldGen.genRand.NextFloat(0.8f, 1.2f));
                }
            }
        }

        public void GenerateLake(Point16 position, float sizeMult)
        {
            float radiusX = 85f * sizeMult;
            float radiusY = 30f * sizeMult;
            float yMult = radiusX / radiusY;

            int type = LiquidID.Water;
            if (position.Y > WorldGen.rockLayer + Main.maxTilesY / 3) type = LiquidID.Lava;

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

            if (!generatedGoldenLake)
            {
                generatedGoldenLake = true;
                type = LiquidID.Honey;
                tileType = TileID.Gold;
            }

            for (int i = position.X - (int)radiusX - 20; i < position.X + (int)radiusX + 20; i++)
            {
                for (int j = position.Y - (int)radiusY - 20; j < position.Y + (int)radiusY + 20; j++)
                {
                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX + WorldGen.genRand.Next(12, 15))
                    {
                        WorldGen.KillTile(i, j, noItem: true);
                    }

                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() <= radiusX)
                    {
                        if (j > position.Y) WorldGen.PlaceLiquid(i, j, (byte)type, 255);
                    }

                    if (new Vector2(i - position.X, (j - position.Y) * yMult).Length() > radiusX && new Vector2(i - position.X, (j - position.Y) * yMult).Length() < radiusX + 15 + WorldGen.genRand.Next(6))
                    {
                        if (type == LiquidID.Water || type == LiquidID.Honey)
                        {
                            if (j > position.Y - 5) WorldGen.PlaceTile(i, j, tileType, forced: true);
                        }
                        else
                        {
                            if (j > position.Y - 5) WorldGen.PlaceTile(i, j, TileID.Ash, forced: true);
                        }
                    }
                }
            }

            lakes.Add(position.ToVector2());
        }
    }
}
