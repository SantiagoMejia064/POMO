
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class PomoAdviceController : MonoBehaviour
    {
        private enum AdviceCategory
        {
            Wellbeing,
            Hydration,
            Rest,
            GoodHabits,
            Activity,
            Motivation
        }

        [Serializable]
        private class AdviceMessage
        {
            public AdviceCategory category;
            public string message;

            public AdviceMessage(AdviceCategory category, string message)
            {
                this.category = category;
                this.message = message;
            }
        }

        // =====================================================
        // POMO NORMAL
        // =====================================================

        [Header("Pomo normal")]
        [SerializeField] private GameObject normalPomo;

        // =====================================================
        // REACCIONES DE POMO
        // =====================================================

        [Header("Reacciones de Pomo")]
        [SerializeField] private GameObject wellbeingPomo;
        [SerializeField] private GameObject hydrationPomo;
        [SerializeField] private GameObject restPomo;
        [SerializeField] private GameObject goodHabitsPomo;
        [SerializeField] private GameObject activityPomo;
        [SerializeField] private GameObject motivationPomo;

        // =====================================================
        // INTERFAZ DEL CONSEJO
        // =====================================================

        [Header("Interfaz del consejo")]
        [SerializeField] private GameObject adviceBackground;
        [SerializeField] private TMP_Text adviceText;

        // =====================================================
        // SONIDO
        // =====================================================

        [Header("Sonido")]
        [SerializeField] private AudioSource adviceAudioSource;
        [SerializeField] private AudioClip adviceSound;

        // =====================================================
        // CONFIGURACIÓN DE TIEMPO
        // =====================================================

        [Header("Configuración de tiempo")]
        [SerializeField] private bool automaticAdvice = true;

        [Tooltip("Tiempo de espera antes del primer consejo.")]
        [SerializeField] private float firstAdviceDelay = 30f;

        [Tooltip("Intervalo entre consejos.")]
        [SerializeField] private float adviceInterval = 180f;

        [Tooltip("Tiempo durante el que se muestra cada consejo.")]
        [SerializeField] private float adviceDuration = 10f;

        // =====================================================
        // CONTROL DEL PANEL
        // =====================================================

        [Header("Control de pantalla")]
        [SerializeField] private GameObject inicioPanel;

        // =====================================================
        // HÁBITOS DEL USUARIO
        // =====================================================

        [Header("Hábitos del usuario")]
        [SerializeField]
        private HealthyHabitsFeedbackController healthyHabits;

        // =====================================================
        // VARIABLES INTERNAS
        // =====================================================

        private readonly List<AdviceMessage> allAdvice =
            new List<AdviceMessage>();

        private Coroutine adviceRoutine;
        private Coroutine currentAdvice;

        private string lastMessage = "";

        // =====================================================
        // INICIALIZACIÓN
        // =====================================================

        private void Awake()
        {
            LoadAdvice();

            if (healthyHabits == null)
            {
                healthyHabits =
                    FindFirstObjectByType<HealthyHabitsFeedbackController>();
            }

            HideAdvice();
        }

        private void OnEnable()
        {
            if (automaticAdvice)
            {
                adviceRoutine =
                    StartCoroutine(AutomaticAdviceRoutine());
            }
        }

        private void OnDisable()
        {
            if (adviceRoutine != null)
            {
                StopCoroutine(adviceRoutine);
                adviceRoutine = null;
            }

            if (currentAdvice != null)
            {
                StopCoroutine(currentAdvice);
                currentAdvice = null;
            }

            HideAdvice();
        }

        // =====================================================
        // CARGAR TODOS LOS CONSEJOS
        // =====================================================

        private void LoadAdvice()
        {
            // -------------------------------------------------
            // BIENESTAR Y PROGRESO
            // -------------------------------------------------

            Add(AdviceCategory.Wellbeing,
                "Entiendo que hoy no ha sido tan fácil. Empecemos con algo pequeño.");

            Add(AdviceCategory.Wellbeing,
                "No tienes que resolver todo ahora, solo dar el siguiente paso.");

            Add(AdviceCategory.Wellbeing,
                "Descansar también es avanzar.");

            Add(AdviceCategory.Wellbeing,
                "Tu cuerpo también necesita atención, no solo tus tareas.");

            Add(AdviceCategory.Wellbeing,
                "Hazlo a tu ritmo, pero no te abandones.");

            Add(AdviceCategory.Wellbeing,
                "¡Cada pequeño paso cuenta! Lo importante es no rendirse.");

            Add(AdviceCategory.Wellbeing,
                "Tu esfuerzo por empezar con buen pie es admirable. ¡Sigue adelante!");

            Add(AdviceCategory.Wellbeing,
                "No te preocupes si hoy fue difícil, mañana será mejor.");

            Add(AdviceCategory.Wellbeing,
                "Recuerda que el cambio no es inmediato, pero tu progreso ya está en marcha.");

            Add(AdviceCategory.Wellbeing,
                "Tu cuerpo ya está comenzando a adaptarse a los nuevos hábitos. ¡Sigue así!");

            Add(AdviceCategory.Wellbeing,
                "Un mes de esfuerzo constante, ¡lo estás logrando!");

            Add(AdviceCategory.Wellbeing,
                "Recuerda que un mes de trabajo constante se traduce en grandes avances.");

            Add(AdviceCategory.Wellbeing,
                "Tu esfuerzo ya está dando frutos. Sigue dándole importancia a cada detalle.");

            Add(AdviceCategory.Wellbeing,
                "Tu progreso es impresionante, sigue así para conseguir lo que deseas.");

            Add(AdviceCategory.Wellbeing,
                "¡Dos meses ya! Estás construyendo una versión más fuerte de ti mismo.");

            Add(AdviceCategory.Wellbeing,
                "Ya no se trata solo de empezar, sino de mantener el ritmo. ¡Estás logrando mucho!");

            Add(AdviceCategory.Wellbeing,
                "Recuerda que lo que has logrado hasta ahora es solo el inicio de algo aún mayor.");

            Add(AdviceCategory.Wellbeing,
                "Tu progreso es impresionante. Sigue empujando y alcanzando nuevas metas.");

            // -------------------------------------------------
            // HIDRATACIÓN
            // -------------------------------------------------

            Add(AdviceCategory.Hydration,
                "Tómate un vaso de agua, tu cuerpo también necesita apoyo para rendir mejor.");

            Add(AdviceCategory.Hydration,
                "Antes de seguir, respira y toma un poco de agua.");

            Add(AdviceCategory.Hydration,
                "A veces el cansancio también es falta de agua. Ve por un vaso y vuelves.");

            Add(AdviceCategory.Hydration,
                "Cuidarte no te quita tiempo, te da energía para continuar.");

            Add(AdviceCategory.Hydration,
                "¡No olvides tomar agua! Esto es clave para mantener tu energía durante el día.");

            Add(AdviceCategory.Hydration,
                "Recuerda que el agua es un aliado para tu mente y cuerpo. ¡Continúa cuidándote!");

            Add(AdviceCategory.Hydration,
                "Cada vaso de agua que tomas contribuye a tu bienestar físico y mental.");

            Add(AdviceCategory.Hydration,
                "Sigue así, tu cuerpo te lo agradecerá. Mantente hidratado para sentirte bien.");

            Add(AdviceCategory.Hydration,
                "Ya lo tienes como hábito, sigue tomando agua en cada oportunidad.");

            Add(AdviceCategory.Hydration,
                "El agua ahora es parte de tu rutina diaria. ¡Continúa así!");

            Add(AdviceCategory.Hydration,
                "Cada vaso de agua refuerza tu bienestar. ¡Sigue hidratándote de manera constante!");

            Add(AdviceCategory.Hydration,
                "Tu cuerpo ahora está más acostumbrado a la hidratación. ¡Mantén este hábito!");

            Add(AdviceCategory.Hydration,
                "¡Tu cuerpo está agradecido por esa hidratación constante! Sigue con este buen hábito.");

            Add(AdviceCategory.Hydration,
                "Recuerda que mantenerte hidratado es vital para tu salud general.");

            Add(AdviceCategory.Hydration,
                "Tu cuerpo ha aprendido a cuidar de sí mismo. ¡Continúa con estos hábitos!");

            Add(AdviceCategory.Hydration,
                "Seguir hidratándote es una forma de cuidar tu mente y cuerpo. ¡No pierdas el enfoque!");

            // -------------------------------------------------
            // DESCANSO Y SUEÑO
            // -------------------------------------------------

            Add(AdviceCategory.Rest,
                "Dormir bien no es perder tiempo, es prepararte para rendir mejor mañana.");

            Add(AdviceCategory.Rest,
                "Tu mente necesita pausas para seguir funcionando bien.");

            Add(AdviceCategory.Rest,
                "Descansar un momento también es parte del proceso.");

            Add(AdviceCategory.Rest,
                "No todo se resuelve forzándote; a veces se resuelve descansando.");

            Add(AdviceCategory.Rest,
                "Tu concentración mejora cuando respetas tus horas de sueño.");

            Add(AdviceCategory.Rest,
                "Recuerda dejar el uso de pantallas 30 minutos antes de ir a dormir.");

            Add(AdviceCategory.Rest,
                "Una rutina tranquila antes de dormir puede ayudarte a relajarte.");

            Add(AdviceCategory.Rest,
                "Sigue creando hábitos saludables de descanso. ¡Tu cuerpo te lo agradecerá!");

            Add(AdviceCategory.Rest,
                "Recuerda que descansar es parte del proceso de crecimiento. ¡No lo subestimes!");

            Add(AdviceCategory.Rest,
                "Tu cuerpo necesita tiempo para recuperarse. Asegúrate de dormir lo suficiente.");

            Add(AdviceCategory.Rest,
                "Tu concentración se verá mucho mejor si priorizas tu sueño. ¡Hazlo parte de tu rutina!");

            Add(AdviceCategory.Rest,
                "Recuerda que descansar bien es fundamental para afrontar los retos de mañana.");

            // -------------------------------------------------
            // BUENOS HÁBITOS
            // -------------------------------------------------

            Add(AdviceCategory.GoodHabits,
                "Los pequeños hábitos construyen los grandes resultados.");

            Add(AdviceCategory.GoodHabits,
                "No busques hacerlo perfecto, busca hacerlo constante.");

            Add(AdviceCategory.GoodHabits,
                "Ordenar un poco tu espacio también puede ayudarte a ordenar tu mente.");

            Add(AdviceCategory.GoodHabits,
                "Empieza por algo simple y deja que el ritmo llegue después.");

            Add(AdviceCategory.GoodHabits,
                "Tu progreso no depende de hacerlo todo hoy sino de empezar y hacer un poco cada día.");

            Add(AdviceCategory.GoodHabits,
                "¡Un día a la vez! Estás creando buenos hábitos, sigue avanzando.");

            Add(AdviceCategory.GoodHabits,
                "No todo debe ser perfecto, lo importante es ser constante. ¡Sigue así!");

            Add(AdviceCategory.GoodHabits,
                "Cada pequeño cambio te acerca a tu objetivo. ¡No te detengas!");

            Add(AdviceCategory.GoodHabits,
                "Haz lo que puedas cada día. ¡Esos pequeños pasos se suman!");

            Add(AdviceCategory.GoodHabits,
                "Estás avanzando. Celebra tus logros, ¡aunque sean pequeños!");

            // -------------------------------------------------
            // ACTIVIDAD FÍSICA
            // -------------------------------------------------

            Add(AdviceCategory.Activity,
                "¿Qué tal si te levantas un momento y estiras el cuerpo?");

            Add(AdviceCategory.Activity,
                "Haz una pausa de dos minutos y mueve los hombros.");

            Add(AdviceCategory.Activity,
                "Camina un poco y vuelve con la mente más despejada.");

            Add(AdviceCategory.Activity,
                "Ve a lavarte la cara y regresa; a veces eso cambia el ánimo.");

            Add(AdviceCategory.Activity,
                "Respira profundo tres veces conmigo antes de seguir.");

            Add(AdviceCategory.Activity,
                "Haz una mini meta: solo abrir el archivo ya cuenta como empezar.");

            Add(AdviceCategory.Activity,
                "Prueba avanzar solo un bloque y luego revisamos cómo te sientes.");

            Add(AdviceCategory.Activity,
                "Levántate y da una pequeña caminata para despejar tu mente.");

            Add(AdviceCategory.Activity,
                "Inicia con 5 minutos de actividad y verás cómo tu energía aumenta.");

            Add(AdviceCategory.Activity,
                "Intenta estirarte por 2 minutos. Te sentirás más liviano y renovado.");

            Add(AdviceCategory.Activity,
                "Haz una mini pausa de 3 minutos, solo para mover un poco el cuerpo.");

            Add(AdviceCategory.Activity,
                "Incorporar actividades físicas diarias te ayudará a mantenerte más enfocado.");

            Add(AdviceCategory.Activity,
                "Has logrado incorporar la actividad física en tu rutina. ¡Sigue así!");

            Add(AdviceCategory.Activity,
                "Mantén la actividad física como parte de tu rutina diaria. ¡Es un gran hábito!");

            // -------------------------------------------------
            // MOTIVACIÓN SUAVE
            // -------------------------------------------------

            Add(AdviceCategory.Motivation,
                "Una tarea a la vez. Tú puedes con eso.");

            Add(AdviceCategory.Motivation,
                "No minimices lo que ya hiciste, cuenta más de lo que crees.");

            Add(AdviceCategory.Motivation,
                "No te detengas. Estás construyendo una mejor versión de ti mismo.");

            Add(AdviceCategory.Motivation,
                "Tu yo del mañana te va a agradecer este pequeño esfuerzo.");

            Add(AdviceCategory.Motivation,
                "Si otras personas lo han logrado, tú también lo lograrás.");

            Add(AdviceCategory.Motivation,
                "Hoy fue un buen día. Mañana será aún mejor, ¡sigue adelante!");

            Add(AdviceCategory.Motivation,
                "Cada tarea que completes te acerca a tus metas. ¡Haz lo que puedas!");

            Add(AdviceCategory.Motivation,
                "La perseverancia es la clave. Cada día cuenta en tu camino hacia tus metas.");

            Add(AdviceCategory.Motivation,
                "No importa lo lento que vayas, siempre y cuando sigas avanzando.");

            Add(AdviceCategory.Motivation,
                "Recuerda que los días difíciles son temporales. ¡La constancia siempre trae resultados!");

            Add(AdviceCategory.Motivation,
                "El progreso se mide en pasos pequeños. ¡Sigue con la misma energía y constancia!");

            Add(AdviceCategory.Motivation,
                "Cada esfuerzo cuenta. ¡No te rindas ahora!");
        }

        private void Add(AdviceCategory category, string message)
        {
            allAdvice.Add(new AdviceMessage(category, message));
        }

        // =====================================================
        // COMPROBAR SI HAY HÁBITOS PENDIENTES
        // =====================================================

        private bool HasPendingHabit(AdviceCategory category)
        {
            if (healthyHabits == null)
                return false;

            List<int> pending =
                healthyHabits.GetHabitsToImprove();

            foreach (int index in pending)
            {
                string title =
                    healthyHabits.GetHabitTitle(index);

                string description =
                    healthyHabits.GetHabitDescription(index);

                string content = (title + " " + description)
                    .ToLowerInvariant();

                if (MatchesCategory(category, content))
                    return true;
            }

            return false;
        }

        // =====================================================
        // CLASIFICAR LOS HÁBITOS
        // =====================================================

        private bool MatchesCategory(
            AdviceCategory category,
            string content)
        {
            switch (category)
            {
                case AdviceCategory.Hydration:

                    return ContainsAny(content,
                        "hidrat",
                        "agua",
                        "vasos de agua");

                case AdviceCategory.Rest:

                    return ContainsAny(content,
                        "descanso",
                        "dormir",
                        "sueño",
                        "sueno",
                        "horas por noche");

                case AdviceCategory.Activity:

                    return ContainsAny(content,
                        "ejercicio",
                        "actividad física",
                        "actividad fisica",
                        "pausas activas",
                        "estiramientos",
                        "caminar",
                        "movimiento");

                case AdviceCategory.GoodHabits:

                    return ContainsAny(content,
                        "alimentación",
                        "alimentacion",
                        "meditación",
                        "meditacion",
                        "lectura",
                        "conexión social",
                        "conexion social",
                        "apoyo",
                        "buenos hábitos",
                        "buenos habitos");

                default:
                    return true;
            }
        }

        private bool ContainsAny(string text, params string[] words)
        {
            return words.Any(word => text.Contains(word));
        }

        // =====================================================
        // COMPROBAR SI ESTÁ EN EL PANEL INICIO
        // =====================================================

        private bool IsInicioActive()
        {
            return inicioPanel != null &&
                   inicioPanel.activeInHierarchy;
        }

        // =====================================================
        // SELECCIONAR UN CONSEJO ALEATORIO
        // =====================================================

        public void MostrarConsejoAleatorio()
        {
            // Nunca mostrar consejos fuera de Inicio.
            if (!IsInicioActive())
                return;

            // Evitar dos consejos simultáneos.
            if (currentAdvice != null)
            {
                StopCoroutine(currentAdvice);
                currentAdvice = null;
            }

            List<AdviceMessage> available = allAdvice
                .Where(advice =>
                    advice.category == AdviceCategory.Wellbeing ||
                    advice.category == AdviceCategory.Motivation ||
                    HasPendingHabit(advice.category))
                .ToList();

            if (available.Count == 0)
                return;

            // Evitar repetir inmediatamente el mismo mensaje.
            if (available.Count > 1)
            {
                List<AdviceMessage> different = available
                    .Where(advice =>
                        advice.message != lastMessage)
                    .ToList();

                if (different.Count > 0)
                    available = different;
            }

            AdviceMessage selected =
                available[UnityEngine.Random.Range(
                    0,
                    available.Count
                )];

            lastMessage = selected.message;

            currentAdvice = StartCoroutine(
                ShowAdviceRoutine(selected)
            );
        }

        // =====================================================
        // MOSTRAR CONSEJO, REACCIÓN Y SONIDO
        // =====================================================

        private IEnumerator ShowAdviceRoutine(AdviceMessage advice)
        {
            if (!IsInicioActive())
            {
                HideAdvice();
                currentAdvice = null;
                yield break;
            }

            SetAllReactions(false);

            if (normalPomo != null)
                normalPomo.SetActive(false);

            GameObject reaction = GetReaction(advice.category);

            if (reaction != null)
                reaction.SetActive(true);

            if (adviceBackground != null)
                adviceBackground.SetActive(true);

            if (adviceText != null)
            {
                adviceText.text = advice.message;
                adviceText.gameObject.SetActive(true);
            }

            // Reproducir el mismo sonido para todas las categorías.
            if (adviceAudioSource != null &&
                adviceSound != null)
            {
                adviceAudioSource.PlayOneShot(adviceSound);
            }

            float elapsed = 0f;

            while (elapsed < adviceDuration)
            {
                // Comprobar continuamente si sigue en Inicio.
                if (!IsInicioActive())
                {
                    HideAdvice();
                    currentAdvice = null;
                    yield break;
                }

                elapsed += Time.deltaTime;

                yield return null;
            }

            HideAdvice();

            currentAdvice = null;
        }

        // =====================================================
        // OBTENER REACCIÓN SEGÚN CATEGORÍA
        // =====================================================

        private GameObject GetReaction(AdviceCategory category)
        {
            switch (category)
            {
                case AdviceCategory.Wellbeing:
                    return wellbeingPomo;

                case AdviceCategory.Hydration:
                    return hydrationPomo;

                case AdviceCategory.Rest:
                    return restPomo;

                case AdviceCategory.GoodHabits:
                    return goodHabitsPomo;

                case AdviceCategory.Activity:
                    return activityPomo;

                case AdviceCategory.Motivation:
                    return motivationPomo;

                default:
                    return normalPomo;
            }
        }

        // =====================================================
        // OCULTAR REACCIONES
        // =====================================================

        private void SetAllReactions(bool active)
        {
            if (wellbeingPomo != null)
                wellbeingPomo.SetActive(active);

            if (hydrationPomo != null)
                hydrationPomo.SetActive(active);

            if (restPomo != null)
                restPomo.SetActive(active);

            if (goodHabitsPomo != null)
                goodHabitsPomo.SetActive(active);

            if (activityPomo != null)
                activityPomo.SetActive(active);

            if (motivationPomo != null)
                motivationPomo.SetActive(active);
        }

        // =====================================================
        // RESTAURAR POMO NORMAL
        // =====================================================

        private void HideAdvice()
        {
            SetAllReactions(false);

            if (adviceText != null)
            {
                adviceText.text = "";
                adviceText.gameObject.SetActive(false);
            }

            if (adviceBackground != null)
                adviceBackground.SetActive(false);

            if (normalPomo != null)
                normalPomo.SetActive(true);
        }

        // =====================================================
        // CONSEJOS AUTOMÁTICOS SOLO EN INICIO
        // =====================================================

        private IEnumerator AutomaticAdviceRoutine()
        {
            bool wasInInicio = false;

            while (true)
            {
                // ---------------------------------------------
                // SI NO ESTÁ EN INICIO
                // ---------------------------------------------

                if (!IsInicioActive())
                {
                    if (currentAdvice != null)
                    {
                        StopCoroutine(currentAdvice);
                        currentAdvice = null;
                    }

                    HideAdvice();

                    wasInInicio = false;

                    yield return null;

                    continue;
                }

                // ---------------------------------------------
                // DETECTAR ENTRADA A INICIO
                // ---------------------------------------------

                if (!wasInInicio)
                {
                    wasInInicio = true;

                    // Esperar antes del primer consejo.
                    yield return new WaitForSeconds(
                        firstAdviceDelay
                    );

                    continue;
                }

                // ---------------------------------------------
                // MOSTRAR CONSEJO
                // ---------------------------------------------

                if (currentAdvice == null)
                {
                    MostrarConsejoAleatorio();
                }

                // Esperar antes del siguiente consejo.
                yield return new WaitForSeconds(
                    Mathf.Max(
                        adviceInterval,
                        adviceDuration + 1f
                    )
                );
            }
        }
    }
}
