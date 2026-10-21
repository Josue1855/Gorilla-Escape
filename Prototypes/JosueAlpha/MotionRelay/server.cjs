const http = require('http'); const fs = require('fs'); const path = require('path');
const rooms = new Map();
http.createServer((req,res)=>{const u=new URL(req.url,'http://localhost');
 if(u.pathname==='/motion'){const room=(u.searchParams.get('room')||'').toUpperCase(); res.setHeader('Cache-Control','no-store');
  if(req.method==='POST'){let b='';req.on('data',d=>b+=d);req.on('end',()=>{rooms.set(room,b);res.end('ok')});return}
  const b=rooms.get(room); if(!b){res.statusCode=204;return res.end()} res.setHeader('Content-Type','application/json');return res.end(b)}
 if(u.pathname==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');return fs.createReadStream(path.join(__dirname,'index.html')).pipe(res)} res.statusCode=404;res.end()
}).listen(8788,'127.0.0.1',()=>console.log('MotionLink local listo en puerto 8788'));
