using Terraria.ModLoader;

namespace WorldGenMod
{
	public class WorldGenMod : Mod
	{
        /// <summary>
		/// Abandoned Hellevators activated in ServerConfig
		/// </summary>
        public static bool generateHellevators;

        /// <summary>
        /// Number of Hellevators to be created during worldgen set in ServerConfig
        /// </summary>
        public static int hellevatorCount;

        /// <summary>
        /// Underground Lakes activated in ServerConfig
        /// </summary>
        public static bool generateLakes;

        /// <summary>
        /// Number of Underground Lakes to be created during worldgen set in ServerConfig
        /// </summary>
        public static int lakeCount;

        /// <summary>
        /// If the golden lake shall be created or not
        /// </summary>
        public static bool createGoldLake;

        /// <summary>
        /// The created Gold Lake will have less gold ore (around 2/3 less)
        /// </summary>
        public static bool smallGoldLake;

        /// <summary>
        /// Frost Fortresses activated in ServerConfig
        /// </summary>
        public static bool generateFrostFortresses;

        /// <summary>
        /// How dense the cobweb in the Frost Fortresses shall generate (0..100%)
        /// </summary>
        public static int configFrostFortressCobwebFilling;

        /// <summary>
        /// Fissure activated in ServerConfig
        /// </summary>
        public static bool generateFissure;

        /// <summary>
        /// Number of Fissure to be created during worldgen set in ServerConfig
        /// </summary>
        public static int fissureCount;

        /// <summary>
        /// Chastised Church activated in ServerConfig
        /// </summary>
        public static bool generateChastisedChurch;

        /// <summary>
        /// Slider for Chastised Church generation preference
        /// </summary>
        public static string chastisedChurchGenerationSide;

        /// <summary>
        /// How dense the cobweb in the Chastised Church shall generate (0..100%)
        /// </summary>
        public static int configChastisedChurchCobwebFilling;



        /// <summary>
        /// Canyons activated in ServerConfig
        /// </summary>
        public static bool generateCanyons;

        /// <summary>
        /// Legacy Lakes activated in ServerConfig
        /// </summary>
        public static bool generateLakes_Legacy;
        
    }
}