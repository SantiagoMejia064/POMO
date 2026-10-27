// Migración de la jerarquía existente. Ejecutar solo si se quiere aplicar de nuevo
// esta distribución: el archivo de escena sigue siendo editable desde Unity.
const fs = require('fs');
const path = require('path');
const scenePath = path.resolve(__dirname, '../Assets/Scenes/Steven 1.unity');
const source = fs.readFileSync(scenePath, 'utf8').replace(/\r\n/g, '\n');
const header = source.slice(0, source.indexOf('--- !u!'));
const blocks = new Map();
for (const m of source.matchAll(/^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm))
    blocks.set(m[2], { type: m[1], text: m[3] });
const originalTexts = new Map([...blocks].map(([id, b]) => [id, b.text]));
const ref = (id, key) => get(id).text.match(new RegExp('^  ' + key + ': \\{fileID: (\\d+)', 'm'))?.[1];
const get = id => { const b = blocks.get(String(id)); if (!b) throw new Error('Falta fileID ' + id); return b; };
const set = (id, key, value) => {
    const b = get(id), re = new RegExp('^  ' + key + ':.*$', 'm');
    if (!re.test(b.text)) throw new Error('Falta propiedad ' + id + ':' + key);
    b.text = b.text.replace(re, () => '  ' + key + ': ' + value);
};
const append = (id, body) => { get(id).text = get(id).text.trimEnd() + '\n' + body + '\n'; };
const components = go => [...get(go).text.matchAll(/^  - component: \{fileID: (\d+)\}/gm)].map(m => m[1]);
const children = id => [...get(id).text.matchAll(/^  - \{fileID: (\d+)\}/gm)].map(m => m[1]);
const setChildren = (id, list) => {
    const b = get(id);
    b.text = b.text.replace(/^  m_Children:.*\n(?:  - \{fileID: \d+\}\n)*/m,
        () => list.length ? '  m_Children:\n' + list.map(c => `  - {fileID: ${c}}\n`).join('') : '  m_Children: []\n');
};
const addComponent = (go, component) => {
    const b = get(go);
    b.text = b.text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/,
        '$1' + `  - component: {fileID: ${component}}\n`);
};
const parent = (id, target) => {
    const old = ref(id, 'm_Father');
    if (old && old !== '0') setChildren(old, children(old).filter(c => c !== String(id)));
    set(id, 'm_Father', `{fileID: ${target}}`);
    setChildren(target, [...children(target).filter(c => c !== String(id)), String(id)]);
};
const rect = (id, min, max, pos = [0, 0], size = [0, 0]) => {
    set(id, 'm_AnchorMin', `{x: ${min[0]}, y: ${min[1]}}`);
    set(id, 'm_AnchorMax', `{x: ${max[0]}, y: ${max[1]}}`);
    set(id, 'm_AnchoredPosition', `{x: ${pos[0]}, y: ${pos[1]}}`);
    set(id, 'm_SizeDelta', `{x: ${size[0]}, y: ${size[1]}}`);
    set(id, 'm_LocalScale', '{x: 1, y: 1, z: 1}');
    set(id, 'm_LocalRotation', '{x: 0, y: 0, z: 0, w: 1}');
};
const rename = (go, name) => set(go, 'm_Name', name);
let nextId = 899000000000;
const allocate = () => { const id = String(++nextId); if (blocks.has(id)) throw new Error('Migración ya aplicada o ID ocupado'); return id; };
const cloneComponent = (template, go) => {
    const id = allocate(), b = get(template);
    blocks.set(id, { type: b.type, text: b.text.replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`) });
    addComponent(go, id); return id;
};
const node = (name, target) => {
    const go = allocate(), tr = allocate();
    blocks.set(go, { type: '1', text: `GameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: ${tr}}\n  m_Layer: 5\n  m_Name: ${name}\n  m_TagString: Untagged\n  m_Icon: {fileID: 0}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n` });
    blocks.set(tr, { type: '224', text: get('964448079').text
        .replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`)
        .replace(/^  m_Father:.*$/m, '  m_Father: {fileID: 0}') });
    setChildren(tr, []); parent(tr, target); rect(tr, [0, 0], [1, 1]);
    return { go, tr };
};
const mono = (go, guid, cls, fields) => {
    const id = allocate();
    blocks.set(id, { type: '114', text: `MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: ${go}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}\n  m_Name: \n  m_EditorClassIdentifier: Assembly-CSharp::${cls}\n${fields}\n` });
    addComponent(go, id); return id;
};
const color = (r, g, b, a = 1) => `{r: ${r / 255}, g: ${g / 255}, b: ${b / 255}, a: ${a}}`;
const navy = color(20, 59, 105), light = color(207, 225, 247), blue = color(68, 110, 166);
const image = (go, tint = light) => {
    cloneComponent('462490042', go); // CanvasRenderer
    const im = cloneComponent('462490041', go);
    set(im, 'm_Color', tint); set(im, 'm_RaycastTarget', '0'); return im;
};
const textStyle = (id, size, tint = navy, align = 2) => {
    set(id, 'm_fontSize', size); set(id, 'm_fontSizeBase', size);
    set(id, 'm_fontColor', tint); set(id, 'm_HorizontalAlignment', align);
    set(id, 'm_VerticalAlignment', 512); set(id, 'm_enableAutoSizing', '0');
    set(id, 'm_RaycastTarget', '0'); set(id, 'm_margin', '{x: 0, y: 0, z: 0, w: 0}');
};
const label = (name, target, value, size) => {
    const n = node(name, target); cloneComponent('572377213', n.go);
    const txt = cloneComponent('572377212', n.go);
    set(txt, 'm_text', JSON.stringify(value)); textStyle(txt, size);
    return { ...n, txt };
};

// Solo esta escena y este panel: conserva los IDs de las cartas y los listeners existentes.
if (!get('1185666114').text.includes('f45dbcd279e049a7b0eaa453bf820943')) throw new Error('Controlador de Memoria inesperado');
const root = '964448079', controller = '1185666114';
const stats = node('StatsBar', root);
const pomoArea = node('PomoFeedbackArea', root);
const timerBadge = node('TimerBadge', '1235065665');
const overflow = label('ScoreOverflow', stats.tr, '', 30);
const symbol = label('ReactionSymbol', pomoArea.tr, '', 90);

// Borde general y fondo de la habitación reutilizado.
image('964448078', navy);
rename('82772659', 'Background');
rect('82772660', [.015, .012], [.985, .988]);
set('82772661', 'm_RaycastTarget', '0');
set('82772661', 'm_Color', color(255, 255, 255));
rename('462490039', 'Header');
rect('462490040', [.02, .905], [.98, .985]);
set('462490041', 'm_Color', light);
set('462490041', 'm_RaycastTarget', '0');
parent('1695994487', '462490040'); rect('1695994487', [.025, .17], [.14, .83]);
parent('1717495630', '462490040'); rect('1717495630', [.19, .1], [.83, .9]);
textStyle('1717495631', 60);
set('1717495631', 'm_text', 'Memoria');
set('1717495631', 'm_fontAsset', '{fileID: 11400000, guid: f2857b70832f1de4d951514de9b6f111, type: 2}');
set('1717495631', 'm_sharedMaterial', '{fileID: 9129254487251998130, guid: f2857b70832f1de4d951514de9b6f111, type: 2}');

// Fila de sonido y puntaje: conserva las imágenes originales de Valentina.
rect(stats.tr, [.035, .817], [.965, .898]);
parent('597901876', stats.tr); rect('597901876', [0, .17], [.31, .83]);
set('597901878', 'm_Color', color(255, 255, 255));
set('1061220476', 'm_IsActive', '0');
parent('1941952355', stats.tr); rect('1941952355', [.345, .12], [.985, .88]);
set('1941952356', 'm_RaycastTarget', '0');
const digits = [
    ['1961524448', '1961524449', .595, .682],
    ['1047765428', '1047765429', .695, .782],
    ['1120641170', '1120641171', .795, .882],
    ['1198535066', '1198535067', .895, .982]
];
for (const [tr, txt, min, max] of digits) { rect(tr, [min, .15], [max, .85]); textStyle(txt, 46); }
rename('1961524447', 'Millares');
rect(overflow.tr, [.98, .25], [1, .75]);

// Tablero 4 x 3, cartas crema y espaciado relativo al panel.
rect('292962106', [.04, .438], [.96, .802]);
set('292962107', 'm_Color', navy);
set('292962107', 'm_RaycastTarget', '0');
rect('1185721605', [.018, .025], [.982, .975]);
set('1185721606', 'm_Spacing', '{x: 10, y: 10}');
set('1185721606', 'm_Constraint', '1'); set('1185721606', 'm_ConstraintCount', '4');
set('1185721606', 'm_ChildAlignment', '4');
const cardTransforms = children('1185721605');
for (const tr of cardTransforms) {
    const go = ref(tr, 'm_GameObject');
    const comps = components(go);
    const im = comps.find(id => get(id).text.includes('UnityEngine.UI::UnityEngine.UI.Image'));
    const view = comps.find(id => get(id).text.includes('Screens.MemoryCardView'));
    set(im, 'm_Color', color(239, 209, 156));
    append(view, '  hiddenColor: ' + color(239, 209, 156) + '\n  revealedColor: ' + color(255, 232, 185) + '\n  matchedColor: ' + color(204, 225, 186));
    for (const child of children(tr)) {
        const childGo = ref(child, 'm_GameObject');
        if (get(childGo).text.includes('Identificador provisional')) set(childGo, 'm_IsActive', '0');
        else rect(child, [.16, .16], [.84, .84]);
    }
}

// Feedback bajo el tablero, sin objetos con offsets de otras zonas de la pantalla.
parent('1235065665', root); rename('1235065664', 'FeedbackPanel');
rect('1235065665', [.04, .307], [.96, .425]);
set('1235065666', 'm_Color', light); set('1235065666', 'm_RaycastTarget', '0');
set('997658097', 'm_IsActive', '0'); rename('997658097', 'LegacyDecoration');
parent('584048430', '1235065665'); rect('584048430', [.035, .42], [.965, .96]);
textStyle('584048431', 33);
set('584048431', 'm_TextWrappingMode', '1');
set('584048431', 'm_fontAsset', '{fileID: 11400000, guid: d63653cef15953f4892b5ff4eb8d2dc3, type: 2}');
set('584048431', 'm_sharedMaterial', '{fileID: -7158013276951197918, guid: d63653cef15953f4892b5ff4eb8d2dc3, type: 2}');
parent('1407511543', '1235065665'); rect('1407511543', [.04, .20], [.68, .42]);
parent('572377211', '1235065665'); rect('572377211', [.04, .015], [.68, .22]);
textStyle('1407511544', 24, navy, 1); textStyle('572377212', 24, navy, 1);
image(timerBadge.go, color(239, 209, 156));
rect(timerBadge.tr, [.70, .035], [.96, .37]);
parent('2055639879', timerBadge.tr); rect('2055639879', [.02, .05], [.98, .95]);
textStyle('2055639880', 45); set('2055639880', 'm_text', '05:00');

// Pomo central inferior. El Animator mueve únicamente la imagen dentro de su zona.
rect(pomoArea.tr, [.04, .028], [.96, .295]);
parent('1982371475', pomoArea.tr); rect('1982371475', [.13, .025], [.87, .83]);
const body = get('1982371476');
body.text = get('462490041').text.replace(/^  m_GameObject:.*$/m, '  m_GameObject: {fileID: 1982371474}');
set('1982371476', 'm_EditorClassIdentifier', 'UnityEngine.UI::UnityEngine.UI.Image');
set('1982371476', 'm_Sprite', '{fileID: 21300000, guid: d6eb870d485210349a7cb07716e117df, type: 3}');
set('1982371476', 'm_Color', color(255, 255, 255));
set('1982371476', 'm_PreserveAspect', '1'); set('1982371476', 'm_RaycastTarget', '0');
const animator = allocate();
blocks.set(animator, { type: '95', text: `Animator:\n  serializedVersion: 7\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1982371474}\n  m_Enabled: 1\n  m_Avatar: {fileID: 0}\n  m_Controller: {fileID: 0}\n  m_CullingMode: 0\n  m_UpdateMode: 2\n  m_ApplyRootMotion: 0\n  m_LinearVelocityBlending: 0\n  m_StabilizeFeet: 0\n  m_AnimatePhysics: 0\n  m_WarningMessage: \n  m_HasTransformHierarchy: 1\n  m_AllowConstantClipSamplingOptimization: 1\n  m_KeepAnimatorStateOnDisable: 0\n  m_WriteDefaultValuesOnDisable: 1\n` });
addComponent('1982371474', animator);
rect(symbol.tr, [.76, .51], [.94, .82]); textStyle(symbol.txt, 100);
const poseSprites = ['d6eb870d485210349a7cb07716e117df', '6576af68c1ccaab4b9baa25ad3c4798d', 'a0f490d19b6d0a4498793419bb9391c9', '6576af68c1ccaab4b9baa25ad3c4798d', 'a0f490d19b6d0a4498793419bb9391c9', '6576af68c1ccaab4b9baa25ad3c4798d'];
const states = ['Idle', 'Observe', 'Match', 'Mismatch', 'RoundComplete', 'Encourage'];
const durations = [3, 3, 1.4, 1.4, 2.2, 2];
const visual = mono(pomoArea.go, 'cf41a4819a0f46748b4eaf80b1bd9f20', 'Pomo.Presentation.Screens.PomoVisualController',
    `  poseImage: {fileID: 1982371476}\n  motionAnimator: {fileID: ${animator}}\n  reactionSymbol: {fileID: ${symbol.txt}}\n  poses:\n` + states.map((s, i) =>
        `  - state: ${i}\n    sprite: {fileID: 21300000, guid: ${poseSprites[i]}, type: 3}\n    animatorState: ${s}\n    duration: ${durations[i]}`).join('\n') + '\n  transitionSeconds: 0.12');
parent('756106242', pomoArea.tr); rect('756106242', [.26, .83], [.74, .995]);
set('756106244', 'm_Color', blue); set('756106244', 'm_RaycastTarget', '1');
rect('1010126908', [0, 0], [1, 1]); textStyle('1010126909', 34, color(255, 255, 255));
parent('1274566613', pomoArea.tr); rect('1274566613', [.73, .84], [.98, .99]);
set('1274566615', 'm_Color', blue); textStyle('1126526323', 25, color(255, 255, 255));
set('1274566612', 'm_IsActive', '0');
setChildren(pomoArea.tr, ['1982371475', symbol.tr, '756106242', '1274566613']);

// Resultado localizado sobre el tablero: Pomo sigue visible debajo.
rect('566347015', [.055, .448], [.945, .792]);
set('566347016', 'm_Color', navy); set('566347016', 'm_RaycastTarget', '1');
rect('154911933', [.06, .06], [.94, .95]); textStyle('154911934', 38, color(255, 255, 255));
set('154911934', 'm_TextWrappingMode', '1');
set('566347014', 'm_IsActive', '0');
append(controller, `  resultDisplaySeconds: 8\n  scoreOverflowIndicator: {fileID: ${overflow.txt}}\n  pomoVisual: {fileID: ${visual}}\n  cardFlipClip: {fileID: 0}\n  buttonClickClip: {fileID: 0}`);
mono('964448078', 'f0828edc580f416ab67fb282a4a14c3c', 'Pomo.Presentation.UI.ScreenSafeArea', '');
mono('1185721604', 'c596e716d69c40fa9c084d75d1850755', 'Pomo.Presentation.UI.MemoryCardGridLayout', '');

setChildren(root, ['82772660', '462490040', stats.tr, '292962106', '1235065665', pomoArea.tr, '566347015']);
set('1185666112', 'm_IsActive', '0');
set('299299769', 'm_IsActive', '1');

const output = (header + [...blocks].map(([id, b]) => {
    const body = b.text === originalTexts.get(id) ? b.text : b.text.replace(/[ \t]+$/gm, '');
    return `--- !u!${b.type} &${id}\n${body}`;
}).join('')).trimEnd() + '\n';
fs.writeFileSync(scenePath, output.replace(/\n/g, '\r\n'));
console.log('Jerarquía y referencias guardadas en Steven 1. Objetos de otras secciones conservados.');
