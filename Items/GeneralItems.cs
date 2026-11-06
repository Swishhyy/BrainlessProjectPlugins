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
                        var safe = TeleportSpot(position);
                        if (!safe.HasValue)
                        {
                            owner.ShowHint("Teleport failed: no safe floor there.", 3);
                            return;
                        }

                        owner.Position = safe.Value;
                        owner.ShowHint("Teleported!", 2);
                    });
                    break;
            }
        }

        // A floor with head room, inside a real room, away from the pocket dimension.
        private Vector3? TeleportSpot(Vector3 position)
        {
            Vector3 floor = Ground(position + Vector3.up * .3f);
            if (Math.Abs(floor.y - position.y) > 2.5f)
                return null;
            var room = Room.Get(floor);
            if (room == null || room.Type == RoomType.Pocket)
                return null;
            if (Physics.Raycast(floor + Vector3.up * .1f, Vector3.up, 2, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return null;
            return floor + Vector3.up * 1.1f;
        }

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
