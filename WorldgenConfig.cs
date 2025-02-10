using Terraria.ModLoader.Config;
using System.ComponentModel;
using Terraria;
using Terraria.Localization;

namespace WorldGenMod
{
    [BackgroundColor(r: 130 / 5, g: 230 / 5, b:255 / 5, a:(int)(255f * 0.75f))]
    class WorldgenConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("Generation")] // Headers are like titles in a config. You only need to declare a header on the item it should appear over, not every item in the category. 




        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateFrostFortress;

        [Increment(1)]
        [Range(0, 100)]
        [DefaultValue(25)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configFrostFortressCobwebFilling;




        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateChastisedChurch;

        // OptionStrings makes a string appear as a choice rather than an input field. Remember that users can manually edit json files, so be aware that a value other than the Options in OptionStrings might populate the field.
        [DrawTicks]
        [OptionStrings(new string[] { "Left", "Right", "Both", "Random" })]
        [DefaultValue("Random")]
        public string configChastisedChurchGenerationSide;

        [Increment(1)]
        [Range(0, 100)]
        [DefaultValue(4)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configChastisedChurchCobwebFilling;




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
        public bool configGenerateGoldLake;

        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configSmallerGoldLake;




        [DefaultValue(true)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateFissure;

        [Increment(1)]
        [Range(1, 10)]
        [DefaultValue(2)]
        [Slider] // The Slider attribute makes this field be presented with a slider rather than a text input. The default ticks is 1.
        public int configFissureCount;

        [DefaultValue(false)]
        //[BackgroundColor(116, 201, 164)]
        public bool configMoveFissureToLaterWorldgen;








        [Header("Legacy")] // Headers are like titles in a config. You only need to declare a header on the item it should appear over, not every item in the category. 

        [DefaultValue(false)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateCanyon;





        [DefaultValue(false)]
        //[BackgroundColor(116, 201, 164)]
        public bool configGenerateLakes_Legacy;



        public override void OnChanged()
        {
            WorldGenMod.generateFissure = configGenerateFissure;
            WorldGenMod.fissureCount = configFissureCount;
            WorldGenMod.fissureCreationAtLaterWorldgen = configMoveFissureToLaterWorldgen;

            WorldGenMod.generateFrostFortresses = configGenerateFrostFortress;
            WorldGenMod.configFrostFortressCobwebFilling = configFrostFortressCobwebFilling;

            WorldGenMod.generateLakes = configGenerateLakes;
            WorldGenMod.lakeCount = configLakesCount;
            WorldGenMod.createGoldLake = configGenerateGoldLake;
            WorldGenMod.smallGoldLake = configSmallerGoldLake;

            WorldGenMod.generateHellevators = configGenerateHellevator;
            WorldGenMod.hellevatorCount = configHellevatorCount;

            WorldGenMod.generateChastisedChurch = configGenerateChastisedChurch;
            WorldGenMod.chastisedChurchGenerationSide = configChastisedChurchGenerationSide;
            WorldGenMod.configChastisedChurchCobwebFilling = configChastisedChurchCobwebFilling;


            // legacy
            WorldGenMod.generateCanyons = configGenerateCanyon;
            WorldGenMod.generateLakes_Legacy = configGenerateLakes_Legacy;
        }
    }
}
