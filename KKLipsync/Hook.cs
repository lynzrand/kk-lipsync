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
        const string PluginVersion = "0.1.1";

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
                    //! This method relies on the fact that GetHashCode() is _not_ overridden.
                    // Thus it returns the same value for every run, and we can safely use this value 
                    // to separate between different objects
                    LipsyncConfig.Instance.frameStore[__instance.fbsCtrl.MouthCtrl.GetHashCode()] = frame;
                    LipsyncConfig.Instance.cleaned = false;
                }

                if (voice == null) LipsyncConfig.Instance.logger.LogWarning("LipDataCreator is null");

                return;
            }

            [HarmonyPatch(typeof(CameraControl), "LateUpdate")]
            [HarmonyPostfix]
            public static void FrameCleanup()
            {
                LipsyncConfig instance = LipsyncConfig.Instance;
                if (instance.cleaned) return;

                var inactiveFrames = instance.inactiveFrames;

                foreach (var hash in instance.frameStore.Keys)
                {
                    if (!instance.activeFrames.Contains(hash))
                    {
                        inactiveFrames.Add(hash);
                    }
                }
                foreach (var inactiveFrame in inactiveFrames)
                {
                    instance.frameStore.Remove(inactiveFrame);
                }

                // Cleanup
                instance.activeFrames.Clear();
                instance.cleaned = true;
                instance.inactiveFrames.Clear();
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
                var nowFace = AccessTools.Field(typeof(FBSCtrlMouth), "dictNowFace").GetValue(__instance) as Dictionary<int, float>;
                var openness = (float)AccessTools.Field(typeof(FBSCtrlMouth), "FixedRate").GetValue(__instance);
                if (nowFace is null) return true;

                if (LipsyncConfig.Instance.frameStore.TryGetValue(__instance.GetHashCode(), out var targetFrame))
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
                [(int)OVRLipSync.Viseme.aa] = (int)KKLips.SmallA,
                [(int)OVRLipSync.Viseme.CH] = (int)KKLips.SmallI,
                [(int)OVRLipSync.Viseme.DD] = (int)KKLips.Hate,
                [(int)OVRLipSync.Viseme.E] = (int)KKLips.BigE,
                [(int)OVRLipSync.Viseme.FF] = (int)KKLips.BigN,
                [(int)OVRLipSync.Viseme.ih] = (int)KKLips.BigI,
                [(int)OVRLipSync.Viseme.kk] = (int)KKLips.SmallE,
                [(int)OVRLipSync.Viseme.nn] = (int)KKLips.BigN,
                [(int)OVRLipSync.Viseme.oh] = (int)KKLips.SmallA,
                [(int)OVRLipSync.Viseme.ou] = (int)KKLips.BigO,
                [(int)OVRLipSync.Viseme.PP] = (int)KKLips.SmallN,
                [(int)OVRLipSync.Viseme.RR] = (int)KKLips.SmallE,
                // sil does nothing. It has contribution set to 0 below.
                [(int)OVRLipSync.Viseme.sil] = (int)KKLips.Default,
                [(int)OVRLipSync.Viseme.SS] = (int)KKLips.SmallI,

                // /th/ is not seen in faces
                [(int)OVRLipSync.Viseme.TH] = (int)KKLips.Hate,
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
                [(int)OVRLipSync.Viseme.E] = 1f,
                [(int)OVRLipSync.Viseme.FF] = .2f,
                [(int)OVRLipSync.Viseme.ih] = 1.5f,
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
                    x = Mathf.Pow(x, 1.2f);
                    // If I didn't get it wrong, openness are clamped inside 0 and 100
                    newOpenness += x * VisemeOpennessCoeff[i];
                }
                {
                    var laughingAmount = Mathf.Pow(frame.laughterScore, 1.2f);
                    newOpenness += laughingAmount;
                }
                newOpenness = Mathf.Clamp(newOpenness * 3f, 0f, 1.2f);


                // Rectify old face data
                morphingCoeff *= Mathf.Clamp(1f - newOpenness * 1.5f, 0, 1);
                foreach (var key in faceDict.Keys.ToList())
                    faceDict[key] *= morphingCoeff;

                // Add new face data
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    x = Mathf.Clamp(Mathf.Pow(x * 1.5f, 1.2f), 0f, 1.1f);
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
                {
                    const int laughId = (int)KKLips.HappyBroad;
                    if (faceDict.TryGetValue(laughId, out var val))
                    {
                        faceDict[laughId] = val + frame.laughterScore * (1 - morphingCoeff);
                    }
                    else
                    {
                        faceDict[laughId] = frame.laughterScore  * (1 - morphingCoeff);
                    }
                }

                

                openness = newOpenness;
            }
        }
    }
}
