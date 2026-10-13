using System.Collections.Generic;
using System.IO;
using Pomo.Business.Store;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.Shared.Constants;
using Pomo.Shared.Enums;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    public class StoreInventoryController : MonoBehaviour
    {
        [SerializeField] private StoreCatalog catalog;

        private StoreInventoryService inventoryService;
        private bool initializationAttempted;

        public IReadOnlyList<string> GetOwnedItemIds()
        {
            EnsureInitialized();
            return inventoryService.GetOwnedItemIds();
        }

        public bool IsOwned(string itemId)
        {
            EnsureInitialized();
            return inventoryService.IsOwned(itemId);
        }

        public StoreInventoryUpdateResult RegisterAcquiredItem(string itemId)
        {
            EnsureInitialized();
            return inventoryService.RegisterAcquiredItem(itemId);
        }

        private void Awake()
        {
            if (initializationAttempted)
            {
                return;
            }

            initializationAttempted = true;
            if (catalog == null)
            {
                Debug.LogError("StoreInventoryController necesita un StoreCatalog asignado.", this);
                return;
            }

            string inventoryPath = Path.Combine(Application.persistentDataPath, AppConstants.StoreInventoryFileName);
            StoreInventoryRepository repository = new StoreInventoryRepository(new JsonStorageService(inventoryPath));
            inventoryService = new StoreInventoryService(catalog, repository);
        }

        private void EnsureInitialized()
        {
            if (inventoryService == null)
            {
                Awake();
            }
        }
    }
}
