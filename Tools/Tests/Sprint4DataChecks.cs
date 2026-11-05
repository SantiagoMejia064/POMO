using System;
using System.IO;
using Pomo.Business.Pomodoro;
using Pomo.Business.Progress;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.Shared.Enums;
using Unity.Notifications.Android;

internal static class Sprint4DataChecks
{
    public static void Run(Action<bool,string> check)
    {
        string root=Path.Combine("Logs","Sprint4","regression-test-data",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        JsonStorageService Storage(string file)=>new JsonStorageService(Path.Combine(root,file));
        TaskService Tasks()=>new TaskService(new TaskRepository(Storage("tasks.json")));
        AndroidNotificationCenter.Reset(PermissionStatus.Denied);
        var tasks=Tasks();
        var a=tasks.CreateTask("Prueba A","Descripción","Materia","05/11/2026");
        var other=tasks.CreateTask("Prueba B","Conservar","Materia","23/11/2026");
        string initial=File.ReadAllText(Path.Combine(root,"tasks.json"));
        check(tasks.UpdateTask(a.id,"","Cambio","Materia","05/11/2026")==null,"Una edición inválida no se guarda.");
        check(File.ReadAllText(Path.Combine(root,"tasks.json"))==initial,"Rechazar la edición conserva el JSON byte por byte.");
        tasks.GetTask(a.id); tasks.GetStoredTasks();
        check(File.ReadAllText(Path.Combine(root,"tasks.json"))==initial,"Consultar tareas no escribe cambios.");
        check(tasks.UpdateTask(a.id,"A guardada","Editada","Otra materia","11/05/2026")!=null,"Confirmar una edición guarda los campos válidos.");
        check(Tasks().GetTask(a.id).title=="A guardada"&&Tasks().GetTask(other.id).title=="Prueba B","La edición se recupera y conserva la otra tarea.");
        check(tasks.DeleteTask(a.id),"Confirmar elimina la tarea seleccionada.");
        check(!tasks.DeleteTask(a.id)&&Tasks().GetTask(other.id)!=null,"Repetir el borrado no elimina una tarea distinta.");

        PomodoroService Blocks()=>new PomodoroService(new WorkBlockRepository(Storage("work-blocks.json")));
        var blocks=Blocks();
        var block=blocks.CreateCustomBlock("Bloque de prueba",25,5,other.id);
        check(block!=null&&blocks.SetFavorite(block.id,true)&&blocks.SelectBlock(block.id),"Se guarda bloque, favorito y selección.");
        check(Blocks().GetSelectedBlock().id==block.id&&Blocks().IsFavorite(block.id),"Bloque y favorito sobreviven a crear un servicio nuevo.");

        ProgressRepository Progress()=>new ProgressRepository(Storage("progress.json"));
        var progress=Progress();
        var session=new CompletedFocusBlockModel {sessionId="sesión-prueba",sessionDate="2026-11-05",workedSeconds=1500,taskId=other.id,taskTitle=other.title};
        check(progress.TryRecordFocusSession(session,out _)&&!progress.TryRecordFocusSession(session,out _),"Una sesión se registra una sola vez.");
        check(Progress().GetCompletedStudySessions().Count==1,"El historial se recupera sin duplicados.");
        check(progress.AddCups(100)&&!progress.TrySpendCups(101)&&Progress().GetCupsBalance()==100,"Una operación rechazada conserva las copas guardadas.");

        StoreInventoryRepository Inventory()=>new StoreInventoryRepository(Storage("store-inventory.json"));
        check(Inventory().AddOwnedItem("artículo-prueba")&&Inventory().IsOwned("artículo-prueba"),"El inventario conserva los artículos entre instancias.");
        check(Inventory().RemoveOwnedItem("artículo-prueba")&&!Inventory().IsOwned("artículo-prueba"),"La eliminación de un artículo se recupera correctamente.");

        var reminderRepo=new RhythmReminderRepository(Storage("rhythm-reminders.json"));
        var reminder=new RhythmReminderModel {id="recordatorio-prueba",name="Estudiar",scheduledAt=DateTimeOffset.Now.AddDays(2).ToString("O"),notificationId=132435,notificationState="pendiente"};
        reminderRepo.Save(reminder);
        var reminders=new RhythmReminderService(reminderRepo);
        string before=File.ReadAllText(Path.Combine(root,"rhythm-reminders.json"));
        check(reminders.GetCalendarReminders().Count==1&&File.ReadAllText(Path.Combine(root,"rhythm-reminders.json"))==before,"Consultar recordatorios no los modifica.");
        reminders.Cancel(reminder.id);
        check(reminders.GetCalendarReminders().Count==0&&reminderRepo.Get(reminder.id).notificationState=="cancelada","Confirmar retira el recordatorio activo y persiste su cancelación.");
        string cancelled=File.ReadAllText(Path.Combine(root,"rhythm-reminders.json"));
        reminders.Cancel(reminder.id);
        check(File.ReadAllText(Path.Combine(root,"rhythm-reminders.json"))==cancelled,"Repetir la cancelación de recordatorio no vuelve a modificar datos.");

        new LocalDataResetService(root,()=>{},()=>{}).Erase(()=>{});
        check(Tasks().GetStoredTasks().Count==0&&Progress().GetRecordedStudySessions().Count==0,"Después del borrado no se recuperan tareas ni sesiones.");
        check(Progress().GetCupsBalance()==0&&Inventory().GetOwnedItemIds().Count==0,"Después del borrado no se recuperan copas ni compras.");
        var defaults=Blocks();
        check(defaults.GetAvailableBlocks().Count==3&&defaults.GetBlock(block.id)==null,
            "Se conservan los tres bloques predefinidos y no reaparece el bloque personal borrado.");
        foreach (var favorite in defaults.GetFavoriteBlocks())
            check(!favorite.isCustom&&string.IsNullOrEmpty(favorite.taskId),"Los favoritos predeterminados no contienen datos de tareas ni bloques personales.");
        check(new RhythmReminderService(new RhythmReminderRepository(Storage("rhythm-reminders.json"))).GetCalendarReminders().Count==0,"Después del borrado no reaparecen recordatorios.");
    }
}
