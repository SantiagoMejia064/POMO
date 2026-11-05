// Adds required local name entry and a separate final data-deletion step.
const fs=require('fs'),assert=require('assert/strict');
const file='Assets/Scenes/Santiago.unity',source=fs.readFileSync(file,'utf8').replace(/\r\n/g,'\n');
const guid='22998596fa534806873b2441763a2d8c';assert(!source.includes(guid),'Name flow already exists.');
const b=new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],suffix:m[3]||'',text:m[4]}]));
const before=new Map([...b].map(([id,v])=>[id,v.text]));
const get=id=>{assert(b.has(id),id);return b.get(id);};
const field=(v,k)=>v.text.match(new RegExp('^  '+k+': (.*)$','m'))?.[1];
const ref=(v,k)=>field(v,k)?.match(/fileID: (-?\d+)/)?.[1];
const set=(id,k,value)=>{const re=new RegExp('^  '+k+(k==='m_text'?':[^\\n]*(?:\\n {4,}[^\\n]*)*':':.*$'),'m');assert(re.test(get(id).text),id+':'+k);get(id).text=get(id).text.replace(re,()=>'  '+k+': '+value);};
let next=951000000000000;const alloc=()=>{const id=String(++next);assert(!b.has(id));return id;};
const attach=(go,id)=>{get(go).text=get(go).text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/,'$1'+`  - component: {fileID: ${id}}\n`);};
const clone=(template,go)=>{const id=alloc();b.set(id,{...get(template),text:get(template).text.replace(/^  m_GameObject:.*$/m,`  m_GameObject: {fileID: ${go}}`)});attach(go,id);return id;};
function anchors(id,min,max){set(id,'m_AnchorMin',`{x: ${min[0]}, y: ${min[1]}}`);set(id,'m_AnchorMax',`{x: ${max[0]}, y: ${max[1]}}`);set(id,'m_AnchoredPosition','{x: 0, y: 0}');set(id,'m_SizeDelta','{x: 0, y: 0}');set(id,'m_LocalScale','{x: 1, y: 1, z: 1}');}
function child(parent,tr){get(parent).text=get(parent).text.includes('  m_Children: []')?get(parent).text.replace('  m_Children: []',`  m_Children:\n  - {fileID: ${tr}}`):get(parent).text.replace(/(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,'$1'+`  - {fileID: ${tr}}\n`);}
function node(name,parent,min=[0,0],max=[1,1]){const go=alloc(),tr=alloc();b.set(go,{...get('946000000000013'),text:get('946000000000013').text.replace(/  m_Component:\n(?:  - component:.*\n)+/,`  m_Component:\n  - component: {fileID: ${tr}}\n`).replace(/^  m_Name:.*$/m,'  m_Name: '+JSON.stringify(name))});b.set(tr,{...get('946000000000014'),text:get('946000000000014').text.replace(/^  m_GameObject:.*$/m,`  m_GameObject: {fileID: ${go}}`).replace(/^  m_Father:.*$/m,`  m_Father: {fileID: ${parent}}`)});anchors(tr,min,max);child(parent,tr);return {go,tr};}
function label(name,parent,value,min,max,size,template='946000000000020'){const n=node(name,parent,min,max);clone('946000000000015',n.go);const text=clone(template,n.go);set(text,'m_text',JSON.stringify(value));for(const k of ['m_fontSize','m_fontSizeBase','m_fontSizeMax'])set(text,k,size);set(text,'m_fontSizeMin',size*.7);set(text,'m_enableAutoSizing',1);set(text,'m_TextWrappingMode',1);set(text,'m_overflowMode',1);set(text,'m_RaycastTarget',0);set(text,'m_isRichText',0);return {...n,text};}
function button(name,parent,value,min,max,danger=false){const n=node(name,parent,min,max);clone('946000000000023',n.go);const image=clone('946000000000048',n.go),btn=clone('946000000000049',n.go);set(btn,'m_TargetGraphic',`{fileID: ${image}}`);set(btn,'m_Interactable',1);if(danger)set(image,'m_Color','{r: 0.67, g: 0.14, b: 0.19, a: 1}');label('Texto '+name,n.tr,value,[.04,.08],[.96,.92],42,'946000000000053');return {...n,button:btn};}
const welcomeHint=label('Aviso del nombre obligatorio','946000000000002','Escribe tu nombre para continuar.',[.12,.155],[.88,.21],30);
const nameController=alloc();b.set(nameController,{type:'114',suffix:'',text:`MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1915654018}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.Screens.NombreBienvenidaController
  nombre: {fileID: 1084177551}
  botonContinuar: {fileID: 1830129152}
  panelBienvenida: {fileID: 96494524}
  panelHola: {fileID: 2106442889}
  privacidad: {fileID: 946000000000084}
  avisoNombre: {fileID: ${welcomeHint.text}}
`});attach('1915654018',nameController);
set('1830129152','m_Interactable',0);
get('1830129152').text=get('1830129152').text.replace(/  m_OnClick:\n[\s\S]*$/,`  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: ${nameController}}
        m_TargetAssemblyTypeName: Pomo.Presentation.Screens.NombreBienvenidaController, Assembly-CSharp
        m_MethodName: Continuar
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument:
          m_BoolArgument: 0
        m_CallState: 2
`);
set('1084177549','m_Name','Nombre de bienvenida');set('1084177551','m_CharacterLimit',60);set('1084177551','m_RichText',0);set('1084177551','m_Text','');set('2048086024','m_isRichText',0);
// Small red entry near the top-left, away from the main lower action area.
anchors('949000000000002',[.08,.80],[.43,.86]);
for(const k of ['m_fontSize','m_fontSizeBase','m_fontSizeMax'])set('949000000000009',k,36);set('949000000000009','m_fontSizeMin',28);
const settingsNames=new Map([...b].filter(([,v])=>v.type==='1').map(([id,v])=>{let name=field(v,'m_Name');if(name?.startsWith('"'))name=JSON.parse(name.replace(/\\x([a-f0-9]{2})/gi,'\\u00$1'));return [name,id];}));
const transformOf=go=>[...b].find(([,v])=>['224','4'].includes(v.type)&&ref(v,'m_GameObject')===go)[0];
anchors(transformOf(settingsNames.get('Título de ajustes')),[.10,.88],[.90,.96]);
anchors(transformOf(settingsNames.get('Descripción de privacidad')),[.09,.72],[.91,.78]);
set('949000000000035','m_text',JSON.stringify('Continuar'));
const final=node('Confirmación final con nombre','949000000000011',[.06,.15],[.94,.85]);clone('946000000000010',final.go);clone('946000000000011',final.go);
label('Título de la confirmación final',final.tr,'Pomo se olvidará de ti',[.08,.85],[.92,.96],52,'946000000000016');
const warning=label('Advertencia de borrado definitivo',final.tr,'No hay vuelta atrás. Se eliminarán todos tus datos y POMO volverá al principio. Pomo perderá toda la memoria que tiene sobre ti: tu nombre, tareas, historial, logros, artículos y preferencias.',[.09,.52],[.91,.82],40);
label('Instrucción del nombre',final.tr,'Escribe el nombre que le diste a Pomo para confirmar la eliminación.',[.09,.39],[.91,.49],34);
// Clone the real TMP field and its viewport; remap every local link.
const transforms=new Map([...b].filter(([,v])=>['224','4'].includes(v.type)).map(([id,v])=>[ref(v,'m_GameObject'),id]));
const objects=[];function collect(go){objects.push(go);const tr=transforms.get(go);for(const [id,v]of b)if(['224','4'].includes(v.type)&&ref(v,'m_Father')===tr)collect(ref(v,'m_GameObject'));}collect('1084177549');
const ids=new Set();for(const go of objects){ids.add(go);for(const m of get(go).text.matchAll(/  - component: \{fileID: (-?\d+)\}/g))ids.add(m[1]);}
const remap=new Map([...ids].map(id=>[id,alloc()]));
for(const [old,id]of remap){const v=get(old);b.set(id,{...v,text:v.text.replace(/\{fileID: (-?\d+)\}/g,(s,n)=>remap.has(n)?`{fileID: ${remap.get(n)}}`:s)});}
const input=remap.get('1084177551'),inputTr=remap.get('1084177550');
set(remap.get('1084177549'),'m_Name','Nombre para confirmar el borrado');set(inputTr,'m_Father',`{fileID: ${final.tr}}`);anchors(inputTr,[.09,.29],[.91,.37]);child(final.tr,inputTr);
set(input,'m_Text','');set(input,'m_CharacterLimit',60);set(input,'m_RichText',0);set(input,'m_GlobalPointSize',42);
for(const old of ['2048086024','954835476']){const id=remap.get(old);assert(id);for(const k of ['m_fontSize','m_fontSizeBase','m_fontSizeMax'])set(id,k,42);set(id,'m_fontSizeMin',28);set(id,'m_enableAutoSizing',1);set(id,'m_isRichText',0);set(id,'m_TextWrappingMode',0);set(id,'m_overflowMode',1);set(id,'m_text',JSON.stringify(old==='954835476'?'Tu nombre':''));}
const erase=button('Borrar definitivamente',final.tr,'Borrar definitivamente',[.09,.16],[.91,.25],true);
const cancel=button('Cancelar borrado definitivo',final.tr,'Cancelar',[.09,.035],[.91,.125]);set(final.go,'m_IsActive',0);set(erase.button,'m_Interactable',0);
get('949000000000045').text=get('949000000000045').text.trimEnd()+`
  primerPaso: {fileID: 949000000000015}
  segundoPaso: {fileID: ${final.go}}
  nombreConfirmacion: {fileID: ${input}}
  mensajeFinal: {fileID: ${warning.text}}
  botonEliminarDefinitivo: {fileID: ${erase.button}}
  botonCancelarFinal: {fileID: ${cancel.button}}
`;
for(const[id,v]of b){if(!before.has(id))v.text=v.text.replace(/[ \t]+$/gm,'');if(before.has(id)&&id!=='1830129152')assert.equal(v.text.match(/  m_OnClick:\n[\s\S]*$/)?.[0],before.get(id).match(/  m_OnClick:\n[\s\S]*$/)?.[0],'Changed event '+id);for(const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g))assert(m[1]==='0'||b.has(m[1]),id+' -> '+m[1]);}
fs.writeFileSync('Logs/Sprint4/Santiago.before-name-confirmation.unity',source);
const last=[...before].at(-1),lastBlock=get(last[0]);
const changes=[...b].filter(([id,v])=>id!==last[0]&&before.has(id)&&v.text!==before.get(id));const added=[...b].filter(([id])=>!before.has(id));
let patch='*** Begin Patch\n*** Update File: '+file+'\n'+changes.map(([id,v])=>'@@\n --- !u!'+v.type+' &'+id+v.suffix+'\n'+before.get(id).trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+v.text.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n').join('');
patch+='@@\n --- !u!'+lastBlock.type+' &'+last[0]+lastBlock.suffix+'\n'+before.get(last[0]).trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+lastBlock.text.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n'+added.map(([id,v])=>('--- !u!'+v.type+' &'+id+v.suffix+'\n'+v.text.trimEnd()).split('\n').map(l=>'+'+l).join('\n')).join('\n')+'\n*** End Patch';
console.error(JSON.stringify({addedBlocks:added.length,continueInitiallyDisabled:true,finalEraseInitiallyDisabled:true,finalStepInitiallyHidden:true,missingReferences:0,otherButtonEventsPreserved:true}));console.log(patch);
