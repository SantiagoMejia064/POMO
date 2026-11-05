// Outputs a surgical apply_patch patch; does not overwrite the scene directly.
const fs = require('fs');
const assert = require('assert/strict');
const path = 'Assets/Scenes/Santiago.unity';
const source = fs.readFileSync(path, 'utf8').replace(/\r\n/g, '\n');
const blocks = new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],suffix:m[3]||'',text:m[4]}]));
const before = new Map([...blocks].map(([id,b])=>[id,b.text]));
function set(id,key,value){const b=blocks.get(id);assert(b,id);const pattern=new RegExp('^  '+key+':.*$','m');assert(pattern.test(b.text),id+': '+key);b.text=b.text.replace(pattern,()=>'  '+key+': '+value);}
function rect(id,min,max){
    set(id,'m_AnchorMin',`{x: ${min[0]}, y: ${min[1]}}`);
    set(id,'m_AnchorMax',`{x: ${max[0]}, y: ${max[1]}}`);
    set(id,'m_AnchoredPosition','{x: 0, y: 0}');
    set(id,'m_SizeDelta','{x: 0, y: 0}');
    set(id,'m_LocalScale','{x: 1, y: 1, z: 1}');
}
rect('99447982',[.07,.36],[.93,.68]);
rect('625270525',[.07,.64],[.93,.93]);
rect('1375918833',[.07,.32],[.93,.58]);
rect('1366938778',[.08,.61],[.92,.615]);
rect('1008230686',[.06,.06],[.48,.23]);
rect('2067360744',[.52,.06],[.94,.23]);
for(const [id,max,min] of [['625270526',60,28],['1375918834',48,26]]){
    set(id,'m_enableAutoSizing',1);set(id,'m_fontSize',max);set(id,'m_fontSizeMax',max);set(id,'m_fontSizeMin',min);
    set(id,'m_TextWrappingMode',1);set(id,'m_overflowMode',1);set(id,'m_RaycastTarget',0);
}
set('99447981','m_Name','Resumen de tarea');
set('625270524','m_Name','Título de la tarea');
set('1375918832','m_Name','Asignatura de la tarea');
set('1366938777','m_Name','Separador del resumen');
// Gagalin/rounding must not override date numbers or the grid's data labels.
const style=blocks.get('946000000000085');
if(!style.text.includes('  - {fileID: 853331057}\n')) style.text+='  - {fileID: 853331057}\n';
const callbacks=t=>t.match(/  m_OnClick:\n[\s\S]*$/)?.[0];
for(const [id,b]of blocks){assert.equal(callbacks(b.text),callbacks(before.get(id)),'Changed callback '+id);for(const m of b.text.matchAll(/\{fileID: (-?\d+)\}/g))assert(m[1]==='0'||blocks.has(m[1]),'Missing local reference '+id+' -> '+m[1]);}
fs.writeFileSync('Logs/Sprint4/Santiago.before-calendar-layout.unity',source);
const changes=[...blocks].filter(([id,b])=>b.text!==before.get(id));
console.error(JSON.stringify({changedBlocks:changes.length,callbacksPreserved:true,missingReferences:0}));
console.log('*** Begin Patch\n*** Update File: '+path+'\n'+changes.map(([id,b])=>'@@\n --- !u!'+b.type+' &'+id+b.suffix+'\n'+before.get(id).trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+b.text.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n').join('')+'*** End Patch');
