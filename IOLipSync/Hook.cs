using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Karenia.IoLipsync
{
    [BepInEx.BepInPlugin(guid, "IOLipsync", "0.1.0")]
    public class Hook : BaseUnityPlugin
    {
        private const string guid = "cc.karenia.iolipsync";

        public static LipsyncConfig config = new LipsyncConfig();

        public Hook()
        {
            var harmony = new Harmony(guid);
            harmony.PatchAll(typeof(TestHook));

            this.AddConfigs();
        }

        public void AddConfigs()
        {
            config.DebugMenu = Config.Bind<bool>(new ConfigDefinition("Debug", "Debug Menu"), false);
        }
    }

    public class LipsyncConfig
    {
        public ConfigEntry<bool> DebugMenu { get; set; }
    }

    public static class TestHook
    {
        [HarmonyPatch(typeof(KutiPaku), "Update")]
        [HarmonyBefore]
        public static bool CheckLip(KutiPaku __instance, Animator ___animator)
        {
            var animator = ___animator;
            if (animator != null)
            {
                // TODO
            }
            return false;
        }

        [HarmonyPatch(typeof(KutiPaku), "OnGUI")]
        [HarmonyAfter]
        public static void IoDebugGui(KutiPaku __instance, ref Animator ___animator)
        {
            if (!Hook.config.DebugMenu.Value) return;

            var animator = ___animator;
            var window = GUILayout.Window(0x21840123, Rect.zero, (id) =>
            {
                GUILayout.BeginVertical();
                var cnt = animator.layerCount;
                for (var i = 0; i < cnt; i++)
                {
                    GUILayout.BeginHorizontal();
                    float layerWeight = animator.GetLayerWeight(i);
                    GUILayout.Label(string.Format("{0}:{1:0.3}", animator.GetLayerName(i), layerWeight), GUILayout.Width(60));
                    var newLayerWeight = GUILayout.HorizontalSlider(layerWeight, 0, 1);
                    GUILayout.EndHorizontal();

                    if (newLayerWeight != layerWeight) animator.SetLayerWeight(i, layerWeight);
                }

                GUILayout.EndVertical();
            }, string.Format("DEBUG/IOLipsync/{0}", __instance.name));
        }
    }
}
