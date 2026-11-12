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

        [System.Serializable]
        private class CatalogPreviewBinding
        {
            [Tooltip("Boton de la tarjeta que el usuario toca en el catalogo.")]
            [SerializeField] private Button button;
            [Tooltip("Imagen completa de Pomo que se mostrara al tocar este boton.")]
            [SerializeField] private Sprite previewSprite;

            public Button Button => button;
            public Sprite PreviewSprite => previewSprite;
        }

        [System.Serializable]
        private class InventoryItemVisual
        {
            [Tooltip("Articulo que desbloquea esta imagen en el inventario.")]
            [SerializeField] private StoreItemDefinition item;
            [Tooltip("Objeto de la imagen o tarjeta dentro del panel Inventario.")]
            [SerializeField] private GameObject itemVisual;
            [Tooltip("Sprite individual que se muestra con un solo clic en este articulo.")]
            [SerializeField] private Sprite itemPreviewSprite;

            public void SetOwned(bool owned)
            {
                if (itemVisual != null)
                {
                    itemVisual.SetActive(owned);
                }
            }

            public bool HasItem => item != null;
            public string ItemId => item != null ? item.ItemId : string.Empty;
            public StoreItemDefinition Item => item;
            public Sprite PreviewSprite => itemPreviewSprite;

            public void ConfigureSelection(StoreInventoryController controller)
            {
                Button button = itemVisual != null ? itemVisual.GetComponent<Button>() : null;
                if (button != null && item != null)
                {
                    button.onClick.AddListener(() => controller.SelectInventoryItem(item, itemPreviewSprite));
                }
            }
        }

        [System.Serializable]
        private class InventoryCombination
        {
            [Tooltip("Prenda principal de la combinacion. Dejela vacia si la imagen solo representa un accesorio.")]
            [SerializeField] private StoreItemDefinition clothingItem;
            [Tooltip("Accesorio de la combinacion. Dejelo vacio para la prenda sin accesorio.")]
            [SerializeField] private StoreItemDefinition accessoryItem;
            [Tooltip("Imagen de Pomo usando esta combinacion.")]
            [SerializeField] private Sprite previewSprite;

            public bool Matches(StoreItemDefinition clothing, StoreItemDefinition accessory)
            {
                return clothingItem == clothing && accessoryItem == accessory && previewSprite != null;
            }

            public Sprite PreviewSprite => previewSprite;
        }

        [SerializeField] private StoreCatalog catalog;
        [Header("Feedback de compra")]
        [SerializeField] private GameObject purchaseSuccessFeedback;
        [SerializeField] private GameObject insufficientCupsFeedback;
        [Header("Visual de artículos adquiridos")]
        [SerializeField] private List<OwnedItemVisual> ownedItemVisuals = new List<OwnedItemVisual>();
        [Header("Vista previa del catalogo")]
        [Tooltip("El componente Image dentro de Preview 1.")]
        [SerializeField] private Image catalogPreviewImage;
        [Tooltip("En cada fila asigna el boton de una tarjeta y su imagen de Pomo correspondiente.")]
        [SerializeField] private List<CatalogPreviewBinding> catalogPreviewButtons = new List<CatalogPreviewBinding>();
        [Tooltip("El boton Comprar dentro de Preview 1.")]
        [SerializeField] private Button previewPurchaseButton;
        [Header("Articulos del inventario")]
        [Tooltip("Al iniciar, las imagenes no adquiridas se desactivan. Se activan al comprar su articulo.")]
        [SerializeField] private List<InventoryItemVisual> inventoryItemVisuals = new List<InventoryItemVisual>();
        [Tooltip("El componente Image de Inventario > Preview 1 que muestra la combinacion elegida.")]
        [SerializeField] private Image inventoryCombinationPreviewImage;
        [Tooltip("Imagenes de Pomo para cada prenda o combinacion disponible.")]
        [SerializeField] private List<InventoryCombination> inventoryCombinations = new List<InventoryCombination>();
        [Tooltip("Imagen de Pomo que muestra el atuendo equipado en Inicio.")]
        [SerializeField] private Image homePomoImage;

        private StoreInventoryService inventoryService;
        private StorePurchaseService purchaseService;
        private Sprite defaultInventorySprite;
        private Sprite defaultHomeSprite;
        private bool initializationAttempted;
        private string selectedPreviewItemId;
        private StoreItemDefinition selectedInventoryClothing;
        private StoreItemDefinition selectedInventoryAccessory;
        private StoreItemDefinition lastClickedInventoryItem;
        private float lastInventoryClickTime = -10f;
        private const float DoubleClickInterval = 0.4f;

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
                RefreshInventoryItemVisuals();
                ShowPurchaseFeedback(purchaseSuccessFeedback);
            }
            else if (result == StorePurchaseResult.InsufficientCups)
            {
                ShowPurchaseFeedback(insufficientCupsFeedback);
            }

            return result;
        }

        // Called by the existing catalog cards. It only remembers the item being previewed.
        public void PurchaseFromButton(string itemId)
        {
            selectedPreviewItemId = itemId;
        }

        /// <summary>
        /// Purchases the article that is currently shown in Preview 1.
        /// This is intended for the Comprar button inside that panel.
        /// </summary>
        public void PurchaseSelectedPreviewItem()
        {
            if (string.IsNullOrWhiteSpace(selectedPreviewItemId))
            {
                return;
            }

            Purchase(selectedPreviewItemId);
        }

        /// <summary>
        /// One click equips the item. A second quick click equips the combination
        /// with the other selected category when a combination sprite exists.
        /// </summary>
        private void SelectInventoryItem(StoreItemDefinition item, Sprite itemPreviewSprite)
        {
            EnsureInitialized();
            if (item == null || !inventoryService.IsOwned(item.ItemId))
            {
                return;
            }

            bool isDoubleClick = lastClickedInventoryItem == item &&
                Time.unscaledTime - lastInventoryClickTime <= DoubleClickInterval;

            lastClickedInventoryItem = item;
            lastInventoryClickTime = Time.unscaledTime;

            if (item.Category == StoreItemCategory.Accessory)
            {
                selectedInventoryAccessory = item;
            }
            else
            {
                selectedInventoryClothing = item;
            }

            if (isDoubleClick)
            {
                Sprite combination = FindCombinationSprite(selectedInventoryClothing, selectedInventoryAccessory);
                EquipOutfit(combination, selectedInventoryClothing, selectedInventoryAccessory);
            }
            else
            {
                EquipOutfit(itemPreviewSprite,
                    item.Category == StoreItemCategory.Accessory ? null : item,
                    item.Category == StoreItemCategory.Accessory ? item : null);
            }
        }

        private void ConfigureCatalogPreviewButtons()
        {
            ConfigurePreviewPurchaseButton();

            if (catalogPreviewImage == null)
            {
                return;
            }

            foreach (CatalogPreviewBinding binding in catalogPreviewButtons)
            {
                if (binding == null || binding.Button == null || binding.PreviewSprite == null)
                {
                    continue;
                }

                Sprite previewSprite = binding.PreviewSprite;
                binding.Button.onClick.AddListener(() => ShowCatalogPreview(previewSprite));
            }

        }

        private void ConfigurePreviewPurchaseButton()
        {
            if (previewPurchaseButton == null)
            {
                foreach (Button button in GetComponentsInChildren<Button>(true))
                {
                    if (button.name.ToLowerInvariant().Contains("comprar"))
                    {
                        previewPurchaseButton = button;
                        break;
                    }
                }
            }

            if (previewPurchaseButton != null)
            {
                previewPurchaseButton.onClick.AddListener(PurchaseSelectedPreviewItem);
            }
        }

        private void ShowCatalogPreview(Sprite previewSprite)
        {
            if (catalogPreviewImage == null || previewSprite == null)
            {
                return;
            }

            catalogPreviewImage.sprite = previewSprite;
            catalogPreviewImage.enabled = true;
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
            RefreshInventoryItemVisuals();
            RestoreEquippedOutfit();
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
            defaultInventorySprite = inventoryCombinationPreviewImage != null ? inventoryCombinationPreviewImage.sprite : null;
            defaultHomeSprite = homePomoImage != null ? homePomoImage.sprite : null;
            RestoreEquippedOutfit();
            RefreshOwnedItemVisuals();
            RefreshInventoryItemVisuals();
            ConfigureInventoryItemButtons();
            ConfigureCatalogPreviewButtons();
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

        private void RefreshInventoryItemVisuals()
        {
            if (inventoryService == null)
            {
                return;
            }

            foreach (InventoryItemVisual visual in inventoryItemVisuals)
            {
                if (visual != null && visual.HasItem)
                {
                    visual.SetOwned(inventoryService.IsOwned(visual.ItemId));
                }
            }
        }

        private void ConfigureInventoryItemButtons()
        {
            foreach (InventoryItemVisual visual in inventoryItemVisuals)
            {
                if (visual != null && visual.HasItem)
                {
                    visual.ConfigureSelection(this);
                }
            }
        }

        private Sprite FindCombinationSprite(StoreItemDefinition clothing, StoreItemDefinition accessory)
        {
            foreach (InventoryCombination combination in inventoryCombinations)
            {
                if (combination != null && combination.Matches(clothing, accessory))
                {
                    return combination.PreviewSprite;
                }
            }

            return null;
        }

        private void EquipOutfit(Sprite sprite, StoreItemDefinition clothing, StoreItemDefinition accessory)
        {
            if (sprite == null || (inventoryCombinationPreviewImage == null && homePomoImage == null))
            {
                return;
            }

            try
            {
                if (inventoryService.TryEquipItems(clothing?.ItemId, accessory?.ItemId))
                {
                    ShowEquippedOutfit(sprite);
                }
            }
            catch (LocalDataWriteException)
            {
                // The storage service reports the failure and keeps the previous outfit.
            }
        }

        private void RestoreEquippedOutfit()
        {
            if (inventoryCombinationPreviewImage == null && homePomoImage == null)
            {
                return;
            }

            var (clothingId, accessoryId) = inventoryService.GetEquippedItems();
            InventoryItemVisual clothing = FindOwnedInventoryVisual(clothingId);
            InventoryItemVisual accessory = FindOwnedInventoryVisual(accessoryId);
            selectedInventoryClothing = clothing?.Item;
            selectedInventoryAccessory = accessory?.Item;

            Sprite sprite = clothing != null && accessory != null
                ? FindCombinationSprite(clothing.Item, accessory.Item)
                : clothing?.PreviewSprite ?? accessory?.PreviewSprite;
            ShowEquippedOutfit(sprite);
        }

        private InventoryItemVisual FindOwnedInventoryVisual(string itemId)
        {
            if (!inventoryService.IsOwned(itemId))
            {
                return null;
            }

            return inventoryItemVisuals.Find(visual => visual != null && visual.ItemId == itemId);
        }

        private void ShowEquippedOutfit(Sprite sprite)
        {
            if (inventoryCombinationPreviewImage != null)
            {
                inventoryCombinationPreviewImage.sprite = sprite != null ? sprite : defaultInventorySprite;
                if (sprite != null)
                {
                    inventoryCombinationPreviewImage.enabled = true;
                }
            }

            if (homePomoImage != null)
            {
                homePomoImage.sprite = sprite != null ? sprite : defaultHomeSprite;
                homePomoImage.preserveAspect = sprite != null;
            }
        }
    }
}
