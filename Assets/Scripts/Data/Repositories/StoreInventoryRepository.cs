using System;
using System.Collections.Generic;
using Pomo.Data.DTOs;
using Pomo.Data.Persistence;

namespace Pomo.Data.Repositories
{
    public class StoreInventoryRepository
    {
        private readonly JsonStorageService storageService;
        private StoreInventoryDTO inventory;

        public StoreInventoryRepository(JsonStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            Reload();
        }

        public IReadOnlyList<string> GetOwnedItemIds()
        {
            return inventory.ownedItemIds.AsReadOnly();
        }

        public bool IsOwned(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) && inventory.ownedItemIds.Contains(itemId);
        }

        public bool AddOwnedItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || inventory.ownedItemIds.Contains(itemId))
            {
                return false;
            }

            inventory.ownedItemIds.Add(itemId);
            SaveChanges();
            return true;
        }

        public bool RemoveOwnedItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !inventory.ownedItemIds.Remove(itemId))
            {
                return false;
            }

            SaveChanges();
            return true;
        }

        public void Reload()
        {
            if (!storageService.TryLoad(out inventory) || inventory == null)
            {
                inventory = new StoreInventoryDTO();
            }

            inventory.ownedItemIds ??= new List<string>();
            inventory.ownedItemIds.RemoveAll(string.IsNullOrWhiteSpace);

            for (int index = inventory.ownedItemIds.Count - 1; index >= 0; index--)
            {
                if (inventory.ownedItemIds.IndexOf(inventory.ownedItemIds[index]) != index)
                {
                    inventory.ownedItemIds.RemoveAt(index);
                }
            }
        }

        private void SaveChanges()
        {
            storageService.Save(inventory);
        }
    }
}
