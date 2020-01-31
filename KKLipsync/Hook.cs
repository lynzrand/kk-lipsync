using System;
using UnityEngine;
using ADV.Commands.Chara;
using BepInEx;
using System.Linq;
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
                if (__instance.asVoice && __instance.asVoice.isPlaying && voice != null)
                {
                    var frame = voice.GetLipData(__instance.asVoice);
                    // TODO
                    LipsyncConfig.Instance.frameStore[0] = frame;
                }

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
                var ctrl = new LipDataCreator(__instance.chaID);
                AccessTools.PropertySetter(typeof(ChaControl), "fbsaaVoice").Invoke(__instance, new[] { ctrl });
                //var manager = __instance.GetOrAddComponent<LipsyncDebugGui>();
                //manager.audioAssist = ctrl;
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
                var openness = (float)AccessTools.Field(typeof(FBSCtrlMouth), "FixedRate").GetValue(__instance);
                if (nowFace is null) return true;

                if (LipsyncConfig.Instance.frameStore.TryGetValue(0, out var targetFrame))
                {
                    MapFrame(targetFrame, ref nowFace, ref openness);
                    AccessTools.Field(typeof(FBSCtrlMouth), "FixedRate").SetValue(__instance, openness);
                    AccessTools.Field(typeof(FBSCtrlMouth), "dictNowFace").SetValue(__instance, nowFace);
                    return true;
                }
                else
                {
                    return true;
                }
            }

            static readonly Dictionary<int, int> VisemeKKFaceId = new Dictionary<int, int>()
            {
                [(int)OVRLipSync.Viseme.aa] = 26,
                [(int)OVRLipSync.Viseme.CH] = 27,
                [(int)OVRLipSync.Viseme.DD] = 12,
                [(int)OVRLipSync.Viseme.E] = 32,
                [(int)OVRLipSync.Viseme.FF] = 3,
                [(int)OVRLipSync.Viseme.ih] = 27,
                [(int)OVRLipSync.Viseme.kk] = 31,
                [(int)OVRLipSync.Viseme.nn] = 36,
                [(int)OVRLipSync.Viseme.oh] = 33,
                [(int)OVRLipSync.Viseme.ou] = 30,
                [(int)OVRLipSync.Viseme.PP] = 36,
                [(int)OVRLipSync.Viseme.RR] = 30,
                // sil does nothing. It has contribution set to 0 below.
                [(int)OVRLipSync.Viseme.sil] = 0,
                [(int)OVRLipSync.Viseme.SS] = 27,

                // /th/ is not seen in faces
                [(int)OVRLipSync.Viseme.TH] = 12,
            };

            /// <summary>
            /// Coeffecient of OVR visemes on openness.
            /// 
            /// <para>
            ///     Vowels always have a .9f contribution, while consonants contributions are 
            ///     based on their relationship with mouth actions.
            /// </para>
            /// </summary>
            static readonly Dictionary<int, float> VisemeOpennessCoeff = new Dictionary<int, float>()
            {
                [(int)OVRLipSync.Viseme.aa] = .9f,
                [(int)OVRLipSync.Viseme.CH] = .9f,
                [(int)OVRLipSync.Viseme.DD] = .2f,
                [(int)OVRLipSync.Viseme.E] = .9f,
                [(int)OVRLipSync.Viseme.FF] = .2f,
                [(int)OVRLipSync.Viseme.ih] = .9f,
                [(int)OVRLipSync.Viseme.kk] = .8f,
                [(int)OVRLipSync.Viseme.nn] = 0f,       // /nn/ should not produce visible mouth actions
                [(int)OVRLipSync.Viseme.oh] = .9f,
                [(int)OVRLipSync.Viseme.ou] = .9f,
                [(int)OVRLipSync.Viseme.PP] = 0f,
                [(int)OVRLipSync.Viseme.RR] = .6f,
                [(int)OVRLipSync.Viseme.sil] = 0f,       // /sil/ also shouldn't
                [(int)OVRLipSync.Viseme.SS] = .2f,
                [(int)OVRLipSync.Viseme.TH] = .6f,
            };

            static readonly HashSet<int> DisabledFaces = new HashSet<int>()
            {
                (int) KKLips.Playful,
                (int) KKLips.Eating,
                (int) KKLips.Kiss,
                (int) KKLips.TongueOut,
                (int) KKLips.CatLike,
                (int) KKLips.Triangle,
                (int) KKLips.CartoonySmile,
            };

            /// <summary>
            /// Maps an OVR frame data output by OVR to KoiKatsu face
            /// </summary>
            /// <param name="frame">OVR Frame input</param>
            /// <param name="faceDict">KoiKatsu face blending dictionary output</param>
            /// <param name="openness">KoiKatsu mouth openness</param>
            private static void MapFrame(in OVRLipSync.Frame frame, ref Dictionary<int, float> faceDict, ref float openness)
            {
                // `openness` is calculated as the sum of all visemes multiplied by their value coefficients
                var newOpenness = 0f;

                // Face morphing is calculated as base face * (1-openness) + mapped face * openness,
                // clamped to a total sum of 1.
                // Hope this can generate a realistic enough face.

                // p.s. for some face types the lipsync morphing is not calculated.
                // They are:
                //  - Playful (20)
                //  - Eating (21)
                //  - Kiss (23)
                //  - TongueOut (24)
                //  - CatLike (37)
                //  - Triangle (38)
                //  - CartoonySmile (39)

                // Calculate the morphing needed for _this_ face status.
                var morphingCoeff = 1f;
                foreach (var faceId in DisabledFaces)
                    if (faceDict.TryGetValue(faceId, out float val))
                        morphingCoeff -= val;


                // I used a explicit for loop here because the index is needed
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    x = Mathf.Pow(x, 1.3f);
                    // If I didn't get it wrong, openness are clamped inside 0 and 100
                    newOpenness += x * VisemeOpennessCoeff[i];
                }
                newOpenness = Mathf.Clamp(newOpenness * 1.5f, 0f, 1f);


                // Rectify old face data
                morphingCoeff *= Mathf.Clamp(1f - newOpenness * 1.5f, 0, 1);
                foreach (var key in faceDict.Keys.ToList())
                    faceDict[key] *= morphingCoeff;

                // Add new face data
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    var faceId = VisemeKKFaceId[i];
                    if (faceDict.TryGetValue(faceId, out var val))
                    {
                        faceDict[faceId] = val + x * (1 - morphingCoeff);
                    }
                    else
                    {
                        faceDict[faceId] = x * (1 - morphingCoeff);
                    }
                }

                openness = newOpenness;
            }
        }
    }
}
