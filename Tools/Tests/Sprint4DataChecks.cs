using System;
using System.IO;
using Pomo.Business.Pomodoro;
using Pomo.Business.Progress;
using Pomo.Business.Tasks;
using Pomo.Data.DTOs;
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

        CheckStorageRecovery(root, check);
        var awarded = new CompletedFocusBlockModel
        {
            sessionId = "con-premio", sessionDate = "2026-11-05", workedSeconds = 60
        };
        check(Progress().TryRecordFocusSession(awarded, out _, 20) &&
              !Progress().TryRecordFocusSession(awarded, out _, 20) &&
              Progress().GetCupsBalance() == 20, "Bloque y premio se guardan juntos una sola vez.");
    }

    private static void CheckStorageRecovery(string root, Action<bool, string> check)
    {
        JsonStorageService.ClearNotices();
        string path = Path.Combine(root, "tasks.json");
        var storage = new JsonStorageService(path);
        check(!storage.TryLoad<TaskDTO>(out _) && !JsonStorageService.TryTakeNotice(out _),
            "Una instalación sin datos se abre sin aviso de corrupción.");
        storage.Save(new TaskDTO { tasks = { new TaskModel { id = "primera", title = "Anterior" } } });
        storage.Save(new TaskDTO { tasks = { new TaskModel { id = "primera", title = "Actual" } } });
        string healthy = File.ReadAllText(path);
        using (var locked = new FileStream(path + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            var tasks = new TaskService(new TaskRepository(storage));
            check(tasks.CreateTask("No se guardó", "", "Materia", "23/11/2026") == null &&
                  tasks.GetStoredTasks().Count == 1 && File.ReadAllText(path) == healthy,
                "Un fallo de escritura conserva el archivo y no confirma una tarea nueva.");
        }
        check(JsonStorageService.TryTakeNotice(out string writeNotice) && writeNotice.Contains("No se guardaron"),
            "El guardado fallido produce un aviso.");
        File.WriteAllText(path, "{\"tasks\": corrupto}");
        check(storage.TryLoad(out TaskDTO recovered) && recovered.tasks[0].title == "Anterior" &&
              new JsonStorageService(path).TryLoad(out TaskDTO reopened) && reopened.tasks[0].title == "Anterior",
            "Se restaura el respaldo y una instancia nueva lee la versión recuperada.");
        check(JsonStorageService.TryTakeNotice(out string recoveredNotice) && recoveredNotice.Contains("Se recuperó"),
            "La recuperación produce un aviso distinto del fallo.");

        File.WriteAllText(path, "daño principal");
        File.WriteAllText(path + ".bak", "daño respaldo");
        check(!storage.TryLoad<TaskDTO>(out _) && JsonStorageService.TryTakeNotice(out string lostNotice) &&
              lostNotice.Contains("no se sobrescribirán"), "Dos copias dañadas producen un aviso de recuperación fallida.");
        bool rejected = false;
        try { storage.Save(new TaskDTO()); } catch (LocalDataWriteException) { rejected = true; }
        check(rejected && File.ReadAllText(path) == "daño principal" &&
              File.ReadAllText(path + ".bak") == "daño respaldo", "Un nuevo guardado conserva las copias dañadas.");
    }
}
