using System;
using System.Collections.Generic;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.Shared.Enums;

namespace Pomo.Business.Store
{
    public class StoreInventoryService
    {
        private readonly StoreCatalog catalog;
        private readonly StoreInventoryRepository inventoryRepository;

        public StoreInventoryService(StoreCatalog catalog, StoreInventoryRepository inventoryRepository)
        {
            this.catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            this.inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        }

        public IReadOnlyList<string> GetOwnedItemIds()
        {
            return inventoryRepository.GetOwnedItemIds();
        }

        public bool IsOwned(string itemId)
        {
            return inventoryRepository.IsOwned(itemId);
        }

        // Invoke only after the purchase flow has successfully charged the user.
        public StoreInventoryUpdateResult RegisterAcquiredItem(string itemId)
        {
            StoreItemDefinition item = catalog.FindById(itemId);
            if (item == null || !item.AvailableInStore)
            {
                return StoreInventoryUpdateResult.InvalidItem;
            }

            if (!item.PriceConfigured)
            {
                return StoreInventoryUpdateResult.PriceNotConfigured;
            }

            return inventoryRepository.AddOwnedItem(itemId)
                ? StoreInventoryUpdateResult.Success
                : StoreInventoryUpdateResult.AlreadyOwned;
        }

        public bool RemoveAcquiredItem(string itemId)
        {
            return inventoryRepository.RemoveOwnedItem(itemId);
        }
    }
}
