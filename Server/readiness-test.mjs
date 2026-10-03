import assert from 'node:assert/strict';
const base=process.env.TEST_SERVER||'http://localhost:8787';
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function request(path,body){const response=await fetch(base+path,body?{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)}:{});return {ok:response.ok,data:await response.json()};}
async function wait(fn,timeout=7000){const deadline=Date.now()+timeout;while(Date.now()<deadline){const result=fn();if(result)return result;await pause(20);}throw Error('Timed out');}
async function connect(join){
 const socket=new WebSocket(base.replace('http','ws')+'/socket');const messages=[];
 socket.addEventListener('message',e=>messages.push(JSON.parse(e.data)));
 await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});
 socket.send(JSON.stringify({type:'join',roomId:join.roomId,token:join.token}));
 await wait(()=>messages.find(m=>m.type==='room'));
 return {socket,messages,join,send:c=>socket.send(JSON.stringify(c))};
}
const latestRoom=c=>c.messages.filter(m=>m.room).at(-1)?.room;
const latestSnapshot=c=>c.messages.filter(m=>m.type==='snapshot').at(-1);
async function expectError(c,command,pattern){const index=c.messages.length;c.send(command);await wait(()=>c.messages.slice(index).some(m=>m.type==='error'&&pattern.test(m.error)));}
const health=await request('/health');assert.equal(health.data.protocol,2);assert.equal(health.data.humanOnly,true);
for(let size=1;size<=4;size++){
 const created=await request('/rooms',{nickname:'팽재현약사',name:'DEPLOY READY TEST '+size,teamSize:4});assert(created.ok);
 const clients=[await connect(created.data)];
 try{
  clients[0].send({type:'hero',hero:0});await expectError(clients[0],{type:'start'},/인원/);
  for(let i=1;i<size*2;i++){const joined=await request('/rooms/'+created.data.roomId+'/join',{nickname:'참가약사'+i});assert(joined.ok);clients.push(await connect(joined.data));}
  await wait(()=>latestRoom(clients[0]).members.length===size*2);
  assert.equal(latestRoom(clients[0]).members.find(p=>p.host).nickname,'팽재현약사','Korean nickname round trip');
  for(let i=0;i<clients.length;i++)clients[i].send({type:'hero',hero:i});await wait(()=>latestRoom(clients[0]).members.every(p=>p.hero>=0));
  clients[1].send({type:'hero',hero:0});await wait(()=>latestRoom(clients[0]).members.filter(p=>p.hero===0).length===2);
  if(size>=2)await expectError(clients[2],{type:'hero',hero:0},/선택/);
  clients[1].send({type:'hero',hero:1});await wait(()=>latestRoom(clients[0]).members.find(p=>p.id===clients[1].join.playerId).hero===1);
  if(size<4){
   clients[1].send({type:'team',team:0});await wait(()=>latestRoom(clients[0]).members.find(p=>p.id===clients[1].join.playerId).team===0);
   await expectError(clients[0],{type:'start'},/인원/);
   clients[1].send({type:'team',team:1});await wait(()=>latestRoom(clients[0]).members.find(p=>p.id===clients[1].join.playerId).team===1);
  }
  if(size===4){const ninth=await request('/rooms/'+created.data.roomId+'/join',{nickname:'ninth'});assert.equal(ninth.ok,false);await expectError(clients[1],{type:'team',team:0},/가득/);}
  await expectError(clients[1],{type:'start'},/방장/);await expectError(clients[0],{type:'start'},/준비/);
  for(let i=1;i<clients.length;i++)clients[i].send({type:'ready',ready:true});await wait(()=>latestRoom(clients[0]).members.every(p=>p.ready));
  clients[0].send({type:'map',map:0});await wait(()=>latestRoom(clients[0]).members.filter(p=>!p.host).every(p=>!p.ready));
  await expectError(clients[0],{type:'start'},/준비/);
  for(let i=1;i<clients.length;i++)clients[i].send({type:'ready',ready:true});await wait(()=>latestRoom(clients[0]).members.every(p=>p.ready));
  clients[0].send({type:'start'});const initial=await wait(()=>latestSnapshot(clients[1]));
  assert.equal(initial.fighters.length,size*2,'Only joined humans, no AI-filled slots');
  for(const p of initial.room.members)assert.equal(p.slot<size?0:1,p.team,'Compact slots preserve selected teams');
  await pause(700);assert.deepEqual(latestSnapshot(clients[1]).fighters.map(f=>f.position),initial.fighters.map(f=>f.position),'Idle humans are not controlled by bots');
  const host=initial.room.members.find(p=>p.host);const before=initial.fighters[host.slot].position;
  for(let i=0;i<15;i++){clients[0].send({type:'input',seq:i,mx:1,mz:0,ax:0,az:10});await pause(34);}
  await wait(()=>latestSnapshot(clients[1]).fighters[host.slot].position.x>before.x+.4);
  await expectError(clients[1],{type:'team',team:0},/전투/);
  const common=latestSnapshot(clients[1]);if(size>1){await wait(()=>clients[2].messages.some(m=>m.type==='snapshot'&&m.tick===common.tick));assert.deepEqual(clients[2].messages.find(m=>m.type==='snapshot'&&m.tick===common.tick).fighters,common.fighters);}
  clients[0].socket.close();await pause(700);const after=latestSnapshot(clients[1]);assert(after.timeLeft<common.timeLeft-.4);assert(after.room.members.some(p=>p.host&&p.id!==host.id));
  const disconnected=after.fighters[host.slot].position;await pause(600);assert.deepEqual(latestSnapshot(clients[1]).fighters[host.slot].position,disconnected,'Disconnected human never becomes AI');
  console.log('HUMAN_ONLY_READY_PASS',size+':'+size,'Korean names, balanced teams, ready reset, host authority, exact human roster, no bots, movement, host migration');
 }finally{for(const c of clients)if(c.socket.readyState===1){c.send({type:'leave'});c.socket.close();}}
}
console.log('MULTIPLAYER_PASS all 1:1 through 4:4 readiness and human-only cases');
