import assert from 'node:assert/strict';
const base=process.env.TEST_SERVER||'http://localhost:8787',pause=ms=>new Promise(r=>setTimeout(r,ms));
async function wait(fn){const end=Date.now()+8000;while(Date.now()<end){const x=fn();if(x)return x;await pause(20);}throw Error('Leave QA timeout');}
async function post(path,body){const r=await fetch(base+path,{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)});assert(r.ok);return r.json();}
async function connect(join){const socket=new WebSocket(base.replace('http','ws')+'/socket'),messages=[];socket.addEventListener('message',e=>messages.push(JSON.parse(e.data)));await new Promise((r,j)=>{socket.addEventListener('open',r,{once:true});socket.addEventListener('error',j,{once:true});});socket.send(JSON.stringify({type:'join',roomId:join.roomId,token:join.token}));await wait(()=>messages.find(m=>m.type==='room'));return {socket,messages,join,send:c=>socket.send(JSON.stringify(c))};}
const created=await post('/rooms',{nickname:'나가기검증방장',name:'LEAVE CONTINUATION QA',teamSize:2}),clients=[];
const snapshot=c=>c.messages.filter(m=>m.type==='snapshot').at(-1),room=c=>c.messages.filter(m=>m.room).at(-1).room;
try{
 for(let i=0;i<4;i++)clients.push(await connect(i===0?created:await post('/rooms/'+created.roomId+'/join',{nickname:'나가기검증'+i})));
 for(let i=0;i<4;i++)clients[i].send({type:'hero',hero:i});await wait(()=>room(clients[0]).members.every(p=>p.hero>=0));
 for(let i=1;i<4;i++)clients[i].send({type:'ready',ready:true});await wait(()=>room(clients[0]).members.every(p=>p.ready));clients[0].send({type:'start'});await wait(()=>snapshot(clients[3]));
 const initial=snapshot(clients[3]),hostSlot=initial.room.members.find(m=>m.id===created.playerId).slot;clients[0].send({type:'leave'});
 await wait(()=>snapshot(clients[3])?.fighters[hostSlot].withdrawn);let s=snapshot(clients[3]);assert(!s.finished);assert.equal(s.room.members.length,3);assert(s.room.members.some(m=>m.host));assert.equal(s.fighters[hostSlot].hp,0);assert.equal(s.fighters[hostSlot].deaths,0);assert.deepEqual(s.score,[0,0]);
 const nextSlot=s.room.members.find(m=>m.id===clients[2].join.playerId).slot;clients[2].send({type:'leave'});await wait(()=>snapshot(clients[3])?.fighters[nextSlot].withdrawn);
 const before=snapshot(clients[3]);await pause(4800);const after=snapshot(clients[3]);assert(after.tick>before.tick+100);assert(after.timeLeft<before.timeLeft-4);assert(!after.finished);assert(after.fighters[hostSlot].withdrawn&&after.fighters[nextSlot].withdrawn);assert.equal(after.room.members.length,2);assert.deepEqual(after.score,[0,0]);
 const other=await wait(()=>clients[1].messages.find(m=>m.type==='snapshot'&&m.tick===after.tick));assert.deepEqual(after.fighters,other.fighters);assert.equal(after.match,initial.match);
 const self=after.room.members.find(m=>m.id===clients[3].join.playerId).slot,p=after.fighters[self].position;for(let i=0;i<15;i++){clients[3].send({type:'input',seq:i,mx:1,mz:0,ax:0,az:1});await pause(35);}await wait(()=>snapshot(clients[3]).fighters[self].position.x>p.x+.3);
 console.log('LEAVE_CONTINUATION_PASS host and guest exit, host migration, fixed slots, no respawn/AI/score, remaining clients move and receive identical snapshots, timer continues');
}finally{for(const c of clients)if(c.socket.readyState===1){c.send({type:'leave'});c.socket.close();}}
