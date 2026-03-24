using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.UI;
using UnityEngine;

[assembly: MelonInfo(typeof(TimeSinceLastSaveMod.TimeSinceLastSave), "Time Since Last Save", "1.1.0", "Krusty")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace TimeSinceLastSaveMod
{
    public class TimeSinceLastSave : MelonMod
    {
        private static float _lastSaveRealTime = -1f;

        public override void OnInitializeMelon()
        {
            _lastSaveRealTime = -1f;
            MelonLogger.Msg("Time Since Last Save mod loaded.");
        }

        // Hook: Reset timer when a new save is created (Auto or Manual)
        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.SaveGame))]
        internal class Patch_SaveGameSystem_SaveGame
        {
            private static void Postfix()
            {
                _lastSaveRealTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("Save detected. Timer reset.");
            }
        }

        // Hook: Reset timer when a game is loaded from the menu
        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        internal class Patch_SaveGameSystem_LoadSceneData
        {
            private static void Postfix()
            {
                _lastSaveRealTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("Save loaded. Timer reset.");
            }
        }

        // Hook: Inject the text into the Quit Confirmation UI
        [HarmonyPatch(typeof(Panel_Confirmation), "Update")]
        internal class Patch_Panel_Confirmation_Update
        {
            private static void Postfix(Panel_Confirmation __instance)
            {
                if (!__instance.IsEnabled()) return;

                var currentRequest = __instance.GetCurrentConfirmationRequest();
                if (currentRequest == null) return;

                if (currentRequest.m_ConfirmationType == Panel_Confirmation.ConfirmationType.QuitGame)
                {
                    if (_lastSaveRealTime < 0) return;

                    float elapsedSeconds = Time.realtimeSinceStartup - _lastSaveRealTime;

                    int hours = Mathf.FloorToInt(elapsedSeconds / 3600);
                    int minutes = Mathf.FloorToInt((elapsedSeconds % 3600) / 60);
                    int seconds = Mathf.FloorToInt(elapsedSeconds % 60);

                    string timeDisplay;
                    if (hours > 0)
                    {
                        timeDisplay = $"{hours}h {minutes}m {seconds}s";
                    }
                    else if (minutes > 0)
                    {
                        timeDisplay = $"{minutes}m {seconds}s";
                    }
                    else
                    {
                        timeDisplay = $"{seconds}s";
                    }

                    string injection = $"\n\nLast saved: {timeDisplay} ago";

                    if (__instance.m_CurrentGroup != null && __instance.m_CurrentGroup.m_MessageLabel != null)
                    {
                        var label = __instance.m_CurrentGroup.m_MessageLabel;
                        if (!label.text.Contains("Last saved:"))
                        {
                            label.text += injection;
                        }
                    }
                }
            }
        }
    }
}
