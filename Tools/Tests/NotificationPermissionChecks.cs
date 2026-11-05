// Standalone regression checks using the real POMO service/repository source and
// an in-memory Android double. These do not request device permissions or touch user data.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.ServicesAdapters.Notifications;
using Unity.Notifications.Android;

internal static class NotificationPermissionChecks
{
    private static int checks;
    private static string directory;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Reset(PermissionStatus status)
    {
        AndroidNotificationCenter.Reset(status);
        UnityEngine.PlayerPrefs.Clear();
        typeof(NotificationService).GetField("permissionRequest", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
    }
    private static TaskService Service(string file) => new TaskService(new TaskRepository(new JsonStorageService(Path.Combine(directory, file))));
    private static TaskModel Create(TaskService service) => service.CreateTask("Tarea de prueba", "Descripción", "POMO", DateTime.Now.AddDays(2).ToString("dd/MM/yyyy"));
    public static int Main()
    {
        directory = Path.Combine("Logs", "Sprint4", "permission-test-data", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var adapter = new NotificationService();

        Reset(PermissionStatus.NotRequested);
        var service = Service("sin-permiso.json");
        var task = Create(service);
        Check(task != null && Service("sin-permiso.json").GetTask(task.id) != null, "Crear conserva la tarea sin permiso.");
        Check(AndroidNotificationCenter.Requests == 0 && AndroidNotificationCenter.Sends == 0, "Crear no abre permisos ni envía sin autorización.");
        adapter.NotifyBlockStarted("Tarea"); adapter.NotifyBreakStarted("Tarea");
        adapter.NotifyDailyGoalsAlmostCompleted(); adapter.NotifyAfterSecondTaskDeletedToday(); adapter.NotifyAfterSecondTaskDeletedToday();
        Check(AndroidNotificationCenter.Requests == 0 && AndroidNotificationCenter.Sends == 0, "Los eventos secundarios no solicitan permisos.");

        adapter.RequestNotificationPermissionFromUser();
        Check(AndroidNotificationCenter.Requests == 1 && adapter.RhythmPermissionPending, "La petición explícita tiene estado pendiente real.");
        adapter.RequestNotificationPermissionFromUser();
        Check(AndroidNotificationCenter.Requests == 1, "No se duplica una solicitud pendiente.");
        PermissionRequest.Last.Complete(PermissionStatus.Allowed);
        Check(adapter.PermissionState == NotificationAccessState.Allowed, "Se reconoce la autorización.");
        Check(service.TryScheduleReminderForTask(task.id), "Se programa después de autorizar.");
        Check(Service("sin-permiso.json").GetTask(task.id).reminderNotificationId > 0, "Se persiste el ID del aviso autorizado.");
        int sends = AndroidNotificationCenter.Sends;
        Check(service.TryScheduleReminderForTask(task.id) && AndroidNotificationCenter.Sends == sends, "Reintentar no duplica un aviso existente.");

        Reset(PermissionStatus.Denied);
        service = Service("rechazo.json"); task = Create(service);
        Check(task != null, "Rechazar no bloquea crear.");
        Check(service.UpdateTask(task.id, "Editada", "Cambio", "POMO", DateTime.Now.AddDays(3).ToString("dd/MM/yyyy")) != null, "Rechazar no bloquea editar.");
        Check(Service("rechazo.json").GetTask(task.id).title == "Editada", "La edición se conserva en JSON.");
        Check(service.DeleteTask(task.id) && Service("rechazo.json").GetTask(task.id) == null, "Rechazar no bloquea eliminar.");
        adapter.RequestNotificationPermissionFromUser();
        Check(AndroidNotificationCenter.Requests == 0 && AndroidNotificationCenter.Sends == 0, "No se vuelve a pedir un permiso rechazado.");

        Reset(PermissionStatus.Allowed);
        AndroidNotificationCenter.ThrowSend = true;
        service = Service("error-nativo.json"); task = Create(service);
        Check(task != null && Service("error-nativo.json").GetTask(task.id) != null, "Un fallo nativo al enviar no pierde la tarea.");
        AndroidNotificationCenter.ThrowSend = false;
        Check(service.TryScheduleReminderForTask(task.id), "Puede reintentarse después de un error nativo.");
        AndroidNotificationCenter.ThrowCancel = true;
        Check(service.UpdateTask(task.id, "Editada con error Android", "", "POMO", DateTime.Now.AddDays(3).ToString("dd/MM/yyyy")) != null, "Un fallo al cancelar no bloquea editar.");
        Check(service.DeleteTask(task.id), "Un fallo al cancelar no bloquea eliminar.");

        Reset(PermissionStatus.Allowed);
        var reminder = new RhythmReminderModel { id = "ritmo", name = "Estudiar", scheduledAt = DateTimeOffset.Now.AddHours(2).ToString("O"), notificationId = 123456 };
        Check(adapter.ScheduleRhythmReminder(reminder, out _) == RhythmNotificationResult.Failed, "Sin alarmas exactas no se afirma programación.");
        Check(AndroidNotificationCenter.ExactRequests == 0, "Programar no abre ajustes Android sin una decisión explícita.");
        adapter.RequestExactSchedulingFromUser();
        Check(AndroidNotificationCenter.ExactRequests == 1, "El acceso exacto se solicita explícitamente.");
        AndroidNotificationCenter.UsingExactScheduling = true;
        Check(adapter.ScheduleRhythmReminder(reminder, out _) == RhythmNotificationResult.Scheduled, "Ritmo se programa con accesos concedidos.");
        sends = AndroidNotificationCenter.Sends;
        Check(adapter.ScheduleRhythmReminder(reminder, out _) == RhythmNotificationResult.Scheduled && AndroidNotificationCenter.Sends == sends, "Ritmo conserva su protección contra duplicados.");

        Reset(PermissionStatus.NotificationsBlockedForApp);
        Check(adapter.PermissionState == NotificationAccessState.Denied, "Bloqueo de Android se trata como rechazo.");
        Check(adapter.ScheduleRhythmReminder(reminder, out _) == RhythmNotificationResult.Failed && AndroidNotificationCenter.Requests == 0, "Ritmo no fuerza permisos rechazados.");
        Console.WriteLine("PASS: " + checks + " comprobaciones; Android simulado; archivos de prueba aislados en " + directory);
        return 0;
    }
}

namespace UnityEngine
{
    public static class Debug { public static void Log(object value) { } public static void LogWarning(object value) { } }
    public static class Application { public static string persistentDataPath => Path.GetFullPath("Logs/Sprint4/permission-test-data"); }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value, bool pretty) => JsonSerializer.Serialize(value, value.GetType(), Options);
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, object> Values = new Dictionary<string, object>();
        public static void Clear() => Values.Clear();
        public static int GetInt(string key, int fallback = 0) => Values.TryGetValue(key, out var value) ? (int)value : fallback;
        public static string GetString(string key, string fallback = "") => Values.TryGetValue(key, out var value) ? (string)value : fallback;
        public static void SetInt(string key, int value) => Values[key] = value;
        public static void SetString(string key, string value) => Values[key] = value;
        public static void Save() { }
    }
}
namespace Pomo.Business.Pomo { public static class PomoReactionService { public static void NotifyTaskCompleted(string id, string title, string date) { } } }
namespace Unity.Notifications.Android
{
    public enum PermissionStatus { NotRequested, RequestPending, Allowed, Denied, NotificationsBlockedForApp }
    public enum NotificationStatus { Unknown, Scheduled, Delivered }
    public enum Importance { High }
    public class PermissionRequest
    {
        public static PermissionRequest Last;
        public PermissionStatus Status { get; set; }
        public PermissionRequest() { AndroidNotificationCenter.Requests++; Status = PermissionStatus.RequestPending; Last = this; }
        public void Complete(PermissionStatus status) { Status = status; AndroidNotificationCenter.UserPermissionToPost = status; }
    }
    public struct AndroidNotificationChannel { public string Id, Name, Description; public Importance Importance; public bool CanShowBadge, EnableVibration; }
    public struct AndroidNotification { public string Title, Text, SmallIcon, LargeIcon; public DateTime FireTime; public bool ShouldAutoCancel; }
    public static class AndroidNotificationCenter
    {
        public static PermissionStatus UserPermissionToPost;
        public static bool UsingExactScheduling, ThrowSend, ThrowCancel;
        public static int Requests, Sends, ExactRequests;
        private static readonly HashSet<int> Scheduled = new HashSet<int>();
        public static void Reset(PermissionStatus status) { UserPermissionToPost = status; UsingExactScheduling = ThrowSend = ThrowCancel = false; Requests = Sends = ExactRequests = 0; Scheduled.Clear(); }
        public static void RegisterNotificationChannel(AndroidNotificationChannel channel) { }
        public static int SendNotification(AndroidNotification notification, string channel) { if (ThrowSend) throw new Exception("Android send failure"); int id = ++Sends; Scheduled.Add(id); return id; }
        public static void SendNotificationWithExplicitID(AndroidNotification notification, string channel, int id) { Sends++; Scheduled.Add(id); }
        public static NotificationStatus CheckScheduledNotificationStatus(int id) => Scheduled.Contains(id) ? NotificationStatus.Scheduled : NotificationStatus.Unknown;
        public static void CancelScheduledNotification(int id) { if (ThrowCancel) throw new Exception("Android cancel failure"); Scheduled.Remove(id); }
        public static void CancelDisplayedNotification(int id) { if (ThrowCancel) throw new Exception("Android cancel failure"); }
        public static void RequestExactScheduling() => ExactRequests++;
    }
}
