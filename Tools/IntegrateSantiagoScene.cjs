// Merge saved scene objects without changing either source scene or the target GUID.
// Dry run by default. Run with --write to update Santiago after validating the union.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const assert = require('assert/strict');
const root = path.resolve(__dirname, '..');
const scenePath = name => path.join(root, 'Assets/Scenes', name + '.unity');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
function parse(raw) {
    const source = raw.toString('utf8').replace(/\r\n/g, '\n');
    const matches = [...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)];
    const blocks = new Map(matches.map(m => [m[2], { type: m[1], stripped: m[3] || '', text: m[4] }]));
    assert.equal(matches.length, blocks.size, 'Duplicate local IDs in source');
    return { header: source.slice(0, source.indexOf('--- !u!')), blocks };
}
const snapshots = new Map(['Steven 1', 'Chantre 3', 'Santiago'].map(n => [n, fs.readFileSync(scenePath(n))]));
const steven = parse(snapshots.get('Steven 1'));
const chantre = parse(snapshots.get('Chantre 3'));
const merged = new Map([...steven.blocks].map(([id, b]) => [id, { ...b }]));
const edits = [];
function list(text, key, itemPattern) {
    const match = text.match(new RegExp('^  ' + key + ':\\n((?:  - ' + itemPattern + '\\n)*)', 'm'));
    return match ? { whole: match[0], items: match[1].split('\n').filter(Boolean) } : null;
}
for (const [id, incoming] of chantre.blocks) {
    if (!merged.has(id)) {
        merged.set(id, { ...incoming });
        continue;
    }
    const destination = merged.get(id);
    assert.equal(destination.type, incoming.type, 'Conflicting object types at ' + id);
    // Retain Steven's existing layout, scripts and events. Add Chantre's children/components.
    for (const [key, pattern] of [['m_Children', '\\{fileID: -?\\d+\\}'], ['m_Component', 'component: \\{fileID: -?\\d+\\}'], ['m_Roots', '\\{fileID: -?\\d+\\}']]) {
        const extra = list(incoming.text, key, pattern);
        if (!extra) continue;
        const existing = list(destination.text, key, pattern);
        const items = [...new Set([...(existing?.items || []), ...extra.items])];
        if (items.length === (existing?.items.length || 0)) continue;
        const replacement = '  ' + key + ':\n' + items.join('\n') + '\n';
        if (existing) destination.text = destination.text.replace(existing.whole, () => replacement);
        else {
            assert(destination.text.includes('  ' + key + ': []'), 'Missing list ' + id + ':' + key);
            destination.text = destination.text.replace('  ' + key + ': []\n', () => replacement);
        }
        edits.push(id + ': additive ' + key);
    }
}
// Chantre's completion UI is an addition to Steven's existing block controller.
for (const key of ['completionPanel', 'completionFireRawImage']) {
    const value = chantre.blocks.get('1521692123').text.match(new RegExp('^  ' + key + ': (.*)$', 'm'))[1];
    const block = merged.get('1521692123');
    assert(block.text.includes('  ' + key + ': {fileID: 0}'), 'Review existing completion assignment');
    block.text = block.text.replace('  ' + key + ': {fileID: 0}', '  ' + key + ': ' + value);
    edits.push('1521692123: ' + key);
}
// Only the welcome panel is visible at startup; all original module entry points remain.
for (const id of ['9271335', '299299769', '849969573', '1185666112', '1427321649']) {
    const block = merged.get(id);
    block.text = block.text.replace(/^  m_IsActive: 1$/m, '  m_IsActive: 0');
    edits.push(id + ': startup hidden behind welcome');
}
const serialized = steven.header + [...merged].map(([id, b]) => `--- !u!${b.type} &${id}${b.stripped}\n${b.text}`).join('');
const result = parse(Buffer.from(serialized));
assert.equal(result.blocks.size, merged.size);
for (const source of [steven, chantre]) for (const id of source.blocks.keys()) assert(result.blocks.has(id), 'Lost source object ' + id);
const missing = [];
for (const [id, block] of result.blocks) for (const reference of block.text.matchAll(/\{fileID: (-?\d+)\}/g)) {
    if (reference[1] !== '0' && !result.blocks.has(reference[1])) missing.push({ owner: id, reference: reference[1] });
}
assert.deepEqual(missing, [], 'Unresolved scene references');
// Validate reciprocal Transform relationships, not merely presence of IDs.
for (const [id, block] of result.blocks) {
    if (!['4', '224'].includes(block.type) || block.stripped) continue;
    const parent = block.text.match(/^  m_Father: \{fileID: (\d+)\}/m)?.[1];
    if (parent && parent !== '0') {
        const children = list(result.blocks.get(parent).text, 'm_Children', '\\{fileID: -?\\d+\\}');
        assert(children?.items.includes(`  - {fileID: ${id}}`), 'Missing parent-child link ' + id);
    }
    for (const line of list(block.text, 'm_Children', '\\{fileID: -?\\d+\\}')?.items || []) {
        const child = line.match(/fileID: (\d+)/)[1];
        const childBlock = result.blocks.get(child);
        if (!childBlock.stripped) assert(childBlock.text.includes(`  m_Father: {fileID: ${id}}`), 'Wrong child parent ' + child);
    }
}
const transitions = [['1830129152', '2106442889', '96494524'], ['1062153587', '950695716', '2106442889'], ['334875461', '1935384951', '950695716'], ['447183979', '299299769', '1935384951']];
for (const [button, show, hide] of transitions) {
    const text = result.blocks.get(button).text;
    assert(text.includes(`m_Target: {fileID: ${show}}`) && text.includes(`m_Target: {fileID: ${hide}}`), 'Broken welcome transition');
    assert.equal(text, chantre.blocks.get(button).text, 'Changed original welcome button');
}
const changedSteven = [...steven.blocks].filter(([id, b]) => result.blocks.get(id).text !== b.text).map(([id]) => id);
const report = {
    sourceHashes: Object.fromEntries([...snapshots].map(([n, bytes]) => [n, hash(bytes)])),
    objectsSteven: steven.blocks.size, objectsChantre: chantre.blocks.size,
    objectsIntegrated: merged.size, addedFromChantre: merged.size - steven.blocks.size,
    changedStevenObjects: changedSteven, edits, missingLocalReferences: missing,
    preservedAllSourceObjectIds: true, welcomeTransitionsPreserved: true,
    integratedHash: hash(serialized), written: process.argv.includes('--write')
};
if (process.argv.includes('--check')) {
    assert.equal(snapshots.get('Santiago').toString('utf8').replace(/\r\n/g, '\n'), serialized,
        'Saved Santiago scene differs from the validated source union');
    report.checkedSavedScene = true;
}
if (report.written) {
    const backup = path.join(root, 'Logs/SceneIntegration');
    fs.mkdirSync(backup, { recursive: true });
    const backupPath = path.join(backup, 'Santiago.before.' + report.sourceHashes.Santiago.slice(0, 12) + '.unity');
    if (!fs.existsSync(backupPath)) fs.writeFileSync(backupPath, snapshots.get('Santiago'));
    fs.writeFileSync(scenePath('Santiago'), serialized.replace(/\n/g, '\r\n'));
    for (const name of ['Steven 1', 'Chantre 3']) assert.equal(hash(fs.readFileSync(scenePath(name))), report.sourceHashes[name], 'Source changed');
    fs.writeFileSync(path.join(backup, 'merge-report.json'), JSON.stringify(report, null, 2));
}
console.log(JSON.stringify(report, null, 2));
