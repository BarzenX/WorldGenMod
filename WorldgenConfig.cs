using Terraria.ModLoader.Config;
using System.ComponentModel;
using Terraria;

namespace PenumbralsWorldgen
{
    [BackgroundColor(130 / 5, 230 / 5, 255 / 5, (int)(255f * 0.75f))]
    class WorldgenConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message)
        {
            return Main.countsAsHostForGameplay[whoAmI];
        }

        [DefaultValue(true)]
        [BackgroundColor(116, 201, 164)]
        [Label("Abandoned Hellevator")]
        [Tooltip("Whether or not the 'Abandoned Hellevator' structure will generate (requires you to generate a new world in order to affect anything)")]
        public bool generateHellevator;

        [DefaultValue(true)]
        [BackgroundColor(116, 201, 164)]
        [Label("Underground Lakes")]
        [Tooltip("Whether or not the 'Underground Lakes' will generate (requires you to generate a new world in order to affect anything)")]
        public bool generateLakes;

        [DefaultValue(true)]
        [BackgroundColor(116, 201, 164)]
        [Label("Frost Fortress")]
        [Tooltip("Whether or not the 'Frost Fortress' microdungeon will generate (requires you to generate a new world in order to affect anything)")]
        public bool generateFrostFortress;

        [DefaultValue(true)]
        [BackgroundColor(116, 201, 164)]
        [Label("Canyon")]
        [Tooltip("Whether or not the 'Canyon' will generate (requires you to generate a new world in order to affect anything)")]
        public bool generateCanyon;

        public override void OnChanged()
        {
            PenumbralsWorldgen.generateCanyons = generateCanyon;
            PenumbralsWorldgen.generateFrostFortresses = generateFrostFortress;
            PenumbralsWorldgen.generateLakes = generateLakes;
            PenumbralsWorldgen.generateHellevators = generateHellevator;
        }
    }
}
