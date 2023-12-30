using Terraria.ModLoader;

namespace PenumbralsWorldgen
{
	public class PenumbralsWorldgen : Mod
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
        /// Canyons activated in ServerConfig
        /// </summary>
        public static bool generateCanyons;

        /// <summary>
        /// Legacy Lakes activated in ServerConfig
        /// </summary>
        public static bool generateLakes_Legacy;
        
    }
}