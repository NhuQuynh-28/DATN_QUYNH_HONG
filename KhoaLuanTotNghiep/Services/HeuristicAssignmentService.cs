using KhoaLuanTotNghiep.Models;

namespace KhoaLuanTotNghiep.Services
{
    // ══════════════════════════════════════════════════════
    // DTOs for Assignment
    // ══════════════════════════════════════════════════════

    public class ZoneNode
    {
        public int    ZoneId     { get; set; }
        public string ZoneName   { get; set; } = "";
        public double CenterLat  { get; set; }
        public double CenterLng  { get; set; }
        public int    ForecastOrders    { get; set; }
        public int    ForecastCustomers { get; set; }
    }

    public class DriverSlot
    {
        public int    DriverId   { get; set; }
        public string DriverName { get; set; } = "";
        public List<ZoneNode> Zones { get; set; } = new();

        public int TotalOrders    => Zones.Sum(z => z.ForecastOrders);
        public int TotalCustomers => Zones.Sum(z => z.ForecastCustomers);
        public double TotalDistance { get; set; } // Tổng kc tới tâm
    }

    public class AssignmentSolution
    {
        public List<DriverSlot> Slots     { get; set; } = new();
        public double           Objective { get; set; }
        public double           PenaltyOrders    { get; set; }
        public double           PenaltyCustomers { get; set; }
        public double           Compactness      { get; set; }
        public string           Phase            { get; set; } = ""; 
        public int              Iteration        { get; set; }
    }

    public class AssignmentResult
    {
        public AssignmentSolution Best        { get; set; } = new();
        public AssignmentSolution GreedyInit  { get; set; } = new();
        public double ImprovementPct          { get; set; }
        public long   ElapsedMs               { get; set; }
        public List<double> ObjectiveHistory  { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════
    // Main Service
    // ══════════════════════════════════════════════════════

    public class HeuristicAssignmentService
    {
        // ── Objective weights
        private const double Alpha = 1.0;   
        private const double Beta  = 1.0;   
        private const double Gamma = 0.001; 

        // ── ALNS parameters
        private const int    MaxIterations    = 600;
        private const double InitTemperature  = 50.0;
        private const double CoolingRate      = 0.995;
        private const double DestroyRatio     = 0.20;  
        private const double ScoreImprove     = 3.0;
        private const double ScoreAccept      = 1.0;
        private const double ScoreReject      = 0.1;

        private readonly Random _rng = new();

        // ══════════════════════════════════════════════════════
        // Public Entry Point
        // ══════════════════════════════════════════════════════

        public AssignmentResult RunFullPipeline(
            List<DriverSlot> drivers,
            List<ZoneNode>   zones,
            int maxOrdersPerDriver   = int.MaxValue,
            int maxCustomersPerDriver = int.MaxValue)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var history = new List<double>();

            // ── Phase 1: Greedy Initialization
            var greedy = GreedyInitialize(drivers, zones);
            EvaluateSolution(greedy, maxOrdersPerDriver, maxCustomersPerDriver);
            greedy.Phase = "Greedy";
            double initObj = greedy.Objective;

            // ── Phase 2: Local Search (fast improvement)
            var localSol = CloneSolution(greedy);
            RunLocalSearch(localSol, maxOrdersPerDriver, maxCustomersPerDriver, history);
            localSol.Phase = "LocalSearch";

            // ── Phase 3: ALNS (deep optimization)
            var best = CloneSolution(localSol);
            RunALNS(best, zones, maxOrdersPerDriver, maxCustomersPerDriver, history);
            best.Phase = "ALNS";

            sw.Stop();

            double improvement = initObj > 0
                ? (initObj - best.Objective) / initObj * 100.0
                : 0.0;

            return new AssignmentResult
            {
                Best            = best,
                GreedyInit      = greedy,
                ImprovementPct  = Math.Round(improvement, 2),
                ElapsedMs       = sw.ElapsedMilliseconds,
                ObjectiveHistory = history
            };
        }

        // ══════════════════════════════════════════════════════
        // Phase 1: Greedy Initialization
        // ══════════════════════════════════════════════════════

        private AssignmentSolution GreedyInitialize(List<DriverSlot> drivers, List<ZoneNode> zones)
        {
            // Deep clone drivers (empty)
            var sol = new AssignmentSolution();
            foreach (var d in drivers)
                sol.Slots.Add(new DriverSlot { DriverId = d.DriverId, DriverName = d.DriverName });

            var unassigned = new HashSet<int>(zones.Select(z => z.ZoneId));
            var zoneMap    = zones.ToDictionary(z => z.ZoneId);

            // Pick random seed polygon for each driver
            var allIds = zones.Select(z => z.ZoneId).ToList();
            var seeds  = allIds.OrderBy(_ => _rng.Next())
                               .Take(sol.Slots.Count)
                               .ToList();

            for (int i = 0; i < sol.Slots.Count && i < seeds.Count; i++)
            {
                var seedId = seeds[i];
                sol.Slots[i].Zones.Add(zoneMap[seedId]);
                unassigned.Remove(seedId);
            }

            // Sequential nearest-neighbor expansion
            while (unassigned.Count > 0)
            {
                // Find driver with fewest zones (balance count first)
                var driver = sol.Slots.OrderBy(d => d.TotalOrders).First();

                // Find closest unassigned zone to this driver's centroid
                var driverCentroid = GetCentroid(driver.Zones);
                int nearest = unassigned
                    .OrderBy(zid => EuclideanDist(driverCentroid, (zoneMap[zid].CenterLat, zoneMap[zid].CenterLng)))
                    .First();

                driver.Zones.Add(zoneMap[nearest]);
                unassigned.Remove(nearest);
            }

            return sol;
        }

        // ══════════════════════════════════════════════════════
        // Phase 2: Local Search (Swap + Relocate)
        // ══════════════════════════════════════════════════════

        private void RunLocalSearch(
            AssignmentSolution sol,
            int maxO, int maxC,
            List<double> history,
            int maxIter = 200)
        {
            bool improved = true;
            int  iter     = 0;

            while (improved && iter < maxIter)
            {
                improved = false;
                iter++;

                // ── Try Relocate: move one zone from one driver to another
                for (int i = 0; i < sol.Slots.Count; i++)
                {
                    for (int zi = sol.Slots[i].Zones.Count - 1; zi >= 0; zi--)
                    {
                        for (int j = 0; j < sol.Slots.Count; j++)
                        {
                            if (i == j) continue;
                            var zone = sol.Slots[i].Zones[zi];

                            double oldObj = sol.Objective;
                            sol.Slots[i].Zones.RemoveAt(zi);
                            sol.Slots[j].Zones.Add(zone);
                            EvaluateSolution(sol, maxO, maxC);

                            if (sol.Objective < oldObj)
                            {
                                improved = true;
                                history.Add(sol.Objective);
                                goto nextIter; // accept and restart
                            }
                            else
                            {
                                // Revert
                                sol.Slots[j].Zones.Remove(zone);
                                sol.Slots[i].Zones.Insert(zi, zone);
                                sol.Objective = oldObj;
                            }
                        }
                    }
                }

                // ── Try Swap: exchange one zone between two drivers
                for (int i = 0; i < sol.Slots.Count; i++)
                {
                    for (int j = i + 1; j < sol.Slots.Count; j++)
                    {
                        if (sol.Slots[i].Zones.Count == 0 || sol.Slots[j].Zones.Count == 0) continue;
                        int zi = _rng.Next(sol.Slots[i].Zones.Count);
                        int zj = _rng.Next(sol.Slots[j].Zones.Count);

                        double oldObj = sol.Objective;
                        (sol.Slots[i].Zones[zi], sol.Slots[j].Zones[zj])
                            = (sol.Slots[j].Zones[zj], sol.Slots[i].Zones[zi]);
                        EvaluateSolution(sol, maxO, maxC);

                        if (sol.Objective < oldObj)
                        {
                            improved = true;
                            history.Add(sol.Objective);
                            goto nextIter;
                        }
                        else
                        {
                            // Revert swap
                            (sol.Slots[i].Zones[zi], sol.Slots[j].Zones[zj])
                                = (sol.Slots[j].Zones[zj], sol.Slots[i].Zones[zi]);
                            sol.Objective = oldObj;
                        }
                    }
                }

                nextIter:;
            }
        }

        // ══════════════════════════════════════════════════════
        // Phase 3: ALNS
        // ══════════════════════════════════════════════════════

        private void RunALNS(
            AssignmentSolution best,
            List<ZoneNode> allZones,
            int maxO, int maxC,
            List<double> history)
        {
            var current    = CloneSolution(best);
            var zoneMap    = allZones.ToDictionary(z => z.ZoneId);

            // ── Operator scores (adaptive weights)
            // Destroy: 0=Random, 1=Worst
            // Repair:  0=Greedy, 1=Regret2
            double[] destroyScores = { 1.0, 1.0 };
            double[] repairScores  = { 1.0, 1.0 };

            double temperature = InitTemperature;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                // ── Select operators by roulette wheel
                int dOp = RouletteSelect(destroyScores);
                int rOp = RouletteSelect(repairScores);

                var candidate = CloneSolution(current);

                // ── Destroy
                var removed = dOp == 0
                    ? DestroyRandom(candidate)
                    : DestroyWorst(candidate, maxO, maxC);

                // ── Repair
                if (rOp == 0)
                    RepairGreedy(candidate, removed, zoneMap);
                else
                    RepairRegret2(candidate, removed, zoneMap);

                EvaluateSolution(candidate, maxO, maxC);
                candidate.Iteration = iter + 1;

                // ── Acceptance (Simulated Annealing criterion)
                double delta = candidate.Objective - current.Objective;
                bool accept  = delta < 0 ||
                               _rng.NextDouble() < Math.Exp(-delta / temperature);

                double scoreReward;
                if (candidate.Objective < best.Objective)
                {
                    // New global best
                    best = CloneSolution(candidate);
                    best.Phase = "ALNS";
                    history.Add(best.Objective);
                    scoreReward = ScoreImprove;
                }
                else if (accept)
                {
                    scoreReward = ScoreAccept;
                }
                else
                {
                    scoreReward = ScoreReject;
                }

                if (accept) current = candidate;

                // ── Update operator scores
                destroyScores[dOp] = destroyScores[dOp] * 0.9 + scoreReward * 0.1;
                repairScores[rOp]  = repairScores[rOp]  * 0.9 + scoreReward * 0.1;

                // ── Cool down
                temperature *= CoolingRate;
            }

            // Copy best back into parameter (ref semantics via list mutation)
            best.Phase = "ALNS";
            var slots = best.Slots;
        }

        // ══════════════════════════════════════════════════════
        // Destroy Operators
        // ══════════════════════════════════════════════════════

        private List<ZoneNode> DestroyRandom(AssignmentSolution sol)
        {
            var removed = new List<ZoneNode>();
            foreach (var slot in sol.Slots)
            {
                int k = Math.Max(1, (int)Math.Ceiling(slot.Zones.Count * DestroyRatio));
                var toRemove = slot.Zones.OrderBy(_ => _rng.Next()).Take(k).ToList();
                foreach (var z in toRemove) { slot.Zones.Remove(z); removed.Add(z); }
            }
            return removed;
        }

        private List<ZoneNode> DestroyWorst(AssignmentSolution sol, int maxO, int maxC)
        {
            var removed = new List<ZoneNode>();
            // For each driver that is over-loaded, remove highest-load zones first
            foreach (var slot in sol.Slots)
            {
                if (slot.TotalOrders > maxO || slot.TotalCustomers > maxC)
                {
                    int k = Math.Max(1, (int)Math.Ceiling(slot.Zones.Count * DestroyRatio));
                    var toRemove = slot.Zones
                        .OrderByDescending(z => z.ForecastOrders + z.ForecastCustomers)
                        .Take(k).ToList();
                    foreach (var z in toRemove) { slot.Zones.Remove(z); removed.Add(z); }
                }
            }
            // If nothing was removed (no violations), fall back to random
            if (removed.Count == 0)
                return DestroyRandom(sol);
            return removed;
        }

        // ══════════════════════════════════════════════════════
        // Repair Operators
        // ══════════════════════════════════════════════════════

        private void RepairGreedy(AssignmentSolution sol, List<ZoneNode> removed, Dictionary<int, ZoneNode> zoneMap)
        {
            foreach (var zone in removed)
            {
                // Assign to driver with best (lowest) marginal cost
                var best  = sol.Slots[0];
                double bestCost = InsertionCost(best, zone);
                foreach (var slot in sol.Slots.Skip(1))
                {
                    double cost = InsertionCost(slot, zone);
                    if (cost < bestCost) { bestCost = cost; best = slot; }
                }
                best.Zones.Add(zone);
            }
        }

        private void RepairRegret2(AssignmentSolution sol, List<ZoneNode> removed, Dictionary<int, ZoneNode> zoneMap)
        {
            // Regret-2: insert zone where the regret (cost difference between 1st and 2nd best) is highest
            var uninserted = new List<ZoneNode>(removed);
            while (uninserted.Count > 0)
            {
                ZoneNode? bestZone  = null;
                DriverSlot? bestSlot = null;
                double maxRegret    = double.MinValue;

                foreach (var zone in uninserted)
                {
                    var costs = sol.Slots
                        .Select(s => (Slot: s, Cost: InsertionCost(s, zone)))
                        .OrderBy(x => x.Cost)
                        .ToList();

                    double regret = costs.Count >= 2
                        ? costs[1].Cost - costs[0].Cost
                        : costs[0].Cost;

                    if (regret > maxRegret)
                    {
                        maxRegret = regret;
                        bestZone  = zone;
                        bestSlot  = costs[0].Slot;
                    }
                }

                if (bestZone != null && bestSlot != null)
                {
                    bestSlot.Zones.Add(bestZone);
                    uninserted.Remove(bestZone);
                }
            }
        }

        // ── Cost of inserting zone into a driver slot
        private double InsertionCost(DriverSlot slot, ZoneNode zone)
        {
            double loadCost = slot.TotalOrders + zone.ForecastOrders
                            + slot.TotalCustomers + zone.ForecastCustomers;

            if (slot.Zones.Count == 0)
                return loadCost;

            var centroid = GetCentroid(slot.Zones);
            double distCost = EuclideanDist(centroid, (zone.CenterLat, zone.CenterLng));
            return loadCost * 0.01 + distCost;
        }

        // ══════════════════════════════════════════════════════
        // Evaluation
        // ══════════════════════════════════════════════════════

        private void EvaluateSolution(AssignmentSolution sol, int maxO, int maxC)
        {
            double penO = 0, penC = 0, compact = 0;
            double avgOrders    = sol.Slots.Average(s => s.TotalOrders);
            double avgCustomers = sol.Slots.Average(s => s.TotalCustomers);

            foreach (var slot in sol.Slots)
            {
                // Soft constraint: deviation from average (balance)
                penO += Math.Abs(slot.TotalOrders    - avgOrders);
                penC += Math.Abs(slot.TotalCustomers - avgCustomers);

                // Hard constraint: over max
                if (slot.TotalOrders    > maxO) penO += (slot.TotalOrders    - maxO)    * 2;
                if (slot.TotalCustomers > maxC) penC += (slot.TotalCustomers - maxC)    * 2;

                // Compactness: sum of distances to centroid
                if (slot.Zones.Count > 1)
                {
                    var centroid = GetCentroid(slot.Zones);
                    double slotDist = 0;
                    foreach (var z in slot.Zones)
                    {
                        var sc = (z.CenterLat, z.CenterLng);
                        double d = EuclideanDist(centroid, sc);
                        slotDist += d;
                    }
                    slot.TotalDistance = slotDist;
                    compact += slotDist;
                }
                else
                {
                    slot.TotalDistance = 0;
                }
            }

            sol.PenaltyOrders    = penO;
            sol.PenaltyCustomers = penC;
            sol.Compactness      = compact;
            sol.Objective        = penO * Alpha + penC * Beta + compact * Gamma;
        }

        // ══════════════════════════════════════════════════════
        // Geometry Helpers
        // ══════════════════════════════════════════════════════

        private static (double Lat, double Lng) GetCentroid(List<ZoneNode> zones)
        {
            if (zones.Count == 0) return (0, 0);
            return (zones.Average(z => z.CenterLat), zones.Average(z => z.CenterLng));
        }

        /// Euclidean distance in (lat, lng) space — adequate for small areas
        private static double EuclideanDist((double Lat, double Lng) a, (double Lat, double Lng) b)
        {
            double dLat = a.Lat - b.Lat;
            double dLng = a.Lng - b.Lng;
            return Math.Sqrt(dLat * dLat + dLng * dLng);
        }

        // ══════════════════════════════════════════════════════
        // Utilities
        // ══════════════════════════════════════════════════════

        private int RouletteSelect(double[] scores)
        {
            double total = scores.Sum();
            double r     = _rng.NextDouble() * total;
            double cum   = 0;
            for (int i = 0; i < scores.Length; i++)
            {
                cum += scores[i];
                if (r <= cum) return i;
            }
            return scores.Length - 1;
        }

        private static AssignmentSolution CloneSolution(AssignmentSolution src)
        {
            return new AssignmentSolution
            {
                Objective        = src.Objective,
                PenaltyOrders    = src.PenaltyOrders,
                PenaltyCustomers = src.PenaltyCustomers,
                Compactness      = src.Compactness,
                Phase            = src.Phase,
                Iteration        = src.Iteration,
                Slots = src.Slots.Select(s => new DriverSlot
                {
                    DriverId   = s.DriverId,
                    DriverName = s.DriverName,
                    Zones      = s.Zones.ToList()
                }).ToList()
            };
        }
    }
}
