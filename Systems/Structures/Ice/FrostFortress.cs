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
    class FrostFortress : ModSystem
    {
        List<Vector2> fortresses = new List<Vector2>();

        public override void PreWorldGen()
        {
            fortresses.Clear();
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref float totalWeight)
        {
            int genIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Buried Chests"));
            tasks.Insert(genIndex + 1, new PassLegacy("FrostFortress", delegate (GenerationProgress progress, GameConfiguration config)
            {
                progress.Message = "Building a snow fort";

                GenerateLakes();
            }));
        }

        public void GenerateFortresses()
        {

        }

        public void GenerateFortresses(Point16 position, float sizeMult)
        {

        }
    }
}
