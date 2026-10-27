export function textPages(text,columns,rows) {
  columns=Math.max(1,Math.floor(columns));rows=Math.max(1,Math.floor(rows));
  const lines=String(text).split('\n').flatMap(line=>{
    const result=[];for(let i=0;i<line.length;i+=columns)result.push(line.slice(i,i+columns));
    return result.length?result:[''];
  });
  const pages=[];for(let i=0;i<lines.length;i+=rows)pages.push(lines.slice(i,i+rows).join('\n'));
  return pages.length?pages:[''];
}
