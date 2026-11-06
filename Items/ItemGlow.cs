using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Features;
using Exiled.API.Features.Lockers;
using Exiled.API.Features.Pickups;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using LightToy = Exiled.API.Features.Toys.Light;

namespace Brainless.Items
{
    // Custom item pickups glow in their rarity colour (vanilla items too if enabled). Lights are
    // separate toys the plugin moves and destroys itself, so a picked-up or destroyed item
    // never leaves a light behind on clients. Items in closed lockers stay dark.
    public sealed partial class Plugin
    {
        private readonly Dictionary<ushort, Glow> glows = new Dictionary<ushort, Glow>();

        private IEnumerator<float> GlowLoop()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(Math.Max(.1f, Config.Glow.UpdateSeconds));
                Safe.Run(UpdateGlows);
            }
        }

        internal Rarity RarityOf(Pickup pickup)
        {
            var custom = CustomItem.Registered.FirstOrDefault(c => c.TrackedSerials.Contains(pickup.Serial));
            if (custom != null)
                return CustomRarity.TryGet(custom.Id, out var rarity) ? rarity : Rarity.Common;
            return VanillaRarity(pickup.Type);
        }

        internal Rarity VanillaRarity(ItemType type) => Config.Glow.VanillaRarity != null && Config.Glow.VanillaRarity.TryGetValue(type, out var rarity) ? rarity : Rarity.Common;

        private void UpdateGlows()
        {
            var settings = Config.Glow;
            if (!settings.Enabled || !Round.IsStarted)
            {
                ClearGlows();
                return;
            }

            // Pickups waiting in closed lockers stay dark until the chamber opens.
            var hidden = new HashSet<ushort>();
            foreach (var locker in Locker.List)
                foreach (var chamber in locker.Chambers)
                    if (!chamber.IsOpen)
                        foreach (var item in chamber.Base.Content.Concat(chamber.Base.ToBeSpawned))
                            if (item != null)
                                hidden.Add(item.Info.Serial);

            var seen = new HashSet<ushort>();
            // Only custom items glow by default: looking up their own serials is far
            // cheaper than scanning every pickup on the map.
            var candidates = settings.GlowVanillaItems ? Pickup.List.ToArray() : CustomItem.Registered.SelectMany(c => c.TrackedSerials).Distinct().Select(serial => Pickup.Get((ushort)serial)).Where(p => p != null).ToArray();
            foreach (var pickup in candidates)
            {
                if (pickup?.Base == null || !pickup.IsSpawned || hidden.Contains(pickup.Serial))
                    continue;
                var rarity = RarityOf(pickup);
                if (rarity < settings.MinimumRarity)
                    continue;
                seen.Add(pickup.Serial);
                Vector3 position = pickup.Position + Vector3.up * settings.HeightOffset;
                if (glows.TryGetValue(pickup.Serial, out var glow))
                {
                    if (glow.Rarity != rarity)
                    {
                        RemoveGlow(pickup.Serial);
                    }
                    else
                    {
                        if ((glow.Light.Position - position).sqrMagnitude > .0025f)
                            glow.Light.Position = position;
                        continue;
                    }
                }

                if (glows.Count >= settings.MaxLights)
                    continue;
                var light = LightToy.Create(position, null, null, true);
                light.Color = ParseColor(RarityRules.Color(rarity));
                light.Intensity = settings.Intensity;
                light.Range = settings.Range;
                light.ShadowType = LightShadows.None;
                glows[pickup.Serial] = new Glow { Light = light, Rarity = rarity };
            }

            foreach (ushort serial in glows.Keys.Where(s => !seen.Contains(s)).ToArray())
                RemoveGlow(serial);
        }

        private void RemoveGlow(ushort serial)
        {
            if (!glows.TryGetValue(serial, out var glow))
                return;
            glows.Remove(serial);
            Safe.Run(() => glow.Light?.Destroy());
        }

        private void ClearGlows()
        {
            foreach (ushort serial in glows.Keys.ToArray())
                RemoveGlow(serial);
        }

        private static Color ParseColor(string hex) => ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;

        // "Medkit (Uncommon)" when a vanilla item is picked up. Custom items keep their own message.
        private void GlowPickupHint(ItemAddedEventArgs ev) => Safe.Run(() =>
        {
            if (!Config.Glow.PickupHint || ev.Pickup == null || ev.Item == null || ev.Player == null || CustomItem.Registered.Any(c => c.TrackedSerials.Contains(ev.Item.Serial)))
                return;
            var rarity = VanillaRarity(ev.Item.Type);
            ev.Player.ShowHint("<size=26>" + Readable(ev.Item.Type) + " (" + RarityRules.Tag(rarity) + ")</size>", 2);
        });

        // "KeycardZoneManager" -> "Keycard Zone Manager", "GunCOM15" -> "Gun COM15".
        private static string Readable(ItemType type) => System.Text.RegularExpressions.Regex.Replace(type.ToString(), "(?<=[a-z])(?=[A-Z0-9])", " ");

        private sealed class Glow
        {
            internal LightToy Light;
            internal Rarity Rarity;
        }
    }
}
