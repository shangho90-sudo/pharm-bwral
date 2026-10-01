mergeInto(LibraryManager.library, {
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
