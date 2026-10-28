
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;

public class TaskReminderController : MonoBehaviour
{
    [Header("Panel de notificación")]
    [SerializeField] private PomoNotifications notificationUI;

    [Header("Configuración")]
    [SerializeField] private float intervaloRevision = 5f;
    [SerializeField] private float tiempoEspera = 5f;

    private TaskService taskService;

    // Evita mostrar varias veces la misma tarea
    private readonly HashSet<string> avisosMostrados =
        new HashSet<string>();

    // Evita iniciar varias esperas para una misma tarea
    private readonly HashSet<string> avisosPendientes =
        new HashSet<string>();

    private void Start()
    {
        taskService = new TaskService();

        StartCoroutine(RevisarTareas());
    }

    private IEnumerator RevisarTareas()
    {
        while (true)
        {
            ComprobarTareas();

            yield return new WaitForSecondsRealtime(
                intervaloRevision
            );
        }
    }

    private void ComprobarTareas()
    {
        if (notificationUI == null)
        {
            Debug.LogError(
                "POMO: No asignaste PomoNotifications."
            );
            return;
        }

        taskService.ReloadStoredTasks();

        IReadOnlyList<TaskModel> tareas =
            taskService.GetStoredTasks();

        DateTime hoy = DateTime.Today;

        foreach (TaskModel tarea in tareas)
        {
            if (tarea == null ||
                tarea.status == TaskStatus.Completed)
            {
                continue;
            }

            if (!DateUtils.TryParseStoredDate(
                tarea.dueDate,
                out DateTimeOffset fechaEntrega))
            {
                continue;
            }

            // Solo comparar la fecha, sin hora
            if (fechaEntrega.LocalDateTime.Date != hoy)
            {
                continue;
            }

            string clave = tarea.id + "_" +
                           hoy.ToString("yyyyMMdd");

            if (avisosMostrados.Contains(clave) ||
                avisosPendientes.Contains(clave))
            {
                continue;
            }

            avisosPendientes.Add(clave);

            Debug.Log(
                "POMO: Tarea detectada con fecha de hoy: " +
                tarea.title
            );

            StartCoroutine(
                EsperarYMostrarNotificacion(tarea.id, clave)
            );
        }
    }

    private IEnumerator EsperarYMostrarNotificacion(
        string taskId,
        string clave)   
    {
        Debug.Log(
            "POMO: Esperando 5 segundos para mostrar notificación."
        );

        yield return new WaitForSecondsRealtime(
            tiempoEspera
        );

        // Volver a consultar por si la tarea fue
        // eliminada, completada o cambió de fecha.
        taskService.ReloadStoredTasks();

        TaskModel tarea = taskService.GetTask(taskId);

        bool debeNotificar = false;

        if (tarea != null &&
            tarea.status != TaskStatus.Completed &&
            DateUtils.TryParseStoredDate(
                tarea.dueDate,
                out DateTimeOffset fechaEntrega))
        {
            debeNotificar =
                fechaEntrega.LocalDateTime.Date ==
                DateTime.Today;
        }

        avisosPendientes.Remove(clave);

        if (!debeNotificar)
        {
            Debug.Log(
                "POMO: Recordatorio cancelado porque " +
                "la tarea ya no cumple las condiciones."
            );
            yield break;
        }

        avisosMostrados.Add(clave);

        Debug.Log(
            "POMO: MOSTRANDO NOTIFICACIÓN DE " +
            tarea.title
        );

        notificationUI.MostrarRecordatorio();
    }
}
