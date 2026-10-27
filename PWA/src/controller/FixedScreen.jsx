import {textPages} from './paging.js';
import { useEffect, useLayoutEffect, useRef, useState } from 'react';

export function FixedScreen({title,children,nav}) {
  useEffect(()=>{
    const viewport=window.visualViewport;
    const update=()=>{
      document.documentElement.style.setProperty('--app-height',`${viewport?.height ?? window.innerHeight}px`);
      document.documentElement.style.setProperty('--app-top',`${viewport?.offsetTop ?? 0}px`);
      document.documentElement.style.setProperty('--app-width',`${viewport?.width ?? window.innerWidth}px`);
      document.documentElement.style.setProperty('--app-left',`${(viewport?.offsetLeft ?? 0)+(viewport?.width ?? window.innerWidth)/2}px`);
    };
    update();viewport?.addEventListener('resize',update);viewport?.addEventListener('scroll',update);window.addEventListener('resize',update);
    return ()=>{viewport?.removeEventListener('resize',update);viewport?.removeEventListener('scroll',update);window.removeEventListener('resize',update);};
  },[]);
  return <main className="fixed-screen">
    <header className="fixed-brand"><img src="/brand/island-seal.png" alt=""/><h1>{title}</h1></header>
    <section className="fixed-content">{children}</section>
    <nav className="fixed-nav" aria-label="Pantallas del control">{nav}</nav>
  </main>;
}

// Preserve the complete text without scroll: wrap into measured, explicit pages.
export function PagedText({text,label='Información'}) {
  const box=useRef(null);const [size,setSize]=useState([24,6]);const [page,setPage]=useState(0);
  useLayoutEffect(()=>{
    const measure=()=>{
      const el=box.current;if(!el)return;const style=getComputedStyle(el);
      setSize([Math.max(1,Math.floor(el.clientWidth/(parseFloat(style.fontSize)*.65))),Math.max(1,Math.floor(el.clientHeight/parseFloat(style.lineHeight)))]);
    };
    const observer=new ResizeObserver(measure);observer.observe(box.current);measure();return ()=>observer.disconnect();
  },[]);
  const pages=textPages(text,...size);const current=Math.min(page,pages.length-1);
  return <div className="paged-text">
    <pre ref={box} aria-label={label}>{pages[current]}</pre>
    <div className="text-pages"><button disabled={current===0} onClick={()=>setPage(current-1)} aria-label="Página anterior">←</button><span role="status">{current+1} / {pages.length}</span><button disabled={current===pages.length-1} onClick={()=>setPage(current+1)} aria-label="Página siguiente">→</button></div>
  </div>;
}
