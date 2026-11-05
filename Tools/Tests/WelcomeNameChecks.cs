// Runs the actual welcome controller against small UI doubles, without Unity or user preferences.
using System;
using System.Reflection;
using Pomo.Data.Persistence;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class WelcomeNameChecks
{
    public static void Run(Action<bool,string> check)
    {
        LocalUserProfile.TrySaveName("Nombre anterior");
        var controller=new NombreBienvenidaController();
        var input=new TMP_InputField();
        var next=new Button();
        var welcome=new GameObject();
        var hello=new GameObject(); hello.SetActive(false);
        var privacy=new PrivacyConsentController { IsBlockingWelcome=true };
        void Assign(string name, object value) => typeof(NombreBienvenidaController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,value);
        Assign("nombre",input); Assign("botonContinuar",next); Assign("panelBienvenida",welcome);
        Assign("panelHola",hello); Assign("privacidad",privacy); Assign("avisoNombre",new TMP_Text());
        typeof(NombreBienvenidaController).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
        check(input.text==""&&!next.interactable,"Un nombre guardado no rellena el campo ni habilita Siguiente.");
        privacy.IsBlockingWelcome=false;
        controller.PrepararEntrada(); // Privacy's Accept calls this before revealing the welcome.
        controller.Continuar();
        check(welcome.activeInHierarchy&&!hello.activeInHierarchy,"Aceptar privacidad sin escribir no permite avanzar ni por llamada directa.");
        input.text="   "; controller.Continuar();
        check(!next.interactable&&welcome.activeInHierarchy,"Solo espacios mantiene bloqueado Siguiente.");
        input.text="Santiago";
        check(next.interactable,"Escribir un nombre válido habilita Siguiente.");
        privacy.IsBlockingWelcome=true; controller.Continuar();
        check(welcome.activeInHierarchy&&!hello.activeInHierarchy,"Escribir nombre no permite saltarse privacidad.");
        privacy.IsBlockingWelcome=false; controller.PrepararEntrada();
        check(input.text==""&&!next.interactable,"Aceptar reinicia el campo y deshabilita Siguiente aunque se hubiera escrito antes.");
        input.text="Santiago"; input.text=""; controller.Continuar();
        check(!next.interactable&&welcome.activeInHierarchy,"Borrar el nombre vuelve a bloquear Siguiente.");
        input.text=" Santiago "; controller.Continuar();
        check(!welcome.activeInHierarchy&&hello.activeInHierarchy&&LocalUserProfile.Name=="Santiago",
            "Solo continuar con un nombre escrito guarda el perfil y abre Hola.");
    }
}

namespace UnityEngine
{
    public class MonoBehaviour { }
    public sealed class SerializeField : Attribute { }
    public sealed class DisallowMultipleComponent : Attribute { }
    public class GameObject { public bool activeInHierarchy=true; public void SetActive(bool value)=>activeInHierarchy=value; }
}
namespace TMPro
{
    public class TMP_Text { public string text; }
    public class TMP_InputField
    {
        public int characterLimit; public bool richText;
        public readonly StringEvent onValueChanged=new StringEvent();
        private string value="";
        public string text { get=>value; set { this.value=value; onValueChanged.Invoke(value); } }
        public void SetTextWithoutNotify(string text)=>value=text;
    }
    public class StringEvent
    {
        private event Action<string> Changed;
        public void AddListener(Action<string> action)=>Changed+=action;
        public void RemoveListener(Action<string> action)=>Changed-=action;
        public void Invoke(string value)=>Changed?.Invoke(value);
    }
}
namespace UnityEngine.UI { public class Button { public bool interactable=true; } }
namespace Pomo.Presentation.Screens { public class PrivacyConsentController { public bool IsBlockingWelcome; } }
