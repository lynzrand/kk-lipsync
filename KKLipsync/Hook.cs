using System;
using UnityEngine;
using ADV.Commands.Chara;
using BepInEx;
using Harmony;
using KKAPI;

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

            harmony.PatchAll(typeof(KKLipsync.Hooks.AwakeHook));

            KKAPI.Chara.CharacterApi.RegisterExtraBehaviour<LipsyncController>(Guid);
        }



    }

    namespace Hooks
    {
        [HarmonyPatch(typeof(ChaControl))]
        [HarmonyPatch("Awake")]

        public class AwakeHook
        {
            //static void Postfix(ChaControl __instance)
            //{
            //    __instance.gameObject.AddComponent<LipsyncController>();
            //}
        }

    }
}
