using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using PharmaBrawl;
using UnityEngine;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:"+(Environment.GetEnvironmentVariable("PORT")??"8787"));
var origins=(Environment.GetEnvironmentVariable("GAME_ORIGINS")??"https://shangho90-sudo.github.io,http://localhost:8765,http://localhost:8787").Split(',');
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();app.UseCors();
var wsOptions=new WebSocketOptions{KeepAliveInterval=TimeSpan.FromSeconds(15),KeepAliveTimeout=TimeSpan.FromSeconds(15)};
foreach(var origin in origins)wsOptions.AllowedOrigins.Add(origin);
app.UseWebSockets(wsOptions);
var hub=new GameHub();
app.MapGet("/health",()=>Results.Json(new{status="ok",protocol=2,tickRate=30,snapshotRate=15,maxPlayers=8,readyCheck=true,humanOnly=true,combatBalance="cooldowns-3-6-charge70-robots800-1600"}));
app.MapGet("/rooms",()=>Results.Text(GameHub.Json(new RoomList{rooms=hub.List()}),"application/json"));
app.MapPost("/rooms",async(HttpContext c)=>{
    try{var request=await JsonSerializer.DeserializeAsync<CreateRequest>(c.Request.Body,GameHub.Options);var result=hub.Create(request);return Results.Text(GameHub.Json(result),"application/json");}
    catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is JsonException){return Results.BadRequest(new{error=e.Message});}
});
app.MapPost("/rooms/{id}/join",async(string id,HttpContext c)=>{
    try{var request=await JsonSerializer.DeserializeAsync<CreateRequest>(c.Request.Body,GameHub.Options);return Results.Text(GameHub.Json(hub.Join(id,request?.nickname)),"application/json");}
    catch(Exception e)when(e is ArgumentException || e is InvalidOperationException || e is JsonException){return Results.BadRequest(new{error=e.Message});}
});
app.Map("/socket",async c=>{
    if(!c.WebSockets.IsWebSocketRequest){c.Response.StatusCode=400;return;}
    using var socket=await c.WebSockets.AcceptWebSocketAsync();
    Player player=null;Room room=null;Peer peer=null;
    using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(c.RequestAborted);
    try{
        using var firstTimeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);firstTimeout.CancelAfter(10000);
        var hello=await Receive(socket,firstTimeout.Token);
        (room,player)=hub.Authenticate(hello.roomId,hello.token);
        peer=new Peer(socket);lock(room.Gate){player.peer?.socket.Abort();player.peer=peer;room.Broadcast();}
        var send=peer.Send(lifetime.Token);
        long window=Stopwatch.GetTimestamp();int messages=0;
        while(socket.State==WebSocketState.Open){
            var command=await Receive(socket,lifetime.Token);
            if(Stopwatch.GetElapsedTime(window).TotalSeconds>=1){window=Stopwatch.GetTimestamp();messages=0;}
            if(++messages>90)throw new InvalidOperationException("요청이 너무 빠릅니다");
            lock(room.Gate)room.Command(player,command);
        }
        lifetime.Cancel();try{await send;}catch(OperationCanceledException){}
    }catch(Exception e)when(e is WebSocketException || e is OperationCanceledException || e is JsonException || e is InvalidOperationException || e is ArgumentException){
        if(socket.State==WebSocketState.Open)try{await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation,"연결 종료",CancellationToken.None);}catch(WebSocketException){}
    }finally{
        lifetime.Cancel();
        if(room!=null && player!=null)lock(room.Gate){if(player.peer==peer){player.peer=null;player.host=false;player.input=default;player.disconnected=DateTime.UtcNow;room.MigrateHost();room.Broadcast();}}
    }
});
using var tickCancellation=new CancellationTokenSource();
var loop=hub.Run(tickCancellation.Token);app.Lifetime.ApplicationStopping.Register(()=>tickCancellation.Cancel());
await app.RunAsync();

static async Task<NetCommand> Receive(WebSocket socket,CancellationToken token)
{
    var buffer=new byte[4096];int offset=0;WebSocketReceiveResult result;
    do{result=await socket.ReceiveAsync(new ArraySegment<byte>(buffer,offset,buffer.Length-offset),token);if(result.MessageType!=WebSocketMessageType.Text)throw new InvalidOperationException("텍스트 메시지만 허용");offset+=result.Count;if(offset>=buffer.Length)throw new InvalidOperationException("메시지가 너무 큽니다");}while(!result.EndOfMessage);
    return JsonSerializer.Deserialize<NetCommand>(buffer.AsSpan(0,offset),GameHub.Options)??throw new JsonException();
}
public sealed class CreateRequest{public string nickname,name;public int teamSize=4;}
public sealed class Peer
{
    public readonly WebSocket socket;
    readonly Channel<string> output=Channel.CreateBounded<string>(new BoundedChannelOptions(2){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true});
    public Peer(WebSocket s){socket=s;}
    public void Enqueue(string text)=>output.Writer.TryWrite(text);
    public async Task Send(CancellationToken token){await foreach(var text in output.Reader.ReadAllAsync(token)){using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token);deadline.CancelAfter(5000);await socket.SendAsync(Encoding.UTF8.GetBytes(text),WebSocketMessageType.Text,true,deadline.Token);}}
}
public sealed class Player
{
    public string id=Guid.NewGuid().ToString("N"),token=Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),nickname;
    public int slot,team,hero=-1,seq=-1;public bool host,ready;public Peer peer;
    public DateTime disconnected=DateTime.UtcNow;public long lastInput;public ArenaSimulation.HumanInput input;
}
public sealed class Room
{
    public readonly object Gate=new();public string id,name;public int map,teamSize,match,tick;
    public readonly List<Player> players=new();public ArenaSimulation sim;public int battleTeamSize;public DateTime started,created=DateTime.UtcNow;
    readonly List<ArenaSimulation.CombatEvent> events=new();
    public RoomInfo Info()=>new(){id=id,name=name,map=map,teamSize=teamSize,count=players.Count,playing=sim!=null&&!sim.finished,members=players.Select(p=>new RoomMember{id=p.id,nickname=p.nickname,slot=p.slot,team=p.team,hero=p.hero,host=p.host,ready=p.host?p.hero>=0:p.ready,connected=p.peer!=null}).ToArray()};
    public void MigrateHost(){if(players.Any(p=>p.host&&(p.peer!=null || (DateTime.UtcNow-p.disconnected).TotalSeconds<30)))return;foreach(var p in players)p.host=false;var next=players.FirstOrDefault(p=>p.peer!=null);if(next!=null)next.host=true;}
    public void ResetReady(){foreach(var member in players)member.ready=false;}
    public void Broadcast(){var text=GameHub.Json(new NetMessage{type="room",room=Info()});foreach(var p in players)p.peer?.Enqueue(text);}
    public void Error(Player p,string message)=>p.peer?.Enqueue(GameHub.Json(new NetMessage{type="error",error=message}));
    public void Command(Player p,NetCommand c)
    {
        bool active=sim!=null&&!sim.finished;
        switch(c.type){
            case "hero":
                if(active){Error(p,"전투 중에는 변경할 수 없습니다");return;}
                if(c.hero<0||c.hero>9||players.Any(x=>x!=p&&x.team==p.team&&x.hero==c.hero)){Error(p,"이미 선택된 캐릭터입니다");return;}
                p.hero=c.hero;ResetReady();Broadcast();break;
            case "team":
                if(active||c.team<0||c.team>1){Error(p,"전투 중에는 팀을 변경할 수 없습니다");return;}
                if(c.team==p.team)return;
                if(players.Count(x=>x.team==c.team)>=teamSize){Error(p,"선택한 팀이 가득 찼습니다");return;}
                if(p.hero>=0&&players.Any(x=>x!=p&&x.team==c.team&&x.hero==p.hero)){Error(p,"선택한 팀에 같은 캐릭터가 있습니다");return;}
                p.team=c.team;ResetReady();Broadcast();break;
            case "ready":
                if(active||p.host)return;
                if(p.hero<0){Error(p,"캐릭터를 먼저 선택해 주세요");return;}
                p.ready=c.ready;Broadcast();break;
            case "map":if(!p.host||active){Error(p,"방장만 맵을 선택할 수 있습니다");return;}map=Math.Clamp(c.map,0,3);ResetReady();Broadcast();break;
            case "start":
                if(!p.host||active){Error(p,"방장만 전투를 시작할 수 있습니다");return;}
                string block=RoomRules.StartBlockReason(Info());if(block.Length>0){Error(p,block);return;}
                battleTeamSize=players.Count/2;
                var ordered=players.OrderBy(x=>x.team).ThenBy(x=>x.slot).ToArray();
                for(int i=0;i<ordered.Length;i++)ordered[i].slot=i;
                var picks=ordered.Select(x=>x.hero).ToArray();
                sim=new(GameHub.Roster,p.hero,Random.Shared.Next(),map,battleTeamSize,picks);for(int i=0;i<ordered.Length;i++){sim.fighters[i].human=true;sim.fighters[i].nickname=ordered[i].nickname;}events.Clear();sim.Event+=e=>{if(events.Count<256)events.Add(e);};match++;tick=0;started=DateTime.UtcNow;foreach(var member in players){member.input=default;member.seq=-1;}Broadcast();Snapshot();break;
            case "input":
                if(!active||c.seq<=p.seq)return;
                if(!float.IsFinite(c.mx)||!float.IsFinite(c.mz)||!float.IsFinite(c.ax)||!float.IsFinite(c.az))return;
                p.seq=c.seq;p.lastInput=Stopwatch.GetTimestamp();p.input=new(){human=true,move=Vector2.ClampMagnitude(new(c.mx,c.mz),1),aim=Vector2.ClampMagnitude(new(c.ax,c.az),35),attack=c.attack,skill=p.input.skill||c.skill,ultimate=p.input.ultimate||c.ultimate};break;
            case "leave":p.peer?.socket.Abort();players.Remove(p);ResetReady();MigrateHost();Broadcast();break;
        }
    }
    public void Step()
    {
        int previousCount=players.Count;
        foreach(var p in players.Where(p=>p.peer==null&&(DateTime.UtcNow-p.disconnected).TotalSeconds>30).ToArray())players.Remove(p);
        MigrateHost();if(previousCount!=players.Count){ResetReady();Broadcast();}
        if(sim==null||sim.finished)return;
        var inputs=new ArenaSimulation.HumanInput[battleTeamSize*2];for(int i=0;i<inputs.Length;i++)inputs[i].human=true;
        foreach(var p in players){if(p.peer==null)continue;inputs[p.slot]=p.input;inputs[p.slot].human=true;if(Stopwatch.GetElapsedTime(p.lastInput).TotalSeconds>.25){inputs[p.slot].move=Vector2.zero;inputs[p.slot].attack=false;inputs[p.slot].skill=false;inputs[p.slot].ultimate=false;}p.input.skill=p.input.ultimate=false;}
        sim.timeLeft=Math.Max(0,180-(float)(DateTime.UtcNow-started).TotalSeconds);
        sim.TickNetwork(1/30f,inputs);tick++;
        if(tick%2==0||sim.finished)Snapshot();
    }
    void Snapshot()
    {
        var message=new NetMessage{type="snapshot",room=Info(),tick=tick,match=match,timeLeft=sim.timeLeft,score=sim.score,finished=sim.finished,winner=sim.winner,
            fighters=sim.fighters.Select(FighterState.From).ToArray(),shots=sim.shots.Select((s,i)=>new IndexedShot{index=i,value=s}).Where(s=>s.value.active).ToArray(),
            zones=sim.zones.Select((z,i)=>new IndexedZone{index=i,value=z}).Where(z=>z.value.active).ToArray(),robots=sim.robots.Select((r,i)=>new IndexedRobot{index=i,value=r}).Where(r=>r.value.active).ToArray(),covers=sim.covers.Select(c=>c.hp).ToArray(),events=events.ToArray()};
        string text=GameHub.Json(message);foreach(var p in players)p.peer?.Enqueue(text);events.Clear();
    }
}
public sealed class GameHub
{
    public static readonly JsonSerializerOptions Options=new(){IncludeFields=true,IgnoreReadOnlyProperties=true};
    public static readonly CharacterDefinition[] Roster=JsonSerializer.Deserialize<CharacterDefinition[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"characters.json")),Options);
    readonly ConcurrentDictionary<string,Room> rooms=new();readonly object gate=new();
    public static string Json(object o)=>JsonSerializer.Serialize(o,Options);
    static string Clean(string text,string fallback,int max){text=(text??fallback).Trim();text=new string(text.Where(c=>!char.IsControl(c)&&c!='<'&&c!='>').Take(max).ToArray());return text.Length==0?fallback:text;}
    public RoomInfo[] List()=>rooms.Values.Select(r=>{lock(r.Gate)return r.Info();}).ToArray();
    public RoomJoin Create(CreateRequest request){lock(gate){if(rooms.Count>=32)throw new InvalidOperationException("서버의 방이 가득 찼습니다");var r=new Room{id=Convert.ToHexString(RandomNumberGenerator.GetBytes(4)),name=Clean(request?.name,"팽브롤 대전",24),teamSize=Math.Clamp(request?.teamSize??4,1,4)};rooms[r.id]=r;return Add(r,request?.nickname,true);}}
    public RoomJoin Join(string id,string nickname){if(!rooms.TryGetValue(id,out var r))throw new InvalidOperationException("방을 찾을 수 없습니다");return Add(r,nickname,false);}
    RoomJoin Add(Room r,string nickname,bool host){lock(r.Gate){if(r.sim!=null&&!r.sim.finished)throw new InvalidOperationException("전투 중인 방입니다");if(r.players.Count>=r.teamSize*2)throw new InvalidOperationException("방이 가득 찼습니다");var occupied=r.players.Select(p=>p.slot).ToHashSet();var available=Enumerable.Range(0,r.teamSize*2).Where(i=>!occupied.Contains(i));int blue=r.players.Count(p=>p.team==0),red=r.players.Count-blue;int slot=available.First();var p=new Player{nickname=Clean(nickname,"약사",12),slot=slot,team=blue<=red?0:1,host=host};r.players.Add(p);r.ResetReady();r.Broadcast();return new(){roomId=r.id,playerId=p.id,token=p.token};}}
    public (Room,Player) Authenticate(string id,string token){if(id==null||!rooms.TryGetValue(id,out var r))throw new InvalidOperationException("방 없음");lock(r.Gate){var p=r.players.FirstOrDefault(x=>x.token==token);return p==null?throw new InvalidOperationException("참가 정보 없음"):(r,p);}}
    public async Task Run(CancellationToken token){using var timer=new PeriodicTimer(TimeSpan.FromSeconds(1/30d));try{while(await timer.WaitForNextTickAsync(token)){foreach(var r in rooms.Values)lock(r.Gate){r.Step();if(r.players.Count==0&&(DateTime.UtcNow-r.created).TotalMinutes>1)rooms.TryRemove(r.id,out _);}}}catch(OperationCanceledException){}}
}
