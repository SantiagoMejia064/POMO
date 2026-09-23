using UnityEngine;
using Pomo.Business.Tasks;
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
}