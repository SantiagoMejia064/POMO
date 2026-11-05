using System;
using Pomo.Data.Persistence;
using UnityEngine;

internal static class UserNameChecks
{
    public static void Run(Action<bool, string> check)
    {
        PlayerPrefs.DeleteKey(LocalUserProfile.NameKey);
        check(LocalUserProfile.Name == string.Empty, "Sin perfil el nombre está vacío.");
        foreach (string invalid in new[] { null, "", "   ", "\t\n", "\u200B", "Ana\nMaría", new string('a', 61), "\uD800" })
        {
            check(!LocalUserProfile.TrySaveName(invalid), "No se guarda un nombre vacío, invisible o inválido.");
            check(!LocalUserProfile.MatchesName(invalid, "Ana"), "Un nombre inválido no autoriza el borrado.");
        }
        check(!PlayerPrefs.HasKey(LocalUserProfile.NameKey), "Los intentos inválidos no crean un perfil.");
        check(LocalUserProfile.TrySaveName("  María José  "), "Se acepta un nombre compuesto con acentos.");
        check(LocalUserProfile.Name == "María José", "El nombre se conserva localmente sin espacios exteriores.");
        check(LocalUserProfile.MatchesName(" maría josé ", LocalUserProfile.Name), "Mayúsculas y espacios exteriores no cambian el nombre.");
        check(LocalUserProfile.MatchesName("Mari\u0301a José", LocalUserProfile.Name), "Las dos representaciones Unicode del mismo acento coinciden.");
        check(!LocalUserProfile.MatchesName("Maria Jose", LocalUserProfile.Name), "Un nombre diferente no confirma el borrado.");
        check(!LocalUserProfile.MatchesName("María", LocalUserProfile.Name), "Un nombre parcial no confirma el borrado.");
        check(!LocalUserProfile.MatchesName("", ""), "La ausencia de nombre nunca autoriza el borrado.");
        check(!LocalUserProfile.TrySaveName(" ") && LocalUserProfile.Name == "María José", "Un intento vacío no reemplaza un nombre guardado.");
    }
}
