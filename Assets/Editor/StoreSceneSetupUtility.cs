#if UNITY_EDITOR
using System;
using System.IO;
using Pomo.Data.Models;
using Pomo.Presentation.Screens;
using Pomo.Shared.Enums;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    internal static class StoreSceneSetupUtility
    {
        private const string TargetScenePath = "Assets/Scenes/Steven 1.unity";
        private const string StoreSectionName = "Sección tienda";
        private const string CatalogAssetPath = "Assets/Data/Store/StoreCatalog.asset";

        [MenuItem("POMO/Tienda/Preparar tienda de Steven 1")]
        public static void PrepareStoreInStevenOne()
        {
            try
            {
                StoreCatalog catalog = CreateOrLoadStoreCatalog();
                ConfigureStoreSectionInStevenOne(catalog);
                Debug.Log("Tienda configurada en Steven 1. La escena valen.unity no fue modificada.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static StoreCatalog CreateOrLoadStoreCatalog()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder("Assets/Data/Store");
            EnsureFolder("Assets/Data/Store/Items");
            AssetDatabase.Refresh();

            StoreItemDefinition[] definitions =
            {
                CreateOrLoadItem(
                    "clothing-gothic-celestial-dress",
                    "Vestido gótico celestial negro y dorado",
                    StoreItemCategory.Clothing,
                    "Assets/Images/Tienda/Vestido gótico celestial negro y dorado.png"),
                CreateOrLoadItem(
                    "clothing-cali-football-shorts",
                    "Shorts de fútbol blancos y verdes con escudo CALI",
                    StoreItemCategory.Clothing,
                    "Assets/Images/Tienda/Shorts de fútbol blancos y verdes con escudo CALI.png"),
                CreateOrLoadItem(
                    "accessory-white-sunglasses",
                    "Gafas blancas de sol",
                    StoreItemCategory.Accessory,
                    "Assets/Images/Tienda/Gafas blancas de sol sobre fondo transparente.png"),
                CreateOrLoadItem(
                    "clothing-pleated-cream-skirt",
                    "Falda plisada crema con volantes",
                    StoreItemCategory.Clothing,
                    "Assets/Images/Tienda/Falda plisada crema con volantes.png"),
                CreateOrLoadItem(
                    "clothing-black-biker-jacket",
                    "Chaqueta biker negra con detalles plateados",
                    StoreItemCategory.Clothing,
                    "Assets/Images/Tienda/Chaqueta biker negra con detalles plateados.png")
            };

            StoreCatalog catalog = AssetDatabase.LoadAssetAtPath<StoreCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoreCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty items = serializedCatalog.FindProperty("items");
            foreach (StoreItemDefinition definition in definitions)
            {
                bool alreadyIncluded = false;
                for (int index = 0; index < items.arraySize; index++)
                {
                    if (items.GetArrayElementAtIndex(index).objectReferenceValue == definition)
                    {
                        alreadyIncluded = true;
                        break;
                    }
                }

                if (!alreadyIncluded)
                {
                    int index = items.arraySize;
                    items.InsertArrayElementAtIndex(index);
                    items.GetArrayElementAtIndex(index).objectReferenceValue = definition;
                }
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static StoreItemDefinition CreateOrLoadItem(
            string itemId,
            string displayName,
            StoreItemCategory category,
            string imagePath)
        {
            string assetPath = $"Assets/Data/Store/Items/{itemId}.asset";
            StoreItemDefinition definition = AssetDatabase.LoadAssetAtPath<StoreItemDefinition>(assetPath);
            if (definition != null)
            {
                return definition;
            }

            Sprite previewImage = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
            if (previewImage == null)
            {
                throw new FileNotFoundException($"No se encontró una imagen Sprite para el artículo: {imagePath}");
            }

            definition = ScriptableObject.CreateInstance<StoreItemDefinition>();
            AssetDatabase.CreateAsset(definition, assetPath);

            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("itemId").stringValue = itemId;
            serializedDefinition.FindProperty("displayName").stringValue = displayName;
            serializedDefinition.FindProperty("description").stringValue = string.Empty;
            serializedDefinition.FindProperty("category").enumValueIndex = (int)category;
            serializedDefinition.FindProperty("priceInCups").intValue = 0;
            serializedDefinition.FindProperty("priceConfigured").boolValue = false;
            serializedDefinition.FindProperty("availableInStore").boolValue = true;
            serializedDefinition.FindProperty("previewImage").objectReferenceValue = previewImage;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureStoreSectionInStevenOne(StoreCatalog catalog)
        {
            Scene targetScene = SceneManager.GetSceneByPath(TargetScenePath);
            bool targetLoadedByUtility = !targetScene.IsValid() || !targetScene.isLoaded;
            if (targetLoadedByUtility)
            {
                targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject targetCanvas = FindObjectInScene(targetScene, "Canvas");
                GameObject storeSection = FindObjectInScene(targetScene, StoreSectionName);
                if (targetCanvas == null || storeSection == null)
                {
                    throw new InvalidOperationException("No se encontró el Canvas principal o la sección de tienda de Steven 1.");
                }

                if (storeSection.transform.parent != targetCanvas.transform)
                {
                    storeSection.transform.SetParent(targetCanvas.transform, false);
                }

                storeSection.SetActive(false);

                GameObject storeButtonObject = FindObjectInScene(targetScene, "Botón tienda");
                Button storeButton = storeButtonObject != null ? storeButtonObject.GetComponent<Button>() : null;
                if (storeButton == null)
                {
                    throw new InvalidOperationException("No se encontró el botón tienda con componente Button en Steven 1.");
                }

                for (int index = storeButton.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
                {
                    UnityEventTools.RemovePersistentListener(storeButton.onClick, index);
                }

                UnityEventTools.AddBoolPersistentListener(storeButton.onClick, storeSection.SetActive, true);

                GameObject closeButtonObject = FindObjectInScene(targetScene, "Botón cerrar");
                Button closeButton = closeButtonObject != null ? closeButtonObject.GetComponent<Button>() : null;
                if (closeButton == null)
                {
                    throw new InvalidOperationException("No se encontró el botón cerrar de la sección tienda en Steven 1.");
                }

                for (int index = closeButton.onClick.GetPersistentEventCount() - 1; index >= 0; index--)
                {
                    UnityEventTools.RemovePersistentListener(closeButton.onClick, index);
                }

                UnityEventTools.AddBoolPersistentListener(closeButton.onClick, storeSection.SetActive, false);

                StoreInventoryController inventoryController = targetCanvas.GetComponent<StoreInventoryController>();
                if (inventoryController == null)
                {
                    inventoryController = Undo.AddComponent<StoreInventoryController>(targetCanvas);
                }

                SerializedObject serializedController = new SerializedObject(inventoryController);
                serializedController.FindProperty("catalog").objectReferenceValue = catalog;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inventoryController);
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }
            finally
            {
                if (targetLoadedByUtility && targetScene.IsValid() && targetScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(targetScene, true);
                }
            }
        }

        private static GameObject FindObjectInScene(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                foreach (Transform candidate in transforms)
                {
                    if (candidate.name == objectName)
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parentPath) || !AssetDatabase.IsValidFolder(parentPath))
            {
                throw new DirectoryNotFoundException($"No existe la carpeta padre de {folderPath}.");
            }

            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }
}
#endif
