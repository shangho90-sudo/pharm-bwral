using UnityEngine;

namespace PharmaBrawl
{
    // Small fixed grid, reused buffers, routes refreshed on moving targets / broken crates.
    public sealed class ArenaNavigation
    {
        const float Cell = .5f;
        const int W = 71, H = 51, N = W * H;
        readonly ArenaSimulation sim;
        readonly bool[] open = new bool[N];
        readonly int[] queue = new int[N], parent = new int[N];
        readonly Vector2[][] paths = new Vector2[18][];
        readonly int[] cursors = new int[18];
        readonly float[] timers = new float[18];
        readonly Vector2[] goals = new Vector2[18];
        int activeCount = -1;
        public ArenaNavigation(ArenaSimulation simulation) { sim = simulation; Rebuild(); }
        Vector2 Point(int i) => new Vector2(-17.5f + (i % W) * Cell, -12.5f + (i / W) * Cell);
        int Index(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt((p.y + 12.5f) / Cell), 0, H - 1) * W + Mathf.Clamp(Mathf.RoundToInt((p.x + 17.5f) / Cell), 0, W - 1);
        void Rebuild()
        {
            activeCount = 0; foreach (var c in sim.covers) if (c.Active) activeCount++;
            for (int i = 0; i < N; i++) open[i] = !sim.Blocked(Point(i), .5f);
            for (int i = 0; i < timers.Length; i++) timers[i] = 0;
        }
        bool WalkLine(Vector2 a, Vector2 b)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / .2f);
            for (int i = 1; i <= steps; i++) if (sim.Blocked(Vector2.Lerp(a, b, i / (float)steps), .49f)) return false;
            return true;
        }
        int NearestOpen(Vector2 p)
        {
            int cell = Index(p); if (open[cell]) return cell;
            int best = -1; float distance = float.MaxValue;
            for (int i = 0; i < N; i++) if (open[i]) { float d = (Point(i) - p).sqrMagnitude; if (d < distance) { best = i; distance = d; } }
            return best;
        }
        public Vector2 Direction(Vector2 start, Vector2 goal, int id, float dt)
        {
            if (WalkLine(start, goal)) return (goal - start).normalized;
            int count = 0; foreach (var c in sim.covers) if (c.Active) count++;
            if (count != activeCount) Rebuild();
            timers[id] -= dt;
            if (timers[id] <= 0 || (goal - goals[id]).sqrMagnitude > 4 || paths[id] == null)
            {
                paths[id] = FindPath(start, goal); cursors[id] = 0; goals[id] = goal; timers[id] = .7f;
            }
            var path = paths[id]; if (path == null || path.Length == 0) return Vector2.zero;
            int cursor = cursors[id];
            while (cursor < path.Length - 1 && Vector2.Distance(start, path[cursor]) < .28f) cursor++;
            // Skip intermediate cells only when the swept character footprint is clear.
            while (cursor < path.Length - 1 && WalkLine(start, path[cursor + 1])) cursor++;
            cursors[id] = cursor; return (path[cursor] - start).normalized;
        }
        public Vector2[] FindPath(Vector2 start, Vector2 goal)
        {
            int source = NearestOpen(start), destination = NearestOpen(goal);
            if (source < 0 || destination < 0) return new Vector2[0];
            for (int i = 0; i < N; i++) parent[i] = -2;
            int read = 0, write = 0; queue[write++] = source; parent[source] = -1;
            while (read < write && parent[destination] == -2)
            {
                int at = queue[read++], x = at % W, y = at / W;
                if (x > 0) Visit(at - 1, at, ref write);
                if (x < W - 1) Visit(at + 1, at, ref write);
                if (y > 0) Visit(at - W, at, ref write);
                if (y < H - 1) Visit(at + W, at, ref write);
            }
            if (parent[destination] == -2) return new Vector2[0];
            int length = 0; for (int at = destination; at >= 0; at = parent[at]) length++;
            var result = new Vector2[length]; int node = destination;
            for (int i = length - 1; i >= 0; i--) { result[i] = Point(node); node = parent[node]; }
            return result;
        }
        void Visit(int next, int from, ref int write)
        {
            if (!open[next] || parent[next] != -2) return;
            parent[next] = from; queue[write++] = next;
        }
    }
}
