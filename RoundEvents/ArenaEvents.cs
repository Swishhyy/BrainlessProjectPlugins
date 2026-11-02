using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using LightContainmentZoneDecontamination;
using MEC;
using PlayerRoles;
using UnityEngine;

namespace Brainless.RoundEvents
{
    public enum ArenaMode
    {
        None,
        Deathmatch,
        Juggernaut
    }

    internal enum ArenaSide
    {
        Chaos,
        Mtf
    }

    // Deathmatch: Chaos holds the facility, MTF raids from the surface. Both sides
    // respawn in waves; first to the kill target (or ahead at the time limit) wins.
    // Juggernaut: one heavily armored Chaos player against NTF hunters. Whoever
    // kills the Juggernaut becomes the next one; most points wins.
    internal sealed class ArenaEvents
    {
        private readonly Plugin plugin;
        private readonly Lifetime lifetime = new Lifetime();
        private readonly System.Random random = new System.Random();
        private readonly Dictionary<string, ArenaSide> sides = new Dictionary<string, ArenaSide>();
        private readonly Dictionary<string, int> playerScores = new Dictionary<string, int>();
        private readonly Dictionary<string, string> names = new Dictionary<string, string>();
        private readonly HashSet<string> waiting = new HashSet<string>();
        private readonly HashSet<Player> owned = new HashSet<Player>();
        private readonly Dictionary<Player, Vector3> placement = new Dictionary<Player, Vector3>();
        private int chaosKills, mtfKills;
        private float started, nextWave, nextReveal, nextScoreboard;
        private string juggernaut, pendingJuggernaut;
        private bool finished, previousWarheadLock;
        private DecontaminationController.DecontaminationStatus? previousDecontamination;
        private LeadingTeam winner = LeadingTeam.Draw;

        internal ArenaEvents(Plugin plugin) => this.plugin = plugin;

        internal ArenaMode Mode { get; private set; }
        internal bool Active => Mode != ArenaMode.None;
        private Config Config => plugin.Config;

        internal string Status()
        {
            if (!Active)
                return "Arena: inactive.";
            float left = Math.Max(0, Limit - (Time.time - started));
            if (Mode == ArenaMode.Deathmatch)
                return $"Deathmatch: Chaos {chaosKills} / MTF {mtfKills} (target {Config.Deathmatch.KillTarget}); {Clock(left)} left; waiting to respawn: {waiting.Count}.";
            return $"Juggernaut: {(names.TryGetValue(juggernaut ?? "", out string name) ? name : "none")}; leader {Leader()}; {Clock(left)} left; waiting to respawn: {waiting.Count}.";
        }

        private float Limit => Mode == ArenaMode.Deathmatch ? Config.Deathmatch.TimeLimitSeconds : Config.Juggernaut.TimeLimitSeconds;
        private static string Clock(float seconds) => $"{(int)seconds / 60}:{(int)seconds % 60:00}";
        private static bool Real(Player player) => player != null && player.IsConnected && !player.IsHost && !string.IsNullOrWhiteSpace(player.UserId) && !player.IsOverwatchEnabled;

        internal bool Start(ArenaMode mode, out string response)
        {
            response = "Arena events need a running round, no other active event and the mode enabled in configuration.";
            if (Active || mode == ArenaMode.None || !Round.IsStarted || Round.IsEnded || plugin.InfectionRunning)
                return false;
            bool enabled = mode == ArenaMode.Deathmatch ? Config.Deathmatch.Enabled : Config.Juggernaut.Enabled;
            int minimum = mode == ArenaMode.Deathmatch ? Config.Deathmatch.MinimumPlayers : Config.Juggernaut.MinimumPlayers;
            var players = Player.List.Where(Real).OrderBy(_ => random.Next()).ToArray();
            if (!enabled)
                return false;
            if (players.Length < minimum)
            {
                response = $"{mode} needs {minimum} players; currently {players.Length}.";
                return false;
            }

            Clear();
            Mode = mode;
            started = Time.time;
            nextWave = Time.time + RespawnWave;
            nextReveal = Time.time + Config.Juggernaut.RevealIntervalSeconds;
            previousWarheadLock = Warhead.IsLocked;
            Warhead.IsLocked = true;
            if (DecontaminationController.Singleton != null)
            {
                previousDecontamination = DecontaminationController.Singleton.DecontaminationOverride;
                DecontaminationController.Singleton.DecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
            }

            if (mode == ArenaMode.Deathmatch)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    sides[players[i].UserId] = i % 2 == 0 ? ArenaSide.Chaos : ArenaSide.Mtf;
                    names[players[i].UserId] = players[i].Nickname;
                    SpawnDeathmatch(players[i]);
                }

                Map.Broadcast(12, "<b><color=#2ecc40>CHAOS</color> HAS TAKEN THE FACILITY</b>\n<color=#3fa9f5>MTF</color>: storm the facility and take it back.\n" + $"First to {Config.Deathmatch.KillTarget} kills wins. Respawn waves every {Config.Deathmatch.RespawnWaveSeconds:0}s.");
                response = $"Deathmatch started: {sides.Count(s => s.Value == ArenaSide.Chaos)} Chaos / {sides.Count(s => s.Value == ArenaSide.Mtf)} MTF.";
            }
            else
            {
                foreach (var player in players)
                {
                    names[player.UserId] = player.Nickname;
                    playerScores[player.UserId] = 0;
                }

                MakeJuggernaut(players[0]);
                foreach (var player in players.Skip(1))
                    SpawnHunter(player);
                Map.Broadcast(12, $"<b><color=#e74c3c>JUGGERNAUT</color></b>\n{players[0].Nickname} is the Juggernaut. Hunters: bring them down.\nKill the Juggernaut to become the next one.");
                response = $"Juggernaut started: {players[0].Nickname} vs {players.Length - 1} hunters.";
            }

            lifetime.Run(Tick());
            Log.Info("[PB Round Events] " + response);
            return true;
        }

        internal void Stop()
        {
            if (!Active)
                return;
            Clear();
        }

        private void Clear()
        {
            lifetime.Clear();
            if (Active)
            {
                Warhead.IsLocked = previousWarheadLock;
                if (previousDecontamination.HasValue && DecontaminationController.Singleton != null)
                    DecontaminationController.Singleton.DecontaminationOverride = previousDecontamination.Value;
            }

            Mode = ArenaMode.None;
            previousDecontamination = null;
            sides.Clear();
            playerScores.Clear();
            names.Clear();
            waiting.Clear();
            owned.Clear();
            placement.Clear();
            chaosKills = mtfKills = 0;
            juggernaut = pendingJuggernaut = null;
            finished = false;
            winner = LeadingTeam.Draw;
        }

        private float RespawnWave => Mode == ArenaMode.Deathmatch ? Config.Deathmatch.RespawnWaveSeconds : Config.Juggernaut.RespawnWaveSeconds;

        private void SpawnDeathmatch(Player player)
        {
            var cfg = Config.Deathmatch;
            if (!sides.TryGetValue(player.UserId, out var side))
                return;
            if (side == ArenaSide.Chaos)
            {
                var role = cfg.ChaosRoles.Count == 0 ? RoleTypeId.ChaosRifleman : cfg.ChaosRoles[random.Next(cfg.ChaosRoles.Count)];
                Spawn(player, role, FacilityPoint(cfg.ChaosZones), cfg.SpawnProtectionSeconds);
            }
            else
            {
                var role = cfg.MtfRoles.Count == 0 ? RoleTypeId.NtfSergeant : cfg.MtfRoles[random.Next(cfg.MtfRoles.Count)];
                Spawn(player, role, null, cfg.SpawnProtectionSeconds);
            }
        }

        private void SpawnHunter(Player player)
        {
            var cfg = Config.Juggernaut;
            var role = cfg.HunterRoles.Count == 0 ? RoleTypeId.NtfSergeant : cfg.HunterRoles[random.Next(cfg.HunterRoles.Count)];
            Spawn(player, role, null, cfg.SpawnProtectionSeconds);
        }

        private void MakeJuggernaut(Player player, Vector3? at = null)
        {
            var cfg = Config.Juggernaut;
            juggernaut = player.UserId;
            pendingJuggernaut = null;
            waiting.Remove(player.UserId);
            if (!Spawn(player, cfg.JuggernautRole, at ?? FacilityPoint(cfg.JuggernautZones), cfg.SpawnProtectionSeconds))
                return;
            foreach (var armor in player.Items.Where(i => i is Armor).ToArray())
                player.RemoveItem(armor);
            player.AddItem(ItemType.ArmorHeavy);
            player.AddItem(ItemType.Adrenaline);
            foreach (var gun in player.Items.OfType<Firearm>())
            {
                gun.MagazineAmmo = gun.MaxMagazineAmmo;
                player.SetAmmo(gun.AmmoType, player.GetAmmoLimit(gun.AmmoType));
            }

            int hunters = Player.List.Count(p => Real(p) && p.UserId != juggernaut);
            player.MaxHealth = cfg.BaseHealth + cfg.HealthPerHunter * hunters;
            player.Health = player.MaxHealth;
            player.ShowHint($"<b>You are the JUGGERNAUT</b>\n{player.MaxHealth:0} HP. Every kill heals you {cfg.KillHeal:0}.", 8);
        }

        private bool Spawn(Player player, RoleTypeId role, Vector3? point, float protection)
        {
            owned.Add(player);
            if (point.HasValue)
                placement[player] = point.Value;
            try
            {
                player.Role.Set(role, SpawnReason.ForceClass, point.HasValue ? RoleSpawnFlags.AssignInventory : RoleSpawnFlags.All);
                if (!player.IsAlive || player.Role.Type != role)
                    return false;
                if (point.HasValue)
                    player.Position = point.Value;
                if (protection > 0)
                    player.EnableEffect(EffectType.SpawnProtected, protection);
                waiting.Remove(player.UserId);
                return true;
            }
            finally
            {
                owned.Remove(player);
                placement.Remove(player);
            }
        }

        // A random, clear floor point inside the given zones, away from hazards.
        private Vector3? FacilityPoint(List<ZoneType> zones)
        {
            var avoid = new HashSet<RoomType> { RoomType.Pocket, RoomType.Hcz106, RoomType.HczTesla, RoomType.Hcz079, RoomType.EzCollapsedTunnel, RoomType.HczTestRoom, RoomType.Unknown };
            var rooms = Room.List.Where(r => r != null && (zones.Count == 0 ? r.Zone == ZoneType.HeavyContainment || r.Zone == ZoneType.Entrance : zones.Contains(r.Zone)) && !avoid.Contains(r.Type)).OrderBy(_ => random.Next()).ToArray();
            foreach (var room in rooms.Take(12))
                for (int attempt = 0; attempt < 10; attempt++)
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

        internal void Spawning(SpawningEventArgs ev)
        {
            if (Active && owned.Contains(ev.Player) && placement.TryGetValue(ev.Player, out var point))
                ev.Position = point;
        }

        internal void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (!Active || !ev.IsAllowed || owned.Contains(ev.Player) || !Real(ev.Player))
                return;
            // Native waves, late joins, escapes and SCP-049 revives cannot add players
            // outside the event; they wait for the next arena wave instead.
            if (ev.Reason == SpawnReason.Respawn || ev.Reason == SpawnReason.RespawnMiniwave || ev.Reason == SpawnReason.LateJoin || ev.Reason == SpawnReason.Revived || ev.Reason == SpawnReason.Escaped)
            {
                ev.IsAllowed = false;
                Enlist(ev.Player);
            }
        }

        private void Enlist(Player player)
        {
            names[player.UserId] = player.Nickname;
            if (Mode == ArenaMode.Deathmatch && !sides.ContainsKey(player.UserId))
                sides[player.UserId] = sides.Count(s => s.Value == ArenaSide.Chaos) <= sides.Count(s => s.Value == ArenaSide.Mtf) ? ArenaSide.Chaos : ArenaSide.Mtf;
            if (Mode == ArenaMode.Juggernaut && !playerScores.ContainsKey(player.UserId))
                playerScores[player.UserId] = 0;
            waiting.Add(player.UserId);
        }

        internal void Respawning(RespawningTeamEventArgs ev)
        {
            if (Active)
                ev.IsAllowed = false;
        }

        internal void Ending(EndingRoundEventArgs ev)
        {
            if (!Active)
                return;
            ev.IsAllowed = finished;
            if (finished)
                ev.LeadingTeam = winner;
        }

        internal void Left(LeftEventArgs ev)
        {
            if (!Active)
                return;
            waiting.Remove(ev.Player.UserId);
            if (ev.Player.UserId == juggernaut)
                juggernaut = null;
        }

        internal void Died(DiedEventArgs ev) => Safe.Run(() =>
        {
            if (!Active || finished || !Real(ev.Player))
                return;
            var victim = ev.Player;
            var killer = ev.Attacker != null && ev.Attacker != victim && Real(ev.Attacker) ? ev.Attacker : null;
            if (Mode == ArenaMode.Deathmatch)
            {
                if (!sides.TryGetValue(victim.UserId, out var lost))
                    return;
                waiting.Add(victim.UserId);
                if (killer != null && sides.TryGetValue(killer.UserId, out var side) && side != lost)
                {
                    if (side == ArenaSide.Chaos)
                        chaosKills++;
                    else
                        mtfKills++;
                    killer.ShowHint($"+1  <color=#2ecc40>Chaos {chaosKills}</color> / <color=#3fa9f5>MTF {mtfKills}</color>", 2);
                }

                victim.ShowHint($"Respawning with the next wave (every {Config.Deathmatch.RespawnWaveSeconds:0}s).", 4);
                int target = Config.Deathmatch.KillTarget;
                if (chaosKills >= target || mtfKills >= target)
                    Finish();
                return;
            }

            waiting.Add(victim.UserId);
            if (victim.UserId == juggernaut)
            {
                juggernaut = null;
                if (killer != null)
                {
                    Score(killer, Config.Juggernaut.PointsForJuggernautKill);
                    pendingJuggernaut = killer.UserId;
                    Vector3 at = killer.Position;
                    Map.Broadcast(6, $"<b>{killer.Nickname}</b> took down {victim.Nickname} and is the new <color=#e74c3c>JUGGERNAUT</color>!");
                    lifetime.Later(1, () =>
                    {
                        if (Active && !finished && killer.IsConnected && pendingJuggernaut == killer.UserId)
                            MakeJuggernaut(killer, killer.IsAlive ? killer.Position : at);
                    });
                }
            }
            else if (killer != null && killer.UserId == juggernaut)
            {
                Score(killer, Config.Juggernaut.PointsPerKill);
                killer.Heal(Config.Juggernaut.KillHeal);
            }

            if (!finished && playerScores.Values.DefaultIfEmpty(0).Max() >= Config.Juggernaut.ScoreTarget)
                Finish();
        });

        private void Score(Player player, int points)
        {
            playerScores[player.UserId] = (playerScores.TryGetValue(player.UserId, out int score) ? score : 0) + points;
            player.ShowHint($"+{points} point(s): {playerScores[player.UserId]} / {Config.Juggernaut.ScoreTarget}", 2);
        }

        private string Leader()
        {
            if (playerScores.Count == 0)
                return "nobody";
            var best = playerScores.OrderByDescending(p => p.Value).First();
            return (names.TryGetValue(best.Key, out string name) ? name : best.Key) + " (" + best.Value + ")";
        }

        private void Finish()
        {
            if (finished)
                return;
            finished = true;
            if (Mode == ArenaMode.Deathmatch)
            {
                winner = chaosKills > mtfKills ? LeadingTeam.ChaosInsurgency : mtfKills > chaosKills ? LeadingTeam.FacilityForces : LeadingTeam.Draw;
                Map.Broadcast(10, winner == LeadingTeam.ChaosInsurgency ? $"<b><color=#2ecc40>CHAOS HOLDS THE FACILITY</color></b>\nChaos {chaosKills} - MTF {mtfKills}" : winner == LeadingTeam.FacilityForces ? $"<b><color=#3fa9f5>MTF RETAKES THE FACILITY</color></b>\nMTF {mtfKills} - Chaos {chaosKills}" : $"<b>DRAW</b>\n{chaosKills} - {mtfKills}");
            }
            else
            {
                winner = LeadingTeam.Draw;
                Map.Broadcast(10, $"<b>JUGGERNAUT OVER</b>\nWinner: {Leader()}");
            }

            lifetime.Later(3, () => Round.EndRound(true));
        }

        private IEnumerator<float> Tick()
        {
            while (Active)
            {
                yield return Timing.WaitForSeconds(1);
                Safe.Run(Update);
            }
        }

        private void Update()
        {
            if (!Active || finished)
                return;
            float now = Time.time;
            if (now - started >= Limit)
            {
                Finish();
                return;
            }

            if (now >= nextWave)
            {
                nextWave = now + RespawnWave;
                foreach (var player in Player.List.Where(p => Real(p) && !p.IsAlive && waiting.Contains(p.UserId)).ToArray())
                {
                    if (Mode == ArenaMode.Deathmatch)
                        SpawnDeathmatch(player);
                    else if (player.UserId != pendingJuggernaut)
                        SpawnHunter(player);
                }
            }

            if (Mode == ArenaMode.Juggernaut)
                UpdateJuggernaut(now);
            if (now >= nextScoreboard)
            {
                nextScoreboard = now + 15;
                string clock = Clock(Math.Max(0, Limit - (now - started)));
                string text = Mode == ArenaMode.Deathmatch ? $"<color=#2ecc40>Chaos {chaosKills}</color> / <color=#3fa9f5>MTF {mtfKills}</color>  |  first to {Config.Deathmatch.KillTarget}  |  {clock} left" : $"Leader: {Leader()}  |  first to {Config.Juggernaut.ScoreTarget}  |  {clock} left";
                foreach (var player in Player.List.Where(p => Real(p) && p.IsAlive))
                    player.ShowHint(text, 3);
            }
        }

        private void UpdateJuggernaut(float now)
        {
            var current = juggernaut == null ? null : Player.List.FirstOrDefault(p => p.UserId == juggernaut);
            if ((current == null || !current.IsAlive || !current.IsConnected) && pendingJuggernaut == null)
            {
                // Lost to a fall, the environment or a disconnect: crown a random player.
                var next = Player.List.Where(p => Real(p) && p.IsAlive).OrderBy(_ => random.Next()).FirstOrDefault() ?? Player.List.Where(Real).OrderBy(_ => random.Next()).FirstOrDefault();
                if (next != null)
                {
                    MakeJuggernaut(next, next.IsAlive ? next.Position : (Vector3?)null);
                    Map.Broadcast(5, $"<b>{next.Nickname}</b> is the new <color=#e74c3c>JUGGERNAUT</color>!");
                }

                return;
            }

            if (current == null || now < nextReveal)
                return;
            nextReveal = now + Config.Juggernaut.RevealIntervalSeconds;
            string where = current.CurrentRoom == null ? current.Zone.ToString() : current.Zone + " - " + current.CurrentRoom.Type;
            foreach (var hunter in Player.List.Where(p => Real(p) && p.IsAlive && p.UserId != juggernaut))
                hunter.ShowHint($"<color=#e74c3c>Juggernaut</color> last seen: {where}", 4);
        }
    }
}
