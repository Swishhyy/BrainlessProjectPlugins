using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using PlayerRoles;

namespace Brainless.RoundEvents
{
    // Lobby event vote. When the automatic event roll hits, the lobby pauses and players
    // vote for one of the eligible events or "No event" with .vote in the console. Anyone
    // can also sit an event round out (.vote optout) and spectate it instead.
    public sealed partial class Plugin
    {
        internal enum Ballot
        {
            None = 0,
            Infection = 1,
            Deathmatch = 2,
            Juggernaut = 3
        }

        private readonly Dictionary<string, Ballot> votes = new Dictionary<string, Ballot>();
        private readonly HashSet<string> sitOut = new HashSet<string>();
        private readonly List<Ballot> options = new List<Ballot>();
        private CoroutineHandle voteLoop;
        private bool voteActive, lobbyRolled, lockedByVote, lobbyReady;
        private float voteEnds;

        internal static bool SitsOut(Player player) => player?.UserId != null && Instance != null && Instance.sitOut.Contains(player.UserId);

        private void StartVoteLoop()
        {
            // A reload during a lobby that already exists can roll right away.
            lobbyReady = !Round.IsStarted && TryLobbyTimer(out _);
            Timing.KillCoroutines(voteLoop);
            voteLoop = Timing.RunCoroutine(VoteLoop());
        }

        private void StopVoteLoop()
        {
            Timing.KillCoroutines(voteLoop);
            EndVoteLock();
        }

        // A new lobby: forget the last round's ballots and sit-outs.
        private void LobbyReset()
        {
            // The lobby countdown only exists once the server is waiting for players.
            lobbyReady = true;
            votes.Clear();
            sitOut.Clear();
            options.Clear();
            voteActive = false;
            lobbyRolled = false;
            EndVoteLock();
        }

        private void EndVoteLock()
        {
            if (lockedByVote)
                Round.IsLobbyLocked = false;
            lockedByVote = false;
        }

        private IEnumerator<float> VoteLoop()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1);
                try
                {
                    VoteTick();
                }
                catch (Exception exception)
                {
                    Log.Error("[PB Round Events] Event vote: " + exception);
                }
            }
        }

        private void VoteTick()
        {
            if (Round.IsStarted)
                return;
            if (voteActive)
            {
                ShowTally();
                var voters = Player.List.Where(p => p.IsConnected && !p.IsHost && !p.IsNPC && !string.IsNullOrEmpty(p.UserId)).ToArray();
                bool everyone = voters.Length > 0 && voters.All(p => votes.ContainsKey(p.UserId) || sitOut.Contains(p.UserId));
                if (everyone || UnityEngine.Time.time >= voteEnds)
                    FinishVote();
                return;
            }

            // Roll once per lobby, just before the countdown would start the round. The
            // countdown object does not exist during startup or a round restart.
            if (!lobbyReady || !TryLobbyTimer(out int timer))
                return;
            if (lobbyRolled || timer < 0 || timer > Config.Vote.TriggerSecondsLeft)
                return;
            lobbyRolled = true;
            if (!Config.AutomaticEvents || eventThisRound || nextInfection || nextArena != ArenaMode.None || UnityEngine.Random.Range(0f, 100f) >= Config.AutomaticEventChancePercent)
                return;
            var pool = EligibleBallots(true);
            if (pool.Count == 0)
                return;
            if (!Config.Vote.Enabled)
            {
                // Voting off: the old behaviour, one random eligible event.
                Queue(pool[UnityEngine.Random.Range(0, pool.Count)]);
                return;
            }

            StartVote(pool);
        }

        private static bool TryLobbyTimer(out int timer)
        {
            try
            {
                timer = Round.LobbyWaitingTime;
                return true;
            }
            catch (NullReferenceException)
            {
                timer = -1;
                return false;
            }
        }

        // A round restart tears the lobby down until the next WaitingForPlayers.
        private void LobbyGone() => lobbyReady = false;

        private List<Ballot> EligibleBallots(bool automatic)
        {
            int players = Player.List.Count(p => p.IsConnected && !p.IsHost && !p.IsNPC);
            var pool = new List<Ballot>();
            if (Config.Infection.Enabled && players >= (automatic ? Config.Infection.AutomaticMinimumPlayers : Config.Infection.MinimumPlayers))
                pool.Add(Ballot.Infection);
            if (Config.Deathmatch.Enabled && players >= (automatic ? Config.Deathmatch.AutomaticMinimumPlayers : Config.Deathmatch.MinimumPlayers))
                pool.Add(Ballot.Deathmatch);
            if (Config.Juggernaut.Enabled && players >= (automatic ? Config.Juggernaut.AutomaticMinimumPlayers : Config.Juggernaut.MinimumPlayers))
                pool.Add(Ballot.Juggernaut);
            return pool;
        }

        // Staff: bpevent vote, during the lobby (uses the staff minimum player counts).
        internal bool StaffVote(out string response)
        {
            if (Round.IsStarted || voteActive)
            {
                response = voteActive ? "A vote is already running." : "Event votes run in the lobby, before the round starts.";
                return false;
            }

            var pool = EligibleBallots(false);
            if (pool.Count == 0)
            {
                response = "No event is enabled with enough players.";
                return false;
            }

            lobbyRolled = true;
            StartVote(pool);
            response = "Event vote started (" + (int)Config.Vote.DurationSeconds + "s).";
            return true;
        }

        private void StartVote(List<Ballot> pool)
        {
            options.Clear();
            options.AddRange(pool);
            votes.Clear();
            voteActive = true;
            voteEnds = UnityEngine.Time.time + Math.Max(5, Config.Vote.DurationSeconds);
            if (!Round.IsLobbyLocked)
            {
                Round.IsLobbyLocked = true;
                lockedByVote = true;
            }

            string list = string.Join("  ", options.Select(o => "<b>.vote " + (int)o + "</b> " + o)) + "  <b>.vote 0</b> No event";
            Map.Broadcast(15, "<size=34><b>EVENT VOTE!</b> Open the console (~) and pick:</size>\n<size=28>" + list + "\nDon't want to play the event? <b>.vote optout</b></size>", Broadcast.BroadcastFlags.Normal, true);
        }

        private void ShowTally()
        {
            int left = Math.Max(0, (int)Math.Ceiling(voteEnds - UnityEngine.Time.time));
            var lines = options.Concat(new[] { Ballot.None }).Select(o => "<b>." + "vote " + (int)o + "</b> " + Label(o) + ": " + votes.Count(v => v.Value == o));
            string text = "<size=24><b>Event vote</b> — " + left + "s left</size>\n<size=20>" + string.Join("\n", lines) + "\nSitting out: " + sitOut.Count + "</size>";
            foreach (var player in Player.List.Where(p => p.IsConnected && !p.IsHost))
                player.ShowHint(text, 1.2f);
        }

        private static string Label(Ballot ballot) => ballot == Ballot.None ? "No event" : ballot.ToString();

        private void FinishVote()
        {
            voteActive = false;
            EndVoteLock();
            var counts = options.Concat(new[] { Ballot.None }).ToDictionary(o => o, o => votes.Count(v => v.Value == o));
            Ballot winner;
            if (counts.Values.Sum() == 0)
                winner = options[UnityEngine.Random.Range(0, options.Count)];
            else
            {
                int best = counts.Values.Max();
                var tied = counts.Where(c => c.Value == best).Select(c => c.Key).ToArray();
                winner = tied[UnityEngine.Random.Range(0, tied.Length)];
            }

            Queue(winner);
            string result = winner == Ballot.None ? "No event this round." : "<b>" + winner + "</b> wins! It starts shortly after the round begins.";
            Map.Broadcast(8, "<size=30>Event vote: " + result + "</size>" + (winner != Ballot.None && sitOut.Count > 0 ? "\n<size=24>" + sitOut.Count + " player(s) sitting it out.</size>" : ""), Broadcast.BroadcastFlags.Normal, true);
        }

        private void Queue(Ballot ballot)
        {
            nextInfection = ballot == Ballot.Infection;
            nextArena = ballot == Ballot.Deathmatch ? ArenaMode.Deathmatch : ballot == Ballot.Juggernaut ? ArenaMode.Juggernaut : ArenaMode.None;
        }

        // .vote <number|name>
        internal bool CastVote(Player player, string choice, out string response)
        {
            if (!voteActive)
            {
                response = "There is no event vote right now. Votes happen in the lobby when an event round comes up.";
                return false;
            }

            Ballot? ballot = null;
            string key = (choice ?? "").Trim().ToLowerInvariant();
            if (int.TryParse(key, out int number) && (number == 0 || options.Contains((Ballot)number)))
                ballot = (Ballot)number;
            else if (key == "none" || key == "no" || key == "noevent")
                ballot = Ballot.None;
            else
                foreach (var option in options.Where(o => o.ToString().ToLowerInvariant().StartsWith(key)))
                    ballot = option;
            if (ballot == null || string.IsNullOrEmpty(key))
            {
                response = "Pick one of: " + string.Join(", ", options.Concat(new[] { Ballot.None }).Select(o => (int)o + " = " + Label(o))) + ".";
                return false;
            }

            votes[player.UserId] = ballot.Value;
            response = "You voted for " + Label(ballot.Value) + ". You can change your vote until the timer runs out.";
            return true;
        }

        // .vote optout / optin: sit the next (or current) event out and spectate.
        internal bool SetSitOut(Player player, bool sit, out string response)
        {
            if (!sit)
            {
                sitOut.Remove(player.UserId);
                response = InfectionRunning || arena.Active ? "You will no longer sit out events, starting with the next one." : "You'll play the event round.";
                return true;
            }

            sitOut.Add(player.UserId);
            if ((InfectionRunning || arena.Active) && player.IsAlive)
                player.Role.Set(RoleTypeId.Spectator, Exiled.API.Enums.SpawnReason.ForceClass);
            response = "You're sitting out the event round and will spectate it. Use .vote optin to change your mind before it starts.";
            return true;
        }

        internal string VoteStatus(Player player)
        {
            string sitting = SitsOut(player) ? " You're sitting out the event." : "";
            if (!voteActive)
                return "No event vote right now." + sitting + " Use .vote optout to sit out an event round.";
            string mine = votes.TryGetValue(player.UserId, out var vote) ? " Your vote: " + Label(vote) + "." : " You haven't voted yet.";
            return "Event vote: " + string.Join(", ", options.Concat(new[] { Ballot.None }).Select(o => (int)o + " = " + Label(o) + " (" + votes.Count(v => v.Value == o) + ")")) + "." + mine + sitting;
        }

        // Players sitting out are moved to spectator just before an event starts.
        private void ParkSitOuts()
        {
            foreach (var player in Player.List.Where(p => p.IsConnected && p.IsAlive && SitsOut(p)).ToArray())
            {
                player.Role.Set(RoleTypeId.Spectator, Exiled.API.Enums.SpawnReason.ForceClass);
                player.ShowHint("You're sitting out this event. Enjoy the show!", 5);
            }
        }
    }
}
