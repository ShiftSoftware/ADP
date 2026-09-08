(() => {
  'use strict';
  const {mechanisms, references, explorationFor, illustrativeMotion, particleAt} = window.HawtaEngineering;
  const motion = window.HawtaMotion;
  const $ = id => document.getElementById(id);
  const esc = value => String(value ?? '').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const stageMap={collect:'file-gate',snapshot:'row-merge',serving:'projection',cosmos:'replication',publish:'publish'};
  const lastScenarios={}, selections={}, drafts={};
  let mechanism=mechanisms[0], selectedScenario=mechanism.scenarios[0], layer='behavior', view;
  let elapsed=0,lastFrame=null,scenePaths=[],evidenceRequest=0,resizeScheduled=false;
  const statusNames={idle:'Outside this path',working:'Work required',passed:'Applies',waiting:'Conditional',skipped:'Bypassed',warning:'Decision matters',blocked:'Refused'};
  $('mechanism-picker').innerHTML=mechanisms.map(m=>'<option value="'+m.id+'">'+esc(m.short)+'</option>').join('');
  $('reference-list').innerHTML=references.map(r=>'<article><h3>'+esc(r.title)+'</h3><p>'+esc(r.text)+'</p><button type="button" class="reference-source" data-reference="'+r.evidence.join(',')+'">Read source evidence</button></article>').join('');
  $('reference-list').addEventListener('click',event=>{
    const button=event.target.closest('[data-reference]');if(!button)return;
    setLayer('evidence');renderEvidence(button.dataset.reference.split(','));
  });
  $('explore-inside').addEventListener('click',()=>{location.hash=stageMap[document.body.dataset.selectedStage]||'file-gate';});
  $('mechanism-picker').addEventListener('change',event=>{location.hash=event.target.value;});
  $('scenario-picker').addEventListener('change',event=>{
    selectedScenario=mechanism.scenarios.find(s=>s.id===event.target.value);
    lastScenarios[mechanism.id]=selectedScenario.id;$('brief-result').hidden=true;renderScenario();
  });
  document.querySelectorAll('[data-layer]').forEach(button=>button.addEventListener('click',()=>setLayer(button.dataset.layer)));
  $('close-inspector').addEventListener('click',()=>{
    $('inspector').dataset.open='false';
    const selected=[...$('scene').querySelectorAll('button[data-select]')].find(b=>b.dataset.select===selections[mechanism.id]);
    selected?.focus({preventScroll:true});
  });
  document.addEventListener('keydown',event=>{
    if(event.key==='Escape'&&window.innerWidth<=900&&$('inspector').dataset.open==='true')$('close-inspector').click();
  });
  $('scene').addEventListener('click',event=>{
    const target=event.target.closest('[data-select]');if(target)selectExplanation(target.dataset.select);
  });
  $('detail-motion').addEventListener('click',()=>motion.toggle());
  document.addEventListener('hawta:motion-state',updateMotionControl);
  document.addEventListener('visibilitychange',()=>{lastFrame=null;});
  window.addEventListener('hashchange',navigate);

  function navigate() {
    const next=mechanisms.find(m=>m.id===location.hash.slice(1));
    $('overview').hidden=Boolean(next);$('engineering').hidden=!next;
    document.body.dataset.view=next?'detail':'overview';lastFrame=null;
    if(next) {
      mechanism=next;selectedScenario=mechanism.scenarios.find(s=>s.id===lastScenarios[mechanism.id])||mechanism.scenarios[0];
      $('mechanism-picker').value=mechanism.id;$('mechanism-title').textContent=mechanism.short;
      $('scenario-picker').innerHTML=mechanism.scenarios.map(s=>'<option value="'+s.id+'">'+esc(s.label)+'</option>').join('');
      $('scenario-picker').value=selectedScenario.id;
      populateChoices();populateDraft();renderScenario();setLayer('behavior',false);$('inspector').dataset.open='false';
    }
    window.scrollTo({top:0,behavior:'instant'});
  }
  function setLayer(name,reveal=true) {
    layer=name;
    if(reveal)$('inspector').dataset.open='true';
    document.querySelectorAll('[data-layer]').forEach(button=>button.setAttribute('aria-pressed',String(button.dataset.layer===name)));
    document.querySelectorAll('.inspection-layer').forEach(panel=>{panel.hidden=panel.id!=='layer-'+name;});
    if(name==='evidence')renderEvidence();
  }
  function updateMotionControl() {
    $('detail-motion').textContent=motion.isPlaying()?'Ⅱ Pause motion':'▶ Resume motion';
    $('detail-motion').setAttribute('aria-label',motion.isPlaying()?'Pause detail motion':'Resume detail motion');
    $('scenario-status').textContent=motion.isPlaying()?'Illustrative paths · continuous':'Motion paused';lastFrame=null;
  }
  function renderScenario() {
    view=explorationFor(mechanism,selectedScenario);elapsed=0;lastFrame=null;
    $('scene').dataset.mechanism=mechanism.id;$('scene').dataset.scenario=selectedScenario.id;$('scene').dataset.mode='continuous';
    $('scene').innerHTML=view.kind==='rows'?rowScene():nodeScene();
    $('scenario-assumptions').textContent=selectedScenario.assumptions;
    $('scenario-context').innerHTML=selectedScenario.events.map(event=>'<article><h3>'+esc(event.title)+'</h3><p>'+esc(event.detail)+'</p></article>').join('');
    const previous=selections[mechanism.id];
    const exists=[...$('scene').querySelectorAll('[data-select]')].some(el=>el.dataset.select===previous);
    selectExplanation(exists?previous:view.defaultSelection,false);
    drawScenePaths();
  }
  function nodeScene() {
    const cards=view.nodes.map(n=>'<button type="button" class="scene-node '+n.status+'" data-scene-node="'+n.id+'" data-select="'+n.id+'" aria-label="Explain '+esc(n.title)+'" aria-pressed="false"><span class="node-title">'+esc(n.title)+'</span>'+(view.kind==='file'&&n.id==='file'?'<span class="file-stack" aria-hidden="true"><i></i><i></i><i></i><i></i></span>':'')+'<span class="node-copy">'+esc(n.body)+'</span><span class="node-state">'+esc(statusNames[n.status])+'</span></button>').join('');
    let footer='';
    if(view.kind==='file') {
      const read=!['Unchanged','SourceAbsent','SourceEmpty'].includes(view.state.change), merge=read&&selectedScenario.id!=='touched';
      footer='<div class="work-summary">'+[['Full read',read],['Typed staging',read],['Row hashing',read],['Hydrate + merge',merge]].map(([label,done])=>'<span>'+label+'<strong class="'+(done?'':'avoided')+'">'+(done?'Required':'Avoided')+'</strong></span>').join('')+'</div>';
    }
    return '<div class="scene-diagram '+(view.kind==='file'?'file-diagram':'graph-diagram')+'">'+cards+'</div>'+footer;
  }

  const sample={
    A:{part:'FILTER',qty:10,note:null,hash:'5955745752850829e1f77c6c51d3cb4e'},
    B:{part:'BELT',qty:8,note:null,hash:'681eba9b08e0412a1d7d4e36c1311caf'},
    C:{part:'PLUG',qty:7,note:null,hash:'7e54d5fe14917bb4f92405d3a7d9c54b'},
    D:{part:'OIL',qty:4,note:null,hash:'db3273c9a72d2b0cd04ae9bab99983ea'},
    E:{part:'OTHER',qty:9,note:null,hash:null}
  };
  let rowFacts={};
  function rowScene() {
    const type=selectedScenario.id, stored={A:sample.A,B:sample.B,D:sample.D,E:sample.E};
    let incoming={A:sample.A,B:{...sample.B,qty:12,hash:'22dd420d465d3bad7a10ba9f644ffe01'},C:sample.C};
    if(['same','source-only','duplicate'].includes(type))incoming={A:sample.A,B:sample.B,D:sample.D};
    if(type==='source-only')incoming.B={...sample.B,note:'corrected note',hash:'330d9cc14ffe566b4d6ca123c42affc1'};
    if(type==='invalid')incoming.B={...sample.B,qty:'bad',hash:null};
    if(type==='wipe')incoming={};
    rowFacts={};const keys=['A','B','C','D','E'].filter(k=>incoming[k]||stored[k]);
    for(const key of keys) {
      const before=stored[key],after=incoming[key];
      const status=key==='E'?'outside':!after?'missing':!before?'inserted':after.hash===before.hash?'same':'updated';
      let outcome=key==='E'?'Outside this merge':type==='invalid'||type==='duplicate'||type==='wipe'?'No accepted mutation':!after?type==='deletes-off'?'Retain stored row':'Tombstone D':!before?'Insert C':status==='same'?'Keep stored row':'Update B';
      let explanation=key==='E'?'E belongs to scope B. It is not a deletion candidate in this scope-A merge.':!after?'No incoming row matches this stored key. Missing is a deletion candidate only with a full-universe input, deletion enabled, and safeguards satisfied.':!before?'C is a new unique key in scope A. It becomes an insert only after the complete staged set passes validation.':status==='same'?'The typed values and row hash agree. There is no business mutation and no new replication work.':'The same key has a different typed row hash. Compare source and replication hashes separately before queuing document work.';
      if(type==='source-only'&&key==='B')explanation='Only Note changes. The full row hash changes from 681eba9… to 330d9cc…, while the Part + Qty replication hash remains ac3b4a6…. Update the source snapshot without new Cosmos work.';
      if(type==='invalid'&&key!=='E')explanation=key==='B'?'Qty="bad" cannot become an integer. Typed staging fails before hashing or merge.':'Typing fails elsewhere in this staged set. No accepted hashes or mutations are produced for the invalid input.';
      if(type==='duplicate'&&key==='A')explanation='A appears twice. Duplicate staging identities are rejected before any merge transaction.';
      rowFacts[key]={before,after,status:type==='invalid'&&key==='B'||type==='duplicate'&&key==='A'?'invalid':status,outcome,explanation};
    }
    const labels={same:'= unchanged',updated:'Δ changed',inserted:'+ new',missing:'− missing',outside:'scope B',invalid:type==='duplicate'?'duplicate':'invalid type'};
    function table(incomingSide) {
      return '<table class="sample-table"><thead><tr><th>Key</th><th>Part / Qty</th><th>Row hash</th></tr></thead><tbody>'+keys.map((key,i)=>{
        const f=rowFacts[key],row=incomingSide?f.after:f.before,valid=type!=='invalid';
        return '<tr class="'+f.status+'" data-select="row-'+key+'" data-row="'+key+'" style="--row-delay:'+(-i*1.2)+'s"><td><button type="button" data-select="row-'+key+'" aria-label="Explain '+(incomingSide?'incoming':'stored')+' row '+key+'" aria-pressed="false">'+key+(incomingSide&&type==='duplicate'&&key==='A'?' ×2':'')+'</button></td><td>'+(row?esc(row.part)+' <strong>'+esc(row.qty)+'</strong>'+(row.note?'<small>'+esc(row.note)+'</small>':''):'—')+'</td><td><code title="'+esc(row?.hash||'')+'">'+(row?.hash&&(!incomingSide||valid)?row.hash.slice(0,7)+'…':'—')+'</code><small>'+esc(incomingSide&&type==='invalid'?'typing failed':labels[f.status])+'</small>'+(!incomingSide?'<small class="row-outcome">'+esc(f.outcome)+'</small>':'<small class="row-outcome" aria-hidden="true">&nbsp;</small>')+(key!=='E'&&row&&valid&&type!=='duplicate'?'<span class="row-signal" aria-hidden="true"></span>':'')+'</td></tr>';
      }).join('')+'</tbody></table>';
    }
    return '<div class="row-pair"><section><h2>'+(type==='invalid'?'Incoming · typing fails':'Typed staging')+' <span>Scope A</span></h2>'+table(true)+'</section><section><h2>Stored baseline <span>Scope A + B</span></h2>'+table(false)+'</section></div><div class="row-decisions">'+[['hash','Typing & hashes'],['guards','Scope & safeguards'],['outcome','Mutation result']].map(([id,label])=>'<button type="button" data-select="'+id+'" aria-pressed="false">'+label+'</button>').join('')+'</div><div class="row-result">'+(['invalid','duplicate','wipe'].includes(type)?'<span class="guard-signal" aria-hidden="true"></span>':'')+esc(type==='invalid'?'Invalid typed input → stop before hashing':type==='duplicate'?'Duplicate key A → reject the staged set':type==='wipe'?'Whole-scope wipe → transaction refused':type==='same'?'Equal typed content → no row mutations':type==='source-only'?'Source-only edit → snapshot update, no new Cosmos work':type==='deletes-off'?'Insert C · update B · retain D':'Insert C · update B · tombstone D')+'</div>';
  }
  function selectExplanation(id,open=true) {
    selections[mechanism.id]=id;
    document.querySelectorAll('#scene [data-select]').forEach(el=>{
      const selected=el.dataset.select===id;el.classList.toggle('selected',selected);
      if(el.tagName==='BUTTON')el.setAttribute('aria-pressed',String(selected));
    });
    let title,detail,result,extra='';
    const n=view.nodes.find(n=>n.id===id);
    if(n) {title=n.title;detail=n.detail;result=n.body;}
    else if(id.startsWith('row-')) {const key=id.slice(4),f=rowFacts[key];title='Row '+key;detail=f.explanation;result=f.outcome;}
    else if(id==='hash') {
      title='Typed values before row hashes';detail='Bind and convert values first: the incoming text "10" becomes integer 10. Then hash the canonical typed fields. NULL is different from empty text, and lengths preserve field boundaries.';
      result=selectedScenario.id==='invalid'?'This input fails typing. No incoming row hashes are accepted.':'Sample A: v6:FILTER ␟ v2:10 ␟ NUL → MD5 → 5955745752850829e1f77c6c51d3cb4e';
    } else if(id==='guards') {title='Scope and safeguards';detail=mechanism.policies[2]+' Duplicate keys and invalid staging values must be rejected before accepted mutations.';result=selectedScenario.assumptions;}
    else {title='Mutation result';detail=view.outcome;result=selectedScenario.assumptions;}
    if(view.kind==='file'&&id==='gate'&&!['SourceAbsent','SourceEmpty'].includes(view.state.change)) {
      const labels=['Metadata available','Prior successful stamp','Configuration matches','Path matches','Size + last-write match','Trust interval permits skip'];
      extra='<ul class="gate-list">'+labels.map((label,i)=>'<li class="'+(view.state.checks[i]?'':'failed')+'"><b>'+ (view.state.checks[i]?'✓':'×')+'</b>'+label+'</li>').join('')+'</ul>';
    }
    $('selection-title').textContent=title;$('selection-detail').textContent=detail;
    $('selection-result').innerHTML='<span>In this scenario</span><p>'+esc(result)+'</p>'+extra;
    if(open)setLayer('behavior');
  }

  function drawScenePaths() {
    resizeScheduled=false;scenePaths=[];
    const container=$('scene').querySelector('.scene-diagram');
    if(!container||$('engineering').hidden)return;
    container.querySelector('.scene-wires')?.remove();
    const bounds=container.getBoundingClientRect();if(!bounds.width||!bounds.height)return;
    const ns='http://www.w3.org/2000/svg';const svg=document.createElementNS(ns,'svg');
    svg.setAttribute('class','scene-wires');svg.setAttribute('viewBox','0 0 '+bounds.width+' '+bounds.height);svg.setAttribute('aria-hidden','true');container.append(svg);
    const defs=document.createElementNS(ns,'defs'),marker=document.createElementNS(ns,'marker'),arrow=document.createElementNS(ns,'path');
    for(const [key,value] of Object.entries({id:'scene-arrow',viewBox:'0 0 8 8',refX:'7',refY:'4',markerWidth:'6',markerHeight:'6',orient:'auto'}))marker.setAttribute(key,value);
    arrow.setAttribute('d','M 1 1 L 7 4 L 1 7');arrow.setAttribute('fill','none');arrow.setAttribute('stroke','var(--route)');marker.append(arrow);defs.append(marker);svg.append(defs);
    view.edges.forEach(([from,to],index)=>{
      const a=container.querySelector('[data-scene-node="'+from+'"]').getBoundingClientRect(),b=container.querySelector('[data-scene-node="'+to+'"]').getBoundingClientRect();
      const dx=b.left+b.width/2-a.left-a.width/2,dy=b.top+b.height/2-a.top-a.height/2;
      let x1,y1,x2,y2,d;
      if(Math.abs(dx)>30&&Math.abs(dx)>Math.abs(dy)*.6) {
        const direction=dx>0?1:-1;x1=(direction===1?a.right:a.left)-bounds.left;y1=a.top+a.height/2-bounds.top;x2=(direction===1?b.left:b.right)-bounds.left;y2=b.top+b.height/2-bounds.top;
        const bend=Math.max(15,Math.abs(x2-x1)*.45);d='M '+x1+' '+y1+' C '+(x1+bend*direction)+' '+y1+', '+(x2-bend*direction)+' '+y2+', '+x2+' '+y2;
      } else {
        const direction=dy>0?1:-1;x1=a.left+a.width/2-bounds.left;y1=(direction===1?a.bottom:a.top)-bounds.top;x2=b.left+b.width/2-bounds.left;y2=(direction===1?b.top:b.bottom)-bounds.top;
        const bend=Math.max(15,Math.abs(y2-y1)*.45);d='M '+x1+' '+y1+' C '+x1+' '+(y1+bend*direction)+', '+x2+' '+(y2-bend*direction)+', '+x2+' '+y2;
      }
      const cue=view.signals.find(s=>s.edge===index);
      const path=document.createElementNS(ns,'path');path.setAttribute('d',d);path.setAttribute('class','scene-wire '+(cue?'applicable':'inactive'));path.dataset.edge=String(index);path.dataset.from=from;path.dataset.to=to;if(cue)path.setAttribute('marker-end','url(#scene-arrow)');svg.append(path);
      const packets=cue?Array.from({length:illustrativeMotion.count},(_,particleIndex)=>{
        const packet=document.createElementNS(ns,'circle');
        packet.setAttribute('r',String(particleAt(0,particleIndex,0).radius));
        packet.setAttribute('class','scene-packet '+cue.kind);packet.dataset.edge=String(index);svg.append(packet);
        let label;if(particleIndex===0&&cue.label){label=document.createElementNS(ns,'text');label.textContent=cue.label;label.setAttribute('class','signal-label');svg.append(label);}
        return {packet,label};
      }):[];
      scenePaths.push({path,length:path.getTotalLength(),packets,offset:index*illustrativeMotion.routeOffset});
    });animatePaths();
  }
  function animatePaths() {
    for(const {path,length,packets,offset} of scenePaths)packets.forEach(({packet,label},index)=>{
      const pose=particleAt(elapsed,index,offset),point=path.getPointAtLength(length*pose.fraction);
      packet.setAttribute('transform','translate('+point.x+' '+point.y+')');packet.setAttribute('opacity',String(pose.opacity));
      if(label){label.setAttribute('x',point.x+7);label.setAttribute('y',point.y-9);label.setAttribute('opacity',String(pose.opacity));}
    });
  }
  new ResizeObserver(()=>{if(!resizeScheduled){resizeScheduled=true;requestAnimationFrame(drawScenePaths);}}).observe($('scene'));
  function populateChoices() {
    $('layer-choices').innerHTML = '<div class="choices-block"><h2>Current behavior and controls</h2><ul>' + mechanism.policies.map(p => '<li>' + esc(p) + '</li>').join('') + '</ul></div><div class="choices-block"><h2>Assumption or tradeoff</h2><p>' + esc(mechanism.tradeoff) + '</p></div><div class="choices-block"><h2>Alternative to evaluate</h2><p>' + esc(mechanism.alternative) + '</p></div><div class="evidence-caveat">The scenarios are authored demonstrations of these rules. Controls on this page never edit Hawta configuration.</div>';
  }
  async function loadEvidence() {
    const response=await fetch('evidence.json');
    if(!response.ok)throw new Error('unavailable');
    return response.json();
  }
  async function renderEvidence(ids = mechanism.evidence) {
    const request = ++evidenceRequest;
    const requestedMechanism = mechanism.id;
    $('evidence-status').textContent = 'Checking the captured excerpts against the local source files…';
    $('evidence-list').replaceChildren();
    try {
      const [evidence, freshness] = await Promise.all([loadEvidence(),fetch('evidence-status.json').then(r=>r.ok?r.json():null).catch(()=>null)]);
      if (request !== evidenceRequest || requestedMechanism !== mechanism.id || layer !== 'evidence') return;
      const sameCapture=freshness?.capturedAt===evidence.capturedAt&&freshness?.hashAlgorithm===evidence.hashAlgorithm;
      const statusMap = new Map((sameCapture?freshness.checks:[]).map(check=>[check.id,check]));
      $('evidence-status').textContent = 'Captured '+evidence.capturedAt.replace('T',' ').slice(0,19)+' UTC. '+(sameCapture ? 'Local file check: '+freshness.checkedAt.replace('T',' ').slice(0,19)+' UTC.' : freshness ? 'Capture changed while loading; reopen Evidence. Freshness unverified.' : 'Live local-file comparison unavailable; treat freshness as unverified.');
      $('evidence-list').innerHTML = '<div class="evidence-caveat">No pipeline timing benchmark is attached to this view. Historical figures inside source comments are context, not validated performance results for this iteration. Source inspection does not establish what is enabled in production.</div>' + ids.map(id => {
        const entry=evidence.entries.find(item=>item.id===id);if(!entry)return '';
        const check=statusMap.get(id),status=check?.sha256===entry.sha256?check.status:undefined;
        const label=status==='unchanged'?'Local file matches captured evidence':status==='changed'?'Changed since capture — review this scene':status==='unavailable'?'Source file unavailable — freshness unverified':'Freshness unverified';
        const lines=entry.excerpt.split('\n').map((line,index)=>String(entry.start+index).padStart(4,' ')+'  '+line).join('\n');
        return '<article class="evidence-card"><h3>'+esc(entry.path.split('/').at(-1))+' · lines '+entry.start+'–'+entry.end+'</h3><p class="evidence-path">'+esc(entry.path)+'</p><span class="source-status '+(status==='unchanged'?'':'stale')+'">'+label+'</span><p class="evidence-path">Captured SHA-256 (LF): '+entry.sha256.slice(0,20)+'…</p><details><summary>Read captured code</summary><pre>'+esc(lines)+'</pre></details></article>';
      }).join('');
    } catch {
      $('evidence-status').textContent='Evidence could not be loaded. Use the running localhost preview; source freshness is unverified.';
    }
  }

  function populateDraft() {
    const draft = drafts[mechanism.id] || {proposal:mechanism.proposal,reason:mechanism.tradeoff+' '+mechanism.alternative,validation:mechanism.validation};
    $('brief-proposal').value=draft.proposal;$('brief-reason').value=draft.reason;$('brief-validation').value=draft.validation;
    $('brief-result').hidden=true;$('copy-status').textContent='';
  }
  ['brief-proposal','brief-reason','brief-validation'].forEach(id=>$(id).addEventListener('input',()=>{
    drafts[mechanism.id]={proposal:$('brief-proposal').value,reason:$('brief-reason').value,validation:$('brief-validation').value};
    $('brief-result').hidden=true;
  }));
  $('prepare-brief').addEventListener('click',async()=>{
    const id=mechanism.id;
    let evidenceLines='Captured source references: '+mechanism.evidence.join(', ')+'. Evidence layer has exact excerpts.';
    try {
      const evidence=await loadEvidence();
      evidenceLines='Evidence captured '+evidence.capturedAt+'\n'+mechanism.evidence.map(ref=>evidence.entries.find(e=>e.id===ref)).filter(Boolean).map(e=>'- '+e.path+':'+e.start+' (SHA-256, LF-normalized: '+e.sha256+')').join('\n');
    } catch { evidenceLines+='\nFreshness unverified: evidence file unavailable.'; }
    if(id!==mechanism.id)return;
    $('brief-output').value=[
      '# Proposed change — '+mechanism.short,
      'UNSAVED REVIEW DRAFT. This is a proposal, not current behavior. No instruction has been sent and no configuration has been changed.',
      '## Context\nMechanism: '+mechanism.id+'\nIllustrative scenario: '+selectedScenario.label+'\nAssumptions: '+selectedScenario.assumptions,
      '## Current behavior\n'+mechanism.policies.map(p=>'- '+p).join('\n'),
      '## Proposed change\n'+$('brief-proposal').value,
      '## Reason and expected behavior\n'+$('brief-reason').value,
      '## Scope\n'+mechanism.scope,
      '## Validation criteria\n'+$('brief-validation').value,
      '## Evidence\n'+evidenceLines,
      '## Limits\nConfirm current source fingerprints in the Evidence layer before implementation. Production configuration and pipeline performance have not been verified by this demo. Scenario records and timing are illustrative. No live telemetry or automatic agent handoff is included.'
    ].join('\n\n');
    $('brief-result').hidden=false;$('copy-status').textContent='';
  });
  $('copy-brief').addEventListener('click',async()=>{
    try { await navigator.clipboard.writeText($('brief-output').value); $('copy-status').textContent='Draft copied. Nothing sent.'; }
    catch { $('copy-status').textContent='Select the draft text and copy it manually.'; }
  });

  function frame(timestamp) {
    if (!$('engineering').hidden && !document.hidden) {
      if (motion.isPlaying() && lastFrame!==null) elapsed=(elapsed+Math.min(100,timestamp-lastFrame))%view.period;
      animatePaths();
    }
    lastFrame=timestamp;requestAnimationFrame(frame);
  }
  updateMotionControl();navigate();requestAnimationFrame(frame);
})();
