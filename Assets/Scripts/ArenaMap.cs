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
        public Vector2 Spawn(int id)
        {
            Vector2 origin=new Vector2((id%3-1)*(theme==ArenaTheme.Alpine?.8f:3.2f),(id<3?-1:1)*(theme==ArenaTheme.Desert?12f:11.1f));
            if(SpawnClear(origin))return origin;
            for(float radius=.5f;radius<=8;radius+=.5f)for(int i=0;i<32;i++){
                float angle=i*Mathf.PI/16;Vector2 p=origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;if(SpawnClear(p))return p;
            }
            throw new System.InvalidOperationException("No safe spawn in "+name);
        }
        bool SpawnClear(Vector2 p)
        {
            if(Mathf.Abs(p.x)>17.5f || Mathf.Abs(p.y)>12.5f)return false;
            foreach(var f in features)if(f.blocksMovement && Mathf.Abs(p.x-f.position.x)<f.size.x/2+.5f && Mathf.Abs(p.y-f.position.y)<f.size.y/2+.5f)return false;
            return true;
        }
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
            foreach(int side in new[]{-1,1}){
                Add(ArenaProp.Water,side*15.6f,3.35f,3.7f,5.5f);
                Add(ArenaProp.Water,side*15.6f,-3.35f,3.7f,5.5f);
                Add(ArenaProp.Bridge,side*15.6f,0,3.7f,1.2f,false);
                foreach(int end in new[]{-1,1}){
                    VillageWall(side,end,406,197,190,45);
                    VillageWall(side,end,665,251,145,39);
                    VillageWall(side,end,490,368,41,132);
                    VillageWall(side,end,554,409,99,39);
                    VillageWall(side,end,219,279,119,18);
                    VillageWall(side,end,280,351,18,105);
                    Bush(side*6.7f,end*4.4f,2.3f,2.1f);
                    Bush(side*12.6f,end*6,2.2f,2.3f);
                    Bush(side*4,end*11.4f,1.4f,1.4f);
                    Bush(side*17,end*9,1,1.8f);
                }
            }
            Add(ArenaProp.CapsuleWall,0,-4.55f,5.4f,1.15f);
        }
        void VillageWall(int side,int end,float px,float py,float width,float depth)
        {
            Add(ArenaProp.CapsuleWall,side*(836.5f-px)/38.1f,end*(470-py)/24.5f,width/38.1f,depth/24.5f);
        }
        void Pixel(ArenaProp kind,float x,float y,float w,float h,bool solid=true)
        {
            Add(kind,(x-836.5f)/38.1f,(470-y)/24.5f,w/38.1f,h/24.5f,solid);
        }
        void Laboratory()
        {
            foreach(int side in new[]{-1,1}){
                float X(float x)=>side<0?x:1673-x;
                foreach(float y in new[]{342f,584f}){
                    Pixel(ArenaProp.Water,X(218),y,183,102);
                    Pixel(ArenaProp.LabWall,X(114),y,24,120);
                    Pixel(ArenaProp.LabWall,X(223),y<470?405:656,220,22);
                }
                foreach(float y in new[]{218f,687f})Pixel(ArenaProp.LabWall,X(551),y,216,29);
                Pixel(ArenaProp.LabWall,X(628),264,34,95);
                Pixel(ArenaProp.LabWall,X(655),318,89,29);
                Pixel(ArenaProp.LabWall,X(628),620,34,105);
                Pixel(ArenaProp.LabWall,X(668),565,96,28);
                Pixel(ArenaProp.LabWall,X(435),478,148,29);
                Pixel(ArenaProp.LabWall,X(403),345,76,28);
                Pixel(ArenaProp.LabWall,X(423),599,70,27);
                Pixel(ArenaProp.LabWall,X(294),275,130,28);
                Pixel(ArenaProp.LabWall,X(295),628,110,29);
                Pixel(ArenaProp.Bush,X(551),177,190,50,false);
                Pixel(ArenaProp.Bush,X(551),727,190,55,false);
                Pixel(ArenaProp.Bush,X(440),439,105,40,false);
                Pixel(ArenaProp.LabWall,X(517),391,50,30);
                Pixel(ArenaProp.LabWall,X(294),179,59,29);
                Pixel(ArenaProp.LabWall,X(240),749,75,28);
            }
        }
        void Desert()
        {
            float[] edge={-18,-12.2f,-9.4f,-1.4f,1.4f,9.4f,12.2f,18};
            for(int i=0;i<8;i+=2)Add(ArenaProp.Water,(edge[i]+edge[i+1])/2,0,edge[i+1]-edge[i],3);
            foreach(float x in new[]{-10.8f,0,10.8f})Add(ArenaProp.Bridge,x,0,2.8f,4.4f,false);
            // Screen-space reference measurements converted to the calibrated orthographic floor.
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1}){
                DesertWall(side,end,587,253,34,77);DesertWall(side,end,547,288,105,32);
                DesertWall(side,end,447,333,34,64);DesertWall(side,end,494,364,120,32);DesertWall(side,end,555,400,32,48);
                DesertWall(side,end,273,324,35,70);
                Add(ArenaProp.Crate,side*3.85f,end*4,.95f,.95f);
                Bush(side*4.6f,end*8.3f,3,1.7f);Bush(side*3.2f,end*5.1f,2.6f,2.1f);
                Bush(side*11.7f,end*5.6f,1.8f,2);Bush(side*16.2f,end*4.4f,1.7f,1.5f);
            }
        }
        void DesertWall(int side,int end,float px,float py,float width,float depth)
        {
            Add(ArenaProp.Sandstone,side*(836.5f-px)/38.1f,end*(470-py)/24.5f,width/38.1f,depth/24.5f);
        }
        void Alpine()
        {
            Pixel(ArenaProp.Ice,836,519,450,188,false);
            foreach(var q in new[]{new Vector4(277,220,123,45),new Vector4(610,185,41,91),new Vector4(699,253,116,44),new Vector4(1055,183,40,90),new Vector4(1374,220,116,43),new Vector4(121,355,42,79),new Vector4(412,355,42,91),new Vector4(604,417,135,48),new Vector4(985,341,120,40),new Vector4(1229,361,43,91),new Vector4(1570,355,41,82),new Vector4(75,619,45,89),new Vector4(399,642,43,88),new Vector4(693,659,108,44),new Vector4(1067,635,126,43),new Vector4(1284,651,46,94),new Vector4(1571,609,124,46),new Vector4(586,849,41,115),new Vector4(1084,849,43,113)})Pixel(ArenaProp.Container,q.x,q.y,q.z,q.w);
            foreach(var q in new[]{new Vector4(490,529,143,27),new Vector4(1182,532,143,29),new Vector4(1072,412,137,30),new Vector4(735,159,83,29),new Vector4(935,159,85,27),new Vector4(966,744,83,30),new Vector4(685,762,61,25),new Vector4(290,745,78,35),new Vector4(1425,743,148,38)})Pixel(ArenaProp.Rock,q.x,q.y,q.z,q.w);
            // The rocky rims keep fighters out of shoreline scenery; all ice is traversable.
            foreach(int side in new[]{-1,1}){
                float X(float x)=>side<0?x:1673-x;
                Pixel(ArenaProp.Ice,X(204),515,182,220,false);
                Pixel(ArenaProp.Rock,X(297),515,25,270);
                Pixel(ArenaProp.Bush,X(506),199,138,50,false);
                Pixel(ArenaProp.Bush,X(502),341,118,80,false);
                Pixel(ArenaProp.Bush,X(496),690,125,60,false);

            }
        }
    }
}


