using Rocket.API;
using System.Collections.Generic;

namespace FreeBuild
{
    public class FreeBuildConfiguration : IRocketPluginConfiguration
    {
        public string Permission;
        public float MaxMoveDistance;
        public List<ushort> BlacklistedBarricadeIds;
        public List<ushort> BlacklistedStructureIds;

        public void LoadDefaults()
        {
            Permission = "freebuild.use";
            MaxMoveDistance = 15f;

            BlacklistedBarricadeIds = new List<ushort>();
            BlacklistedStructureIds = new List<ushort>();
        }
    }
}
