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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Drawing;
using static Humanizer.In;

namespace WorldGenMod.Structures.Overworld
{
    class Fissure : ModSystem
    {
        List<int> previousFissures = new();
        List<Point16> extraOrePositions = new();

        public override void PreWorldGen()
        {
            previousFissures.Clear(); // in case of more than 1 world generated during a game
            extraOrePositions.Clear();
        }


        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateFissure)
            {
                previousFissures.Add(0); //init list
                int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Fissure", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Cracking world layers";

                    for (int i = 1; i <= WorldGenMod.fissureCount; i++)
                    {
                        GenerateFissure(
                            sizeY: (int)(Main.maxTilesY * 0.6 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                            sizeXTop: (int)(25 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                            sizeXBottom: (int)(5 * WorldGen.genRand.NextFloat(0.8f, 1.2f)),
                            minDistanceFromSpawn: 75,
                            shiftEveryXVerticalTiles: 4, // every x tiles in y-direction the fissure may move 1 tile in +/- x-direction
                            shiftMaxAllowed: 40,
                            forcedShiftSide: WorldGen.genRand.NextBool(), // to make the fissure more curvy: False = left, True = right
                            checkForPreviousFissure: true);
                    }

                }));

                genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Fissure Ores", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Placing some precious in the Fissure";

                    #region define world ores
                    int copperTier;
                    int ironTier;
                    int silverTier;
                    int goldTier;
                    int crimtaneTier;
                    // add copper or tin tier
                    if (GenVars.copper == TileID.Copper)
                    {
                        copperTier = TileID.Copper;
                    }
                    else
                    {
                        copperTier = TileID.Tin;
                    }

                    // add iron or lead tier
                    if (GenVars.iron == TileID.Iron)
                    {
                        ironTier = TileID.Iron;
                    }
                    else
                    {
                        ironTier = TileID.Lead;
                    }

                    // add silver or tungsten tier
                    if (GenVars.silver == TileID.Silver)
                    {
                        silverTier = TileID.Silver;
                    }
                    else
                    {
                        silverTier = TileID.Tungsten;
                    }

                    // add gold or platinum tier
                    if (GenVars.gold == TileID.Gold)
                    {
                        goldTier = TileID.Gold;
                    }
                    else
                    {
                        goldTier = TileID.Platinum;
                    }

                    // add crimtane or demonite tier
                    if (WorldGen.crimson)
                    {
                        crimtaneTier = TileID.Crimtane;
                    }
                    else
                    {
                        crimtaneTier = TileID.Demonite;
                    }
                    #endregion

                    #region create ore layers

                    List<int> surfaceOres = new()
                    {
                        copperTier,
                        ironTier
                    };

                    List<int> undergroundOres = new()
                    {
                        ironTier,
                        silverTier,
                        crimtaneTier
                    };

                    List<int> cavernOres = new()
                    {
                        silverTier,
                        goldTier,
                        crimtaneTier
                    };

                    #endregion

                    //Generate ores
                    if (extraOrePositions.Count > 0)
                    {
                        foreach (Point16 pos in extraOrePositions)
                        {
                            if (pos.Y < Main.worldSurface) // surface
                            {
                                WorldGen.TileRunner(pos.X, pos.Y, WorldGen.genRand.Next(6, 10), 5, WorldGen.genRand.Next(surfaceOres), false, 0, 0, false, true);
                            }
                            else if (pos.Y < Main.rockLayer) // underground
                            {
                                WorldGen.TileRunner(pos.X, pos.Y, WorldGen.genRand.Next(6, 10), 5, WorldGen.genRand.Next(undergroundOres), false, 0, 0, false, true);
                            }
                            else // all below underground
                            {
                                WorldGen.TileRunner(pos.X, pos.Y, WorldGen.genRand.Next(6, 10), 5, WorldGen.genRand.Next(cavernOres), false, 0, 0, false, true);
                            }
                        }
                    }
                }));
            }

        }

        public void GenerateFissure(int sizeY, int sizeXTop, int sizeXBottom, int minDistanceFromSpawn, int shiftEveryXVerticalTiles, int shiftMaxAllowed, bool forcedShiftSide, bool checkForPreviousFissure = false)
        {
            if (!WorldGenMod.generateFissure)
            {
                return;
            }

            int positionX = 0;
            int positionY = 0;
            bool canGenHere = false;
            bool previousFissureTooClose = false;
            int mapMiddle = Main.maxTilesX / 2;
            int maxDistanceFromSpawnOnDungeonSide = Main.maxTilesX / 2 * 6 / 10;
            int breakCounter = 0; // only for emergency, so worldgen doesn't freeze

            //Find a position to generate the fissure
            while (!canGenHere)
            {
                do
                {
                    positionX = Main.rand.Next(450, Main.maxTilesX - 450); // get random new x-position and do checks

                    if (checkForPreviousFissure)
                    {
                        previousFissureTooClose = false; // re-init before for-loop
                        for (int i = 0; i < previousFissures.Count; i++)
                        {
                            previousFissureTooClose = previousFissureTooClose || Math.Abs(positionX - previousFissures[i]) < shiftMaxAllowed + 20;
                        }
                        if (previousFissureTooClose)
                        {
                            breakCounter++;
                            if (breakCounter > 20)
                            { return; }
                        }
                    }
                } while (Math.Abs(positionX - mapMiddle) < minDistanceFromSpawn ||
                         Math.Sign(positionX - mapMiddle) == GenVars.dungeonSide && Math.Abs(positionX - mapMiddle) > maxDistanceFromSpawnOnDungeonSide ||
                         checkForPreviousFissure && previousFissureTooClose);//(checkForPreviousFissure && Math.Abs(positionX - previousFissure) < (shiftMaxAllowed + 20) )

                positionY = 100; // initial value
                while (Main.tile[positionX, positionY] == null ||
                        !Main.tile[positionX, positionY].HasTile ||
                        !Main.tileSolid[Main.tile[positionX, positionY].TileType])
                {
                    positionY++; // go further down until solid tile got detected
                }


                List<int> allowedTiles = new()
                {
                    TileID.Dirt, TileID.Grass, TileID.Sand, TileID.Stone, TileID.ClayBlock, TileID.SnowBlock, TileID.IceBlock
                };
                if (allowedTiles.Contains(Main.tile[positionX, positionY].TileType) && positionY > 100)
                {
                    canGenHere = true;
                }
            }

            previousFissures.Add(positionX);

            // initialization
            int tilesUntilOreSpot = WorldGen.genRand.Next(15, 25);
            int positionXShifted = positionX; // to make the fissure look more natural: radom x displacement 
            int tilesUntilXShift = shiftEveryXVerticalTiles;
            int xShift;
            int oreSide;

            for (int j = positionY - 30; j < positionY + sizeY; j++)
            {
                float progressDown = (j - (float)positionY) / sizeY;
                int sizeXCurrent = (int)MathHelper.Lerp(sizeXTop, sizeXBottom, progressDown);

                #region Ore Spots
                tilesUntilOreSpot--;
                if (tilesUntilOreSpot <= 0)
                {
                    tilesUntilOreSpot = WorldGen.genRand.Next(20, 55); // reset value

                    //randomize placement side
                    if (WorldGen.genRand.NextBool())
                    {
                        oreSide = 1; // on the right of the fissure
                    }
                    else
                    {
                        oreSide = -1; // on the left of the fissure
                    }

                    extraOrePositions.Add(new Point16(positionXShifted + sizeXCurrent / 2 * oreSide + WorldGen.genRand.Next(25) * oreSide, j));
                }
                #endregion

                #region xShift
                tilesUntilXShift--;
                if (tilesUntilXShift <= 0)
                {
                    tilesUntilXShift = shiftEveryXVerticalTiles; // reset value
                    if (!forcedShiftSide)
                    {
                        xShift = WorldGen.genRand.Next(-1, 1); // [-1, 0] --> shift forced to the left
                    }
                    else
                    {
                        xShift = WorldGen.genRand.Next(0, 2); // [0, 1] --> shift forced to the right
                    }


                    if (Math.Abs(positionX - (positionXShifted + xShift)) <= shiftMaxAllowed)
                    {
                        positionXShifted += xShift;
                    }


                    if (positionXShifted - positionX == -shiftMaxAllowed) // hit the left limit
                    {
                        forcedShiftSide = true; // change forced shift direction
                    }
                    else if (positionXShifted - positionX == shiftMaxAllowed) // hit the right limit
                    {
                        forcedShiftSide = false; // change forced shift direction
                    }
                }
                else
                {
                    xShift = WorldGen.genRand.Next(-1, 2); // [-1, 0, 1] --> random 1-tile shift
                    if (Math.Abs(positionX - (positionXShifted + xShift)) <= shiftMaxAllowed)
                    {
                        positionXShifted += xShift;
                    }
                }
                #endregion

                // carve out tiles and liquid
                for (int i = positionXShifted - sizeXCurrent / 2; i < positionXShifted + sizeXCurrent / 2; i++)
                {
                    WorldGen.KillTile(i, j, false, false, true);
                    WorldGen.EmptyLiquid(i, j);
                }
            }
        }
    }
}
