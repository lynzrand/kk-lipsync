using System;
using UnityEngine;
using ADV.Commands.Chara;
using BepInEx;
using Harmony;
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
            BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, $"Loaded {PluginName} {PluginVersion}");
            var harmony = HarmonyInstance.Create(Guid);

            harmony.PatchAll(typeof(Hooks.Hook));

            //KKAPI.Chara.CharacterApi.RegisterExtraBehaviour<LipsyncController>(Guid);
        }



    }

    namespace Hooks
    {
        public static class Hook
        {
            [HarmonyPatch(typeof(ChaControl), "UpdateBlendShapeVoice")]
            [HarmonyPrefix]
            public static bool NewCalcBlendShape(ChaControl __instance)
            {

                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Message, $"It works!");
                // Console.WriteLine($"{__instance.FixedRate}, {sb.ToString()}");
                // Don't run the original method
                return true;
            }

            [HarmonyPatch(typeof(FaceBlendShape), "Awake")]
            [HarmonyPostfix]
            public static void ReplaceAudioAssist(
                FaceBlendShape __instance
            )
            {
                //__instance.MouthCtrl = ;
            }
        }

        [HarmonyPatch(typeof(FBSCtrlMouth), "CalcBlend")]
        public class BlendShapeHook
        {
            [HarmonyPrefix]
            public static bool NewCalcBlendShape(FBSBase __instance, Dictionary<int, float> ___dictNowFace)
            {
                var sb = new StringBuilder();
                sb.AppendLine("{");
                foreach (var line in ___dictNowFace)
                {
                    sb.AppendLine($"  {line.Key}: {line.Value},");
                }
                sb.AppendLine("}");
                BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Message, $"{__instance.FixedRate}, {sb.ToString()}");
                Console.WriteLine($"{__instance.FixedRate}, {sb.ToString()}");
                // Don't run the original method
                return true;
            }
        }
    }
}
