// Match only the added notice/settings panels to the existing POMO palette.
const fs=require('fs'),assert=require('assert/strict');
const file='Assets/Scenes/Santiago.unity',source=fs.readFileSync(file,'utf8').replace(/\r\n/g,'\n');
const b=new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],suffix:m[3]||'',text:m[4]}]));
const before=new Map([...b].map(([id,v])=>[id,v.text]));
const added=id=>/^(946|947|948|949|951)\d{12}$/.test(id);
const field=(v,k)=>v.text.match(new RegExp('^  '+k+': (.*)$','m'))?.[1];
const ref=(v,k)=>field(v,k)?.match(/fileID: (-?\d+)/)?.[1];
const set=(id,k,value)=>{const re=new RegExp('^  '+k+':.*$','m');assert(re.test(b.get(id).text),id+':'+k);b.get(id).text=b.get(id).text.replace(re,()=>'  '+k+': '+value);};
const panel='{r: 0.039215688, g: 0.19607843, b: 0.4, a: 1}'; // Existing #0A3266.
const button='{r: 0.294, g: 0.451, b: 0.663, a: 1}'; // Existing #4B73A9.
const white='{r: 1, g: 1, b: 1, a: 1}';
const targets=new Set([...b].filter(([id,v])=>added(id)&&/::UnityEngine.UI.(?:Button|Toggle|Scrollbar|InputField)\n|::TMPro.TMP_InputField\n/.test(v.text)).map(([,v])=>ref(v,'m_TargetGraphic')));
let repaired=0,images=0,texts=0;
for(const[id,v]of b){
    if(!added(id))continue;
    if(v.text.includes('Unity.TextMeshPro::TMPro.TextMeshProUGUI\n')){
        // Older one-line replacements left a folded YAML continuation behind.
        // Remove it only when the first line is already a complete quoted scalar.
        const scalar=v.text.match(/^  m_text: ([^\n]*)(?:\n {4,}[^\n]*)*/m);
        if(scalar&&scalar[0].includes('\n')){
            let complete=false;try{JSON.parse(scalar[1].replace(/\\x([a-f0-9]{2})/gi,'\\u00$1'));complete=true;}catch{}
            if(complete){v.text=v.text.replace(scalar[0],()=> '  m_text: '+scalar[1]);repaired++;}
        }
        set(id,'m_Color',white);set(id,'m_fontColor',white);set(id,'m_enableVertexGradient',0);
        v.text=v.text.replace(/(  m_fontColor32:\n    serializedVersion: \d+\n    rgba:) \d+/, '$1 4294967295');
        texts++;
    }else if(v.text.includes('UnityEngine.UI::UnityEngine.UI.Text\n')){set(id,'m_Color',white);texts++;}
    if(v.text.includes('UnityEngine.UI::UnityEngine.UI.Image\n')){
        // Preserve the small red Settings entry; modal action buttons are blue.
        if(id==='949000000000004')continue;
        const color=field(v,'m_Color');
        if(color&&!color.endsWith('a: 1}'))continue; // Backdrop dimming is unchanged.
        set(id,'m_Color',targets.has(id)||id==='946000000000035'?button:panel);images++;
    }
}
// Keep the destructive explanation fully visible inside its own text area.
for(const id of ['949000000000026','951000000000017','951000000000021']){
    set(id,'m_TextWrappingMode',1);set(id,'m_enableAutoSizing',1);
    set(id,'m_overflowMode',0); // Never silently hide the destructive-action notice.
    set(id,'m_HorizontalAlignment',1);set(id,'m_VerticalAlignment',256);
}
for(const[id,v]of b){
    if(!added(id))assert.equal(v.text,before.get(id),'Changed original object '+id);
    assert.equal(v.text.match(/  m_OnClick:\n[\s\S]*$/)?.[0],before.get(id).match(/  m_OnClick:\n[\s\S]*$/)?.[0],'Changed event '+id);
    if(v.type==='224')assert.equal(v.text,before.get(id),'Changed layout '+id);
    for(const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g))assert(m[1]==='0'||b.has(m[1]),id+' -> '+m[1]);
}
fs.writeFileSync('Logs/Sprint4/Santiago.before-blue-notices.unity',source);
const changes=[...b].filter(([id,v])=>v.text!==before.get(id));
console.error(JSON.stringify({panelsAndControlsColored:images,textsWhite:texts,malformedTextScalarsRepaired:repaired,originalObjectsUntouched:true,callbacksAndLayoutPreserved:true,missingReferences:0}));
const patches=[];
for(let i=0;i<changes.length;i+=6)patches.push('*** Begin Patch\n*** Update File: '+file+'\n'+changes.slice(i,i+6).map(([id,v])=>'@@\n --- !u!'+v.type+' &'+id+v.suffix+'\n'+before.get(id).trimEnd().split('\n').map(l=>'-'+l).join('\n')+'\n'+v.text.trimEnd().split('\n').map(l=>'+'+l).join('\n')+'\n').join('')+'*** End Patch');
fs.writeFileSync('Logs/Sprint4/blue-notices-patches.json',JSON.stringify(patches));
console.log(patches.length);
