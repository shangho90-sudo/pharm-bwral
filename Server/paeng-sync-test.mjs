import assert from 'node:assert/strict';
import fs from 'node:fs';
const base=process.env.TEST_SERVER||'https://paengbrawl-rooms-production.up.railway.app';
const geometry=JSON.parse(fs.readFileSync(process.env.TEST_GEOMETRY||'../../work/paeng-test-map.json','utf8'));
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function wait(fn,ms=8000){const end=Date.now()+ms;while(Date.now()<end){const value=fn();if(value)return value;await pause(20);}throw Error('Timed out');}
async function post(path,body){const response=await fetch(base+path,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)});assert(response.ok);return response.json();}
async function connect(join){const socket=new WebSocket(base.replace('http','ws')+'/socket');const messages=[];socket.addEventListener('message',e=>messages.push(JSON.parse(e.data)));await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});socket.send(JSON.stringify({type:'join',roomId:join.roomId,token:join.token}));await wait(()=>messages.find(m=>m.type==='room'));return {socket,messages,send:c=>socket.send(JSON.stringify(c))};}
const room=await post('/rooms',{nickname:'팽재현검증',name:'PAENG SKILL SYNC QA',teamSize:1});
const host=await connect(room),guest=await connect(await post('/rooms/'+room.roomId+'/join',{nickname:'동기화검증'}));
const snap=c=>c.messages.filter(m=>m.type==='snapshot').at(-1);
let seq=0;
function input(move={x:0,y:0},attack=false,skill=false,ultimate=false){const s=snap(host),p=s.fighters[0].position,e=s.fighters[1].position;host.send({type:'input',seq:++seq,mx:move.x,mz:move.y,ax:e.x-p.x,az:e.y-p.y,attack,skill,ultimate});}
const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
function blocked(p,r=.49,shot=false){const s=snap(host);if(Math.abs(p.x)>18-r||Math.abs(p.y)>13-r)return true;return geometry.covers.some((c,i)=>(!c.destructible||s.covers[i]>0)&&(!shot||c.blocksShots)&&Math.abs(p.x-c.position.x)<c.size.x*.5+r&&Math.abs(p.y-c.position.y)<c.size.y*.5+r);}
function clear(a,b){const n=Math.ceil(distance(a,b)/.2);for(let i=1;i<n;i++)if(blocked({x:a.x+(b.x-a.x)*i/n,y:a.y+(b.y-a.y)*i/n},.3,true))return false;return true;}
try{
 host.send({type:'hero',hero:0});guest.send({type:'hero',hero:0});host.send({type:'map',map:0});
 await wait(()=>host.messages.filter(m=>m.room).at(-1).room.members.every(p=>p.hero===0));guest.send({type:'ready',ready:true});await wait(()=>host.messages.filter(m=>m.room).at(-1).room.members.every(p=>p.ready));host.send({type:'start'});await wait(()=>snap(guest));await wait(()=>snap(host));
 const start=snap(host).fighters[0].position,target=snap(host).fighters[1].position;
 const key=p=>Math.round((p.x+17.5)*2)+','+Math.round((p.y+12.5)*2),point=k=>{const [x,y]=k.split(',').map(Number);return {x:x*.5-17.5,y:y*.5-12.5};};
 const queue=[key(start)],parents=new Map([[queue[0],null]]);let goal;
 for(let n=0;n<queue.length;n++){const here=queue[n],p=point(here);if(distance(p,target)<5.5&&distance(p,target)>2&&clear(p,target)){goal=here;break;}for(const delta of [{x:.5,y:0},{x:-.5,y:0},{x:0,y:.5},{x:0,y:-.5}]){const q={x:p.x+delta.x,y:p.y+delta.y},k=key(q);if(!parents.has(k)&&!blocked(q)){parents.set(k,here);queue.push(k);}}}
 assert(goal,'Reachable clear firing position');const path=[];for(let k=goal;k;k=parents.get(k))path.unshift(point(k));
 for(const destination of path){const end=Date.now()+3500;while(distance(snap(host).fighters[0].position,destination)>.25){assert(Date.now()<end,'Movement route blocked '+JSON.stringify({position:snap(host).fighters[0].position,destination}));const p=snap(host).fighters[0].position,d={x:destination.x-p.x,y:destination.y-p.y},length=Math.hypot(d.x,d.y);input({x:d.x/Math.max(.8,length),y:d.y/Math.max(.8,length)});await pause(70);}}
 input();await pause(100);input(undefined,false,true);const skill=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.events.some(e=>e.type==='skill'&&e.actor===0)));input();
 const impact=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.events.some(e=>e.type==='explosion'&&e.actor===0&&e.size===1.6)));
 assert(snap(host).fighters[0].skillTimer>8);assert.equal(snap(host).fighters[1].damageTaken,900);input(undefined,false,true);await pause(250);input();assert.equal(host.messages.flatMap(m=>m.events||[]).filter(e=>e.type==='skill'&&e.actor===0).length,1);
 const deadline=Date.now()+16000;while(snap(host).fighters[0].charge<4000){assert(Date.now()<deadline,'Could not charge ultimate');input(undefined,true);await pause(50);}input();await wait(()=>snap(host).fighters[1].hp>0,6000);input(undefined,false,false,true);input();
 const warning=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.zones.some(z=>z.value.owner===0&&z.value.kind===3)));
 const zone=warning.zones.find(z=>z.value.owner===0&&z.value.kind===3).value;assert.equal(zone.radius,3.5);assert(zone.remaining>0&&zone.remaining<=.9);
 const ultimate=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.events.some(e=>e.type==='explosion'&&e.actor===0&&e.size===3.5)));
 assert((ultimate.tick-warning.tick)/30>.75&&(ultimate.tick-warning.tick)/30<1.05);assert(!ultimate.zones.some(z=>z.value.owner===0&&z.value.kind===3));
 for(const message of [skill,impact,warning,ultimate]){const other=await wait(()=>guest.messages.find(m=>m.type==='snapshot'&&m.tick===message.tick));assert.deepEqual(other.events,message.events);assert.deepEqual(other.shots,message.shots);assert.deepEqual(other.zones,message.zones);assert.deepEqual(other.fighters,message.fighters);}
 console.log('PAENG_TWO_CLIENT_SYNC_PASS',JSON.stringify({skillDamage:900,splashRadius:1.6,ultimateRadius:3.5,fuseSeconds:(ultimate.tick-warning.tick)/30,matchingSnapshots:4,server:base}));
}finally{for(const c of [host,guest])if(c.socket.readyState===1){c.send({type:'leave'});c.socket.close();}}
