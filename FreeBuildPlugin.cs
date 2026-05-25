using Rocket.Core;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using Rocket.Unturned;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using System.Collections.Generic;

namespace FreeBuild
{
    public class FreeBuildPlugin : RocketPlugin<FreeBuildConfiguration>
    {
        private readonly Dictionary<uint, Vector3> barricadeOriginalPositions = new Dictionary<uint, Vector3>();
        private readonly Dictionary<uint, Vector3> structureOriginalPositions = new Dictionary<uint, Vector3>();

        protected override void Load()
        {
            U.Events.OnPlayerConnected += OnPlayerConnected;

            BarricadeManager.onTransformRequested += OnBarricadeTransformRequested;
            StructureManager.onTransformRequested += OnStructureTransformRequested;

            foreach (SteamPlayer steamPlayer in Provider.clients)
            {
                if (steamPlayer?.player != null)
                {
                    TryGrantWorkzone(steamPlayer.player);
                }
            }

            Rocket.Core.Logging.Logger.Log("FreeBuild loaded.");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;

            BarricadeManager.onTransformRequested -= OnBarricadeTransformRequested;
            StructureManager.onTransformRequested -= OnStructureTransformRequested;

            barricadeOriginalPositions.Clear();
            structureOriginalPositions.Clear();

            Rocket.Core.Logging.Logger.Log("FreeBuild unloaded.");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player?.Player == null)
                return;

            TryGrantWorkzone(player.Player);
        }

        private bool HasFreeBuildPermission(UnturnedPlayer player)
        {
            return player != null &&
                   R.Permissions.HasPermission(
                       player,
                       new List<string> { Configuration.Instance.Permission }
                   );
        }

        private void TryGrantWorkzone(Player player)
        {
            if (player == null)
                return;

            UnturnedPlayer uPlayer = UnturnedPlayer.FromPlayer(player);

            if (uPlayer.IsAdmin || HasFreeBuildPermission(uPlayer))
            {
                player.look.sendWorkzoneAllowed(true);
            }
        }

        private void OnBarricadeTransformRequested(
            CSteamID steamID,
            byte x,
            byte y,
            ushort plant,
            uint instanceID,
            ref Vector3 point,
            ref byte angle_x,
            ref byte angle_y,
            ref byte angle_z,
            ref bool shouldAllow)
        {
            if (!shouldAllow)
                return;

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamID);

            if (player == null)
            {
                shouldAllow = false;
                return;
            }

            if (player.IsAdmin)
                return;

            if (!HasFreeBuildPermission(player))
            {
                shouldAllow = false;
                return;
            }

            BarricadeDrop drop = FindBarricade(instanceID);

            if (drop == null)
            {
                shouldAllow = false;
                return;
            }

            BarricadeData data = drop.GetServersideData();

            if (data.owner != steamID.m_SteamID)
            {
                shouldAllow = false;
                return;
            }

            if (drop.asset != null &&
                Configuration.Instance.BlacklistedBarricadeIds.Contains(drop.asset.id))
            {
                shouldAllow = false;
                return;
            }

            if (!barricadeOriginalPositions.ContainsKey(instanceID))
            {
                barricadeOriginalPositions[instanceID] = data.point;
            }

            Vector3 originalPoint = barricadeOriginalPositions[instanceID];

            if (Vector3.Distance(originalPoint, point) > Configuration.Instance.MaxMoveDistance)
            {
                shouldAllow = false;
                point = data.point;
                return;
            }
        }

        private void OnStructureTransformRequested(
            CSteamID steamID,
            byte x,
            byte y,
            uint instanceID,
            ref Vector3 point,
            ref byte angle_x,
            ref byte angle_y,
            ref byte angle_z,
            ref bool shouldAllow)
        {
            if (!shouldAllow)
                return;

            UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamID);

            if (player == null)
            {
                shouldAllow = false;
                return;
            }

            if (player.IsAdmin)
                return;

            if (!HasFreeBuildPermission(player))
            {
                shouldAllow = false;
                return;
            }

            StructureDrop drop = FindStructure(instanceID);

            if (drop == null)
            {
                shouldAllow = false;
                return;
            }

            StructureData data = drop.GetServersideData();

            if (data.owner != steamID.m_SteamID)
            {
                shouldAllow = false;
                return;
            }

            if (drop.asset != null &&
                Configuration.Instance.BlacklistedStructureIds.Contains(drop.asset.id))
            {
                shouldAllow = false;
                return;
            }

            if (!structureOriginalPositions.ContainsKey(instanceID))
            {
                structureOriginalPositions[instanceID] = data.point;
            }

            Vector3 originalPoint = structureOriginalPositions[instanceID];

            if (Vector3.Distance(originalPoint, point) > Configuration.Instance.MaxMoveDistance)
            {
                shouldAllow = false;
                point = data.point;
                return;
            }
        }

        private BarricadeDrop FindBarricade(uint instanceID)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    BarricadeRegion region = BarricadeManager.regions[x, y];

                    foreach (BarricadeDrop drop in region.drops)
                    {
                        if (drop.instanceID == instanceID)
                            return drop;
                    }
                }
            }

            foreach (VehicleBarricadeRegion vehicleRegion in BarricadeManager.vehicleRegions)
            {
                foreach (BarricadeDrop drop in vehicleRegion.drops)
                {
                    if (drop.instanceID == instanceID)
                        return drop;
                }
            }

            return null;
        }

        private StructureDrop FindStructure(uint instanceID)
        {
            for (byte x = 0; x < Regions.WORLD_SIZE; x++)
            {
                for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                {
                    StructureRegion region = StructureManager.regions[x, y];

                    foreach (StructureDrop drop in region.drops)
                    {
                        if (drop.instanceID == instanceID)
                            return drop;
                    }
                }
            }

            return null;
        }
    }
}
