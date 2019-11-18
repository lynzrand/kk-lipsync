using System;
using UnityEngine;
using ADV.Commands.Chara;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Harmony;
using HarmonyLib;
using KKAPI;
using System.Collections.Generic;
using System.Text;

namespace KKLipsync
{
    [BepInPlugin(Guid, PluginName, PluginVersion)]
    public class LipsyncPlugin : BaseUnityPlugin
    {
        const string Guid = "me.rynco.kk-lipsync";
        const string PluginName = "KKLipsync";
        const string PluginVersion = "0.1.0";

        public LipsyncPlugin()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Info, $"Loaded {PluginName} {PluginVersion}");
            var harmony = new Harmony(Guid);

            harmony.PatchAll(typeof(Hooks.UpdateBlendShapeHook));
            harmony.PatchAll(typeof(Hooks.AssistHook));
            harmony.PatchAll(typeof(Hooks.BlendShapeHook));

            //KKAPI.Chara.CharacterApi.RegisterExtraBehaviour<LipsyncController>(Guid);
        }



    }

    namespace Hooks
    {
        public static class UpdateBlendShapeHook
        {
            [HarmonyPatch(typeof(ChaControl), "UpdateBlendShapeVoice")]
            [HarmonyPostfix]
            public static void NewUpdateBlendShape(ChaControl __instance)
            {
                var voice = AccessTools.PropertyGetter(typeof(ChaControl), "fbsaaVoice").Invoke(__instance, new object[] { }) as LipDataCreator;
                if (__instance.asVoice && __instance.asVoice.isPlaying)
                    voice?.GetLipData(__instance.asVoice);

                if (voice == null) LipsyncConfig.Instance.logger.LogWarning("LipDataCreator is null");

                return;
            }
        }

        public static class AssistHook
        {
            [HarmonyPatch(typeof(ChaControl), "InitializeControlFaceObject")]
            [HarmonyPrefix]
            public static void ReplaceAssist(
                ChaControl __instance
            )
            {
                var ctrl = new LipDataCreator();
                AccessTools.PropertySetter(typeof(ChaControl), "fbsaaVoice").Invoke(__instance, new[] { ctrl });
                var manager = __instance.GetOrAddComponent<LipsyncDebugGui>();
                manager.reference = ctrl;
                LipsyncConfig.Instance.logger.LogInfo($"Initialized at {__instance.chaID}");
            }
        }

        public static class BlendShapeHook
        {
            //static float progress = 0f;

            [HarmonyPatch(typeof(FBSCtrlMouth), "CalcBlend")]
            [HarmonyPrefix]
            public static bool NewCalcBlendShape(FBSBase __instance)
            {
                var sb = new StringBuilder();
                var nowFace = AccessTools.Field(typeof(FBSCtrlMouth), "dictNowFace").GetValue(__instance) as Dictionary<int, float>;
                if (nowFace is null) return true;
                //sb.AppendLine("{");
                //foreach (var line in nowFace)
                //{
                //    sb.AppendLine($"  {line.Key}: {line.Value},");
                //}
                //sb.AppendLine("}");
                //LipsyncConfig.Instance.src.LogInfo($"{__instance.FixedRate}, {sb.ToString()}");
                // Don't run the original method
                //nowFace.Clear();

                //if (progress < 0.3f)
                //{
                //    nowFace[25] = progress / 0.3f;
                //    nowFace[27] = 0f;
                //}
                //else if (progress < 0.7f)
                //{
                //    nowFace[25] = (0.7f - progress) / 0.4f;
                //    nowFace[27] = (progress - 0.3f) / 0.4f;
                //}
                //else
                //{
                //    nowFace[25] = 0f;
                //    nowFace[27] = (1f - progress) / 0.3f;
                //}
                //float openness = 0;
                //if (progress < 0.2f) openness = progress / 0.2f;
                //else if (progress > 0.8f) openness = (1f - progress) / 0.2f;
                //else openness = 1;

                //progress += 0.01f;
                //if (progress > 1) progress = 0;

                //AccessTools.Field(typeof(FBSCtrlMouth), "FixedRate").SetValue(__instance, openness);

                return true;
            }
        }
    }
}
