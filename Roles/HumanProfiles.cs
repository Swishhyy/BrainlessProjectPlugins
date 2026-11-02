using System;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Pickups;
using Exiled.API.Structs;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Roles
{
    public sealed partial class Plugin
    {
        private bool rogueAlertQueued;
        internal const string RogueFriendlyFire = "BrainlessRogue";

        // Rogues stay real Guard/Scientist roles. EXILED's per-player friendly-fire
        // table lets them damage, and be damaged by, every role including their own.
        private void ConfigureRogue(Player player, ActiveRole state)
        {
            player.SessionVariables[RoleMarkers.Rogue] = player.Role.Base;
            state.PreviousUniqueRole = player.UniqueRole;
            player.UniqueRole = RogueFriendlyFire;
            player.CustomRoleFriendlyFireMultiplier[RogueFriendlyFire] = Enum.GetValues(typeof(RoleTypeId)).Cast<RoleTypeId>().Distinct().ToDictionary(r => r, r => 1f);
            if (Config.AnnounceRogue && Round.IsStarted && Round.ElapsedTime.TotalSeconds < 10 && !rogueAlertQueued)
            {
                rogueAlertQueued = true;
                lifetime.Later((float)Math.Max(0, 7 - Round.ElapsedTime.TotalSeconds), () =>
                {
                    if (Round.IsStarted && Round.ElapsedTime.TotalSeconds < 10 && active.Any(p => p.Value.Definition.Neutral && p.Key.IsAlive))
                        Exiled.API.Features.Cassie.MessageTranslated("attention all personnel . dangerous personnel have been detected in the entrance zone . exercise caution", "Security alert: a rogue employee has been detected in Entrance. They are hostile to all factions.");
                });
            }
        }

        private static void RemoveRogue(Player player, ActiveRole state)
        {
            if (!state.Definition.Neutral)
                return;
            if (player.SessionVariables.TryGetValue(RoleMarkers.Rogue, out var marked) && ReferenceEquals(marked, state.Role.Base))
                player.SessionVariables.Remove(RoleMarkers.Rogue);
            player.CustomRoleFriendlyFireMultiplier.Remove(RogueFriendlyFire);
            if (player.UniqueRole == RogueFriendlyFire)
                player.UniqueRole = state.PreviousUniqueRole ?? string.Empty;
        }

        private bool TryProfileSpawn(Player player, RoleSpawnArea area, out Vector3 position)
        {
            if (area == RoleSpawnArea.MicroRoom)
                return TryOutsideMicroRoom(player, out position);
            var rooms = Room.List.Where(r => area == RoleSpawnArea.MicroRoom ? r.Type == RoomType.HczHid : r.Zone == ZoneType.Entrance).OrderBy(r => r.Type == RoomType.EzGateA || r.Type == RoomType.EzGateB ? 0 : r.Type == RoomType.EzShelter ? 1 : 2).ThenBy(_ => random.Next()).ToArray();
            foreach (var room in rooms)
            {
                var micro = area == RoleSpawnArea.MicroRoom ? Pickup.List.FirstOrDefault(p => p.Type == ItemType.MicroHID && Room.Get(p.Position) == room) : null;
                Vector3 center = micro?.Position ?? room.Position;
                for (int attempt = 0; attempt < 28; attempt++)
                {
                    float radius = area == RoleSpawnArea.MicroRoom && micro != null ? 1.2f + attempt % 3 * .3f : 1 + attempt % 7;
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    Vector3 probe = center + new Vector3(Mathf.Cos(angle) * radius, 3, Mathf.Sin(angle) * radius);
                    foreach (var hit in Physics.RaycastAll(probe, Vector3.down, 7, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                    {
                        if (hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() != null || hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() != null || hit.normal.y < .8f)
                            continue;
                        Vector3 candidate = hit.point + Vector3.up * 1.1f;
                        if (Room.Get(candidate) != room || Physics.CheckCapsule(hit.point + Vector3.up * .4f, hit.point + Vector3.up * 1.65f, .3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                            break;
                        if (area == RoleSpawnArea.Entrance && Player.List.Any(p => p != player && p.IsAlive && (p.Position - candidate).sqrMagnitude < Config.RogueSpawnSeparation * Config.RogueSpawnSeparation))
                            break;
                        position = candidate;
                        return true;
                    }
                }
            }

            position = Vector3.zero;
            return false;
        }

        // Containment Engineer: a clear floor point in a room next to the HCZ Micro
        // H.I.D. room, a few meters out from its door, never inside the room itself.
        private bool TryOutsideMicroRoom(Player player, out Vector3 position)
        {
            var hid = Room.List.FirstOrDefault(r => r.Type == RoomType.HczHid);
            if (hid != null)
                foreach (var door in hid.Doors.OrderBy(_ => random.Next()))
                {
                    var outside = door.Rooms?.FirstOrDefault(r => r != null && r != hid);
                    if (outside == null)
                        continue;
                    Vector3 away = door.Position - hid.Position;
                    away.y = 0;
                    if (away.sqrMagnitude < .01f)
                        away = outside.Position - door.Position;
                    away.y = 0;
                    away.Normalize();
                    Vector3 side = Vector3.Cross(Vector3.up, away);
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        Vector3 probe = door.Position + away * (1.5f + attempt % 4 * .75f) + side * ((attempt / 4) - 1) * .8f + Vector3.up * 2;
                        foreach (var hit in Physics.RaycastAll(probe, Vector3.down, 5, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                        {
                            if (hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() != null || hit.normal.y < .8f)
                                continue;
                            Vector3 candidate = hit.point + Vector3.up * 1.1f;
                            if (Room.Get(candidate) != outside || Physics.CheckCapsule(hit.point + Vector3.up * .4f, hit.point + Vector3.up * 1.65f, .3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                                break;
                            position = candidate;
                            return true;
                        }
                    }
                }

            position = Vector3.zero;
            return false;
        }
    }
}
