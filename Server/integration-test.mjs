import assert from 'node:assert/strict';
const base=process.env.TEST_SERVER||'http://localhost:8787';
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function request(path,body){const response=await fetch(base+path,body?{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)}:{});return {ok:response.ok,data:await response.json()};}
async function wait(fn,timeout=7000){const deadline=Date.now()+timeout;while(Date.now()<deadline){const result=fn();if(result)return result;await pause(20);}throw Error('Timed out');}
async function connect(join){const socket=new WebSocket(base.replace('http','ws')+'/socket');const messages=[];socket.addEventListener('message',e=>messages.push(JSON.parse(e.data)));await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',reject,{once:true});});socket.send(JSON.stringify({type:'join',roomId:join.roomId,token:join.token}));await wait(()=>messages.find(m=>m.type==='room'));return {socket,messages,join,send:command=>socket.send(JSON.stringify(command))};}
const health=await request('/health');assert.equal(health.data.maxPlayers,8);
const created=await request('/rooms',{nickname:'host',name:'integration',teamSize:4});assert(created.ok);
const clients=[await connect(created.data)];
try{
 for(let i=1;i<8;i++){const joined=await request('/rooms/'+created.data.roomId+'/join',{nickname:'guest'+i});assert(joined.ok);clients.push(await connect(joined.data));}
 const ninth=await request('/rooms/'+created.data.roomId+'/join',{nickname:'ninth'});assert.equal(ninth.ok,false);
 for(const c of clients)c.send({type:'hero',hero:0});
 await wait(()=>clients[0].messages.find(m=>m.room?.members.length===8 && m.room.members.some(p=>p.hero===0)));
 await pause(200);
 let room=clients[0].messages.filter(m=>m.room).at(-1).room;assert.equal(room.members.filter(p=>p.hero===0).length,1,'atomic character claim');
 const winner=room.members.find(p=>p.hero===0).id;let hero=1;
 for(const c of clients)if(c.join.playerId!==winner)c.send({type:'hero',hero:hero++});
 await wait(()=>clients[0].messages.filter(m=>m.room).at(-1)?.room.members.every(p=>p.hero>=0));
 clients[1].send({type:'start'});await wait(()=>clients[1].messages.some(m=>m.type==='error' && /방장/.test(m.error)));
 clients[0].send({type:'start'});const initial=await wait(()=>clients[1].messages.find(m=>m.type==='snapshot'));assert.equal(initial.fighters.length,8);
 const host=initial.room.members.find(p=>p.id===clients[0].join.playerId);assert(host.host);
 const before=initial.fighters[host.slot].position;
 for(let i=0;i<15;i++){clients[0].send({type:'input',seq:i,mx:1,mz:0,ax:0,az:10,attack:false});await pause(34);}
 const moved=await wait(()=>clients[1].messages.filter(m=>m.type==='snapshot').at(-1)?.fighters[host.slot].position.x>before.x+.4);
 assert(moved,'remote input movement');
 const common=clients[1].messages.filter(m=>m.type==='snapshot').at(-1);await wait(()=>clients[2].messages.some(m=>m.tick===common.tick&&m.type==='snapshot'));
 assert.deepEqual(clients[2].messages.find(m=>m.type==='snapshot'&&m.tick===common.tick).fighters,common.fighters,'same authoritative state');
 clients[0].socket.close();await pause(1300);
 const after=clients[1].messages.filter(m=>m.type==='snapshot').at(-1);assert(after.timeLeft<common.timeLeft-.9,'clock continues after host disconnect');
 assert(after.room.members.some(p=>p.host&&p.id!==clients[0].join.playerId),'host migration');
 console.log('MULTIPLAYER_PASS guest join, max 8, atomic hero claims, host authority, remote movement, shared snapshots, clock and host migration');
}finally{for(const c of clients)c.socket.close();}
