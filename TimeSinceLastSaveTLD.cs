using System;
using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.UI;
using UnityEngine;

[assembly: MelonInfo(typeof(TimeSinceLastSaveMod.TimeSinceLastSave), "Time Since Last Save", "1.3.0", "Krusty")]
[assembly: MelonGame("Hinterland", "TheLongDark")]

namespace TimeSinceLastSaveMod
{
    public class TimeSinceLastSave : MelonMod
    {
        private static float _lastSaveRealTime = -1f;

        public override void OnInitializeMelon()
        {
            _lastSaveRealTime = -1f;
            MelonLogger.Msg("Time Since Last Save mod loaded. UI stability fix applied.");
        }

        // --- SAVING: Use GameManager to catch Save Manager hotkeys + Vanilla saves ---
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveGameAndDisplayHUDMessage))]
        internal class Patch_GameManager_SaveGameAndDisplayHUDMessage
        {
            private static void Postfix()
            {
                _lastSaveRealTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("Save detected. Timer reset.");
            }
        }

        // --- LOADING: Use LoadSceneData for better UI timing ---
        [HarmonyPatch(typeof(SaveGameSystem), nameof(SaveGameSystem.LoadSceneData))]
        internal class Patch_SaveGameSystem_LoadSceneData
        {
            private static void Postfix()
            {
                _lastSaveRealTime = Time.realtimeSinceStartup;
                MelonLogger.Msg("Save loaded. Timer reset.");
            }
        }

        // --- UI INJECTION ---
        [HarmonyPatch(typeof(Panel_Confirmation), "Update")]
        internal class Patch_Panel_Confirmation_Update
        {
            private static void Postfix(Panel_Confirmation __instance)
            {
                // Only run if the panel is actually visible and active
                if (!__instance.IsEnabled() || !__instance.gameObject.activeInHierarchy) return;

                var currentRequest = __instance.GetCurrentConfirmationRequest();
                if (currentRequest == null) return;

                if (currentRequest.m_ConfirmationType == Panel_Confirmation.ConfirmationType.QuitGame)
                {
                    if (_lastSaveRealTime < 0) return;

                    float elapsedSeconds = Time.realtimeSinceStartup - _lastSaveRealTime;

                    int hours = Mathf.FloorToInt(elapsedSeconds / 3600);
                    int minutes = Mathf.FloorToInt((elapsedSeconds % 3600) / 60);
                    int seconds = Mathf.FloorToInt(elapsedSeconds % 60);

                    string timeDisplay = hours > 0 ? $"{hours}h {minutes}m {seconds}s" :
                                         minutes > 0 ? $"{minutes}m {seconds}s" :
                                         $"{seconds}s";

                    string injection = $"\n\nLast saved: {timeDisplay} ago";

                    if (__instance.m_CurrentGroup != null && __instance.m_CurrentGroup.m_MessageLabel != null)
                    {
                        var label = __instance.m_CurrentGroup.m_MessageLabel;

                        // Check if we need to update the text
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
