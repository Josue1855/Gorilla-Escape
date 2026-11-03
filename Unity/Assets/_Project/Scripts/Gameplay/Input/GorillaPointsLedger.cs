using System;
using System.Collections.Generic;
using System.Linq;

namespace GorillaEscape.Gameplay.Input
{
    // Receives official placements from the game; never compares unlike game scores.
    public sealed class GorillaPointsLedger
    {
        private static readonly int[] PointsByPlace = { 10, 7, 5, 3 };
        private readonly Dictionary<int, Entry> players = new Dictionary<int, Entry>();
        private readonly HashSet<string> recordedGames = new HashSet<string>(StringComparer.Ordinal);

        public sealed class Standing
        {
            public int PlayerId { get; internal set; }
            public int Points { get; internal set; }
            public int Firsts { get; internal set; }
            public int Seconds { get; internal set; }
            public int Thirds { get; internal set; }
        }

        private sealed class Entry
        {
            public int Points;
            public readonly int[] Placements = new int[4];
        }

        public GorillaPointsLedger(IEnumerable<int> playerIds)
        {
            if (playerIds == null) throw new ArgumentNullException(nameof(playerIds));
            foreach (int id in playerIds)
            {
                if (id < 1 || id > 4 || players.ContainsKey(id))
                    throw new ArgumentException("Player slots must be unique and between 1 and 4.", nameof(playerIds));
                players.Add(id, new Entry());
            }
            if (players.Count == 0 || players.Count > 4)
                throw new ArgumentException("A tournament requires one to four players.", nameof(playerIds));
        }

        // Placements must already be resolved by the discipline. No partial writes on invalid input.
        public void RecordPlacements(string gameId, IReadOnlyList<int> orderedPlayers)
        {
            if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("Game ID is required.", nameof(gameId));
            if (recordedGames.Contains(gameId)) throw new InvalidOperationException("Game already recorded.");
            if (orderedPlayers == null || orderedPlayers.Count != players.Count)
                throw new ArgumentException("Placements must contain every player.", nameof(orderedPlayers));
            var seen = new HashSet<int>();
            foreach (int id in orderedPlayers)
                if (!players.ContainsKey(id) || !seen.Add(id))
                    throw new ArgumentException("Placements must contain each player exactly once.", nameof(orderedPlayers));

            for (int i = 0; i < orderedPlayers.Count; i++)
            {
                Entry entry = players[orderedPlayers[i]];
                entry.Points += PointsByPlace[i];
                entry.Placements[i]++;
            }
            recordedGames.Add(gameId);
        }

        public Standing[] Standings()
        {
            return players.Select(pair => new Standing {
                PlayerId = pair.Key, Points = pair.Value.Points,
                Firsts = pair.Value.Placements[0], Seconds = pair.Value.Placements[1], Thirds = pair.Value.Placements[2]
            }).OrderByDescending(p => p.Points).ThenByDescending(p => p.Firsts)
              .ThenByDescending(p => p.Seconds).ThenByDescending(p => p.Thirds)
              .ThenBy(p => p.PlayerId).ToArray();
        }

        // Multiple leaders mean that a separately authorized sudden-death result is still required.
        public int[] LeadingPlayers()
        {
            Standing[] sorted = Standings();
            Standing first = sorted[0];
            return sorted.Where(p => p.Points == first.Points && p.Firsts == first.Firsts &&
                p.Seconds == first.Seconds && p.Thirds == first.Thirds).Select(p => p.PlayerId).ToArray();
        }
    }
}
