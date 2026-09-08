// Demo contracts and motion checks; these do not execute or benchmark Hawta.
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import vm from 'node:vm';

const read = name => readFile(new URL(name, import.meta.url), 'utf8');
const context = {window:{}};
vm.runInNewContext(await read('mechanisms.js'), context);
const {mechanisms, references, stateAt, explorationFor, illustrativeMotion, particleAt} = context.window.HawtaEngineering;
const evidence = JSON.parse(await read('evidence.json'));
const ids = new Set(evidence.entries.map(e=>e.id));
assert.equal(ids.size, evidence.entries.length);
for (const reference of references) assert.ok(reference.evidence.every(id=>ids.has(id)), reference.title);
for (const entry of evidence.entries) {
  assert.match(entry.path, /^ADP\.Hawta\/(Hawta|Hawta\.Tests)\/[A-Za-z0-9]+\.cs$/);
  assert.equal(entry.excerpt.split('\n').length, entry.end-entry.start+1);
}
assert.equal(mechanisms.length, 8);
assert.equal(mechanisms.reduce((n,m)=>n+m.scenarios.length,0), 30);
for (const m of mechanisms) for (const s of m.scenarios) {
  assert.equal(s.origin, 'illustrative');
  for (const [i,event] of s.events.entries()) {
    assert.equal(event.schemaVersion, 1);
    assert.equal(event.mechanismId, m.id);
    assert.equal(event.scenarioId, s.id);
    assert.equal(event.observedAt, null);
    assert.equal(event.origin, 'illustrative');
    assert.ok(event.evidenceRefs.every(ref=>ids.has(ref)));
    assert.ok(i===0 ? event.at===0 : event.at>s.events[i-1].at);
    assert.equal(stateAt(s.events,event.at).current,i);
    if(i>0) assert.equal(stateAt(s.events,event.at-1).current,i-1);
  }
  const final = stateAt(s.events, Number.MAX_SAFE_INTEGER);
  assert.equal(final.complete, true);
  assert.equal(final.state.kind, m.kind);
  const view=explorationFor(m,s);
  assert.equal(view.scenarioId,s.id);
  assert.equal(view.mechanismId,m.id);
  assert.equal(view.origin,'illustrative');
  assert.equal(new Set(view.signals.map(s=>s.edge)).size,view.signals.length);
  for(const cue of view.signals) {
    assert.equal(cue.start,undefined);
    assert.equal(cue.end,undefined);
    const edge=view.edges[cue.edge];
    assert.ok(edge && edge.every(id=>view.nodes.some(node=>node.id===id)));
    // Every applicable connection is populated at entry and throughout a loop.
    for(let elapsed=0;elapsed<=illustrativeMotion.period;elapsed+=10) {
      const poses=Array.from({length:illustrativeMotion.count},(_,i)=>particleAt(elapsed,i,cue.edge*illustrativeMotion.routeOffset));
      assert.ok(poses.filter(p=>p.opacity>.1).length>=2,`${m.id}/${s.id}: empty or delayed route ${cue.edge}`);
      assert.ok(poses.every(p=>p.fraction>=0&&p.fraction<1));
    }
  }
}
assert.equal(illustrativeMotion.period,3400);
assert.equal(illustrativeMotion.count,3);
assert.equal(illustrativeMotion.routeOffset,.17);
for(let index=0;index<3;index++) {
  const entry=particleAt(0,index,.17),loop=particleAt(3400,index,.17);
  assert.equal(entry.radius,index===0?3.4:2.4);
  assert.ok(Math.abs(entry.fraction-loop.fraction)<1e-12);
  assert.ok(Math.abs(entry.opacity-loop.opacity)<1e-12);
  assert.ok(Math.abs(particleAt(850,index,.17).fraction-((.25+index/3+.17)%1))<1e-12);
}
const explore=(m,s)=>explorationFor(mechanisms.find(x=>x.id===m),mechanisms.find(x=>x.id===m).scenarios.find(x=>x.id===s));
const paths=view=>Array.from(new Set(view.signals.map(cue=>view.edges[cue.edge].join('>'))));
assert.deepEqual(paths(explore('file-gate','unchanged')),['file>gate','gate>skip']);
assert.ok(!explore('file-gate','unchanged').signals.some(s=>s.kind==='rows'));
for(const id of ['absent','empty']) assert.ok(!explore('file-gate',id).signals.some(s=>s.kind==='rows'));
assert.ok(paths(explore('file-gate','touched')).includes('file>read'));
assert.ok(paths(explore('file-gate','touched')).includes('identity>skip'));
assert.ok(!paths(explore('file-gate','touched')).includes('identity>merge'));
for(const id of ['changed','configuration','expired']) assert.ok(paths(explore('file-gate',id)).includes('identity>merge'));
assert.deepEqual(paths(explore('projection','unchanged')),['a>g','b>g']);
assert.ok(!paths(explore('replication','failure')).includes('c>a'));
assert.ok(!paths(explore('replication','success')).includes('c>f'));
assert.ok(!paths(explore('publish','interrupted')).includes('f>m'));
assert.equal(explore('publish','interrupted').signals.find(s=>s.edge===3).label,'P1');
assert.deepEqual(paths(explore('residency','defer')),['p>g','g>w']);
assert.deepEqual(paths(explore('recon','blind')),['s>g','c>g']);
assert.ok(!paths(explore('recon','pending')).includes('g>d'));
assert.ok(!paths(explore('recon','divergent')).includes('g>p'));
const ordered=explore('fetch-window','order');
assert.equal(ordered.nodes.find(n=>n.id==='m').body,'Serial: A1 → A2 → B3');
assert.deepEqual(paths(ordered).sort(),['a>q','b>q','c>q','q>m']);
const final = (m,s) => stateAt(mechanisms.find(x=>x.id===m).scenarios.find(x=>x.id===s).events,Number.MAX_SAFE_INTEGER).state;
assert.equal(final('file-gate','unchanged').outcome,'skip');
assert.equal(final('file-gate','touched').change,'FileChanged');
assert.equal(final('file-gate','absent').outcome,'guard');
assert.equal(final('file-gate','empty').outcome,'guard');
assert.equal(final('row-merge','duplicate').phase,3);
assert.equal(final('row-merge','invalid').phase,1);
assert.equal(final('row-merge','wipe').phase,4);
assert.equal(final('row-merge','mixed').phase,5);
assert.equal(final('projection','unchanged').nodes.p,'skipped');
assert.equal(final('replication','newer').values.r,'Current version v18');
assert.equal(final('replication','newer').values.d,'Still dirty · v18 pending');
assert.equal(final('replication','failure').nodes.a,'skipped');
assert.equal(final('publish','interrupted').values.r,'Still P1 = A1 + B1');
assert.equal(final('publish','interrupted').nodes.m,'blocked');
assert.equal(final('residency','defer').nodes.h,'skipped');
assert.equal(final('residency','hydrate').values.r,'Resident until process ends');
assert.equal(final('recon','blind').values.g,'InSync here · freshness unknown');

// Check the sample's canonical typed-row hashes independently.
const hash = values => createHash('md5').update(values.map(v=>v===null?'\0':'v'+String(v).length+':'+v).join('\x1f')).digest('hex');
assert.equal(hash(['FILTER',10,null]),'5955745752850829e1f77c6c51d3cb4e');
assert.equal(hash(['BELT',8,null]),'681eba9b08e0412a1d7d4e36c1311caf');
assert.equal(hash(['BELT',12,null]),'22dd420d465d3bad7a10ba9f644ffe01');
assert.notEqual(hash(['FILTER',10,null]),hash(['FILTER',10,'']));
assert.notEqual(hash(['ab','c']),hash(['a','bc']));

// Exercise the same shared motion controller used by overview and detail views.
const animation = await read('animation.js');
for (const initiallyReduced of [false,true]) {
  class Element {
    constructor() { this.dataset={};this.listeners={};this.attrs={};this.classList={toggle(){}}; }
    setAttribute(k,v) { this.attrs[k]=v; }
    addEventListener(k,fn) { this.listeners[k]=fn; }
    click() { this.listeners.click?.(); }
    append() {} replaceChildren() {}
    getBoundingClientRect() { return {left:0,top:0,right:100,bottom:100,width:100,height:100}; }
    getTotalLength() { return 100; }
    getPointAtLength(n) { return {x:n,y:n}; }
  }
  const elements=new Map();
  const element=id=>{ if(!elements.has(id))elements.set(id,new Element());return elements.get(id); };
  const buttons=['collect','snapshot','serving','cosmos','publish'].map(stage=>{
    const b=new Element();b.dataset.stage=stage;b.parentElement=new Element();return b;
  });
  const media={matches:initiallyReduced,addEventListener(_,fn){this.changed=fn;}};
  const emitted=[];
  const page={body:new Element(),documentElement:new Element(),hidden:false,
    querySelector:element,getElementById:element,querySelectorAll:()=>buttons,
    createElementNS:()=>new Element(),addEventListener(){},dispatchEvent:e=>emitted.push(e)};
  const frames=[];
  const sandbox={window:{innerWidth:1440,matchMedia:()=>media},document:page,
    requestAnimationFrame:fn=>frames.push(fn),ResizeObserver:class{observe(){}},
    CustomEvent:class{constructor(type,options){this.type=type;this.detail=options.detail;}}};
  vm.runInNewContext(animation,sandbox);
  const motion=sandbox.window.HawtaMotion;
  assert.equal(motion.isPlaying(),!initiallyReduced);
  motion.pause();assert.equal(page.body.dataset.playing,'false');
  motion.resume();assert.equal(page.body.dataset.playing,'true');
  buttons[2].click();
  for(let i=0;i<20;i++) frames.shift()(i*100);
  assert.equal(page.body.dataset.selectedStage,'serving');
  media.matches=true;media.changed();
  assert.equal(motion.isPlaying(),false);
  assert.equal(page.body.dataset.motionVisible,'false');
  assert.equal(emitted.at(-1).detail.playing,false);
}
console.log('PASS: 30 scene/event contracts, simultaneous particles over a full loop, valid-route masks, typed hashes, reduced-motion controller and stable overview selection.');
