using System;
using System.Collections.Generic;
using System.IO;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.ServicesAdapters.Notifications;
using Unity.Notifications.Android;
using UnityEngine;

internal static class LocalDataResetChecks
{
    public static void Run(Action<bool, string> check)
    {
        string directory = Path.Combine("Logs", "Sprint4", "reset-test-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var owned = new List<string>();
        foreach (string name in new[] { "tasks.json", "work-blocks.json", "progress.json", "store-inventory.json", "rhythm-reminders.json" })
            foreach (string suffix in new[] { "", ".tmp" })
            {
                string path = Path.Combine(directory, name + suffix);
                File.WriteAllText(path, "datos de prueba");
                owned.Add(path);
            }
        string unrelated = Path.Combine(directory, "otro-archivo.json");
        File.WriteAllText(unrelated, "conservar");
        var order = new List<string>();
        var service = new LocalDataResetService(directory, () => order.Add("avisos"), () => order.Add("preferencias"));
        check(owned.TrueForAll(File.Exists) && order.Count == 0, "Preparar el borrado no cambia datos ni preferencias.");
        // A rejected confirmation never invokes Erase; constructing/opening the service has no effects.
        var failure = new LocalDataResetService(directory, () => throw new IOException("fallo de avisos"), () => order.Add("incorrecto"));
        bool failed = false;
        try { failure.Erase(() => order.Add("incorrecto")); } catch (IOException) { failed = true; }
        check(failed && owned.TrueForAll(File.Exists) && order.Count == 0, "Un fallo al cancelar avisos conserva los archivos y no inicia el borrado.");
        using (var locked = new FileStream(owned[4], FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            failed = false;
            try { service.Erase(() => order.Add("incorrecto")); } catch (IOException) { failed = true; }
            check(failed && owned.TrueForAll(File.Exists) && order.Count == 0, "Un archivo bloqueado se detecta antes de cancelar avisos o borrar datos.");
        }
        foreach (string key in LocalDataResetService.PreferenceKeys)
            if (key == LocalUserProfile.NameKey) LocalUserProfile.TrySaveName("Santiago");
            else PlayerPrefs.SetInt(key, 1);
        const string osKey = "com.unity.androidnotifications.PostNotificationsPermission";
        PlayerPrefs.SetInt(osKey, 1);
        service = new LocalDataResetService(directory, () => order.Add("avisos"), () => {
            foreach (string key in LocalDataResetService.PreferenceKeys) PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save(); order.Add("preferencias");
        });
        service.Erase(() => order.Add("sesión"));
        check(string.Join(",", order) == "avisos,sesión,preferencias", "Se cancelan avisos y detiene la sesión antes de finalizar el borrado.");
        foreach (string path in owned) check(!File.Exists(path), "Se elimina " + Path.GetFileName(path));
        foreach (string key in LocalDataResetService.PreferenceKeys) check(!PlayerPrefs.HasKey(key), "Se elimina la preferencia propia " + key);
        check(PlayerPrefs.GetInt(osKey) == 1, "El borrado no altera la autorización del sistema operativo.");
        check(File.ReadAllText(unrelated) == "conservar", "No se eliminan archivos ajenos a POMO.");
        check(new TaskRepository(new JsonStorageService(Path.Combine(directory,"tasks.json"))).GetAll().Count == 0,
            "Un repositorio nuevo no recupera las tareas borradas.");
        service.Erase(() => { });
        check(owned.TrueForAll(path => !File.Exists(path)), "Reintentar sobre datos vacíos es seguro y no recupera información.");
        AndroidNotificationCenter.Reset(PermissionStatus.Denied);
        new NotificationService().CancelAllForDataReset();
        check(AndroidNotificationCenter.CancelAllCalls == 1 && AndroidNotificationCenter.Requests == 0,
            "El borrado cancela avisos sin pedir permisos.");
        AndroidNotificationCenter.ThrowCancel = true;
        failed = false;
        try { new NotificationService().CancelAllForDataReset(); } catch (Exception) { failed = true; }
        check(failed, "Un error nativo de cancelación llega al flujo de borrado.");
        AndroidNotificationCenter.Reset(PermissionStatus.Denied);
    }
}
