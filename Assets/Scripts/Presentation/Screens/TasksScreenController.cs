using TMPro;
using UnityEngine;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;

namespace Pomo.Presentation.Screens
{
    public class TasksScreenController : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField titleInput;
        public TMP_InputField descriptionInput;
        public TMP_InputField subjectInput;
        public TMP_InputField dueDateInput;

        [Header("Feedback")]
        public TMP_Text feedbackText;


        private TaskService taskService;


        private void Awake()
        {
            taskService = new TaskService();
        }


        public void CreateTask()
        {
            TaskValidationResult validationResult =
                taskService.ValidateTask(
                    titleInput.text,
                    subjectInput.text,
                    dueDateInput.text
                );


            if(validationResult == TaskValidationResult.Valid)
            {
                TaskModel task = taskService.CreateTask(
                    titleInput.text,
                    descriptionInput.text,
                    subjectInput.text,
                    dueDateInput.text
                );


                if(task != null)
                {
                    feedbackText.text = "Tarea creada correctamente";
                    titleInput.text = "";
                    descriptionInput.text = "";
                    subjectInput.text = "";
                    dueDateInput.text = "";

                    Debug.Log("Tarea creada: " + task.title);
                }
            }
            else
            {
                ShowValidationMessage(validationResult);
            }
        }

        private void ShowValidationMessage(TaskValidationResult result)
        {
            switch(result)
            {
                case TaskValidationResult.EmptyTitle:
                    feedbackText.text = "El nombre de la tarea es obligatorio.";
                    break;

                case TaskValidationResult.EmptySubject:
                    feedbackText.text = "La asignatura es obligatoria.";
                    break;

                case TaskValidationResult.EmptyDueDate:
                    feedbackText.text = "La fecha límite es obligatoria.";
                    break;

                case TaskValidationResult.InvalidDateFormat:
                    feedbackText.text = 
                    "La fecha debe tener formato DD/MM/AAAA.";
                    break;
            }
        }
    }
}