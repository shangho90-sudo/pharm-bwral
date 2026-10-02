mergeInto(LibraryManager.library, {
  RoomEditText: function(target,value,limit,x,y,width,height) {
    if(window.pharmaTextInput)window.pharmaTextInput.blur();
    var objectName=UTF8ToString(target),input=document.createElement('input');
    var canvas=document.getElementById('unity-canvas'),rect=canvas.getBoundingClientRect();
    input.type='text';input.value=UTF8ToString(value);input.maxLength=limit;
    input.setAttribute('aria-label',objectName.indexOf('닉네임')>=0?'닉네임':'방 이름');
    input.autocomplete='off';input.spellcheck=false;
    Object.assign(input.style,{position:'fixed',zIndex:'30',left:(rect.left+x*rect.width)+'px',top:(rect.top+y*rect.height)+'px',width:(width*rect.width)+'px',height:(height*rect.height)+'px',boxSizing:'border-box',background:'#13273c',color:'white',border:'2px solid #53d6bd',borderRadius:'5px',fontSize:Math.max(14,rect.width/1600*24)+'px',padding:'0 8px'});
    input.oninput=function(){SendMessage(objectName,'OnBrowserText',input.value);};
    input.onblur=function(){SendMessage(objectName,'OnBrowserDone',input.value);input.remove();if(window.pharmaTextInput===input)window.pharmaTextInput=null;};
    input.onkeydown=function(event){event.stopPropagation();if(event.key==='Enter'&&!event.isComposing)input.blur();};
    document.body.appendChild(input);window.pharmaTextInput=input;input.focus();input.select();
  },
  RoomEndText: function(target){var input=window.pharmaTextInput;if(input)input.blur();},
  RoomSocketConnect: function(url, target, hello) {
    var address=UTF8ToString(url), objectName=UTF8ToString(target), greeting=UTF8ToString(hello);
    if(window.pharmaSocket) { window.pharmaSocket.onclose=null;window.pharmaSocket.close(); }
    var socket=new WebSocket(address);window.pharmaSocket=socket;
    socket.onopen=function(){socket.send(greeting);SendMessage(objectName,'OnSocketOpen','');};
    socket.onmessage=function(event){SendMessage(objectName,'OnSocketMessage',event.data);};
    socket.onclose=function(){SendMessage(objectName,'OnSocketClose','');};
    socket.onerror=function(){socket.close();};
  },
  RoomSocketSend: function(message){if(window.pharmaSocket && window.pharmaSocket.readyState===1)window.pharmaSocket.send(UTF8ToString(message));},
  RoomSocketClose: function(){if(window.pharmaSocket){window.pharmaSocket.onclose=null;window.pharmaSocket.close();window.pharmaSocket=null;}},
  RoomIsTouch: function(){return new URLSearchParams(location.search).get("touch")==="1" || navigator.maxTouchPoints>0 || /Android|iPhone|iPad/i.test(navigator.userAgent);}
});
