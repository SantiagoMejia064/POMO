using System.Collections.Generic;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    /// <summary>Applies the scene's button style, including lists built at runtime.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("POMO/Interfaz/Estilo de botones de la escena")]
    public sealed class EstiloBotonesEscena : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset fuenteBotones;
        [SerializeField] private Font fuenteClasica;
        [SerializeField] private Sprite fondoRedondeado;
        [SerializeField] private Sprite fondoPlanoAnterior;
        [SerializeField] private Sprite[] fondosIntegrados;
        [SerializeField, Range(1f, 32f)] private float radioEsquinas = 12f;
        [Tooltip("Listas que conservan el diseño y las medidas originales de sus tarjetas.")]
        [SerializeField] private Transform[] contenidosSinModificar;

        private readonly List<Button> botones = new List<Button>();
        private readonly List<TMP_Text> textos = new List<TMP_Text>();
        private readonly List<Text> textosClasicos = new List<Text>();
        private float siguienteRevision;

        private void OnEnable() => Aplicar();

        private void LateUpdate()
        {
            if (Time.unscaledTime < siguienteRevision) return;
            siguienteRevision = Time.unscaledTime + .35f;
            Aplicar();
        }

        public void Aplicar()
        {
            if (fuenteBotones == null || fondoRedondeado == null) return;
            foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
            {
                raiz.GetComponentsInChildren(false, botones);
                foreach (Button boton in botones)
                {
                    if (ConservaEstiloOriginal(boton)) continue;
                    boton.GetComponentsInChildren(true, textos);
                    foreach (TMP_Text texto in textos)
                    {
                        if (texto.GetComponentInParent<Button>() != boton || texto.GetComponentInParent<TMP_InputField>() != null) continue;
                        if (texto.font != fuenteBotones)
                        {
                            float tamano = texto.fontSize;
                            texto.font = fuenteBotones;
                            texto.fontSharedMaterial = fuenteBotones.material;
                            texto.enableAutoSizing = true;
                            texto.fontSizeMax = tamano;
                            texto.fontSizeMin = Mathf.Min(tamano, Mathf.Max(10f, tamano * .65f));
                        }
                        if (texto.fontStyle != FontStyles.Normal) texto.fontStyle = FontStyles.Normal;
                        if (texto.textWrappingMode != TextWrappingModes.Normal) texto.textWrappingMode = TextWrappingModes.Normal;
                        if (texto.overflowMode != TextOverflowModes.Ellipsis) texto.overflowMode = TextOverflowModes.Ellipsis;
                    }
                    if (fuenteClasica != null)
                    {
                        boton.GetComponentsInChildren(true, textosClasicos);
                        foreach (Text texto in textosClasicos)
                            if (texto.GetComponentInParent<Button>() == boton && texto.GetComponentInParent<InputField>() == null && texto.font != fuenteClasica)
                                texto.font = fuenteClasica;
                    }
                    if (!(boton.targetGraphic is Image imagen) || !EsFondoPlano(imagen.sprite)) continue;
                    if (imagen.sprite == fondoRedondeado && imagen.type == Image.Type.Sliced &&
                        !Mathf.Approximately(imagen.pixelsPerUnitMultiplier, 1f)) continue;
                    if (imagen.sprite != fondoRedondeado) imagen.sprite = fondoRedondeado;
                    if (imagen.type != Image.Type.Sliced) imagen.type = Image.Type.Sliced;
                    float escala = 12f / Mathf.Max(1f, radioEsquinas);
                    if (!Mathf.Approximately(imagen.pixelsPerUnitMultiplier, escala)) imagen.pixelsPerUnitMultiplier = escala;
                }
            }
        }

        private bool ConservaEstiloOriginal(Button boton)
        {
            // A card's Image supplies its preferred width to the layout group.
            // Replacing it with a 64px button sprite collapses the entire row.
            if (boton.GetComponentInParent<TaskItemView>(true) != null ||
                boton.GetComponentInParent<BlockTaskSelectionItemView>(true) != null ||
                boton.GetComponentInParent<WorkBlockItemView>(true) != null ||
                boton.GetComponentInParent<DailyProgressTaskItemView>(true) != null) return true;
            if (contenidosSinModificar == null) return false;
            foreach (Transform contenido in contenidosSinModificar)
                if (contenido != null && boton.transform.IsChildOf(contenido)) return true;
            return false;
        }

        private bool EsFondoPlano(Sprite sprite)
        {
            if (sprite == null || sprite == fondoRedondeado || sprite == fondoPlanoAnterior) return true;
            if (fondosIntegrados == null) return false;
            foreach (Sprite fondo in fondosIntegrados) if (sprite == fondo) return true;
            return false;
        }
    }
}
