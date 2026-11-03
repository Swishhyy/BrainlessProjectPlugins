using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using UnityEngine;

namespace Brainless.RoundEvents
{
    // Clear floor spots inside facility rooms for event spawns, away from hazards.
    internal static class FacilitySpawns
    {
        private static readonly HashSet<RoomType> Avoid = new HashSet<RoomType> { RoomType.Pocket, RoomType.Hcz106, RoomType.HczTesla, RoomType.Hcz079, RoomType.EzCollapsedTunnel, RoomType.HczTestRoom, RoomType.Unknown };

        internal static Room[] Rooms(List<ZoneType> zones, System.Random random) => Room.List
            .Where(r => r != null && (zones == null || zones.Count == 0 ? r.Zone == ZoneType.HeavyContainment || r.Zone == ZoneType.Entrance : zones.Contains(r.Zone)) && !Avoid.Contains(r.Type))
            .OrderBy(_ => random.Next())
            .ToArray();

        // A random clear standing point in the room, or null if none was found.
        internal static Vector3? PointIn(Room room, System.Random random, int attempts = 10)
        {
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                float radius = .5f + attempt % 5;
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                Vector3 probe = room.Position + new Vector3(Mathf.Cos(angle) * radius, 3, Mathf.Sin(angle) * radius);
                foreach (var hit in Physics.RaycastAll(probe, Vector3.down, 7, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                {
                    if (hit.collider.GetComponentInParent<ReferenceHub>() != null || hit.collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() != null || hit.collider.GetComponentInParent<AdminToys.AdminToyBase>() != null || hit.normal.y < .8f)
                        continue;
                    Vector3 candidate = hit.point + Vector3.up * 1.1f;
                    if (Room.Get(candidate) != room || Physics.CheckCapsule(hit.point + Vector3.up * .4f, hit.point + Vector3.up * 1.65f, .3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        break;
                    return candidate;
                }
            }

            return null;
        }

        // A random clear point anywhere in the zones (first 12 rooms tried).
        internal static Vector3? Anywhere(List<ZoneType> zones, System.Random random)
        {
            foreach (var room in Rooms(zones, random).Take(12))
            {
                var point = PointIn(room, random);
                if (point.HasValue)
                    return point;
            }

            return null;
        }
    }
}
