// Adds an editable, initially hidden confirmation dialog to Santiago.
const fs=require('fs'), assert=require('assert/strict');
const file='Assets/Scenes/Santiago.unity', source=fs.readFileSync(file,'utf8').replace(/\r\n/g,'\n');
const guid='152533695fd7496bba8691d173ae7321';
assert(!source.includes(guid),'Data reset is already integrated.');
const blocks=new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],suffix:m[3]||'',text:m[4]}]));
const before=new Map([...blocks].map(([id,b])=>[id,b.text]));
const get=id=>{assert(blocks.has(id),id);return blocks.get(id);};
const field=(b,k)=>b.text.match(new RegExp('^  '+k+': (.*)$','m'))?.[1];
const ref=(b,k)=>field(b,k)?.match(/fileID: (-?\d+)/)?.[1];
function set(id,k,v){const p=new RegExp('^  '+k+(k==='m_text'?':[^\\n]*(?:\\n {4,}[^\\n]*)*':':.*$'),'m');assert(p.test(get(id).text),id+': '+k);get(id).text=get(id).text.replace(p,()=>'  '+k+': '+v);}
let next=949000000000000;
function allocate(){const id=String(++next);assert(!blocks.has(id));return id;}
function attach(go,id){get(go).text=get(go).text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/,'$1'+`  - component: {fileID: ${id}}\n`);}
function clone(template,go){const id=allocate();blocks.set(id,{...get(template),text:get(template).text.replace(/^  m_GameObject:.*$/m,`  m_GameObject: {fileID: ${go}}`)});attach(go,id);return id;}
function anchors(id,min,max){set(id,'m_AnchorMin',`{x: ${min[0]}, y: ${min[1]}}`);set(id,'m_AnchorMax',`{x: ${max[0]}, y: ${max[1]}}`);set(id,'m_AnchoredPosition','{x: 0, y: 0}');set(id,'m_SizeDelta','{x: 0, y: 0}');}
function node(name,parent,min=[0,0],max=[1,1]){
    const go=allocate(),tr=allocate();blocks.set(go,{...get('946000000000013'),text:get('946000000000013').text.replace(/  m_Component:\n(?:  - component:.*\n)+/,`  m_Component:\n  - component: {fileID: ${tr}}\n`).replace(/^  m_Name:.*$/m,'  m_Name: '+JSON.stringify(name))});
    blocks.set(tr,{...get('946000000000014'),text:get('946000000000014').text.replace(/^  m_GameObject:.*$/m,`  m_GameObject: {fileID: ${go}}`).replace(/^  m_Father:.*$/m,`  m_Father: {fileID: ${parent}}`)});anchors(tr,min,max);
    get(parent).text=get(parent).text.includes('  m_Children: []')?get(parent).text.replace('  m_Children: []',`  m_Children:\n  - {fileID: ${tr}}`):get(parent).text.replace(/(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,'$1'+`  - {fileID: ${tr}}\n`);
    return {go,tr};
}
function label(name,parent,value,min,max,size,template='946000000000020'){
    const n=node(name,parent,min,max);clone('946000000000015',n.go);const text=clone(template,n.go);set(text,'m_text',JSON.stringify(value));
    for(const k of ['m_fontSize','m_fontSizeBase','m_fontSizeMax'])set(text,k,size);
    set(text,'m_enableAutoSizing',1);set(text,'m_fontSizeMin',size*.7);set(text,'m_TextWrappingMode',1);set(text,'m_overflowMode',1);set(text,'m_RaycastTarget',0);
    return {...n,text};
}
function button(name,parent,title,min,max,danger=false){
    const n=node(name,parent,min,max);clone('946000000000023',n.go);const image=clone('946000000000048',n.go),btn=clone('946000000000049',n.go);set(btn,'m_TargetGraphic',`{fileID: ${image}}`);set(btn,'m_Interactable',1);
    if(danger)set(image,'m_Color','{r: 0.67, g: 0.14, b: 0.19, a: 1}');
    label('Texto '+name,n.tr,title,[.04,.08],[.96,.92],44,'946000000000053');return {...n,button:btn};
}
const settings=[...blocks].find(([,b])=>b.text.includes('Assembly-CSharp::Pomo.Presentation.Screens.AjustesPrivacidadController\n'));assert(settings);
const settingsGo=ref(settings[1],'panelAjustes');
const transformOf=go=>[...blocks].find(([,b])=>['224','4'].includes(b.type)&&ref(b,'m_GameObject')===go)[0];
const settingsTr=transformOf(settingsGo);
const decodeName=value=>value?.startsWith('"')?JSON.parse(value.replace(/\\x([0-9a-f]{2})/gi,'\\u00$1')):value;
const childNamed=name=>[...blocks].find(([,b])=>b.type==='1'&&decodeName(field(b,'m_Name'))===name);
for(const [name,min,max]of [['Descripción de privacidad',[.09,.74],[.91,.82]],['Título de ajustes',[.07,.85],[.93,.94]]]){const item=childNamed(name);assert(item,name);anchors(transformOf(item[0]),min,max);}
for(const [fieldName,min,max]of [['botonLeerAviso',[.10,.62],[.90,.71]],['botonNotificaciones',[.10,.48],[.90,.57]],['botonVolver',[.10,.08],[.90,.17]],['estadoNotificaciones',[.10,.40],[.90,.46]]])anchors(transformOf(ref(get(ref(settings[1],fieldName)),'m_GameObject')),min,max);
const open=button('Borrar datos locales',settingsTr,'Borrar mis datos',[.10,.28],[.90,.37],true);
const overlay=node('Confirmación de borrado de datos','1915654019');clone('946000000000006',overlay.go);clone('946000000000007',overlay.go);
const group=clone('946000000000012',overlay.go);set(group,'m_IgnoreParentGroups',1);set(group,'m_Interactable',1);set(group,'m_BlocksRaycasts',1);
const card=node('Cuadro de borrado de datos',overlay.tr,[.06,.20],[.94,.80]);clone('946000000000010',card.go);clone('946000000000011',card.go);
label('Título del borrado',card.tr,'¿Borrar todos tus datos?',[.08,.80],[.92,.94],54,'946000000000016');
const body=label('Consecuencias del borrado',card.tr,'Se eliminarán de este dispositivo tus tareas, bloques y favoritos, recordatorios, historial, trofeos, copas, artículos adquiridos y preferencias. Se detendrá cualquier sesión activa y POMO volverá a la bienvenida. Esta acción no se puede deshacer.',[.09,.35],[.91,.77],40);
const confirm=button('Confirmar borrado de datos',card.tr,'Borrar mis datos',[.09,.19],[.91,.30],true);
const cancel=button('Cancelar borrado de datos',card.tr,'Cancelar',[.09,.04],[.91,.15]);set(overlay.go,'m_IsActive',0);
const notification=[...blocks].find(([,b])=>b.text.includes('Assembly-CSharp::Pomo.Presentation.Screens.NotificationPermissionController\n'));assert(notification);
const rootGroup=ref(notification[1],'interfazPrincipal');assert(rootGroup);
const controller=allocate();blocks.set(controller,{type:'114',suffix:'',text:`MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1915654018}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.Screens.BorradoDatosController
  botonBorrar: {fileID: ${open.button}}
  panelConfirmacion: {fileID: ${overlay.go}}
  botonConfirmar: {fileID: ${confirm.button}}
  botonCancelar: {fileID: ${cancel.button}}
  mensaje: {fileID: ${body.text}}
  interfazPrincipal: {fileID: ${rootGroup}}
`});attach('1915654018',controller);
const rhythm=[...blocks].find(([,b])=>b.text.includes('Assembly-CSharp::Pomo.Presentation.Screens.WorkRhythmScreenController\n'));assert(rhythm);set(rhythm[0],'initializeDemoDataInEditor',0);
for(const [id,b]of blocks){if(!before.has(id))b.text=b.text.replace(/[ \t]+$/gm,'');if(before.has(id))assert.equal(b.text.match(/  m_OnClick:\n[\s\S]*$/)?.[0],before.get(id).match(/  m_OnClick:\n[\s\S]*$/)?.[0],'Changed callback '+id);for(const m of b.text.matchAll(/\{fileID: (-?\d+)\}/g))assert(m[1]==='0'||blocks.has(m[1]),id+' -> '+m[1]);}
fs.mkdirSync('Logs/Sprint4',{recursive:true});fs.writeFileSync('Logs/Sprint4/Santiago.before-data-reset.unity',source);
const changes=[...blocks].filter(([id,b])=>before.has(id)&&b.text!==before.get(id));
const added=[...blocks].filter(([id])=>!before.has(id));
let patch='*** Begin Patch\n*** Update File: '+file+'\n'+changes.map(([id,b])=>'@@\n --- !u!'+b.type+' &'+id+b.suffix+'\n'+before.get(id).trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+b.text.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n').join('');
const last=[...before].at(-1),lastBlock=get(last[0]);
patch+='@@\n '+('--- !u!'+lastBlock.type+' &'+last[0]+lastBlock.suffix)+'\n'+last[1].trimEnd().split('\n').map(l=>' '+l).join('\n')+'\n'+added.map(([id,b])=>('--- !u!'+b.type+' &'+id+b.suffix+'\n'+b.text.trimEnd()).split('\n').map(l=>'+'+l).join('\n')).join('\n')+'\n*** End Patch';
console.error(JSON.stringify({addedBlocks:added.length,existingCallbacksPreserved:true,missingReferences:0,dialogInitiallyHidden:true}));
console.log(patch);
