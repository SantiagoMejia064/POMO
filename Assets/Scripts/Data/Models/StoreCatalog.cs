using System.Collections.Generic;
using UnityEngine;

namespace Pomo.Data.Models
{
    [CreateAssetMenu(fileName = "StoreCatalog", menuName = "POMO/Store/Catalog")]
    public class StoreCatalog : ScriptableObject
    {
        [SerializeField] private List<StoreItemDefinition> items = new List<StoreItemDefinition>();

        public IReadOnlyList<StoreItemDefinition> Items => items;

        public StoreItemDefinition FindById(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            return items.Find(item => item != null && item.ItemId == itemId);
        }
    }
}
