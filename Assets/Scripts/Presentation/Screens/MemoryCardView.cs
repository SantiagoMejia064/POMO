using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Vista intercambiable: Valentina podrá sustituir los números por sprites
    /// sin cambiar las reglas ni el orden de las cartas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MemoryCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image cardBackground;
        [SerializeField] private TMP_Text faceLabel;
        [SerializeField] private Image figureImage;
        [Header("Colores del prototipo")]
        [SerializeField] private Color hiddenColor = new Color32(239, 209, 156, 255);
        [SerializeField] private Color revealedColor = new Color32(255, 232, 185, 255);
        [SerializeField] private Color matchedColor = new Color32(204, 225, 186, 255);

        private int cardIndex;
        private Action<int> onSelected;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (cardBackground == null)
            {
                cardBackground = GetComponent<Image>();
            }

            button?.onClick.AddListener(NotifySelected);
        }

        private void OnDestroy()
        {
            button?.onClick.RemoveListener(NotifySelected);
        }

        public void Bind(int index, Action<int> selectionHandler)
        {
            cardIndex = index;
            onSelected = selectionHandler;
        }

        public void SetVisual(int pairId, bool isFaceUp, bool isMatched, bool canSelect, Sprite figureSprite)
        {
            if (button != null)
            {
                button.interactable = canSelect;
            }

            if (cardBackground != null)
            {
                cardBackground.color = isMatched
                    ? matchedColor
                    : isFaceUp
                        ? revealedColor : hiddenColor;
            }

            bool showSprite = isFaceUp && figureSprite != null && figureImage != null;
            if (figureImage != null)
            {
                figureImage.sprite = showSprite ? figureSprite : null;
                figureImage.gameObject.SetActive(showSprite);
            }

            if (faceLabel != null)
            {
                // Las cartas ocultas son lisas, como en el prototipo.
                faceLabel.gameObject.SetActive(isFaceUp && !showSprite);
                faceLabel.text = isFaceUp ? (pairId + 1).ToString() : string.Empty;
                faceLabel.color = isFaceUp
                    ? new Color32(20, 59, 105, 255)
                    : Color.white;
            }
        }

        private void NotifySelected()
        {
            onSelected?.Invoke(cardIndex);
        }
    }
}
