// Corrección de propiedad y navegación del temporizador en Steven 1.
// Migración puntual ya aplicada: no reconstruir manualmente la escena con este archivo.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const scenePath = path.resolve(__dirname, '../Assets/Scenes/Steven 1.unity');
const raw = fs.readFileSync(scenePath, 'utf8');
const source = raw.replace(/\r\n/g, '\n');
const header = source.slice(0, source.indexOf('--- !u!'));
const blocks = new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)]
    .map(m => [m[2], { type: m[1], text: m[3] }]));
const originals = new Map([...blocks].map(([id, b]) => [id, b.text]));
const viewGuid = '0e2047ddf7534a80960fbd39df17df85';
if (source.includes('guid: ' + viewGuid)) {
    console.log('Steven 1 ya tiene navegación de sesión persistente; no se modifica la escena.');
    process.exit(0);
}
const get = id => { const b = blocks.get(String(id)); if (!b) throw new Error('Falta objeto ' + id); return b; };
if (!get('1521692121').text.includes('guid: 4f45921800d165c4bbe1db13bc9edc57') ||
    !get('1521692123').text.includes('guid: 6550781b342c03443a33b0a704a179c9'))
    throw new Error('La estructura de bloques cambió; revisar antes de modificar.');
let next = 896000000000;
const id = () => { const n = String(++next); if (blocks.has(n)) throw new Error('ID ocupado ' + n); return n; };
const set = (n, key, value) => {
    const b = get(n), re = new RegExp('^  ' + key + ':.*$', 'm');
    if (!re.test(b.text)) throw new Error('Falta propiedad ' + n + ':' + key);
    b.text = b.text.replace(re, () => '  ' + key + ': ' + value);
};
const addComponent = (go, n) => {
    const b = get(go);
    b.text = b.text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/,
        '$1' + `  - component: {fileID: ${n}}\n`);
};
const addChild = (tr, n) => {
    const b = get(tr);
    if (b.text.includes('  m_Children: []'))
        b.text = b.text.replace('  m_Children: []', `  m_Children:\n  - {fileID: ${n}}`);
    else b.text = b.text.replace(/(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/,
        '$1' + `  - {fileID: ${n}}\n`);
};
const node = (name, parent, min, max, pos = [0, 0], size = [0, 0]) => {
    const go = id(), tr = id();
    blocks.set(go, { type: '1', text: `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${tr}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
` });
    blocks.set(tr, { type: '224', text: `RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parent}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: ${min[0]}, y: ${min[1]}}
  m_AnchorMax: {x: ${max[0]}, y: ${max[1]}}
  m_AnchoredPosition: {x: ${pos[0]}, y: ${pos[1]}}
  m_SizeDelta: {x: ${size[0]}, y: ${size[1]}}
  m_Pivot: {x: 0.5, y: 0.5}
` });
    addChild(parent, tr);
    return { go, tr };
};
const clone = (template, go) => {
    const n = id(), b = get(template);
    blocks.set(n, { type: b.type, text: b.text.replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`) });
    addComponent(go, n); return n;
};
const mono = (go, guid, cls, fields) => {
    const n = id();
    blocks.set(n, { type: '114', text: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::${cls}
${fields}
` });
    addComponent(go, n); return n;
};
const spriteRef = guid => `{fileID: 21300000, guid: ${guid}, type: 3}`;
const blue = '{r: 0.039215688, g: 0.19607843, b: 0.4, a: 1}';
const white = '{r: 1, g: 1, b: 1, a: 1}';
const explaining = spriteRef('6576af68c1ccaab4b9baa25ad3c4798d');
const smiling = spriteRef('a0f490d19b6d0a4498793419bb9391c9');
const waving = spriteRef('d6eb870d485210349a7cb07716e117df');
const image = (go, template, sprite, enabled = true) => {
    clone('462490042', go);
    const n = clone(template, go);
    set(n, 'm_RaycastTarget', '0'); set(n, 'm_Enabled', enabled ? '1' : '0');
    if (sprite) set(n, 'm_Sprite', sprite);
    return n;
};
const label = (name, parent, min, max, text, size) => {
    const n = node(name, parent, min, max); clone('572377213', n.go);
    const tmp = clone('584048431', n.go);
    set(tmp, 'm_text', JSON.stringify(text));
    set(tmp, 'm_fontColor', white); set(tmp, 'm_fontSize', String(size));
    set(tmp, 'm_fontSizeBase', String(size)); set(tmp, 'm_HorizontalAlignment', '1');
    set(tmp, 'm_RaycastTarget', '0');
    return tmp;
};
const ref = (n, key) => new RegExp('^  '+key+': \\{fileID: (\\d+)','m').exec(get(n).text)?.[1];
const children = n => [...get(n).text.matchAll(/^  - \{fileID: (\d+)\}/gm)].map(m => m[1]);
const setChildren = (n, list) => {
    get(n).text = get(n).text.replace(/^  m_Children:.*\n(?:  - \{fileID: \d+\}\n)*/m,
        () => list.length ? '  m_Children:\n'+list.map(c=>'  - {fileID: '+c+'}\n').join('') : '  m_Children: []\n');
};
const runtime = node('Pomodoro session', '1915654019', [0, 0], [1, 1]);
get('1521692120').text = get('1521692120').text.replace('  - component: {fileID: 1521692121}\n', '');
addComponent(runtime.go, '1521692121');
set('1521692121', 'm_GameObject', '{fileID: '+runtime.go+'}');
// Mantener el mismo AudioSource y su clip/volumen, fuera del panel que se oculta.
setChildren('1521692122', children('1521692122').filter(n=>n!=='1124069833'));
set('1124069833', 'm_Father', '{fileID: '+runtime.tr+'}');
addChild(runtime.tr, '1124069833');
// La finalización ya no ejecuta una coroutine que cambie de pantalla o reinicie el motor.
get('1521692123').text = get('1521692123').text.replace(/^  homePanel:.*\n/m,'')
    .replace(/^  completionMessageSeconds:.*\n/m,'');

const nav = mono(runtime.go, viewGuid, 'Pomo.Presentation.Navigation.WorkBlockNavigationController',
`  pomodoroController: {fileID: 1521692121}
  blockSelection: {fileID: 1521692123}
  homePanel: {fileID: 299299769}
  workSection: {fileID: 849969573}
  workMenu: {fileID: 1701934920}
  blockSection: {fileID: 1521692120}
  blockMenu: {fileID: 295995678}
  personalizePanel: {fileID: 770463786}
  favoritesPanel: {fileID: 1908151007}
  timerPanel: {fileID: 57170268}
  savePanel: {fileID: 1048410720}
  rhythmSection: {fileID: 862581708}
  calendarSection: {fileID: 2012272391}`);

const wire = (button, target, cls, method) => {
    const b = get(button), at = b.text.indexOf('  m_OnClick:');
    if (at<0) throw new Error('El objeto no es un Button: '+button);
    b.text = b.text.slice(0,at)+`  m_OnClick:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: ${target}}
        m_TargetAssemblyTypeName: ${cls}, Assembly-CSharp
        m_MethodName: ${method}
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument:
          m_BoolArgument: 0
        m_CallState: 2
`;
};
const navigationClass='Pomo.Presentation.Navigation.WorkBlockNavigationController';
for(const [button,method] of [
    ['1577161642','OpenWorkMenu'], ['158153425','OpenBlocks'],
    ['1532921832','OpenWorkMenu'], ['1219237862','OpenWorkMenu'],
    ['1766565010','ReturnHome'], ['437433910','ReturnHome'],
    ['1234765633','HandleTimerPrimaryAction']
]) wire(button,nav,navigationClass,method);
wire('2029911466','1521692121','Pomo.Presentation.Screens.PomodoroController','Pause');
wire('845962102','1521692121','Pomo.Presentation.Screens.PomodoroController','Resume');

const context = label('Tarea de la sesión', '57170271', [.5,.5], [.5,.5], '', 26);
const contextGo=ref(context,'m_GameObject');
const contextTr=[...blocks].find(([n,b])=>b.type==='224'&&ref(n,'m_GameObject')===contextGo)[0];
set(contextTr,'m_AnchoredPosition','{x: 0, y: 760}');
set(contextTr,'m_SizeDelta','{x: 850, y: 64}');
set(context,'m_HorizontalAlignment','2');
set(context,'m_TextWrappingMode','0');
set(context,'m_overflowMode','1');
set(context,'m_isRichText','0');

const restart = node('Reiniciar bloque', '57170271', [.5,.5], [.5,.5], [0,-650], [470,95]);
const restartImage=image(restart.go,'462490041');
set(restartImage,'m_Color','{r: 0.3137255, g: 0.47058824, b: 0.6862745, a: 1}');
set(restartImage,'m_RaycastTarget','1');
const restartButton=clone('1234765633',restart.go);
set(restartButton,'m_TargetGraphic','{fileID: '+restartImage+'}');
set(restartButton,'m_Interactable','1');
wire(restartButton,'1521692121','Pomo.Presentation.Screens.PomodoroController','ResetTimer');
label('Texto',restart.tr,[.03,.06],[.97,.94],'Reiniciar bloque',28);

// El diseño heredado también contenía Cronometro en el mismo recuadro:
// dejar un único escritor de los dígitos en el temporizador funcional.
const legacy=[...blocks].find(([n,b])=>b.text.includes('guid: eb43f8d590b829f4cb1f55bcde8ce4a4')&&ref(n,'m_GameObject')==='1622156922');
if(!legacy) throw new Error('Revisar el Cronometro heredado del panel funcional.');
const legacyId=legacy[0];
const pauseFeedback=ref(legacyId,'feedbackPausa')??'0';
const warning=ref(legacyId,'panelAdvertencia')??'0';
set(legacyId,'m_Enabled','0');
if(warning!=='0') set(warning,'m_IsActive','0');
get('1622156924').text+=`  taskTitleText: {fileID: ${context}}
  startButtonLabel: {fileID: 436099217}
  startButton: {fileID: 1234765633}
  pauseButton: {fileID: 2029911466}
  resumeButton: {fileID: 845962102}
  restartButton: {fileID: ${restartButton}}
  addToFavoritesButton: {fileID: 621471124}
  pauseFeedback: {fileID: ${pauseFeedback}}
  legacyWarningPanel: {fileID: ${warning}}
`;
const allowed=new Set(['1915654019','57170271','1521692120','1521692121','1521692122','1521692123',
    '1124069833','1622156924',legacyId,warning,'1577161642','158153425','1532921832',
    '1219237862','1766565010','437433910','1234765633','2029911466','845962102']);
const changed=[...originals].filter(([n,t])=>get(n).text!==t).map(([n])=>n);
if(changed.some(n=>!allowed.has(n)))
    throw new Error('Se intentó modificar fuera del flujo de bloques: '+changed.filter(n=>!allowed.has(n)).join(','));
for(const [n,b] of blocks) if(!originals.has(n)) b.text=b.text.replace(/[ \t]+$/gm,'');
const result=header+[...blocks].map(([n,b])=>`--- !u!${b.type} &${n}\n${b.text}`).join('');
if(fs.readFileSync(scenePath,'utf8')!==raw) throw new Error('Unity guardó durante la migración. Revisar.');
fs.writeFileSync(scenePath,raw.includes('\r\n')?result.replace(/\n/g,'\r\n'):result,'utf8');
console.log(JSON.stringify({scene:scenePath,runtime:runtime.go,navigation:nav,
    originalPomodoroController:'1521692121',disabledLegacyTimer:legacyId,
    newRestartButton:restartButton,changedExistingBlocks:changed,
    addedBlocks:blocks.size-originals.size,
    beforeHash:crypto.createHash('sha256').update(raw).digest('hex')},null,2));

