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
        /// Frost Fortresses activated in ServerConfig
        /// </summary>
        public static bool generateFrostFortresses;

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
        /// Canyons activated in ServerConfig
        /// </summary>
        public static bool generateCanyons;

        /// <summary>
        /// Legacy Lakes activated in ServerConfig
        /// </summary>
        public static bool generateLakes_Legacy;
        
    }
}