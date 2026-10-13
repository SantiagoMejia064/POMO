using System;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Shared.Enums;

namespace Pomo.Business.Store
{
    /// <summary>
    /// Coordinates a store purchase without making the UI responsible for business rules.
    /// </summary>
    public class StorePurchaseService
    {
        private readonly StoreCatalog catalog;
        private readonly StoreInventoryService inventoryService;
        private readonly ProgressService progressService;

        public StorePurchaseService(
            StoreCatalog catalog,
            StoreInventoryService inventoryService,
            ProgressService progressService)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            this.progressService = progressService ?? throw new ArgumentNullException(nameof(progressService));
        }

        public StorePurchaseResult TryPurchase(string itemId)
        {
            StoreItemDefinition item = catalog.FindById(itemId);
            if (item == null || !item.AvailableInStore)
            {
                return StorePurchaseResult.InvalidItem;
            }

            if (!item.PriceConfigured)
            {
                return StorePurchaseResult.PriceNotConfigured;
            }

            if (inventoryService.IsOwned(itemId))
            {
                return StorePurchaseResult.AlreadyOwned;
            }

            if (progressService.GetCupsBalance() < item.PriceInCups)
            {
                return StorePurchaseResult.InsufficientCups;
            }

            // Charge first; if inventory persistence rejects the item, immediately restore the exact charge.
            if (!progressService.TrySpendCups(item.PriceInCups))
            {
                return StorePurchaseResult.InsufficientCups;
            }

            if (inventoryService.RegisterAcquiredItem(itemId) == StoreInventoryUpdateResult.Success)
            {
                return StorePurchaseResult.Success;
            }

            progressService.AddCups(item.PriceInCups);
            return StorePurchaseResult.InventoryUpdateFailed;
        }
    }
}
