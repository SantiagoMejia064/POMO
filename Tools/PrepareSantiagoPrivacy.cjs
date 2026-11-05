// One-time additive migration: saved, editable privacy UI in Santiago only.
const fs = require('fs');
const path = require('path');
const assert = require('assert/strict');
const crypto = require('crypto');
const root = path.resolve(__dirname, '..');
const target = path.join(root, 'Assets/Scenes/Santiago.unity');
const original = fs.readFileSync(target, 'utf8');
const source = original.replace(/\r\n/g, '\n');
const controllerGuid = '643776de3c894a30bb289e64641a7c2e';
if (source.includes(controllerGuid)) throw new Error('Privacy UI already exists. Edit it directly; do not rebuild.');
const header = source.slice(0, source.indexOf('--- !u!'));
const matches = [...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)];
const blocks = new Map(matches.map(m => [m[2], { type: m[1], stripped: m[3] || '', text: m[4] }]));
const baseline = new Map([...blocks].map(([id, b]) => [id, b.text]));
assert.equal(matches.length, blocks.size);
const get = id => { assert(blocks.has(String(id)), 'Missing object ' + id); return blocks.get(String(id)); };
const set = (id, key, value) => {
    const b = get(id), expression = new RegExp('^  ' + key + ':.*$', 'm');
    assert(expression.test(b.text), 'Missing property ' + key);
    b.text = b.text.replace(expression, () => '  ' + key + ': ' + value);
};
const children = id => [...(get(id).text.match(/  m_Children:\n((?:  - \{fileID: \d+\}\n)*)/)?.[1] || '').matchAll(/fileID: (\d+)/g)].map(m => m[1]);
const setChildren = (id, ids) => {
    get(id).text = get(id).text.replace(/  m_Children: (?:\[\]\n)|  m_Children:\n(?:  - \{fileID: \d+\}\n)*/,
        ids.length ? '  m_Children:\n' + ids.map(c => `  - {fileID: ${c}}\n`).join('') : '  m_Children: []\n');
};
let next = 946000000000000;
const allocate = () => { const id = String(++next); assert(!blocks.has(id)); return id; };
const attach = (go, component) => {
    get(go).text = get(go).text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/, '$1' + `  - component: {fileID: ${component}}\n`);
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
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
` });
    blocks.set(tr, { type: '224', stripped: '', text: `RectTransform:
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
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
` });
    setChildren(parent, [...children(parent), tr]);
    return { go, tr };
};
const clone = (template, go) => {
    const id = allocate(), b = get(template);
    blocks.set(id, { ...b, text: b.text.replace(/^  m_GameObject:.*$/m, `  m_GameObject: {fileID: ${go}}`) });
    attach(go, id); return id;
};
const group = (go, interactable = true) => {
    const id = allocate();
    blocks.set(id, { type: '225', stripped: '', text: `CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${go}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: ${interactable ? 1 : 0}
  m_BlocksRaycasts: ${interactable ? 1 : 0}
  m_IgnoreParentGroups: 0
` });
    attach(go, id); return id;
};
const tint = (r, g, b, a = 1) => `{r: ${r / 255}, g: ${g / 255}, b: ${b / 255}, a: ${a}}`;
const ink = tint(20, 54, 92), cream = tint(249, 247, 240), blue = tint(32, 84, 139), white = tint(255, 255, 255);
const image = (go, color, raycast = true) => {
    clone('1830129154', go);
    const id = clone('1830129153', go);
    set(id, 'm_Color', color); set(id, 'm_RaycastTarget', raycast ? 1 : 0);
    return id;
};
const label = (name, parent, value, min, max, size = 40, color = ink, align = 2) => {
    const n = node(name, parent, min, max);
    clone('572377213', n.go);
    const text = clone('572377212', n.go);
    set(text, 'm_text', JSON.stringify(value));
    set(text, 'm_fontAsset', '{fileID: 11400000, guid: d63653cef15953f4892b5ff4eb8d2dc3, type: 2}');
    set(text, 'm_sharedMaterial', '{fileID: -7158013276951197918, guid: d63653cef15953f4892b5ff4eb8d2dc3, type: 2}');
    for (const key of ['m_fontSize', 'm_fontSizeBase']) set(text, key, size);
    set(text, 'm_enableAutoSizing', 0); set(text, 'm_fontStyle', 0);
    set(text, 'm_fontColor', color); set(text, 'm_HorizontalAlignment', align);
    set(text, 'm_VerticalAlignment', 512); set(text, 'm_RaycastTarget', 0);
    set(text, 'm_isRichText', 0); set(text, 'm_margin', '{x: 0, y: 0, z: 0, w: 0}');
    set(text, 'm_TextWrappingMode', 1);
    return { ...n, text };
};
const button = (name, parent, title, min, max, background = blue, foreground = white, size = 42) => {
    const n = node(name, parent, min, max), graphic = image(n.go, background);
    const id = clone('1830129152', n.go);
    set(id, 'm_TargetGraphic', `{fileID: ${graphic}}`);
    get(id).text = get(id).text.replace(/  m_OnClick:\n[\s\S]*$/, '  m_OnClick:\n    m_PersistentCalls:\n      m_Calls: []\n');
    label(name + ' Label', n.tr, title, [.04, .08], [.96, .92], size, foreground);
    return { ...n, button: id };
};
const welcome = '96494525', oldChildren = children(welcome);
const content = node('Welcome Content', welcome);
setChildren(content.tr, oldChildren);
for (const id of oldChildren) set(id, 'm_Father', `{fileID: ${content.tr}}`);
setChildren(welcome, [content.tr]);
const welcomeGroup = group(content.go, false);
const overlay = node('Privacy Consent', welcome);
const overlayImage = image(overlay.go, tint(10, 29, 54, .92));
set(overlayImage, 'm_Sprite', '{fileID: 0}');
const card = node('Consent Card', overlay.tr, [.065, .24], [.935, .76]);
image(card.go, cream);
const cardGroup = group(card.go);
label('Consent Title', card.tr, 'Antes de comenzar', [.08, .79], [.92, .94], 60);
label('Consent Explanation', card.tr, 'Lee cómo POMO utiliza y guarda tu información.\nSi estás de acuerdo, marca la casilla para continuar.', [.09, .53], [.91, .77], 42);
const terms = button('Read Terms', card.tr, 'Leer términos y privacidad', [.09, .37], [.91, .49], cream, blue, 40);
const check = node('Accept Terms Toggle', card.tr, [.09, .19], [.91, .32]);
const box = node('Checkbox Background', check.tr, [0, .16], [.105, .84]);
const boxImage = image(box.go, white);
const mark = label('Checkmark', box.tr, '✓', [.05, .05], [.95, .95], 50, blue);
const toggle = clone('238294835', check.go);
set(toggle, 'm_TargetGraphic', `{fileID: ${boxImage}}`);
set(toggle, 'graphic', `{fileID: ${mark.text}}`);
set(toggle, 'm_IsOn', 0);
label('Acceptance Label', check.tr, 'He leído y acepto los términos\ny el aviso de privacidad.', [.15, 0], [1, 1], 35, ink, 1);
const accept = button('Accept and Continue', card.tr, 'Aceptar y continuar', [.09, .04], [.91, .15]);
set(accept.button, 'm_Interactable', 0);

const notice = node('Full Privacy Notice', overlay.tr, [.04, .065], [.96, .935]);
image(notice.go, cream);
label('Notice Title', notice.tr, 'Términos y privacidad', [.07, .87], [.93, .97], 46);
const scrollRoot = node('Notice Scroll', notice.tr, [.07, .16], [.93, .85]);
image(scrollRoot.go, white);
const viewport = node('Viewport', scrollRoot.tr);
const mask = clone('851415649', viewport.go);
set(mask, 'm_Padding', '{x: 0, y: 0, z: 0, w: 0}');
const noticeBody = label('Notice Text', viewport.tr, fs.readFileSync(path.join(root, 'Assets/Resources/PrivacyNotice.txt'), 'utf8'), [0, 1], [1, 1], 36, ink, 1);
set(noticeBody.tr, 'm_Pivot', '{x: 0.5, y: 1}');
set(noticeBody.text, 'm_VerticalAlignment', 256);
set(noticeBody.text, 'm_margin', '{x: 20, y: 20, z: 20, w: 20}');
const fitterTemplate = [...blocks].find(([id, b]) => b.text.includes('UnityEngine.UI::UnityEngine.UI.ContentSizeFitter\n'))[0];
const fitter = clone(fitterTemplate, noticeBody.go);
set(fitter, 'm_HorizontalFit', 0); set(fitter, 'm_VerticalFit', 2);
const scroll = clone('1350904825', scrollRoot.go);
set(scroll, 'm_Content', `{fileID: ${noticeBody.tr}}`);
set(scroll, 'm_Viewport', `{fileID: ${viewport.tr}}`);
set(scroll, 'm_VerticalScrollbar', '{fileID: 0}');
set(scroll, 'm_MovementType', 2); set(scroll, 'm_ScrollSensitivity', 40);
const close = button('Close Terms', notice.tr, 'Volver', [.08, .035], [.92, .115]);
set(notice.go, 'm_IsActive', 0);

const controller = allocate();
blocks.set(controller, { type: '114', stripped: '', text: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 96494524}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${controllerGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.Screens.PrivacyConsentController
  welcomeContent: {fileID: ${welcomeGroup}}
  consentPanel: {fileID: ${overlay.go}}
  consentCard: {fileID: ${cardGroup}}
  noticePanel: {fileID: ${notice.go}}
  acceptanceToggle: {fileID: ${toggle}}
  acceptButton: {fileID: ${accept.button}}
  termsButton: {fileID: ${terms.button}}
  closeNoticeButton: {fileID: ${close.button}}
  noticeText: {fileID: ${noticeBody.text}}
  noticeScroll: {fileID: ${scroll}}
  privacyNotice: {fileID: 4900000, guid: bccf9b24049b4381b9525fd489a374ae, type: 3}
` });
attach('96494524', controller);

// Preserve original objects and all existing welcome button callbacks.
for (const id of baseline.keys()) assert(blocks.has(id));
for (const id of ['1830129152', '1062153587', '334875461', '447183979']) assert.equal(get(id).text, baseline.get(id));
for (const [id, b] of blocks) for (const m of b.text.matchAll(/\{fileID: (-?\d+)\}/g))
    assert(m[1] === '0' || blocks.has(m[1]), `Unresolved reference ${id} -> ${m[1]}`);
const changed = [...baseline].filter(([id, text]) => text !== get(id).text).map(([id]) => id);
assert.deepEqual(new Set(changed), new Set(['96494524', welcome, ...oldChildren]));
const result = header + [...blocks].map(([id, b]) => `--- !u!${b.type} &${id}${b.stripped}\n${b.text}`).join('');
const backup = path.join(root, 'Logs/Sprint4');
fs.mkdirSync(backup, { recursive: true });
const signature = crypto.createHash('sha256').update(original).digest('hex').slice(0, 12);
fs.writeFileSync(path.join(backup, `Santiago.before-privacy.${signature}.unity`), original);
fs.writeFileSync(target, result.replace(/\n/g, '\r\n'));
console.log(JSON.stringify({ addedObjects: blocks.size - baseline.size, changedOriginalObjects: changed, originalCallbacksPreserved: true, missingReferences: 0, controller }, null, 2));
