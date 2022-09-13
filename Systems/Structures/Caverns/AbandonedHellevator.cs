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
    class AbandonedHellevator : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Micro Biomes"));
            tasks.Insert(genIndex + 1, new PassLegacy("AbandonedHellevator", delegate (GenerationProgress progress, GameConfiguration config)
            {
                progress.Message = "Blood is Fuel";

                GenerateHellevator();
                GenerateHellevator();
            }));
        }

        public void GenerateHellevator()
        {
            Point16 position = new Point16(WorldGen.genRand.Next(200, Main.maxTilesX), Main.maxTilesY - WorldGen.genRand.Next(125, 700));

            int tunnelHeight = WorldGen.genRand.Next(Main.maxTilesY / 6, Main.maxTilesY / 3);
            for (int i = -3 + position.X; i <= 3 + position.X; i++)
            {
                int timeUntilPlacable = WorldGen.genRand.Next(10);
                for (int j = 0 + position.Y; j > -tunnelHeight - WorldGen.genRand.Next(10) + position.Y; j--)
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
