// Saved permission explanation, shared by Tasks, Rhythm and Settings in Santiago.
const fs = require('fs'), path = require('path'), assert = require('assert/strict');
const root = path.resolve(__dirname, '..'), target = path.join(root, 'Assets/Scenes/Santiago.unity');
const original = fs.readFileSync(target, 'utf8'), source = original.replace(/\r\n/g, '\n');
const guid = '4b8611a11b0e4474b455df1e6c8b00d1';
assert(!source.includes(guid), 'Permission UI already exists; do not rebuild.');
const matches = [...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)];
const blocks = new Map(matches.map(m => [m[2], { type: m[1], stripped: m[3] || '', text: m[4] }]));
const before = new Map([...blocks].map(([id, v]) => [id, v.text]));
const get = id => { assert(blocks.has(id), id); return blocks.get(id); };
const set = (id, key, value) => { const re = new RegExp('^  ' + key + ':.*$', 'm'); assert(re.test(get(id).text), key); get(id).text = get(id).text.replace(re, () => '  ' + key + ': ' + value); };
let next = 948000000000000;
const allocate = () => { const id = String(++next); assert(!blocks.has(id)); return id; };
const attach = (go, id) => { get(go).text = get(go).text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/, '$1' + `  - component: {fileID: ${id}}\n`); };
const clone = (template, go) => { const id = allocate(); blocks.set(id, { ...get(template), text: get(template).text.replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`) }); attach(go, id); return id; };
const append = (id, fields) => { get(id).text = get(id).text.trimEnd() + '\n' + fields + '\n'; };
const node = (name, parent, min = [0, 0], max = [1, 1]) => {
    const go = allocate(), tr = allocate();
    blocks.set(go, { ...get('946000000000013'), text: get('946000000000013').text
        .replace(/  m_Component:\n(?:  - component:.*\n)+/, `  m_Component:\n  - component: {fileID: ${tr}}\n`)
        .replace(/^  m_Name:.*$/m, '  m_Name: ' + JSON.stringify(name)) });
    blocks.set(tr, { ...get('946000000000014'), text: get('946000000000014').text
        .replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`)
        .replace(/^  m_Father:.*$/m, `  m_Father: {fileID: ${parent}}`) });
    set(tr, 'm_AnchorMin', `{x: ${min[0]}, y: ${min[1]}}`); set(tr, 'm_AnchorMax', `{x: ${max[0]}, y: ${max[1]}}`);
    set(tr, 'm_AnchoredPosition', '{x: 0, y: 0}'); set(tr, 'm_SizeDelta', '{x: 0, y: 0}');
    if (get(parent).text.includes('  m_Children: []')) get(parent).text = get(parent).text.replace('  m_Children: []', `  m_Children:\n  - {fileID: ${tr}}`);
    else get(parent).text = get(parent).text.replace(/(  m_Children:\n(?:  - \{fileID: \d+\}\n)*)/, '$1' + `  - {fileID: ${tr}}\n`);
    return { go, tr };
};
const label = (name, parent, value, min, max, size, template = '946000000000016') => {
    const n = node(name, parent, min, max); clone('946000000000015', n.go);
    const text = clone(template, n.go); set(text, 'm_text', JSON.stringify(value));
    for (const key of ['m_fontSize', 'm_fontSizeBase', 'm_fontSizeMax']) set(text, key, size);
    set(text, 'm_fontSizeMin', Math.min(size, size * .7)); set(text, 'm_TextWrappingMode', 1);
    return { ...n, text };
};
const button = (name, parent, value, min, max) => {
    const n = node(name, parent, min, max); clone('946000000000023', n.go);
    const image = clone('946000000000048', n.go), id = clone('946000000000049', n.go);
    set(id, 'm_TargetGraphic', `{fileID: ${image}}`); set(id, 'm_Interactable', 1);
    const text = label('Texto ' + name, n.tr, value, [.04, .08], [.96, .92], 44, '946000000000053');
    return { ...n, button: id, text: text.text };
};
const group = (go, ignoreParents) => {
    const id = clone('946000000000012', go);
    set(id, 'm_Interactable', 1); set(id, 'm_BlocksRaycasts', 1); set(id, 'm_IgnoreParentGroups', ignoreParents ? 1 : 0);
    return id;
};
const rootGroup = group('1915654018', false);
const overlay = node('Explicación de permisos', '1915654019');
clone('946000000000006', overlay.go); clone('946000000000007', overlay.go);
group(overlay.go, true);
const card = node('Cuadro de permisos', overlay.tr, [.065, .22], [.935, .78]);
clone('946000000000010', card.go); clone('946000000000011', card.go);
const title = label('Título del permiso', card.tr, '¿Permitir notificaciones?', [.08, .79], [.92, .94], 58);
const explanation = label('Motivo del permiso', card.tr,
    'POMO puede mostrar avisos para tus tareas y bloques. Si no los permites, las demás funciones siguen disponibles.',
    [.09, .38], [.91, .76], 42, '946000000000020');
const allow = button('Permitir avisos', card.tr, 'Permitir avisos', [.09, .21], [.91, .32]);
const skip = button('Ahora no', card.tr, 'Ahora no', [.09, .055], [.91, .165]);
set(overlay.go, 'm_IsActive', 0);
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
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.Screens.NotificationPermissionController
  interfazPrincipal: {fileID: ${rootGroup}}
  panelPermisos: {fileID: ${overlay.go}}
  titulo: {fileID: ${title.text}}
  explicacion: {fileID: ${explanation.text}}
  textoPermitir: {fileID: ${allow.text}}
  botonPermitir: {fileID: ${allow.button}}
  botonAhoraNo: {fileID: ${skip.button}}
` });
attach('1915654018', controller);
append('1803286396', `  permisosNotificaciones: {fileID: ${controller}}`);
const rhythm = [...blocks].find(([id, v]) => v.text.includes('Assembly-CSharp::Pomo.Presentation.Screens.RhythmReminderFlowController'));
assert(rhythm); append(rhythm[0], `  permisosNotificaciones: {fileID: ${controller}}`);
const settings = [...blocks].find(([id, v]) => v.text.includes('Assembly-CSharp::Pomo.Presentation.Screens.AjustesPrivacidadController'));
assert(settings);
const settingsGo = settings[1].text.match(/  panelAjustes: \{fileID: (\d+)/)[1];
const settingsTransform = [...blocks].find(([id, v]) => ['4', '224'].includes(v.type) && v.text.includes(`  m_GameObject: {fileID: ${settingsGo}}`))[0];
const enable = button('Permiso de notificaciones', settingsTransform, 'Notificaciones', [.10, .33], [.90, .42]);
const status = label('Estado de notificaciones', settingsTransform, '', [.10, .235], [.90, .31], 30, '946000000000020');
append(settings[0], `  permisosNotificaciones: {fileID: ${controller}}\n  botonNotificaciones: {fileID: ${enable.button}}\n  estadoNotificaciones: {fileID: ${status.text}}`);
for (const [id, old] of before) {
    assert(blocks.has(id));
    assert.equal(get(id).text.match(/  m_OnClick:\n[\s\S]*$/)?.[0], old.match(/  m_OnClick:\n[\s\S]*$/)?.[0]);
}
for (const [id, v] of blocks) for (const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g)) assert(m[1] === '0' || blocks.has(m[1]), id + ' -> ' + m[1]);
fs.mkdirSync(path.join(root, 'Logs/Sprint4'), { recursive: true });
fs.writeFileSync(path.join(root, 'Logs/Sprint4/Santiago.before-permissions.unity'), original);
const output = source.slice(0, source.indexOf('--- !u!')) + [...blocks].map(([id, v]) => `--- !u!${v.type} &${id}${v.stripped}\n${v.text}`).join('');
fs.writeFileSync(target, output.replace(/\n/g, '\r\n'));
console.log(JSON.stringify({ addedObjects: blocks.size - before.size, startsHidden: true, callbacksPreserved: true, sharedPermissionController: controller }, null, 2));
