// Read-only audit of the saved scene's interaction links and confirmation fields.
const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const source=fs.readFileSync('Assets/Scenes/Santiago.unity','utf8').replace(/\r\n/g,'\n');
const b=new Map([...source.matchAll(/^--- !u!(\d+) &(-?\d+)(?: stripped)?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],text:m[3]}]));
const field=(v,k)=>v?.text.match(new RegExp('^  '+k+': (.*)$','m'))?.[1];
const ref=(v,k)=>field(v,k)?.match(/fileID: (-?\d+)/)?.[1];
for(const[id,v]of b)for(const m of v.text.matchAll(/\{fileID: (-?\d+)\}/g))assert(m[1]==='0'||b.has(m[1]),id+' -> '+m[1]);
const files=[];function walk(dir){for(const file of fs.readdirSync(dir,{withFileTypes:true})){const name=path.join(dir,file.name);if(file.isDirectory())walk(name);else if(name.endsWith('.cs'))files.push(name);}}walk('Assets/Scripts');
const classes=new Map();for(const file of files){const code=fs.readFileSync(file,'utf8');for(const m of code.matchAll(/\bclass\s+(\w+)(?:\s*:\s*([\w.]+))?/g))classes.set(m[1],{code,parent:m[2]?.split('.').at(-1)});}
function hasMethod(type,method,visited=new Set()){
    if(visited.has(type))return false;visited.add(type);
    const info=classes.get(type);if(!info)return false;
    if(new RegExp('\\b'+method+'\\s*\\(').test(info.code))return true;
    return info.parent&&hasMethod(info.parent,method,visited);
}
let buttons=0,callbacks=0;const methods=new Set(),unassigned=[],unknownMethods=[];
for(const[id,v]of b){if(!v.text.includes('UnityEngine.UI::UnityEngine.UI.Button\n'))continue;buttons++;
    const click=v.text.slice(v.text.indexOf('  m_OnClick:'));
    for(const m of click.matchAll(/      - m_Target: \{fileID: (-?\d+)\}([\s\S]*?)(?=      - m_Target:|$)/g)){
        const method=m[2].match(/        m_MethodName: (.+)/)?.[1];if(!method)continue;
        const target=b.get(m[1]);
        if(!target){unassigned.push({button:id,method,target:m[1]});continue;}
        callbacks++;methods.add(method);
        if(target.type==='1')assert.equal(method,'SetActive','Unexpected GameObject callback '+method);
        else if(target.text.startsWith('AudioSource:'))assert(['Play','Stop','Pause','UnPause'].includes(method),'Unknown audio callback '+method);
        else if(target.text.startsWith('VideoPlayer:'))assert(['Play','Pause','Stop'].includes(method),'Unknown video callback '+method);
        else {
            const type=field(target,'m_EditorClassIdentifier')?.split('::').at(-1).split('.').at(-1);
            if(!type||!hasMethod(type,method))unknownMethods.push({button:id,type,method});
        }
    }
}
for(const method of ['SaveEditedTask','RequestCloseEdit','ReturnToEdit','DiscardChanges','ConfirmDelete','CancelDelete','RequestDeleteEditingTask','DeleteCalendarSelectedTask'])assert(methods.has(method),'No button bound to '+method);
const required={
    TasksScreenController:['taskListPanel','taskFormPanel','editTaskPanel','unsavedChangesPanel','savedChangesPanel','deleteConfirmationPanel','editTitleInput','editDueDateInput','calendarEditSummaryPanel'],
    PrivacyConsentController:['consentPanel','acceptanceToggle','acceptButton','noticePanel','nombreBienvenida'],
    BorradoDatosController:['panelConfirmacion','primerPaso','segundoPaso','nombreConfirmacion','botonConfirmar','botonCancelar','botonEliminarDefinitivo','botonCancelarFinal','interfazPrincipal'],
    NombreBienvenidaController:['nombre','botonContinuar','panelBienvenida','panelHola','privacidad'],
    AjustesPrivacidadController:['panelInicio','panelAjustes','botonAbrirAjustes','botonVolver','botonLeerAviso','permisosNotificaciones'],
    NotificationPermissionController:['panelPermisos','interfazPrincipal','botonPermitir','botonAhoraNo']
};
for(const[type,fields]of Object.entries(required)){
    const found=[...b].filter(([,v])=>v.text.includes('::Pomo.Presentation.Screens.'+type+'\n'));
    assert.equal(found.length,1,type+' must exist once');
    for(const key of fields)assert(b.has(ref(found[0][1],key)),type+'.'+key+' is unassigned');
}
assert.equal([...b.values()].filter(v=>v.text.includes('::UnityEngine.EventSystems.EventSystem\n')).length,1,'Duplicate/missing EventSystem');
const transforms=new Map([...b].filter(([,v])=>['224','4'].includes(v.type)).map(([id,v])=>[ref(v,'m_GameObject'),id]));
function belongsToWeeklySummary(button){let tr=transforms.get(ref(b.get(button),'m_GameObject'));while(tr&&tr!=='0'){if(tr==='1379639315')return true;tr=ref(b.get(tr),'m_Father');}return false;}
assert(unassigned.every(x=>x.method==='SelectDayOfCurrentWeek'&&belongsToWeeklySummary(x.button)),'Unassigned action outside the known weekly-summary copies.');
assert(unknownMethods.every(x=>x.button==='1801677499'&&x.type==='PomoNotifications'&&x.method==='ProbarNotificacion'),'Unknown action outside the legacy notification test button.');
console.log(JSON.stringify({sceneBlocks:b.size,buttons,resolvedTargets:callbacks,validatedCallbacks:callbacks-unknownMethods.length,confirmationControllersReviewed:Object.keys(required).length,missingLocalReferences:0,unassignedWeeklySummaryCallbacks:unassigned,obsoleteDebugCallbacks:unknownMethods,result:unassigned.length||unknownMethods.length?'PASS_CONFIRMATIONS_WITH_UNRELATED_FINDINGS':'PASS'},null,2));
