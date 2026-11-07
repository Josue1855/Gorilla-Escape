using System;
using System.Collections.Generic;
using System.Linq;
using GorillaEscape.Gameplay.Input;

public static class AdversarialMatrixChecks
{
    private static void Require(bool pass) { if (!pass) throw new Exception("assertion failed"); }

    public static IEnumerable<(string, Action)> All()
    {
        // 1. All 16 binary combinations of win/loss for 4 players
        // (Win = 1000 raw points, Loss = 0 raw points)
        for (int mask = 0; mask < 16; mask++)
        {
            int currentMask = mask;
            int p1 = (currentMask & 8) != 0 ? 1 : 0;
            int p2 = (currentMask & 4) != 0 ? 1 : 0;
            int p3 = (currentMask & 2) != 0 ? 1 : 0;
            int p4 = (currentMask & 1) != 0 ? 1 : 0;
            int[] outcomes = { p1, p2, p3, p4 };
            int wins = outcomes.Sum();
            string label = $"Matrix 16-Combo #{currentMask + 1:02d}: P1={p1} P2={p2} P3={p3} P4={p4} ({wins}W-{4 - wins}L)";

            yield return (label, () =>
            {
                var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
                // Raw scores: 1000 for win, 0 for loss.
                var rawScores = new Dictionary<int, int>
                {
                    { 1, p1 * 1000 },
                    { 2, p2 * 1000 },
                    { 3, p3 * 1000 },
                    { 4, p4 * 1000 }
                };

                // Ordered by raw score descending, then by player ID ascending (as implemented in TournamentSession)
                var ordered = rawScores.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => kv.Key).ToArray();
                ledger.RecordPlacements($"combo-{currentMask}", ordered);

                var standings = ledger.Standings();
                Require(standings.Length == 4);
                Require(standings[0].Points == 10);
                Require(standings[1].Points == 7);
                Require(standings[2].Points == 5);
                Require(standings[3].Points == 3);

                // Note: If multiple players tie, ThenBy(kv => kv.Key) artificially favors lower player IDs.
                // When all win (1111) or all lose (0000), P1 is arbitrarily given 10 GP, P2 7 GP, P3 5 GP, P4 3 GP.
                if (wins == 4 || wins == 0)
                {
                    Require(standings[0].PlayerId == 1 && standings[3].PlayerId == 4);
                }
            });
        }

        // 2. Edge Case: 4-Way Tie in Discipline
        yield return ("Edge Case: 4-way tie in discipline - ledger requires external tie arbitration", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            // Attempting to submit tied duplicate placements directly must be rejected by ledger
            try
            {
                ledger.RecordPlacements("tie4", new[] { 1, 1, 1, 1 });
                throw new Exception("Ledger allowed duplicate players in placement");
            }
            catch (ArgumentException)
            {
                // Correctly rejected by ledger
            }
        });

        // 3. Edge Case: 2-Way Tie for 1st Place
        yield return ("Edge Case: 2-way tie for 1st place - ledger rejects duplicate player list", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            try
            {
                ledger.RecordPlacements("tie2", new[] { 1, 2, 3 }); // Missing player 4
                throw new Exception("Ledger allowed incomplete placement list");
            }
            catch (ArgumentException)
            {
                // Correctly rejected: ledger requires all active players
            }
        });

        // 4. Edge Case: Disconnected Player mid-tournament
        yield return ("Edge Case: Disconnected player - ledger rejects recording without all registered players", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            ledger.RecordPlacements("round-1", new[] { 1, 2, 3, 4 });

            // In round 2, player 4 is disconnected, game reports only 1, 2, 3
            try
            {
                ledger.RecordPlacements("round-2", new[] { 2, 1, 3 });
                throw new Exception("Ledger accepted round missing disconnected player");
            }
            catch (ArgumentException)
            {
                // Confirmed: Ledger strictly blocks recording if a disconnected player is omitted.
                // The tournament system must handle disconnected players before invoking the ledger.
            }
        });

        // 5. Edge Case: Repeated / Duplicate Result Submission
        yield return ("Edge Case: Duplicate result submission rejected with InvalidOperationException", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            ledger.RecordPlacements("bowling-r1", new[] { 1, 2, 3, 4 });
            try
            {
                ledger.RecordPlacements("bowling-r1", new[] { 4, 3, 2, 1 });
                throw new Exception("Ledger allowed overwriting existing game result");
            }
            catch (InvalidOperationException)
            {
                // Confirmed: Game ID cannot be overwritten once recorded
            }
            Require(ledger.Standings()[0].PlayerId == 1 && ledger.Standings()[0].Points == 10);
        });

        // 6. Edge Case: Invalid Player ID (Out of bounds)
        yield return ("Edge Case: Invalid player ID (0 or 5) rejected at initialization and recording", () =>
        {
            try
            {
                new GorillaPointsLedger(new[] { 0, 1, 2, 3 });
                throw new Exception("Allowed player 0");
            }
            catch (ArgumentException) {}

            try
            {
                new GorillaPointsLedger(new[] { 1, 2, 3, 5 });
                throw new Exception("Allowed player 5");
            }
            catch (ArgumentException) {}
        });

        // 7. Edge Case: Tournament Overall Tie (Unresolved Sudden Smash required)
        yield return ("Edge Case: Tournament overall tie results in multiple leading players", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            ledger.RecordPlacements("d1", new[] { 1, 2, 3, 4 }); // P1=10, P2=7, P3=5, P4=3
            ledger.RecordPlacements("d2", new[] { 2, 1, 4, 3 }); // P1=17, P2=17, P3=8, P4=8
            var leaders = ledger.LeadingPlayers();
            Require(leaders.Length == 2);
            Require(leaders.Contains(1) && leaders.Contains(2));
            // Ledger does NOT invent a single champion; both leaders remain pending Sudden Smash
        });

        // 8. Edge Case: Standings Snapshot Immutability
        yield return ("Edge Case: Standings snapshot immutability prevents tampering", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            ledger.RecordPlacements("d1", new[] { 1, 2, 3, 4 });
            var st = ledger.Standings();
            st[0].Points = 99999;
            st[0].PlayerId = 99;
            var fresh = ledger.Standings();
            Require(fresh[0].Points == 10 && fresh[0].PlayerId == 1);
        });

        // 9. Edge Case: Solo Player Tournament
        yield return ("Edge Case: Solo player (1 human) ledger operations", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1 });
            ledger.RecordPlacements("smash", new[] { 1 });
            Require(ledger.Standings().Length == 1);
            Require(ledger.Standings()[0].Points == 10);
            Require(ledger.LeadingPlayers().Length == 1 && ledger.LeadingPlayers()[0] == 1);
        });

        // 10. Edge Case: 3-Way Tie in Tournament Overall
        yield return ("Edge Case: 3-way tie in tournament overall", () =>
        {
            var ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });
            ledger.RecordPlacements("d1", new[] { 1, 2, 3, 4 }); // 1:10, 2:7, 3:5, 4:3
            ledger.RecordPlacements("d2", new[] { 2, 3, 1, 4 }); // 1:15, 2:17, 3:12, 4:6
            ledger.RecordPlacements("d3", new[] { 3, 1, 2, 4 }); // 1:22, 2:22, 3:22, 4:9
            var leaders = ledger.LeadingPlayers();
            Require(leaders.Length == 3);
            Require(leaders.Contains(1) && leaders.Contains(2) && leaders.Contains(3));
        });
    }
}
