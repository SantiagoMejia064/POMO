// Migración puntual de la vista de logros en Steven 1. Ya aplicada, conserva el diseño existente.
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
const viewGuid = 'b1f7d66ca2024a90a6fcfc85d8b21720';
if (source.includes('guid: ' + viewGuid)) {
    console.log('Steven 1 ya tiene el coordinador de logros; no se modifica la escena.');
    process.exit(0);
}
const get = id => { const b = blocks.get(String(id)); if (!b) throw new Error('Falta objeto ' + id); return b; };
if (!get('1915654018').text.includes('m_Name: Canvas'))
    throw new Error('Revisar el Canvas principal antes de preparar la vista.');
let next = 897000000000;
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
const root = node('Pomo feedback', '1915654019', [0, 0], [1, 1]);
mono(root.go, 'f0828edc580f416ab67fb282a4a14c3c', 'Pomo.Presentation.UI.ScreenSafeArea', '');
const card = node('Reconocimiento de Pomo', root.tr, [.5, 1], [.5, 1], [0, -165], [940, 210]);
const background = image(card.go, '462490041'); set(background, 'm_Color', blue);
const group = id();
blocks.set(group, { type: '225', text: `CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${card.go}}
  m_Enabled: 1
  m_Alpha: 0
  m_Interactable: 0
  m_BlocksRaycasts: 0
  m_IgnoreParentGroups: 0
` });
addComponent(card.go, group);
const actor = node('Pomo animado', card.tr, [.12, .5], [.12, .5], [0, 0], [160, 180]);
const previous = node('Pose anterior', actor.tr, [0, 0], [1, 1]);
const previousImage = image(previous.go, '1982371476', explaining, false);
const current = node('Pose actual', actor.tr, [0, 0], [1, 1]);
const currentImage = image(current.go, '1982371476', explaining);
const animator = clone('899000000028', actor.go);
set(animator, 'm_Controller', '{fileID: 9100000, guid: 22f10307e712265488a08791297cf3d4, type: 2}');
const heading = label('Título del logro', card.tr, [.24, .72], [.97, .96], '', 30);
const message = label('Mensaje', card.tr, [.24, .30], [.97, .72], '', 26);
const detail = label('Origen del logro', card.tr, [.24, .05], [.97, .30], '', 22);
set(detail, 'm_isRichText', '0');
const visual = mono(card.go, 'cf41a4819a0f46748b4eaf80b1bd9f20', 'Pomo.Presentation.Screens.PomoVisualController',
`  poseImage: {fileID: ${currentImage}}
  previousPoseImage: {fileID: ${previousImage}}
  motionAnimator: {fileID: ${animator}}
  reactionSymbol: {fileID: 0}
  poses:
  - state: 0
    sprite: ${explaining}
    animatorState: Idle
    duration: 1.5
  - state: 10
    sprite: ${smiling}
    animatorState: Match
    duration: 2
  - state: 11
    sprite: ${smiling}
    animatorState: RoundComplete
    duration: 2.2
  - state: 12
    sprite: ${smiling}
    animatorState: RoundComplete
    duration: 2.5
  transitionSeconds: 0.2`);
const view = mono(card.go, 'e6127b9498e34e6e8e5f1ac108bca210', 'Pomo.Presentation.Feedback.PomoAchievementView',
`  visibility: {fileID: ${group}}
  pomoVisual: {fileID: ${visual}}
  headingText: {fileID: ${heading}}
  messageText: {fileID: ${message}}
  detailText: {fileID: ${detail}}
  minimumDisplaySeconds: 3
  taskHeading: "¡TAREA COMPLETADA!"
  taskMessage: "Terminaste tu tarea. ¡Buen trabajo!"
  blockHeading: "¡BLOQUE COMPLETADO!"
  blockMessage: "Terminaste tu bloque de trabajo. ¡Buen avance!"
  streakHeading: "¡NUEVO LOGRO!"
  streakMessage: "Tu constancia está dando frutos. ¡Sigue así!"`);
const coordinator = mono(root.go, viewGuid, 'Pomo.Presentation.Feedback.PomoFeedbackCoordinator',
`  achievementView: {fileID: ${view}}
  suppressWhilePanelsActive:
  - {fileID: 1185666112}
  logPresentations: 0`);
const changed = [...originals].filter(([n, text]) => get(n).text !== text).map(([n]) => n);
if (changed.some(n => n !== '1915654019')) throw new Error('Se intentó modificar fuera del padre Canvas.');
for (const [n, b] of blocks) if (!originals.has(n)) b.text = b.text.replace(/[ \t]+$/gm, '');
const result = header + [...blocks].map(([n, b]) => `--- !u!${b.type} &${n}\n${b.text}`).join('');
if (fs.readFileSync(scenePath, 'utf8') !== raw) throw new Error('Unity guardó durante la preparación; revisar.');
fs.writeFileSync(scenePath, raw.includes('\r\n') ? result.replace(/\n/g, '\r\n') : result, 'utf8');
console.log(JSON.stringify({scene:scenePath, root:root.go, view, coordinator,
    changedExistingBlocks:changed, addedBlocks:blocks.size-originals.size,
    beforeHash:crypto.createHash('sha256').update(raw).digest('hex')},null,2));

