using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace WorldGenMod.Structures.Overworld
{
    //TODO: create Sandstone for Fissures that generate in the desert, so the player doesn't need to cut through all the sand
    class Fissure : ModSystem
    {
        List<int> previousFissuresStartX = [];
        List<Point16> extraOrePositions = [];

        public override void PreWorldGen()
        {
            previousFissuresStartX.Clear(); // in case of more than 1 world generated during a game
            extraOrePositions.Clear();
        }


        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            if (WorldGenMod.generateFissure)
            {
                previousFissuresStartX.Add(0); //init list

                int genIndex;
                if (WorldGenMod.fissureCreationAtLaterWorldgen) genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Lakes"));
                else                                            genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Jungle"));


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
                            minDistanceFromSides: 400,
                            forceShiftEveryXVerticalTiles: 4,
                            forceShiftSide: WorldGen.genRand.NextBool(),
                            shiftMaxAllowed: 40,
                            checkForPreviousFissures: true);
                    }

                }));



                if (WorldGenMod.fissureCreationAtLaterWorldgen) genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("#WGM: Fissure"));
                else                                            genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Shinies"));
                tasks.Insert(genIndex + 1, new PassLegacy("#WGM: Fissure Ores", delegate (GenerationProgress progress, GameConfiguration config)
                {
                    progress.Message = "Placing some precious in the Fissure";

                    #region define world ores
                    int copperTier;
                    int ironTier;
                    int silverTier;
                    int goldTier;
                    int crimtaneTier;

                    // decide if copper / tin
                    if (GenVars.copper == TileID.Copper) copperTier = TileID.Copper;
                    else                                 copperTier = TileID.Tin;

                    // decide if iron or lead
                    if (GenVars.iron == TileID.Iron) ironTier = TileID.Iron;
                    else                             ironTier = TileID.Lead;

                    // decide if silver or tungsten
                    if (GenVars.silver == TileID.Silver) silverTier = TileID.Silver;
                    else                                 silverTier = TileID.Tungsten;

                    // add gold or platinum
                    if (GenVars.gold == TileID.Gold) goldTier = TileID.Gold;
                    else                             goldTier = TileID.Platinum;

                    // decide if crimtane or demonite
                    if (WorldGen.crimson) crimtaneTier = TileID.Crimtane;
                    else                  crimtaneTier = TileID.Demonite;
                    #endregion

                    #region create ore layers

                    List<int> surfaceOres =
                    [
                        copperTier,
                        ironTier
                    ];

                    List<int> undergroundOres =
                    [
                        ironTier,
                        silverTier,
                        crimtaneTier
                    ];

                    List<int> cavernOres =
                    [
                        silverTier,
                        goldTier,
                        crimtaneTier
                    ];

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

        /// <summary>
        /// Looks for a suitable position to generate the Fissure, generates it and prepares positions for placing ores later
        /// </summary>
        /// <param name="sizeY">The vertical size of the Fissure in Tiles</param>
        /// <param name="sizeXTop">The horizontal size of the Fissure at the surface (where generation starts)...the hole "diameter"</param>
        /// <param name="sizeXBottom">The horizontal size of the Fissure at the bottom (where generation ends)...the hole "diameter" 
        ///                      <br/>--> If sizeXBottom is smaller than sizeXTop, the Fissure gets narrower during its descent.</param>
        /// <param name="minDistanceFromSpawn">The distance to the left and right of the center position of the map, where no Fissures may be generated</param>
        /// <param name="minDistanceFromSides">The distance from the left and right end of the map, where no Fissures may be generated</param>
        /// <param name="forceShiftEveryXVerticalTiles">The count of vertical tiles, after which the center x-position of the Fissure gets forcibly shifted in the x-direction, stated by forceShiftSide</param>
        /// <param name="forceShiftSide">The actual direction, the Fissure's center point is being shifted to, when force shifting -> False = to the left; True = to the right</param>
        /// <param name="shiftMaxAllowed">The maximum allowed horizontal displacement of the Fissure center point from its starting center x-position</param>
        /// <param name="checkForPreviousFissures">If this Fissure's starting center x-position shall be checked for not colliding with any already generated Fissure</param>
        public void GenerateFissure(int sizeY, int sizeXTop, int sizeXBottom, int minDistanceFromSpawn, int minDistanceFromSides, int forceShiftEveryXVerticalTiles, bool forceShiftSide, int shiftMaxAllowed, bool checkForPreviousFissures = false)
        {
            if (!WorldGenMod.generateFissure) return;

            if (sizeY > Main.maxTilesY) sizeY = Main.maxTilesY - 200;

            int fissureStartPosX = 0;
            int fissureStartPosY = 0;
            bool canGenHere = false;
            bool previousFissureTooClose = false;
            int spawnX = Main.maxTilesX / 2; // x-coordinate of the spawn point
            int fissureDistanceToSpawnX;
            int maxDistanceFromSpawnOnDungeonSide = Main.maxTilesX / 2;//(int)((Main.maxTilesX / 2) * 0.6f);
            int breakCounter = 0; // only for emergency, so worldgen doesn't enter a never ending loop, looking for possible Fissure positions
            List<int> allowedTileTypesForFissureStart =
            [
                TileID.Dirt, TileID.Grass,
                TileID.Sand,
                TileID.Stone,
                TileID.ClayBlock,
                TileID.Mud, TileID.JungleGrass,
                TileID.SnowBlock, TileID.IceBlock,
                TileID.Ebonsand, TileID.Ebonstone, TileID.CorruptGrass,
                TileID.Crimsand, TileID.Crimstone, TileID.CrimsonGrass
            ];

            # region Find a position to generate the Fissure

            while (!canGenHere)
            {
                // generate starting X-position for the Fissure
                do
                {
                    fissureStartPosX = Main.rand.Next(minDistanceFromSides, Main.maxTilesX - minDistanceFromSides); // get random new x-position

                    // check distance from previous fissures
                    if (checkForPreviousFissures)
                    {
                        previousFissureTooClose = false; // re-init before for-loop
                        foreach (int previousFissurePosX in previousFissuresStartX)
                        {
                            previousFissureTooClose |= (Math.Abs(fissureStartPosX - previousFissurePosX) < shiftMaxAllowed + 20);
                        }
                        if (previousFissureTooClose)
                        {
                            breakCounter++;
                            if (breakCounter > 20) return;
                        }
                    }
                    fissureDistanceToSpawnX = fissureStartPosX - spawnX;
                }
                while (Math.Abs(fissureDistanceToSpawnX) < minDistanceFromSpawn ||
                       Math.Sign(fissureDistanceToSpawnX) == GenVars.dungeonSide && Math.Abs(fissureDistanceToSpawnX) > maxDistanceFromSpawnOnDungeonSide ||
                       checkForPreviousFissures && previousFissureTooClose);


                fissureStartPosY = 100; // initial value
                while ( Main.tile[fissureStartPosX, fissureStartPosY] == null ||
                       !Main.tile[fissureStartPosX, fissureStartPosY].HasTile ||
                       !Main.tileSolid[Main.tile[fissureStartPosX, fissureStartPosY].TileType] )
                {
                    fissureStartPosY++; // go further down until solid tile got detected
                }


                if (allowedTileTypesForFissureStart.Contains(Main.tile[fissureStartPosX, fissureStartPosY].TileType) && fissureStartPosY > 100)
                {
                    canGenHere = true;
                }


                // check if starting point is on a floating island
                bool floatingIslandDetected = false;
                for (int j = fissureStartPosY + 1; j < fissureStartPosY + 100; j++)
                {
                    floatingIslandDetected |= Main.tile[fissureStartPosX, j].TileType == TileID.Cloud ||
                                              Main.tile[fissureStartPosX, j].TileType == TileID.RainCloud ||
                                              Main.tile[fissureStartPosX, j].TileType == TileID.SnowCloud;

                    if (floatingIslandDetected)
                    {
                        //go further down until the surface gets hit
                        int jj = j + 15;
                        do jj++;
                        while (!allowedTileTypesForFissureStart.Contains(Main.tile[fissureStartPosX, jj].TileType) || !Main.tile[fissureStartPosX, jj].HasTile);

                        fissureStartPosY = jj;
                        break;
                    }
                }
            }

            previousFissuresStartX.Add(fissureStartPosX);

            #endregion


            // initialization
            int tilesUntilOreSpot = WorldGen.genRand.Next(15, 25); //init
            int positionXShifted = fissureStartPosX; // to make the fissure look more natural: random x displacement 
            int tilesUntilXShift = forceShiftEveryXVerticalTiles;
            int xShift;
            int oreSide;

            while (fissureStartPosY + sizeY > Main.maxTilesY)
            {
                sizeY -= 100;
            }

            for (int j = fissureStartPosY - 30; j < fissureStartPosY + sizeY; j++) // "-30" to erase some mountain slopes, if the Fissure happens to generate on a slope
            {
                float progressDown = (j - (float)fissureStartPosY) / sizeY;
                int sizeXCurrent = (int)MathHelper.Lerp(sizeXTop, sizeXBottom, progressDown);

                #region Ore Spots
                tilesUntilOreSpot--;
                if (tilesUntilOreSpot <= 0)
                {
                    tilesUntilOreSpot = WorldGen.genRand.Next(20, 55); // reset value

                    //randomize ore placement side
                    if (WorldGen.genRand.NextBool()) oreSide =  1; // on the right of the fissure
                    else                             oreSide = -1; // on the left of the fissure

                    extraOrePositions.Add(new Point16(positionXShifted + sizeXCurrent / 2 * oreSide + WorldGen.genRand.Next(25) * oreSide, j));
                }
                #endregion

                #region xShift
                tilesUntilXShift--;
                if (tilesUntilXShift <= 0)
                {
                    tilesUntilXShift = forceShiftEveryXVerticalTiles + WorldGen.genRand.Next(-1, 2); // reset value and randomize it a bit in the range of [-1, 1]
                    if (!forceShiftSide) xShift = WorldGen.genRand.Next(-1, 1); // [-1, 0] --> shift forced to the left
                    else               xShift = WorldGen.genRand.Next( 0, 2); // [ 0, 1] --> shift forced to the right


                    if (Math.Abs(fissureStartPosX - (positionXShifted + xShift)) <= shiftMaxAllowed) positionXShifted += xShift; // apply shift if application stays within limits


                    if      (positionXShifted - fissureStartPosX == -shiftMaxAllowed) forceShiftSide = true;  // hit the left limit, change the shifting direction to "to the right"
                    else if (positionXShifted - fissureStartPosX ==  shiftMaxAllowed) forceShiftSide = false; // hit the right limit, change the shifting direction to "to the left"
                }
                else
                {
                    xShift = WorldGen.genRand.Next(-1, 2); // [-1, 0, 1] --> random 1-tile shift
                    if (Math.Abs(fissureStartPosX - (positionXShifted + xShift)) <= shiftMaxAllowed) positionXShifted += xShift; // apply shift if application stays within limits
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
