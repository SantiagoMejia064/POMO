using System.Collections.Generic;
using System.IO;
using Pomo.Business.Store;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.Shared.Constants;
using Pomo.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    public class StoreInventoryController : MonoBehaviour
    {
        [System.Serializable]
        private class OwnedItemVisual
        {
            [SerializeField] private string itemId;
            [SerializeField] private RawImage itemPreviewImage;

            public string ItemId => itemId;

            public void SetOwned(bool owned)
            {
                if (itemPreviewImage == null)
                {
                    return;
                }

                Color color = itemPreviewImage.color;
                color.a = owned ? 0.5f : 1f;
                itemPreviewImage.color = color;
            }
        }

        [SerializeField] private StoreCatalog catalog;
        [Header("Feedback de compra")]
        [SerializeField] private GameObject purchaseSuccessFeedback;
        [SerializeField] private GameObject insufficientCupsFeedback;
        [Header("Visual de artículos adquiridos")]
        [SerializeField] private List<OwnedItemVisual> ownedItemVisuals = new List<OwnedItemVisual>();

        private StoreInventoryService inventoryService;
        private StorePurchaseService purchaseService;
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

        public StorePurchaseResult Purchase(string itemId)
        {
            EnsureInitialized();
            StorePurchaseResult result = purchaseService.TryPurchase(itemId);

            if (result == StorePurchaseResult.Success)
            {
                RefreshOwnedItemVisuals();
                ShowPurchaseFeedback(purchaseSuccessFeedback);
            }
            else if (result == StorePurchaseResult.InsufficientCups)
            {
                ShowPurchaseFeedback(insufficientCupsFeedback);
            }

            return result;
        }

        // Unity Button events expose public void methods, so this is the Inspector entry point.
        public void PurchaseFromButton(string itemId)
        {
            Purchase(itemId);
        }

        // Debug-only Inspector entry point. It does not refund cups.
        public void ClearOwnedItemsForDebug()
        {
            EnsureInitialized();
            List<string> ownedItemIds = new List<string>(inventoryService.GetOwnedItemIds());

            foreach (string itemId in ownedItemIds)
            {
                inventoryService.RemoveAcquiredItem(itemId);
            }

            RefreshOwnedItemVisuals();
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
            purchaseService = new StorePurchaseService(catalog, inventoryService, new ProgressService());
            RefreshOwnedItemVisuals();
        }

        private void EnsureInitialized()
        {
            if (inventoryService == null)
            {
                Awake();
            }
        }

        private void ShowPurchaseFeedback(GameObject feedback)
        {
            if (purchaseSuccessFeedback != null)
            {
                purchaseSuccessFeedback.SetActive(false);
            }

            if (insufficientCupsFeedback != null)
            {
                insufficientCupsFeedback.SetActive(false);
            }

            if (feedback != null)
            {
                feedback.SetActive(true);
            }
        }

        private void RefreshOwnedItemVisuals()
        {
            if (inventoryService == null)
            {
                return;
            }

            foreach (OwnedItemVisual visual in ownedItemVisuals)
            {
                if (visual != null)
                {
                    visual.SetOwned(inventoryService.IsOwned(visual.ItemId));
                }
            }
        }
    }
}
