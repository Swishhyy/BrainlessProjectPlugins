using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using PlayerRoles;
using UnityEngine;

namespace Brainless.Items
{
    // Revision 15 general items: Energy Drink, Healing Grenade, Decoy Corpse, Teleport
    // Grenade, Motion Tracker and Ammo Box.
    public sealed partial class Plugin
    {
        private readonly Dictionary<Player, Tracking> trackers = new Dictionary<Player, Tracking>();
        private float trackerTick;
        private const float TrackerSeconds = 30, HealRadius = 6, HealAmount = 40;

        private static void EnergyDrinkEffect(Player player)
        {
            player.EnableEffect(EffectType.MovementBoost, 10, 20, false);
            player.EnableEffect(EffectType.Invigorated, 1, 20, false);
            player.ShowHint("<b>Energy Drink</b>\nFaster, with endless stamina for 20 seconds.", 3);
        }

        // Returns true when the item was one of these (handled or refused).
        private bool ActivateGeneral(Player player, Item held, RegisteredItem item)
        {
            switch (item.Id)
            {
                case Items.Config.DecoyCorpse:
                    var spot = Ground(player.Position + player.Rotation * Vector3.forward * .8f);
                    if (Spend(player, held))
                    {
                        Ragdoll.CreateAndSpawn(player.Role.Type, player.Nickname, "Unknown cause of death", spot + Vector3.up * .1f, player.Rotation);
                        player.ShowHint("Decoy corpse dropped.", 3);
                    }

                    return true;
                case Items.Config.AmmoBox:
                    var guns = player.Items.OfType<Firearm>().ToArray();
                    if (guns.Length == 0)
                    {
                        player.ShowHint("You have no guns to refill.", 3);
                        return true;
                    }

                    if (Spend(player, held))
                    {
                        foreach (var type in guns.Select(g => g.AmmoType).Distinct())
                            player.SetAmmo(type, player.GetAmmoLimit(type));
                        player.ShowHint("Reserve ammo refilled.", 3);
                    }

                    return true;
                case Items.Config.MotionTracker:
                    if (trackers.TryGetValue(player, out var current) && current.Until > Time.time)
                    {
                        player.ShowHint($"Tracker already running ({current.Until - Time.time:0}s left).", 2);
                        return true;
                    }

                    if (Spend(player, held))
                        trackers[player] = new Tracking { Until = Time.time + TrackerSeconds, Life = player.Role.Base };
                    return true;
            }

            return false;
        }

        private void DetonateGeneral(Payload payload, Vector3 position)
        {
            switch (payload.Item.Id)
            {
                case Items.Config.HealingGrenade:
                    lifetime.Later(0, () =>
                    {
                        foreach (var human in Player.List.Where(p => p.IsConnected && p.IsAlive && p.IsHuman && (p.Position - position).sqrMagnitude <= HealRadius * HealRadius && World.ClearLine(position + Vector3.up * .3f, p.CameraTransform.position)).ToArray())
                        {
                            human.Heal(HealAmount);
                            human.ShowHint("Healing Grenade: +" + HealAmount + " HP", 2);
                        }
                    });
                    break;
                case Items.Config.TeleportGrenade:
                    var owner = payload.Source.Owner;
                    lifetime.Later(0, () =>
                    {
                        if (owner == null || !owner.IsConnected || !owner.IsAlive || !owner.IsHuman || owner.IsCuffed)
                            return;
                        string why = TeleportSpot(position, owner, out var safe);
                        if (why != null)
                        {
                            owner.ShowHint("Teleport failed: " + why, 3);
                            return;
                        }

                        owner.Position = safe;
                        owner.ShowHint("Teleported!", 2);
                    });
                    break;
            }
        }

        // Where a teleport may land, or why not. Out-of-map spots (on top of the map, above
        // ceilings, inside geometry, past walls) fail at least one check:
        //  - solid floor right under the pop, inside a real room (not the pocket dimension);
        //  - a ceiling overhead everywhere except Surface (open sky in the facility = above it);
        //  - the whole body fits (capsule clear of world geometry);
        //  - the thrower can see the spot (no going over walls, through doors or gaps).
        private string TeleportSpot(Vector3 position, Player owner, out Vector3 destination)
        {
            destination = default;
            Vector3 floor = Ground(position + Vector3.up * .3f);
            if (Math.Abs(floor.y - position.y) > 2.5f || !SolidBelow(position + Vector3.up * .3f))
                return "no floor there.";
            var room = Room.Get(floor);
            if (room == null || room.Type == RoomType.Pocket || room.Type == RoomType.Unknown)
                return "that's not a real room.";
            if (room.Zone != ZoneType.Surface && !HitsGeometry(floor + Vector3.up * .1f, Vector3.up, 40))
                return "that spot is outside the facility.";
            if (Physics.OverlapCapsule(floor + Vector3.up * .45f, floor + Vector3.up * 1.75f, .35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(IsWorld))
                return "not enough room there.";
            Vector3 eye = owner.CameraTransform.position;
            Vector3 target = floor + Vector3.up * 1.2f;
            if (HitsGeometry(eye, target - eye, Vector3.Distance(eye, target)))
                return "you can't see that spot.";
            destination = floor + Vector3.up * 1.1f;
            return null;
        }

        // Geometry only: players, pickups and admin toys don't count as walls or floors.
        private static bool IsWorld(Collider collider) => collider != null && collider.GetComponentInParent<ReferenceHub>() == null && collider.GetComponentInParent<InventorySystem.Items.Pickups.ItemPickupBase>() == null && collider.GetComponentInParent<AdminToys.AdminToyBase>() == null;
        private static bool HitsGeometry(Vector3 origin, Vector3 direction, float distance) => Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).Any(hit => IsWorld(hit.collider));
        private static bool SolidBelow(Vector3 point) => HitsGeometry(point + Vector3.up, Vector3.down, 4);

        private void UpdateTrackers()
        {
            if (trackers.Count == 0 || Time.time < trackerTick)
                return;
            trackerTick = Time.time + 1;
            foreach (var pair in trackers.ToArray())
            {
                var player = pair.Key;
                if (!player.IsConnected || !player.IsAlive || player.Role.Base != pair.Value.Life || Time.time >= pair.Value.Until)
                {
                    trackers.Remove(player);
                    if (player.IsConnected && player.IsAlive)
                        player.ShowHint("Motion Tracker: off.", 2);
                    continue;
                }

                var nearest = Player.List.Where(p => p.IsConnected && p.IsAlive && p.Role.Team == Team.SCPs && p.Role.Type != RoleTypeId.Scp079).OrderBy(p => (p.Position - player.Position).sqrMagnitude).FirstOrDefault();
                string text = nearest == null ? "No SCPs detected." : TrackerLine(player, nearest);
                player.ShowHint("<size=24><b>Motion Tracker</b> (" + (pair.Value.Until - Time.time).ToString("0") + "s)</size>\n<size=22>" + text + "</size>", 1.2f);
            }
        }

        private static string TrackerLine(Player player, Player scp)
        {
            Vector3 offset = scp.Position - player.Position;
            float distance = offset.magnitude;
            Vector3 flat = new Vector3(offset.x, 0, offset.z);
            Vector3 forward = player.CameraTransform.forward;
            forward.y = 0;
            float angle = flat.sqrMagnitude < .01f || forward.sqrMagnitude < .01f ? 0 : Vector3.SignedAngle(forward.normalized, flat.normalized, Vector3.up);
            string[] names = { "ahead", "ahead-right", "right", "behind-right", "behind", "behind-left", "left", "ahead-left" };
            int index = (int)Math.Round(((angle + 360) % 360) / 45) % 8;
            string height = offset.y > 3 ? ", above you" : offset.y < -3 ? ", below you" : "";
            return scp.Role.Name + ": " + distance.ToString("0") + " m, " + names[index] + height;
        }

        private sealed class Tracking
        {
            internal float Until;
            internal PlayerRoleBase Life;
        }
    }
}
