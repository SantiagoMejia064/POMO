using System;
using System.Globalization;
using Pomo.Business.Tasks;
using Pomo.Shared.Utilities;

internal static class TaskDateChecks
{
    public static void Run(Action<bool, string> check, Func<string, TaskService> service)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (string culture in new[] { "es-CO", "en-US" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (string uiDate in new[] { "05/11/2026", "11/05/2026", "23/11/2026", "29/02/2028" })
                {
                    check(DateUtils.TryParseUiDate(uiDate, out DateTimeOffset selected), "La fecha seleccionada es válida: " + uiDate);
                    check(DateUtils.FormatForUi(uiDate) == uiDate, "El formulario conserva día/mes: " + uiDate + " / " + culture);
                    check(DateUtils.TryParseStoredDate(uiDate, out DateTimeOffset legacy) && legacy.Date == selected.Date,
                        "Una fecha antigua día/mes conserva el día seleccionado.");
                    string file = "fecha-" + culture + "-" + uiDate.Replace('/', '-') + ".json";
                    var writer = service(file);
                    var task = writer.CreateTask("Fecha desde calendario", "", "POMO", DateUtils.FormatForUi(uiDate));
                    check(task != null, "Se crea la tarea con la fecha elegida.");
                    var persisted = service(file).GetTask(task.id);
                    check(DateUtils.TryParseStoredDate(persisted.dueDate, out DateTimeOffset stored) && stored.Date == selected.Date,
                        "Al recuperar el JSON se conserva el día y mes seleccionados.");
                    check(DateUtils.FormatForUi(persisted.dueDate) == uiDate, "El detalle muestra la misma fecha tras recargar.");
                    check(DateUtils.TryNormalizeToIso(uiDate, out string iso) && DateUtils.FormatForUi(iso) == uiDate,
                        "Normalizar una fecha antigua a ISO no intercambia día y mes.");
                }
            }
            check(!DateUtils.TryParseUiDate("31/02/2026", out _), "Se rechaza un día inexistente.");
            check(!DateUtils.TryParseUiDate("11/23/2026", out _), "El formulario no acepta mes/día.");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
