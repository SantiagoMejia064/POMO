using System;
using System.IO;
using UnityEngine;
using Pomo.Business.Tasks;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.Data.Models;
using Pomo.Shared.Enums;

public class TaskServiceTest : MonoBehaviour
{
    [Header("Datos de prueba de la tarea")]
    public string taskTitle;
    public string taskDescription;
    public string taskSubject;
    public string taskDueDate;


    private void Start()
    {
        TestCreateTask();
    }


    public void TestCreateTask()
    {
        TaskService taskService = new TaskService();

        TaskValidationResult validationResult = taskService.ValidateTask(taskTitle, taskSubject, taskDueDate);

        Debug.Log("Resultado de validación: " + validationResult);

        if(validationResult == TaskValidationResult.Valid)
        {
            TaskModel task = taskService.CreateTask(taskTitle, taskDescription, taskSubject, taskDueDate);

            if(task != null)
            {
                Debug.Log("----- TAREA CREADA -----");
                Debug.Log("ID: " + task.id);
                Debug.Log("Título: " + task.title);
                Debug.Log("Descripción: " + task.description);
                Debug.Log("Asignatura: " + task.subject);
                Debug.Log("Fecha límite: " + task.dueDate);
                Debug.Log("Estado: " + task.status);
            }
            else
            {
                Debug.LogError("La tarea no pudo crearse");
            }
        }
        else
        {
            Debug.LogWarning("No se pudo crear la tarea. Motivo: " + validationResult);
        }
    }

    [ContextMenu("POMO-153/Validar transiciones de estado")]
    public void ValidateStatusTransitions()
    {
        string testFilePath = Path.Combine(
            Application.temporaryCachePath,
            "pomo-task-status-transition-test.json");

        try
        {
            TaskRepository repository = new TaskRepository(new JsonStorageService(testFilePath));
            TaskService service = new TaskService(repository);
            TaskModel task = service.CreateTask(
                "Prueba de estados",
                "Tarea temporal para validar POMO-153.",
                "POMO",
                "30/12/2030");

            if (task == null)
            {
                Debug.LogError("POMO-153: no fue posible crear la tarea temporal.");
                return;
            }

            LogTransitionResult(
                "Completar sin iniciar",
                service.TransitionStatus(task.id, TaskStatus.Completed),
                TaskStatusTransitionResult.InvalidTransition);

            LogTransitionResult(
                "Iniciar tarea",
                service.TransitionStatus(task.id, TaskStatus.InProgress),
                TaskStatusTransitionResult.Success);

            LogTransitionResult(
                "Iniciar una tarea ya iniciada",
                service.TransitionStatus(task.id, TaskStatus.InProgress),
                TaskStatusTransitionResult.StatusUnchanged);

            LogTransitionResult(
                "Completar tarea",
                service.TransitionStatus(task.id, TaskStatus.Completed),
                TaskStatusTransitionResult.Success);

            LogTransitionResult(
                "Reabrir una tarea completada",
                service.TransitionStatus(task.id, TaskStatus.InProgress),
                TaskStatusTransitionResult.InvalidTransition);

            TaskModel persistedTask = new TaskRepository(new JsonStorageService(testFilePath)).GetById(task.id);
            bool persisted = persistedTask != null && persistedTask.status == TaskStatus.Completed;
            Debug.Log(persisted
                ? "POMO-153: persistencia de estado correcta."
                : "POMO-153: la persistencia de estado falló.");
        }
        finally
        {
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    private void LogTransitionResult(
        string action,
        TaskStatusTransitionResult actualResult,
        TaskStatusTransitionResult expectedResult)
    {
        string message = $"POMO-153 — {action}: {actualResult}";

        if (actualResult == expectedResult)
        {
            Debug.Log(message);
        }
        else
        {
            Debug.LogError($"{message}. Se esperaba: {expectedResult}.");
        }
    }
}
