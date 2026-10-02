import assert from 'node:assert/strict';
import fs from 'node:fs';
const base=process.env.TEST_SERVER||'https://paengbrawl-rooms-production.up.railway.app';
const geometry=JSON.parse(fs.readFileSync(process.env.TEST_GEOMETRY||'../../work/paeng-test-map.json','utf8'));
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function wait(fn,ms=8000){const end=Date.now()+ms;while(Date.now()<end){const value=fn();if(value)return value;await pause(20);}throw Error('Timed out');}
async function post(path,body){const response=await fetch(base+path,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)});assert(response.ok);return response.json();}
async function connect(join){const socket=new WebSocket(base.replace('http','ws')+'/socket');const messages=[];socket.addEventListener('message',e=>messages.push(JSON.parse(e.data)));await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});socket.send(JSON.stringify({type:'join',roomId:join.roomId,token:join.token}));await wait(()=>messages.find(m=>m.type==='room'));return {socket,messages,send:c=>socket.send(JSON.stringify(c))};}
async function verify(hero){
const room=await post('/rooms',{nickname:'기술검증'+hero,name:'REFERENCE ABILITY QA '+hero,teamSize:1});
const host=await connect(room),guest=await connect(await post('/rooms/'+room.roomId+'/join',{nickname:'동기화검증'}));
const snap=c=>c.messages.filter(m=>m.type==='snapshot').at(-1);
let seq=0;
function input(move={x:0,y:0},attack=false,skill=false,ultimate=false){const s=snap(host),p=s.fighters[0].position,e=s.fighters[1].position;host.send({type:'input',seq:++seq,mx:move.x,mz:move.y,ax:e.x-p.x,az:e.y-p.y,attack,skill,ultimate});}
const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
function blocked(p,r=.7,shot=false){const s=snap(host);if(Math.abs(p.x)>18-r||Math.abs(p.y)>13-r)return true;return geometry.covers.some((c,i)=>(!c.destructible||s.covers[i]>0)&&(!shot||c.blocksShots)&&Math.abs(p.x-c.position.x)<c.size.x*.5+r&&Math.abs(p.y-c.position.y)<c.size.y*.5+r);}
function clear(a,b){const n=Math.ceil(distance(a,b)/.2);for(let i=1;i<n;i++)if(blocked({x:a.x+(b.x-a.x)*i/n,y:a.y+(b.y-a.y)*i/n},.3,true))return false;return true;}
try{

 host.send({type:'hero',hero});guest.send({type:'hero',hero:0});host.send({type:'map',map:0});
 await wait(()=>host.messages.filter(m=>m.room).at(-1).room.members.every(p=>p.hero>=0));guest.send({type:'ready',ready:true});await wait(()=>host.messages.filter(m=>m.room).at(-1).room.members.every(p=>p.ready));host.send({type:'start'});await wait(()=>snap(guest));await wait(()=>snap(host));
 const start=snap(host).fighters[0].position,target=snap(host).fighters[1].position;
 const key=p=>Math.round((p.x+17.5)*2)+','+Math.round((p.y+12.5)*2),point=k=>{const [x,y]=k.split(',').map(Number);return {x:x*.5-17.5,y:y*.5-12.5};};
 const queue=[key(start)],parents=new Map([[queue[0],null]]);let goal;
 for(let n=0;n<queue.length;n++){const here=queue[n],p=point(here);if(distance(p,target)<2.8&&distance(p,target)>2&&clear(p,target)){goal=here;break;}for(const delta of [{x:.5,y:0},{x:-.5,y:0},{x:0,y:.5},{x:0,y:-.5}]){const q={x:p.x+delta.x,y:p.y+delta.y},k=key(q);if(!parents.has(k)&&!blocked(q)){parents.set(k,here);queue.push(k);}}}
 assert(goal,'Reachable clear firing position');const path=[];for(let k=goal;k;k=parents.get(k))path.unshift(point(k));
 for(const destination of path){const end=Date.now()+3500;while(distance(snap(host).fighters[0].position,destination)>.25){assert(Date.now()<end,'Movement route blocked '+JSON.stringify({position:snap(host).fighters[0].position,destination}));const p=snap(host).fighters[0].position,d={x:destination.x-p.x,y:destination.y-p.y},length=Math.hypot(d.x,d.y);input({x:d.x/Math.max(2.5,length),y:d.y/Math.max(2.5,length)});await pause(70);}}

 input();await pause(100);input(undefined,false,true);
 const skill=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.events.some(e=>e.type==='skill'&&e.actor===0)));input();
 if([2,4,8].includes(hero)){input(undefined,true);await pause(250);input();}
 const cooldown=skill.fighters[0].skillTimer;const expectedCooldown=hero===6?3:hero===9?8:6;assert(cooldown>expectedCooldown-.2&&cooldown<=expectedCooldown);
 if(hero===0)assert(skill.shots.some(s=>s.value.owner===0&&s.value.kind===1&&s.value.damage===600));
 if(hero===7){
  const summoned=skill.robots.find(r=>r.value.owner===0&&!r.value.elite);assert.equal(summoned.value.hp,800);assert.equal(summoned.value.maxHp,800);
  let guestSeq=0;const end=Date.now()+6500;let damageSnapshot;
  while(snap(host).robots.some(r=>r.index===summoned.index)){
   assert(Date.now()<end,'Support robot could not be destroyed');const s=snap(host),r=s.robots.find(r=>r.index===summoned.index),p=s.fighters[1].position;
   guest.send({type:'input',seq:++guestSeq,mx:0,mz:0,ax:r.value.position.x-p.x,az:r.value.position.y-p.y,attack:true});
   if(r.value.hp<800)damageSnapshot=s;await pause(60);
  }
  guest.send({type:'input',seq:++guestSeq,mx:0,mz:0,ax:0,az:1,attack:false});assert(damageSnapshot,'Robot HP decreased before removal');
  const other=await wait(()=>guest.messages.find(m=>m.type==='snapshot'&&m.tick===damageSnapshot.tick));assert.deepEqual(other.robots,damageSnapshot.robots);const removed=snap(host);await wait(()=>guest.messages.find(m=>m.type==='snapshot'&&m.tick===removed.tick));assert(removed.events.some(e=>e.type==='robotDestroyed')||host.messages.some(m=>m.events?.some(e=>e.type==='robotDestroyed')));console.log('ROBOT_TWO_CLIENT_DESTRUCTION_PASS hp=800 damaged and removed, identical snapshots');
 }
 const deadline=Date.now()+75000;while(snap(host).fighters[0].charge<Number(fs.readFileSync(fs.readdirSync("Assets/Resources/Characters").filter(f=>f.startsWith(String(hero).padStart(2,"0"))&&f.endsWith(".asset")).map(f=>"Assets/Resources/Characters/"+f)[0],"utf8").match(/ultimateRequirement: ([0-9.]+)/)[1])){assert(Date.now()<deadline,'Could not charge ultimate hero '+hero+' '+JSON.stringify(snap(host).fighters));input(undefined,true);await pause(60);}input();
 await wait(()=>snap(host).fighters[1].hp>0,6500);input(undefined,false,false,true);
 const ultimate=await wait(()=>host.messages.find(m=>m.type==='snapshot'&&m.events.some(e=>e.type==='ultimate'&&e.actor===0)));input();
 if(hero===0){const zone=ultimate.zones.find(z=>z.value.owner===0&&z.value.kind===3).value;assert.equal(zone.radius,3.5);assert.equal(zone.damage,1700);assert(zone.pending);}
 if(hero===1)assert(ultimate.events.some(e=>e.type==='lightning'&&e.size===1.7));
 if(hero===2)assert(ultimate.events.some(e=>e.type==='beam'&&e.size===35));
 if(hero===5)assert.equal(ultimate.zones.filter(z=>z.value.owner===0&&z.value.pending).length,7);
 if(hero===6)assert.equal(ultimate.events.filter(e=>e.type==='blink').length,2);
 if(hero===7){const robot=ultimate.robots.find(r=>r.value.elite).value;assert(robot.remaining>14.8&&robot.remaining<=15);assert.equal(robot.hp,1600);assert.equal(robot.maxHp,1600);}
 if(hero===8)assert.equal(ultimate.shots.filter(s=>s.value.owner===0&&s.value.damage===300).length,17);
 if(hero===9){const zone=ultimate.zones.find(z=>z.value.owner===0&&z.value.kind===2).value;assert.equal(zone.radius,4.5);assert(zone.remaining>7.8&&zone.remaining<=8);}
 await pause(250);const updated=snap(host);
 if(hero===0)await wait(()=>host.messages.some(m=>m.tick>=ultimate.tick&&m.events?.some(e=>e.type==='explosion'&&e.actor===0&&e.size===3.5)),2000);
 for(const message of [skill,ultimate,updated]){const other=await wait(()=>guest.messages.find(m=>m.type==='snapshot'&&m.tick===message.tick));for(const field of ['events','shots','zones','robots','fighters'])assert.deepEqual(other[field],message[field]);}
 console.log('REFERENCE_TWO_CLIENT_SYNC_PASS',JSON.stringify({hero,cooldown,matchingSnapshots:3,server:base}));

}finally{for(const c of [host,guest])if(c.socket.readyState===1){c.send({type:'leave'});c.socket.close();}}
}
for(let hero=Number(process.argv[2]||1);hero<Number(process.argv[3]||10);hero++)await verify(hero);
console.log('REFERENCE_REQUESTED_HEROES_SYNC_PASS');



