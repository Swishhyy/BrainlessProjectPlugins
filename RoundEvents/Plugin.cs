using System;
using System.Collections.Generic;
using System.Linq;
using Brainless.Internal;
using Brainless.Internal.Updates;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;
using UnityEngine;
using P = Exiled.Events.Handlers.Player;
using S = Exiled.Events.Handlers.Server;
using Skeleton = Exiled.Events.Handlers.Scp3114;

namespace Brainless.RoundEvents
{
    public sealed class Plugin : Plugin<Config>
    {
        public override string Name => "Project Brainless Round Events";
        public override string Prefix => "project_brainless_round_events";
        public override string Author => "Project Brainless";
        public override Version Version => new Version(0, 1, 0);
        public override Version RequiredExiledVersion => new Version(9, 15, 2);
        public override PluginPriority Priority => PluginPriority.Last;
        public static Plugin Instance { get; private set; }

        private readonly Lifetime lifetime = new Lifetime();
        private readonly InfectionState<Death> state = new InfectionState<Death>();
        private readonly Dictionary<Player, Death> dying = new Dictionary<Player, Death>();
        private readonly Dictionary<Player, NativeStats> stats = new Dictionary<Player, NativeStats>();
        private readonly Dictionary<Player, Vector3> placement = new Dictionary<Player, Vector3>();
        private readonly HashSet<Player> ownedSpawns = new HashSet<Player>();
        private readonly HashSet<string> participants = new HashSet<string>();
        private IDisposable updates;
        private bool nextInfection, scheduled, preparing, announcedWinner;
        private float started;
        public override void OnEnabled()
        {
            Config.Validate();
            Instance = this;
            S.RoundStarted += RoundStarted;
            S.WaitingForPlayers += Cleanup;
            S.RestartingRound += Cleanup;
            S.RoundEnded += RoundEnded;
            S.RespawningTeam += Respawning;
            S.EndingRound.Subscribe(Ending, -1000);
            P.Dying.Subscribe(Dying, -1000);
            P.Died += Died;
            P.Hurting += Hurting;
            P.ChangingRole.Subscribe(ChangingRole, -1000);
            P.Spawning += Spawning;
            P.Spawned += Spawned;
            P.Left += Left;
            Skeleton.Disguising += Disguising;
            Skeleton.TryUseBody += UsingBody;
            updates = PluginUpdates.Start(GetType().Assembly, "RoundEvents", Config.AutoUpdate);
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            S.RoundStarted -= RoundStarted;
            S.WaitingForPlayers -= Cleanup;
            S.RestartingRound -= Cleanup;
            S.RoundEnded -= RoundEnded;
            S.RespawningTeam -= Respawning;
            S.EndingRound -= Ending;
            P.Dying -= Dying;
            P.Died -= Died;
            P.Hurting -= Hurting;
            P.ChangingRole -= ChangingRole;
            P.Spawning -= Spawning;
            P.Spawned -= Spawned;
            P.Left -= Left;
            Skeleton.Disguising -= Disguising;
            Skeleton.TryUseBody -= UsingBody;
            Cleanup();
            updates?.Dispose();
            updates = null;
            Instance = null;
            base.OnDisabled();
        }

        private static bool Real(Player player) => player != null && player.IsConnected && !player.IsHost && !string.IsNullOrWhiteSpace(player.UserId);
        private static bool Eligible(Player player) => Real(player) && player.IsAlive && !player.IsOverwatchEnabled && player.Role.Type != RoleTypeId.Tutorial;
        private static bool Finite(Vector3 point) => !(float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z) || float.IsInfinity(point.x) || float.IsInfinity(point.y) || float.IsInfinity(point.z));
        private bool Infected(Player player) => Eligible(player) && player.Role.Type == RoleTypeId.Scp3114 && state.IsInfected(player.UserId, player.Role.Base);
        private int Humans => Player.List.Count(p => Eligible(p) && participants.Contains(p.UserId) && p.IsHuman);
        private int InfectedCount => Player.List.Count(Infected);

        private void Cleanup()
        {
            state.Stop();
            lifetime.Clear();
            foreach (var player in stats.Keys.ToArray())
                RestoreStats(player);
            dying.Clear();
            participants.Clear();
            placement.Clear();
            ownedSpawns.Clear();
            scheduled = preparing = announcedWinner = false;
        }

        private void RoundEnded(RoundEndedEventArgs ev) => Cleanup();
        private void RoundStarted()
        {
            Cleanup();
            bool selected = nextInfection || Config.AutomaticEvents && UnityEngine.Random.Range(0f, 100f) < Config.AutomaticEventChancePercent;
            nextInfection = false;
            if (!selected || !Config.Infection.Enabled)
                return;
            scheduled = true;
            lifetime.Later(Config.RoundStartDelaySeconds, () =>
            {
                scheduled = false;
                if (!StartInfection(out string response))
                    Log.Info("[PB Round Events] " + response);
            });
        }

        internal bool QueueInfection(out string response)
        {
            response = "Infection is disabled in configuration.";
            if (!Config.Infection.Enabled)
                return false;
            nextInfection = true;
            response = "Infection queued for the next round. Minimum-player checks still apply.";
            return true;
        }

        internal bool StartInfection(out string response)
        {
            response = "Infection requires a running round and must be enabled, with no event already active/scheduled.";
            if (!Config.Infection.Enabled || !Round.IsStarted || Round.IsEnded || state.Active || scheduled)
                return false;
            var players = Player.List.Where(Eligible).OrderBy(_ => UnityEngine.Random.value).ToArray();
            var cfg = Config.Infection;
            if (players.Length < cfg.MinimumPlayers)
            {
                response = $"Infection requires {cfg.MinimumPlayers} living participants; currently {players.Length}.";
                return false;
            }

            int count = InfectionRules.SeedCount(players.Length, cfg.StartingInfectedCount, cfg.StartingInfectedPercent, cfg.MaximumStartingInfected, cfg.MinimumSurvivors);
            if (count < 1)
            {
                response = "No starting infected fit the survivor minimum.";
                return false;
            }

            Cleanup();
            state.Start();
            preparing = true;
            started = Time.time;
            var seeds = new HashSet<Player>(players.Take(count));
            try
            {
                foreach (var player in players)
                {
                    participants.Add(player.UserId);
                    Vector3? point = player.Role is FpcRole && Finite(player.Position) ? player.Position : (Vector3? )null;
                    SetRole(player, seeds.Contains(player), point);
                }
            }
            finally
            {
                preparing = false;
            }

            lifetime.Run(Tick());
            response = $"Infection started: {InfectedCount} SCP-3114 / {Humans} survivors. Infected health {cfg.InfectedHealth:0.#}.";
            Map.Broadcast(12, "<b>INFECTION</b>\nSCP-3114 kills turn humans into skeletons at their death position. Other deaths stay dead.\nSurvivors: eliminate the infected.");
            Log.Info("[PB Round Events] " + response);
            return true;
        }

        private bool SetRole(Player player, bool infected, Vector3? point)
        {
            ownedSpawns.Add(player);
            if (point.HasValue)
                placement[player] = point.Value;
            try
            {
                player.Role.Set(infected ? RoleTypeId.Scp3114 : Config.Infection.SurvivorRole, SpawnReason.ForceClass, point.HasValue ? RoleSpawnFlags.AssignInventory : RoleSpawnFlags.All);
                if (player.Role.Type != (infected ? RoleTypeId.Scp3114 : Config.Infection.SurvivorRole) || !player.IsAlive)
                    return false;
                if (point.HasValue)
                    player.Position = point.Value;
                if (infected)
                {
                    state.MarkInfected(player.UserId, player.Role.Base);
                    CapInfected(player, true);
                    player.ShowHint($"Infected SCP-3114 — {Config.Infection.InfectedHealth:0.#} HP. Kill humans to spread infection.", 8);
                }
                else
                {
                    player.MaxHealth = Config.Infection.SurvivorHealth;
                    player.Health = player.MaxHealth;
                    player.ShowHint("Infection survivor: eliminate the skeletons. Only their direct kills infect you.", 8);
                }

                return true;
            }
            finally
            {
                ownedSpawns.Remove(player);
                placement.Remove(player);
            }
        }

        private void Spawning(SpawningEventArgs ev)
        {
            if (state.Active && ownedSpawns.Contains(ev.Player) && placement.TryGetValue(ev.Player, out var point))
                ev.Position = point;
        }

        private void Spawned(SpawnedEventArgs ev) => Safe.Run(() =>
        {
            if (!state.Active || !Real(ev.Player))
                return;
            state.LifeChanged(ev.Player.UserId, ev.Player.Role.Base);
            if (ownedSpawns.Contains(ev.Player) || !Eligible(ev.Player))
                return;
            participants.Add(ev.Player.UserId);
            if (ev.Player.Role.Type == RoleTypeId.Scp3114)
            {
                state.MarkInfected(ev.Player.UserId, ev.Player.Role.Base);
                CapInfected(ev.Player, true);
                return;
            }

            Vector3? point = ev.Player.Role is FpcRole ? ev.Player.Position : (Vector3? )null;
            SetRole(ev.Player, ev.Player.IsScp, point);
        });
        private void ChangingRole(ChangingRoleEventArgs ev)
        {
            if (!state.Active || !ev.IsAllowed || ownedSpawns.Contains(ev.Player))
                return;
            if (ev.NewRole != RoleTypeId.Spectator && !ev.Player.IsAlive && ev.Reason != SpawnReason.ForceClass)
            {
                bool lateJoin = ev.Reason == SpawnReason.LateJoin && Config.Infection.AllowLateJoinSurvivors && !participants.Contains(ev.Player.UserId);
                if (!lateJoin && Config.Infection.BlockReinforcements)
                {
                    ev.IsAllowed = false;
                    return;
                }

                if (ev.Reason == SpawnReason.LateJoin && !lateJoin)
                {
                    ev.IsAllowed = false;
                    return;
                }
            }

            if (ev.Reason == SpawnReason.LateJoin && !Config.Infection.AllowLateJoinSurvivors)
            {
                ev.IsAllowed = false;
                return;
            }

            if (!(ev.NewRole == RoleTypeId.Spectator && ev.Reason == SpawnReason.Died))
                dying.Remove(ev.Player);
            RestoreStats(ev.Player);
        }

        private void Left(LeftEventArgs ev)
        {
            state.Forget(ev.Player.UserId);
            dying.Remove(ev.Player);
            RestoreStats(ev.Player);
        }

        private void Respawning(RespawningTeamEventArgs ev)
        {
            if (state.Active && Config.Infection.BlockReinforcements)
                ev.IsAllowed = false;
        }

        private void Hurting(HurtingEventArgs ev)
        {
            if (state.Active && Config.Infection.PreventHumanFriendlyFire && ev.Attacker != ev.Player && ev.Player.IsHuman && ev.Attacker?.IsHuman == true)
                ev.IsAllowed = false;
        }

        private void Disguising(Exiled.Events.EventArgs.Scp3114.DisguisingEventArgs ev)
        {
            if (state.Active && !Config.Infection.AllowDisguises && Infected(ev.Player))
                ev.IsAllowed = false;
        }

        private void UsingBody(Exiled.Events.EventArgs.Scp3114.TryUseBodyEventArgs ev)
        {
            if (state.Active && !Config.Infection.AllowDisguises && Infected(ev.Player))
                ev.IsAllowed = false;
        }

        private void Dying(DyingEventArgs ev) => Safe.Run(() =>
        {
            dying.Remove(ev.Player);
            if (!state.Active || preparing || !ev.IsAllowed || !Eligible(ev.Player) || !participants.Contains(ev.Player.UserId))
                return;
            InfectionAttack attack = NativeAttack.Classify(ev.DamageHandler.Base);
            bool owner = Infected(ev.Attacker) && ev.DamageHandler.Base is Scp3114DamageHandler handler && handler.Attacker.Role == RoleTypeId.Scp3114;
            if (!InfectionRules.Infects(state.Active, ev.Player.IsHuman, owner, ev.Player != ev.Attacker, attack, Config.Infection.AllowSlapInfections, Config.Infection.AllowStrangulationInfections))
                return;
            var death = new Death
            {
                Event = ev,
                Handler = ev.DamageHandler.Base,
                Role = ev.Player.Role.Type,
                Position = ev.Player.Position,
                Attack = attack
            };
            if (!Finite(death.Position))
                return;
            dying[ev.Player] = death;
            lifetime.Later(.5f, () =>
            {
                if (dying.TryGetValue(ev.Player, out var current) && ReferenceEquals(current, death))
                    dying.Remove(ev.Player);
            });
        });
        private void Died(DiedEventArgs ev) => Safe.Run(() =>
        {
            if (!dying.TryGetValue(ev.Player, out var death))
                return;
            dying.Remove(ev.Player);
            if (!state.Active || !Round.IsStarted || Round.IsEnded || !Real(ev.Player) || ev.Player.IsAlive || !death.Event.IsAllowed || ev.TargetOldRole != death.Role || !ReferenceEquals(ev.DamageHandler.Base, death.Handler) || NativeAttack.Classify(ev.DamageHandler.Base) != death.Attack)
                return;
            death.Ragdoll = ev.Ragdoll;
            state.LifeChanged(ev.Player.UserId, ev.Player.Role.Base);
            var ticket = state.Queue(ev.Player.UserId, ev.Player.Role.Base, death);
            if (ticket == null)
                return;
            ev.Player.ShowHint($"Infected. You will rise here in {Config.Infection.RespawnDelaySeconds:0.#} seconds.", Config.Infection.RespawnDelaySeconds + 1);
            lifetime.Later(Config.Infection.RespawnDelaySeconds, () => Convert(ev.Player, ticket));
        });
        private void Convert(Player player, PendingInfection<Death> ticket)
        {
            if (!Real(player) || player.UserId != ticket.User)
            {
                state.Cancel(ticket);
                return;
            }

            if (!state.Take(ticket, player.Role.Base) || !Round.IsStarted || Round.IsEnded || player.IsAlive || player.IsOverwatchEnabled || player.Role.Type != RoleTypeId.Spectator)
                return;
            Vector3 point = ticket.Payload.Position + Vector3.up * Config.Infection.SpawnVerticalOffset;
            if (!SetRole(player, true, point))
            {
                Log.Warn("[PB Infection] A confirmed conversion was blocked by the server/another role handler.");
                return;
            }

            if (Config.Infection.RemoveConvertedRagdoll && ticket.Payload.Ragdoll?.Base != null)
                ticket.Payload.Ragdoll.Destroy();
        }

        private void CapInfected(Player player, bool fill)
        {
            if (!stats.TryGetValue(player, out var original) || original.Role != player.Role.Base)
                stats[player] = original = new NativeStats
                {
                    Role = player.Role.Base,
                    Health = player.MaxHealth,
                    Shield = player.MaxHumeShield
                };
            player.MaxHealth = Config.Infection.InfectedHealth;
            player.MaxHumeShield = Config.Infection.InfectedHumeShield;
            player.Health = fill ? player.MaxHealth : Math.Min(player.Health, player.MaxHealth);
            player.HumeShield = fill ? player.MaxHumeShield : Math.Min(player.HumeShield, player.MaxHumeShield);
        }

        private void RestoreStats(Player player)
        {
            if (!stats.TryGetValue(player, out var old))
                return;
            stats.Remove(player);
            if (!player.IsConnected || player.Role.Base != old.Role)
                return;
            player.MaxHealth = old.Health;
            player.MaxHumeShield = old.Shield;
            player.Health = Math.Min(player.Health, player.MaxHealth);
            player.HumeShield = Math.Min(player.HumeShield, player.MaxHumeShield);
        }

        private IEnumerator<float> Tick()
        {
            while (state.Active)
            {
                yield return Timing.WaitForSeconds(.25f);
                Safe.Run(() =>
                {
                    foreach (var pair in stats.ToArray())
                        if (Infected(pair.Key))
                            CapInfected(pair.Key, false);
                        else
                            RestoreStats(pair.Key);
                });
            }
        }

        private void Ending(EndingRoundEventArgs ev)
        {
            if (!state.Active)
                return;
            if (preparing || Time.time - started < Config.Infection.EndingGraceSeconds || Round.IsLocked)
            {
                ev.IsAllowed = false;
                return;
            }

            var result = InfectionRules.Outcome(Humans, InfectedCount, state.PendingCount);
            ev.IsAllowed = result != InfectionOutcome.Continue;
            if (!ev.IsAllowed)
                return;
            ev.LeadingTeam = result == InfectionOutcome.Humans ? LeadingTeam.FacilityForces : result == InfectionOutcome.Infected ? LeadingTeam.Anomalies : LeadingTeam.Draw;
            if (!announcedWinner)
            {
                announcedWinner = true;
                Map.Broadcast(8, result == InfectionOutcome.Humans ? "<b>INFECTION: SURVIVORS WIN</b>" : result == InfectionOutcome.Infected ? "<b>INFECTION: INFECTED WIN</b>" : "<b>INFECTION: DRAW</b>");
            }
        }

        internal string Status() => $"Infection: {(state.Active ? "ACTIVE" : scheduled ? "starting" : "inactive")}; next round: {nextInfection}; humans: {Humans}; infected: {InfectedCount}; pending conversions: {state.PendingCount}.\nInfected HP: {Config.Infection.InfectedHealth}; shield: {Config.Infection.InfectedHumeShield}; automatic events: {Config.AutomaticEvents}.";
        internal void StopEvent()
        {
            nextInfection = false;
            Cleanup();
            Map.Broadcast(5, "Round event stopped. Restart the round to reset roles.");
        }

        internal bool Reload(out string response)
        {
            try
            {
                if (state.Active || scheduled)
                    throw new InvalidOperationException("Stop the active/scheduled event before reloading event tuning.");
                var next = ConfigReload.Read(this);
                next.Validate();
                if (!next.IsEnabled)
                    throw new InvalidOperationException("Enable/disable requires a full server restart.");
                ConfigReload.Apply(Config, next);
                updates?.Dispose();
                updates = PluginUpdates.Start(GetType().Assembly, "RoundEvents", Config.AutoUpdate);
                response = "Round-event settings reloaded.";
                return true;
            }
            catch (Exception error)
            {
                response = "Config unchanged: " + error.Message;
                return false;
            }
        }

        private sealed class Death
        {
            internal DyingEventArgs Event;
            internal PlayerStatsSystem.DamageHandlerBase Handler;
            internal RoleTypeId Role;
            internal Vector3 Position;
            internal InfectionAttack Attack;
            internal Ragdoll Ragdoll;
        }

        private sealed class NativeStats
        {
            internal PlayerRoleBase Role;
            internal float Health, Shield;
        }
    }
}
