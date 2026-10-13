using Pomo.Business.Progress;
using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    /// <summary>Displays the persisted cups balance; the text itself owns no balance logic.</summary>
    [DisallowMultipleComponent]
    public class CupsTextController : MonoBehaviour
    {
        [SerializeField] private TMP_Text cupsText;
        [Header("Depuración")]
        [SerializeField, Min(1)] private int debugCupsAmount = 2000;

        private ProgressService progressService;

        private void Awake()
        {
            progressService = new ProgressService();
        }

        private void OnEnable()
        {
            ProgressService.CupsBalanceChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            ProgressService.CupsBalanceChanged -= Refresh;
        }

        public void Refresh()
        {
            if (progressService != null && cupsText != null)
            {
                cupsText.text = progressService.GetCupsBalance().ToString();
            }
        }

        // Unity Button entry point used only to speed up manual store tests.
        public void AddDebugCups()
        {
            progressService?.AddCups(debugCupsAmount);
        }

        private void Refresh(int balance)
        {
            if (cupsText != null)
            {
                cupsText.text = balance.ToString();
            }
        }
    }
}
