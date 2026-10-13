using UnityEngine;
using Pomo.Shared.Enums;

namespace Pomo.Data.Models
{
    [CreateAssetMenu(fileName = "StoreItem", menuName = "POMO/Store/Item")]
    public class StoreItemDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Catálogo")]
        [SerializeField] private StoreItemCategory category;
        [Min(0)]
        [SerializeField] private int priceInCups;
        [SerializeField] private bool priceConfigured;
        [SerializeField] private bool availableInStore = true;
        [SerializeField] private Sprite previewImage;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public StoreItemCategory Category => category;
        public int PriceInCups => priceInCups;
        public bool PriceConfigured => priceConfigured;
        public bool AvailableInStore => availableInStore;
        public Sprite PreviewImage => previewImage;
    }
}
