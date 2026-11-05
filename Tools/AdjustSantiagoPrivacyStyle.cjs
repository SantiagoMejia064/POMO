// Preserve scene layout edits; fix text wrapping and apply the scene's button style.
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');
const assert = require('assert/strict');
const crypto = require('crypto');
const root = path.resolve(__dirname, '..');
const scenePath = path.join(root, 'Assets/Scenes/Santiago.unity');
const original = fs.readFileSync(scenePath, 'utf8');
const source = original.replace(/\r\n/g, '\n');
const styleGuid = '8ff189da3d014c34bed9328f90a80c9d';
const spriteGuid = '82480cc5aa62413f91267cf95ae4b53d';
assert(!source.includes(styleGuid), 'Style already added; edit the scene directly.');
const matches = [...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)];
const b = new Map(matches.map(m => [m[2], { type: m[1], stripped: m[3] || '', text: m[4] }]));
assert.equal(matches.length, b.size);
const before = new Map([...b].map(([id, v]) => [id, v.text]));
const get = id => { assert(b.has(id), 'Missing ID ' + id); return b.get(id); };
const field = (v, key) => v.text.match(new RegExp('^  ' + key + ': (.*)$', 'm'))?.[1];
const reference = (v, key) => field(v, key)?.match(/fileID: (-?\d+)/)?.[1];
const set = (id, key, value) => {
    const expression = new RegExp('^  ' + key + ':.*$', 'm');
    assert(expression.test(get(id).text), 'Missing property ' + id + ':' + key);
    get(id).text = get(id).text.replace(expression, () => '  ' + key + ': ' + value);
};

// Native 64px nine-slice sprite. Corners have a 12px radius; no existing art is edited.
function crc32(buffer) {
    let crc = -1;
    for (const value of buffer) {
        crc ^= value;
        for (let bit = 0; bit < 8; bit++) crc = (crc >>> 1) ^ ((crc & 1) ? 0xedb88320 : 0);
    }
    return (crc ^ -1) >>> 0;
}
function chunk(type, data) {
    const name = Buffer.from(type), length = Buffer.alloc(4), checksum = Buffer.alloc(4);
    length.writeUInt32BE(data.length); checksum.writeUInt32BE(crc32(Buffer.concat([name, data])));
    return Buffer.concat([length, name, data, checksum]);
}
const size = 64, radius = 12, pixels = Buffer.alloc(size * (1 + size * 4));
for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) {
    const centerX = Math.max(radius, Math.min(size - radius, x + .5));
    const centerY = Math.max(radius, Math.min(size - radius, y + .5));
    let coverage = 0;
    for (let sy = 0; sy < 4; sy++) for (let sx = 0; sx < 4; sx++)
        if (Math.hypot(x + (sx + .5) / 4 - centerX, y + (sy + .5) / 4 - centerY) <= radius) coverage++;
    const offset = y * (1 + size * 4) + 1 + x * 4;
    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = 255;
    pixels[offset + 3] = Math.round(255 * coverage / 16);
}
const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(size); ihdr.writeUInt32BE(size, 4); ihdr[8] = 8; ihdr[9] = 6;
const png = Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(pixels)), chunk('IEND', Buffer.alloc(0))]);
const spritePath = path.join(root, 'Assets/Resources/BotonRedondeado12.png');
assert(!fs.existsSync(spritePath), 'Do not overwrite an existing rounded sprite.');
let importer = fs.readFileSync(path.join(root, 'Assets/Images/botones/CUADRO REDONDEADO-01.png.meta'), 'utf8');
importer = importer.replace(/^guid:.*$/m, 'guid: ' + spriteGuid)
    .replace(/spriteBorder:.*$/, 'spriteBorder: {x: 12, y: 12, z: 12, w: 12}')
    .replace(/spriteBorder:.*$/m, 'spriteBorder: {x: 12, y: 12, z: 12, w: 12}')
    .replace(/filterMode: \d/g, 'filterMode: 1')
    .replace(/textureCompression: \d/g, 'textureCompression: 0')
    .replace(/maxTextureSize: \d+/g, 'maxTextureSize: 64')
    .replace(/spriteID: .*/g, 'spriteID: 6412dc8463a54776b7a390944ed0b967');

const names = {
    '008': 'Cuadro de consentimiento', '013': 'Título del consentimiento',
    '017': 'Explicación del consentimiento', '021': 'Leer términos y privacidad',
    '026': 'Texto del botón leer términos', '030': 'Casilla de aceptación',
    '032': 'Fondo de la casilla', '036': 'Marca de aceptación', '041': 'Texto de aceptación',
    '045': 'Aceptar y continuar', '050': 'Texto del botón aceptar', '054': 'Aviso completo de privacidad',
    '058': 'Título del aviso', '062': 'Área de lectura', '066': 'Zona visible del aviso',
    '069': 'Texto completo del aviso', '075': 'Volver al consentimiento', '080': 'Texto del botón volver'
};
for (const [suffix, name] of Object.entries(names)) set('946000000000' + suffix, 'm_Name', JSON.stringify(name));
// Each paragraph follows the actual width of its RectTransform. The scroll content only fits vertically.
for (const [id, v] of b) if (id.startsWith('946') && v.text.includes('Unity.TextMeshPro::TMPro.TextMeshProUGUI')) {
    set(id, 'm_TextWrappingMode', 1);
    if (id === '946000000000072') set(id, 'm_overflowMode', 0);
    else {
        set(id, 'm_overflowMode', 2);
        const max = Number(field(v, 'm_fontSize'));
        set(id, 'm_enableAutoSizing', 1);
        set(id, 'm_fontSizeMax', max);
        set(id, 'm_fontSizeMin', Math.min(max, Math.max(20, max * .7)));
    }
}
set('946000000000073', 'm_HorizontalFit', 0);
set('946000000000073', 'm_VerticalFit', 2);

const transformByGo = new Map([...b].filter(([id, v]) => ['4', '224'].includes(v.type)).map(([id, v]) => [reference(v, 'm_GameObject'), id]));
const buttonGos = new Set([...b].filter(([id, v]) => v.text.includes('UnityEngine.UI::UnityEngine.UI.Button\n')).map(([id, v]) => reference(v, 'm_GameObject')));
const inputGos = new Set([...b].filter(([id, v]) => /::(?:TMPro.TMP_InputField|UnityEngine.UI.InputField)\n/.test(v.text)).map(([id, v]) => reference(v, 'm_GameObject')));
function buttonAncestor(go) {
    while (go && go !== '0') {
        if (inputGos.has(go)) return null;
        if (buttonGos.has(go)) return go;
        const transform = b.get(transformByGo.get(go));
        go = transform && b.get(reference(transform, 'm_Father')) ? reference(b.get(reference(transform, 'm_Father')), 'm_GameObject') : null;
    }
    return null;
}
let labels = 0, rounded = 0;
// These lists use their original sprite dimensions to calculate card widths.
const originalCardLists = new Set(['112485988', '873348477', '319139577', '330073916', '105291175']);
function belongsToCardList(go) {
    let transform = transformByGo.get(go);
    while (transform && transform !== '0') {
        if (originalCardLists.has(transform)) return true;
        transform = reference(b.get(transform), 'm_Father');
    }
    return false;
}
for (const [id, v] of b) {
    if (belongsToCardList(reference(v, 'm_GameObject'))) continue;
    if (v.text.includes('Unity.TextMeshPro::TMPro.TextMeshProUGUI') && buttonAncestor(reference(v, 'm_GameObject'))) {
        set(id, 'm_fontAsset', '{fileID: 11400000, guid: f2857b70832f1de4d951514de9b6f111, type: 2}');
        set(id, 'm_sharedMaterial', '{fileID: 9129254487251998130, guid: f2857b70832f1de4d951514de9b6f111, type: 2}');
        set(id, 'm_fontStyle', 0); set(id, 'm_TextWrappingMode', 1); set(id, 'm_overflowMode', 1);
        const max = Number(field(v, 'm_fontSize'));
        set(id, 'm_enableAutoSizing', 1); set(id, 'm_fontSizeMax', max);
        set(id, 'm_fontSizeMin', Math.min(max, Math.max(10, max * .65)));
        labels++;
    } else if (v.text.includes('UnityEngine.UI::UnityEngine.UI.Text\n') && buttonAncestor(reference(v, 'm_GameObject'))) {
        v.text = v.text.replace(/(m_Font: )\{[^\n]+\}/, '$1{fileID: 12800000, guid: befcd39ff37b63743bb99a6ba301b1c1, type: 3}');
    }
    if (v.text.includes('UnityEngine.UI::UnityEngine.UI.Button\n')) {
        const imageId = reference(v, 'm_TargetGraphic'), image = b.get(imageId);
        if (!image?.text.includes('UnityEngine.UI::UnityEngine.UI.Image\n')) continue;
        const sprite = field(image, 'm_Sprite');
        if (sprite === '{fileID: 0}' || sprite?.includes('0000000000000000f000000000000000') || sprite?.includes('0a80d47907fc98647a448423022d60b9')) {
            set(imageId, 'm_Sprite', `{fileID: 21300000, guid: ${spriteGuid}, type: 3}`);
            set(imageId, 'm_Type', 1); set(imageId, 'm_PixelsPerUnitMultiplier', 1); rounded++;
        }
    }
}
const styleId = '946000000000085';
assert(!b.has(styleId));
b.set(styleId, { type: '114', stripped: '', text: `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1915654018}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${styleGuid}, type: 3}
  m_Name:
  m_EditorClassIdentifier: Assembly-CSharp::Pomo.Presentation.UI.EstiloBotonesEscena
  fuenteBotones: {fileID: 11400000, guid: f2857b70832f1de4d951514de9b6f111, type: 2}
  fuenteClasica: {fileID: 12800000, guid: befcd39ff37b63743bb99a6ba301b1c1, type: 3}
  fondoRedondeado: {fileID: 21300000, guid: ${spriteGuid}, type: 3}
  fondoPlanoAnterior: {fileID: 21300000, guid: 0a80d47907fc98647a448423022d60b9, type: 3}
  fondosIntegrados:
  - {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}
  - {fileID: 10907, guid: 0000000000000000f000000000000000, type: 0}
  radioEsquinas: 12
` });
get('1915654018').text = get('1915654018').text.replace(/(  m_Component:\n(?:  - component:.*\n)+)/, '$1' + `  - component: {fileID: ${styleId}}\n`);
for (const [id, previous] of before) {
    assert(b.has(id));
    const callbacks = t => t.match(/  m_OnClick:\n[\s\S]*$/)?.[0];
    assert.equal(callbacks(get(id).text), callbacks(previous), 'Changed button callbacks ' + id);
}
for (const [id, v] of b) for (const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g)) assert(m[1] === '0' || b.has(m[1]), `Missing reference ${id} -> ${m[1]}`);
const backupDir = path.join(root, 'Logs/Sprint4'); fs.mkdirSync(backupDir, { recursive: true });
const signature = crypto.createHash('sha256').update(original).digest('hex').slice(0, 12);
fs.writeFileSync(path.join(backupDir, `Santiago.before-style.${signature}.unity`), original);
fs.writeFileSync(spritePath, png); fs.writeFileSync(spritePath + '.meta', importer);
const output = source.slice(0, source.indexOf('--- !u!')) + [...b].map(([id, v]) => `--- !u!${v.type} &${id}${v.stripped}\n${v.text}`).join('');
fs.writeFileSync(scenePath, output.replace(/\n/g, '\r\n'));
console.log(JSON.stringify({ buttonLabelsStyled: labels, rectangularBackgroundsRounded: rounded, privacyObjectsRenamed: Object.keys(names).length, callbacksPreserved: true, missingLocalReferences: 0 }, null, 2));
