using System.Collections.Generic;
using UnityEngine;

namespace PharmaBrawl
{
    public enum ArenaTheme { Village, Laboratory, Desert, Alpine }
    public enum ArenaProp { CapsuleWall, LabWall, Sandstone, Container, Crate, Rock, Water, Bush, Bridge, Ice }
    // These footprints are the source of truth for BOTH art and simulation.
    public sealed class ArenaMap
    {
        public sealed class Feature
        {
            public ArenaProp kind;
            public Vector2 position, size;
            public bool blocksMovement, blocksShots, breakable;
        }
        public readonly ArenaTheme theme;
        public readonly string name, key, description;
        public readonly List<Feature> features = new List<Feature>();
        public static readonly string[] Keys = { "village", "lab", "desert", "alpine" };
        public static readonly string[] Names = { "약국마을", "네온 약품 연구소", "사막 약초 오아시스", "알파인 의약품 보급 기지" };
        public Vector2 Spawn(int id) => new Vector2((id % 3 - 1) * 3.2f, id < 3 ? -11.1f : 11.1f);
        public ArenaMap(int index)
        {
            index = Mathf.Clamp(index, 0, 3); theme = (ArenaTheme)index; name = Names[index]; key = Keys[index];
            description = index == 0 ? "길·다리·수풀 이동 가능 / 수로·캡슐 벽 이동 불가" : index == 1 ? "바닥·발광 식물 이동 가능 / 실험 수조·격벽 이동 불가" : index == 2 ? "모래·약초·세 다리 이동 가능 / 강·유적 벽 이동 불가" : "눈길·얼음·수풀 이동 가능 / 컨테이너·바위 이동 불가";
            if (theme == ArenaTheme.Village) Village();
            if (theme == ArenaTheme.Laboratory) Laboratory();
            if (theme == ArenaTheme.Desert) Desert();
            if (theme == ArenaTheme.Alpine) Alpine();
        }
        void Add(ArenaProp kind, float x, float z, float w, float d, bool solid = true)
        {
            features.Add(new Feature { kind = kind, position = new Vector2(x, z), size = new Vector2(w, d), blocksMovement = solid, blocksShots = solid && kind != ArenaProp.Water, breakable = kind == ArenaProp.Crate });
        }
        void Bush(float x, float z, float w, float d) => Add(ArenaProp.Bush, x, z, w, d, false);
        void Village()
        {
            for (int s = -1; s <= 1; s += 2)
            {
                // Side canals split at a usable wooden bridge.
                Add(ArenaProp.Water, s * 14.2f, 2.6f, 3.4f, 3.5f);
                Add(ArenaProp.Water, s * 14.2f, -2.6f, 3.4f, 3.5f);
                Add(ArenaProp.Bridge, s * 14.2f, 0, 3.4f, 1.7f, false);
                for (int t = -1; t <= 1; t += 2)
                {
                    Add(ArenaProp.CapsuleWall, s * 10.1f, t * 7.4f, 5.2f, 1);
                    Bush(s * 8.3f, t * 8.6f, 2.4f, 2);
                    Add(ArenaProp.CapsuleWall, s * 7.7f, t * 3.3f, 1, 3.1f);
                    Add(ArenaProp.CapsuleWall, s * 6.4f, t * 2.1f, 2.8f, 1);
                    Bush(s * 6.1f, t * 3.7f, 2.2f, 2);
                    Bush(s * 2.4f, t * 8.1f, 1.5f, 1.6f);
                    Add(ArenaProp.CapsuleWall, s * 3.2f, t * 5.7f, 2.7f, .9f);
                    Add(ArenaProp.Crate, s * 3.2f, t * 6.4f, .9f, .9f);
                    Bush(s * 16.6f, t * 7.4f, .9f, 2.1f);
                    Bush(s * 12.2f, t * 5.2f, 1.4f, 1.5f);
                }
                Add(ArenaProp.CapsuleWall, 0, s * 3.1f, 4.4f, .85f);
            }
        }
        void Laboratory()
        {
            for (int s = -1; s <= 1; s += 2) for (int t = -1; t <= 1; t += 2)
            {
                Add(ArenaProp.Water, s * 14.2f, t * 3.7f, 3.5f, 3.1f);
                Add(ArenaProp.LabWall, s * 14.2f, t * 5.5f, 3.9f, .65f);
                Add(ArenaProp.LabWall, s * 16.3f, t * 3.7f, .65f, 3.1f);
                Add(ArenaProp.LabWall, s * 11.6f, t * 3.4f, 1.5f, .8f);
                Add(ArenaProp.LabWall, s * 6.7f, t * 6.6f, 4.7f, .8f);
                Add(ArenaProp.LabWall, s * 4.7f, t * 5.1f, .8f, 3.7f);
                Add(ArenaProp.LabWall, s * 3.8f, t * 3.4f, 2.1f, .8f);
                Bush(s * 6.8f, t * 7.8f, 3.7f, 1.5f);
                Bush(s * 9.1f, t * 1.9f, 2.2f, 1.3f);
                Add(ArenaProp.Crate, s * 11.5f, t * 8.8f, .85f, .85f);
                Add(ArenaProp.Crate, s * 7.2f, t * 3.4f, 1, 1);
            }
        }
        void Desert()
        {
            // Four water rectangles leave THREE genuine openings, no invisible bridge collision.
            float[] edge = { -18, -12.2f, -9.4f, -1.6f, 1.6f, 9.4f, 12.2f, 18 };
            for (int i = 0; i < 8; i += 2) Add(ArenaProp.Water, (edge[i] + edge[i + 1]) / 2, 0, edge[i + 1] - edge[i], 2.3f);
            foreach (float x in new[] { -10.8f, 0, 10.8f }) Add(ArenaProp.Bridge, x, 0, x == 0 ? 3.2f : 2.8f, 3.3f, false);
            for (int s = -1; s <= 1; s += 2) for (int t = -1; t <= 1; t += 2)
            {
                Add(ArenaProp.Sandstone, s * 6.6f, t * 7.9f, .9f, 3.2f);
                Add(ArenaProp.Sandstone, s * 7.5f, t * 6.7f, 2.7f, .9f);
                Bush(s * 4.6f, t * 8.3f, 3, 1.5f);
                Add(ArenaProp.Sandstone, s * 7.6f, t * 2.6f, .9f, 2.6f);
                Add(ArenaProp.Sandstone, s * 9f, t * 3.6f, 3.4f, .9f);
                Add(ArenaProp.Sandstone, s * 10.5f, t * 4.5f, .9f, 2);
                Bush(s * 11.7f, t * 5.6f, 1.5f, 2);
                Bush(s * 3.2f, t * 5.1f, 2.3f, 2.1f);
                Add(ArenaProp.Crate, s * 3.7f, t * 3.9f, .9f, .9f);
                Add(ArenaProp.Sandstone, s * 15.6f, t * 5.7f, 1, 2.1f);
                Bush(s * 16.5f, t * 3.8f, 1.5f, 1.3f);
            }
        }
        void Alpine()
        {
            Add(ArenaProp.Ice, 0, 0, 10.6f, 4.8f, false);
            for (int s = -1; s <= 1; s += 2)
            {
                Add(ArenaProp.Ice, s * 15, 0, 3.7f, 5, false);
                for (int t = -1; t <= 1; t += 2)
                {
                    Add(ArenaProp.Container, s * 5.1f, t * 3.2f, 3.1f, 1.5f);
                    Add(ArenaProp.Container, s * 8.4f, t * 4.6f, 1.3f, 3);
                    Bush(s * 6.9f, t * 5.4f, 2.3f, 2.3f);
                    Add(ArenaProp.Container, s * 3.9f, t * 6.8f, 3, 1.4f);
                    Add(ArenaProp.Container, s * 5.5f, t * 9.1f, 1.2f, 2.4f);
                    Bush(s * 7.8f, t * 8.7f, 3.2f, 1.7f);
                    Add(ArenaProp.Container, s * 13.5f, t * 8f, 2.7f, 1.4f);
                    Add(ArenaProp.Rock, s * 11.7f, t * 6.4f, .85f, 2.1f);
                    Add(ArenaProp.Crate, s * 10.9f, t * 7.1f, .9f, .9f);
                    Add(ArenaProp.Container, s * 16.2f, t * 3.9f, 1.3f, 2.5f);
                    Add(ArenaProp.Rock, s * 2.1f, t * 8.5f, 1.5f, .8f);
                    Add(ArenaProp.Crate, s * 2.8f, t * 3.2f, .8f, .8f);
                }
                Add(ArenaProp.Rock, s * 8.2f, 0, 2.9f, .9f);
            }
        }
    }
}
