using Terraria.ModLoader.Config;
using System.ComponentModel;
using Terraria;
using Terraria.Localization;

namespace PenumbralsWorldgen
{
    [BackgroundColor(130 / 5, 230 / 5, 255 / 5, (int)(255f * 0.75f))]
    class PenumbralsWorldgenConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
        {
            return Main.countsAsHostForGameplay[whoAmI];
        }

        [Header("Generation")] // Headers are like titles in a config. You only need to declare a header on the item it should appear over, not every item in the category. 

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateFrostFortress;

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateChastisedChurch;

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateHellevator;

        [Increment(1)]
        [Range(1, 10)]
        [DefaultValue(2)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configHellevatorCount;

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateLakes;

        [Increment(1)]
        [Range(1, 20)]
        [DefaultValue(5)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configLakesCount;

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateFissure;

        [Increment(1)]
        [Range(1, 10)]
        [DefaultValue(2)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configFissureCount;      



        [Header("Legacy")] // Headers are like titles in a config. You only need to declare a header on the item it should appear over, not every item in the category. 

        [DefaultValue(false)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateCanyon;

        [DefaultValue(false)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateLakes_Legacy;



        public override void OnChanged()
        {
            PenumbralsWorldgen.generateFissure = configGenerateFissure;
            PenumbralsWorldgen.fissureCount = configFissureCount;
            PenumbralsWorldgen.generateFrostFortresses = configGenerateFrostFortress;
            PenumbralsWorldgen.generateLakes = configGenerateLakes;
            PenumbralsWorldgen.lakeCount = configLakesCount;
            PenumbralsWorldgen.generateHellevators = configGenerateHellevator;
            PenumbralsWorldgen.hellevatorCount = configHellevatorCount;
            PenumbralsWorldgen.generateChastisedChurch = configGenerateChastisedChurch;

            PenumbralsWorldgen.generateCanyons = configGenerateCanyon;
            PenumbralsWorldgen.generateLakes_Legacy = configGenerateLakes_Legacy;
        }
    }
}
