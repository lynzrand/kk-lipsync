using System;
using System.Collections.Generic;
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

        private struct EnabledToggleKey : IEquatable<EnabledToggleKey>
        {
            public int id;
            public int layer;

            #region toggle

            public override bool Equals(object obj)
            {
                return obj is EnabledToggleKey key && Equals(key);
            }

            public bool Equals(EnabledToggleKey other)
            {
                return id == other.id &&
                       layer == other.layer;
            }

            public override int GetHashCode()
            {
                int hashCode = 2005647062;
                hashCode = hashCode * -1521134295 + id.GetHashCode();
                hashCode = hashCode * -1521134295 + layer.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(EnabledToggleKey left, EnabledToggleKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(EnabledToggleKey left, EnabledToggleKey right)
            {
                return !(left == right);
            }

            #endregion toggle
        }

        private struct EnabledToggleValue
        {
            public bool enabled;
            public float value;
        }

        private static readonly Dictionary<int, Rect> windows = new Dictionary<int, Rect>();
        private static readonly Dictionary<EnabledToggleKey, EnabledToggleValue> enabledKeys = new Dictionary<EnabledToggleKey, EnabledToggleValue>();

        [HarmonyPatch(typeof(FaceMotion), "Awake")]
        [HarmonyPostfix]
        public static void AddDebugGui(FaceMotion __instance, Animator ___animator)
        {
            var guiComponent = __instance.gameObject.AddComponent<KutiPakuDebugGui>();
            guiComponent.target = ___animator;
        }

        private class KutiPakuDebugGui : MonoBehaviour
        {
            public Animator target;

            public void OnGUI()
            {
                if (!Hook.config.DebugMenu.Value) return;
                var id = target.GetHashCode() + 0x14243;

                var animator = target;
                string title = string.Format("DEBUG/IOLipsync/{0}", target.gameObject.name);

                if (!windows.TryGetValue(id, out var lastWindow))
                {
                    lastWindow = new Rect(20, 20, 480, 480);
                }

                var window = GUILayout.Window(
                    id, lastWindow,
                (id1) =>
                {
                    GUI.DragWindow();
                    //GUILayout.BeginArea(new Rect(0, 0, 240, 480));
                    GUILayout.BeginVertical();
                    GUILayout.Label(title);
                    var cnt = animator.layerCount;
                    for (var i = 0; i < cnt; i++)
                    {
                        var toggle = new EnabledToggleKey { id = id, layer = i };
                        if (!enabledKeys.TryGetValue(toggle, out var value))
                        {
                            value = new EnabledToggleValue { enabled = false, value = 0 };
                        }

                        GUILayout.BeginHorizontal();
                        float layerWeight = animator.GetLayerWeight(i);
                        value.enabled = GUILayout.Toggle(value.enabled, string.Format("{0}", animator.GetLayerName(i), layerWeight), GUILayout.Width(90));
                        GUILayout.Label(string.Format("{0}", layerWeight), GUILayout.Width(90));
                        value.value = GUILayout.HorizontalSlider(value.value, 0, 1);
                        GUILayout.EndHorizontal();

                        if (value.enabled) animator.SetLayerWeight(i, value.value);

                        enabledKeys[toggle] = value;
                    }

                    GUILayout.EndVertical();
                    //GUILayout.EndArea();
                },
                title);

                windows[id] = window;
            }
        }
    }
}
