// Adds the privacy entry point to the existing Santiago home, without cloning the notice.
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const root = path.resolve(__dirname, '..');
const target = path.join(root, 'Assets/Scenes/Santiago.unity');
const original = fs.readFileSync(target, 'utf8');
const source = original.replace(/\r\n/g, '\n');
const guid = 'e0bf4e654efb490c80532c7c239947e4';
assert(!source.includes(guid), 'Settings entry already exists. Do not rebuild.');
const matches = [...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)];
const blocks = new Map(matches.map(m => [m[2], { type: m[1], stripped: m[3] || '', text: m[4] }]));
const before = new Map([...blocks].map(([id, v]) => [id, v.text]));
const get = id => { assert(blocks.has(id), 'Missing ID ' + id); return blocks.get(id); };
const set = (id, key, value) => {
    const re = new RegExp('^  ' + key + ':.*$', 'm');
    assert(re.test(get(id).text), key);
    get(id).text = get(id).text.replace(re, () => '  ' + key + ': ' + value);
};
let next = 947000000000000;
const allocate = () => { const id = String(++next); assert(!blocks.has(id)); return id; };
const attach = (go, id) => {
    get(go).text = get(go).text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/, '$1' + `  - component: {fileID: ${id}}\n`);
};
const clone = (template, go) => {
    const id = allocate(), v = get(template);
    blocks.set(id, { ...v, text: v.text.replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`) });
    attach(go, id); return id;
};
const node = (name, parent, min = [0, 0], max = [1, 1]) => {
    const go = allocate(), tr = allocate();
    blocks.set(go, { type: '1', stripped: '', text: `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${tr}}
  m_Layer: 5
  m_Name: ${JSON.stringify(name)}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
` });
    blocks.set(tr, { ...get('946000000000014'), text: get('946000000000014').text
        .replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`)
        .replace(/^  m_Father:.*$/m, `  m_Father: {fileID: ${parent}}`) });
    set(tr, 'm_AnchorMin', `{x: ${min[0]}, y: ${min[1]}}`);
    set(tr, 'm_AnchorMax', `{x: ${max[0]}, y: ${max[1]}}`);
    set(tr, 'm_AnchoredPosition', '{x: 0, y: 0}'); set(tr, 'm_SizeDelta', '{x: 0, y: 0}');
    if (get(parent).text.includes('  m_Children: []')) get(parent).text = get(parent).text.replace('  m_Children: []', `  m_Children:\n  - {fileID: ${tr}}`);
    else get(parent).text = get(parent).text.replace(/(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/, '$1' + `  - {fileID: ${tr}}\n`);
    return { go, tr };
};
const text = (name, parent, title, min, max, size, template = '946000000000016') => {
    const n = node(name, parent, min, max);
    clone('946000000000015', n.go);
    const label = clone(template, n.go);
    set(label, 'm_text', JSON.stringify(title));
    for (const key of ['m_fontSize', 'm_fontSizeBase', 'm_fontSizeMax']) set(label, key, size);
    set(label, 'm_fontSizeMin', Math.min(size, size * .7));
    return { ...n, label };
};
const button = (name, parent, title, min, max) => {
    const n = node(name, parent, min, max);
    clone('946000000000023', n.go);
    const image = clone('946000000000048', n.go);
    const button = clone('946000000000049', n.go);
    set(button, 'm_TargetGraphic', `{fileID: ${image}}`); set(button, 'm_Interactable', 1);
    text('Texto ' + name, n.tr, title, [.04, .08], [.96, .92], 44, '946000000000053');
    return { ...n, button };
};
const settings = node('Ajustes', '1915654019');
clone('946000000000010', settings.go);
const background = clone('946000000000011', settings.go);
set(background, 'm_Sprite', '{fileID: 0}');
text('Título de ajustes', settings.tr, 'Ajustes', [.07, .83], [.93, .94], 66);
text('Descripción de privacidad', settings.tr, 'Consulta cómo POMO utiliza y guarda tu información.', [.09, .65], [.91, .80], 42, '946000000000020');
const read = button('Consultar aviso de privacidad', settings.tr, 'Aviso de privacidad', [.10, .46], [.90, .55]);
const back = button('Volver a inicio', settings.tr, 'Volver a inicio', [.10, .12], [.90, .21]);
const open = button('Abrir ajustes', '299299772', 'Ajustes', [.75, .025], [.965, .085]);
set(settings.go, 'm_IsActive', 0);
const controller = allocate();
blocks.set(controller, { type: '114', stripped: '', text: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1915654018}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${guid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.Screens.AjustesPrivacidadController
  panelInicio: {fileID: 299299769}
  panelAjustes: {fileID: ${settings.go}}
  botonAbrirAjustes: {fileID: ${open.button}}
  botonLeerAviso: {fileID: ${read.button}}
  botonVolver: {fileID: ${back.button}}
  avisoPrivacidad: {fileID: 946000000000084}
` });
attach('1915654018', controller);
// The initial state is serialized; no script activates the consent panel at startup.
for (const [id, active] of [['96494524', 1], ['946000000000004', 1], ['946000000000008', 1], ['946000000000054', 0], ['299299769', 0]])
    set(id, 'm_IsActive', active);
for (const [id, old] of before) {
    assert(blocks.has(id));
    assert.equal(get(id).text.match(/  m_OnClick:\n[\s\S]*$/)?.[0], old.match(/  m_OnClick:\n[\s\S]*$/)?.[0]);
}
for (const [id, v] of blocks) for (const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g)) assert(m[1] === '0' || blocks.has(m[1]), id + ' -> ' + m[1]);
const directory = path.join(root, 'Logs/Sprint4'); fs.mkdirSync(directory, { recursive: true });
fs.writeFileSync(path.join(directory, 'Santiago.before-settings.unity'), original);
const output = source.slice(0, source.indexOf('--- !u!')) + [...blocks].map(([id, v]) => `--- !u!${v.type} &${id}${v.stripped}\n${v.text}`).join('');
fs.writeFileSync(target, output.replace(/\n/g, '\r\n'));
console.log(JSON.stringify({ addedObjects: blocks.size - before.size, existingCallbacksPreserved: true, consentInitiallyActive: true, settingsInitiallyInactive: true }, null, 2));
