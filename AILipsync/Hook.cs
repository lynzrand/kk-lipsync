using System;
using UnityEngine;
using ADV.Commands.Chara;
using BepInEx;
using System.Linq;
using BepInEx.Logging;
using BepInEx.Harmony;
using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using AIChara;



namespace AILipsync
{
    [BepInPlugin(Guid, PluginName, PluginVersion)]
    public class LipsyncPlugin : BaseUnityPlugin
    {
        const string Guid = "me.rynco.ai-lipsync";
        const string PluginName = "AILipsync";
        const string PluginVersion = "0.1.2";

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
                if (__instance.asVoice != null && __instance.asVoice.isPlaying && voice != null)
                {
                    var frame = voice.GetLipData(__instance.asVoice);
                    //! This method relies on the fact that GetHashCode() is _not_ overridden.
                    // Thus it returns the same value for every run, and we can safely use this value 
                    // to separate between different objects
                    LipsyncConfig.Instance.frameStore[__instance.fbsCtrl.MouthCtrl.GetHashCode()] = frame;
                    LipsyncConfig.Instance.cleaned = false;
                }
                else if (__instance.asVoice != null && !__instance.asVoice.isPlaying)
                {
                    LipsyncConfig.Instance.frameStore.Remove(__instance.fbsCtrl.MouthCtrl.GetHashCode());
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
            [HarmonyPatch(typeof(ChaControl), "InitializeControlFaceAll")]
            [HarmonyPostfix]
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
                [(int)OVRLipSync.Viseme.aa] = (int)AILips.A,
                [(int)OVRLipSync.Viseme.CH] = (int)AILips.I,
                [(int)OVRLipSync.Viseme.DD] = (int)AILips.I,
                [(int)OVRLipSync.Viseme.E] = (int)AILips.E,
                [(int)OVRLipSync.Viseme.FF] = (int)AILips.Kiss,
                [(int)OVRLipSync.Viseme.ih] = (int)AILips.I,
                [(int)OVRLipSync.Viseme.kk] = (int)AILips.Hate,
                [(int)OVRLipSync.Viseme.nn] = (int)AILips.N,
                [(int)OVRLipSync.Viseme.oh] = (int)AILips.O,
                [(int)OVRLipSync.Viseme.ou] = (int)AILips.O,
                [(int)OVRLipSync.Viseme.PP] = (int)AILips.N,
                [(int)OVRLipSync.Viseme.RR] = (int)AILips.A,
                // sil does nothing. It has contribution set to 0 below.
                [(int)OVRLipSync.Viseme.sil] = (int)AILips.Default,
                [(int)OVRLipSync.Viseme.SS] = (int)AILips.SmileBroad,

                // /th/ is not seen in faces
                [(int)OVRLipSync.Viseme.TH] = (int)AILips.I,
            };

            static readonly Dictionary<int, float> VisemeBlendCoeff = new Dictionary<int, float>()
            {
                [(int)OVRLipSync.Viseme.aa] = 1f,
                [(int)OVRLipSync.Viseme.CH] = 1f,
                [(int)OVRLipSync.Viseme.DD] = .3f,
                [(int)OVRLipSync.Viseme.E] = 1f,
                [(int)OVRLipSync.Viseme.FF] = .6f,
                [(int)OVRLipSync.Viseme.ih] = 1f,
                [(int)OVRLipSync.Viseme.kk] = .8f,
                [(int)OVRLipSync.Viseme.nn] = 1f,
                [(int)OVRLipSync.Viseme.oh] = 1f,
                [(int)OVRLipSync.Viseme.ou] = .9f,
                [(int)OVRLipSync.Viseme.PP] = 1f,
                [(int)OVRLipSync.Viseme.RR] = .6f,
                [(int)OVRLipSync.Viseme.sil] = 0f,
                [(int)OVRLipSync.Viseme.SS] = .2f,
                [(int)OVRLipSync.Viseme.TH] = .6f,
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
                [(int)OVRLipSync.Viseme.E] = .8f,
                [(int)OVRLipSync.Viseme.FF] = .2f,
                [(int)OVRLipSync.Viseme.ih] = 1.5f,
                [(int)OVRLipSync.Viseme.kk] = .8f,
                [(int)OVRLipSync.Viseme.nn] = 0f,       // /nn/ should not produce visible mouth actions
                [(int)OVRLipSync.Viseme.oh] = .7f,
                [(int)OVRLipSync.Viseme.ou] = .9f,
                [(int)OVRLipSync.Viseme.PP] = 0f,
                [(int)OVRLipSync.Viseme.RR] = .6f,
                [(int)OVRLipSync.Viseme.sil] = 0f,       // /sil/ also shouldn't
                [(int)OVRLipSync.Viseme.SS] = .2f,
                [(int)OVRLipSync.Viseme.TH] = .6f,
            };

            static readonly HashSet<int> DisabledFaces = new HashSet<int>()
            {
                (int) AILips.LickLeftCorner,
                (int) AILips.OpenMouthSlight,
                (int) AILips.OpenMouthLarge,
                (int) AILips.Kiss,
                (int) AILips.OpenMouthExtraLarge,
            };

            private static List<int> scratchpad = new List<int>();
            /// <summary>
            /// Maps an OVR frame data output by OVR to AI Girl face
            /// </summary>
            /// <param name="frame">OVR Frame input</param>
            /// <param name="faceDict">KoiKatsu face blending dictionary output</param>
            /// <param name="openness">KoiKatsu mouth openness</param>
            private static void MapFrame(in OVRLipSync.Frame frame, ref Dictionary<int, float> faceDict, ref float openness)
            {
                // `openness` is calculated as the sum of all visemes multiplied by their value coefficients. Because the vowel faces has no morphing when changing openness, this value only effects the base face.
                var newOpenness = 0f;

                // Face morphing is calculated as base face * (1-openness) + mapped face * openness,
                // clamped to a total sum of 1.
                // Hope this can generate a realistic enough face.

                // Calculate the morphing needed for _this_ face status.
                var morphingCoeff = 1f;
                foreach (var faceId in DisabledFaces)
                    if (faceDict.TryGetValue(faceId, out float val))
                        morphingCoeff -= val;

                // AI Shoujou's vowels does not morph when openness changes
                // but we calculate these values anyway
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    x = Mathf.Pow(x, 1.2f);
                    newOpenness += x * VisemeOpennessCoeff[i];
                }
                {
                    var laughingAmount = Mathf.Pow(frame.laughterScore, 1.7f);
                    newOpenness += laughingAmount;
                }
                newOpenness = Mathf.Clamp(newOpenness, 0f, 1.2f);


                // Rectify old face data
                morphingCoeff *= Mathf.Clamp(1f - newOpenness * 1.5f, 0, 1);
                {
                    scratchpad.AddRange(faceDict.Keys);
                    foreach (var key in scratchpad)
                        faceDict[key] *= (1f - morphingCoeff);
                    scratchpad.Clear();
                }


                // Add new face data
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    x = Mathf.Clamp(Mathf.Pow(x, 1.2f), 0f, 1.1f);
                    var faceId = VisemeKKFaceId[i];
                    if (faceDict.TryGetValue(faceId, out var val))
                    {
                        faceDict[faceId] = val + x * morphingCoeff * VisemeBlendCoeff[i];
                    }
                    else
                    {
                        faceDict[faceId] = x * morphingCoeff * VisemeBlendCoeff[i];
                    }
                }
                {
                    const int laughId = (int)AILips.Smile;
                    if (faceDict.TryGetValue(laughId, out var val))
                    {
                        faceDict[laughId] = val + frame.laughterScore * (1 - morphingCoeff);
                    }
                    else
                    {
                        faceDict[laughId] = frame.laughterScore * (1 - morphingCoeff);
                    }
                }

                {
                    var sum = faceDict.Sum(val => val.Value);
                    if (sum > 1)
                    {
                        scratchpad.AddRange(faceDict.Keys);
                        foreach (var key in scratchpad)
                            faceDict[key] /= sum;
                        scratchpad.Clear();
                    }
                }

                openness = newOpenness;

            }
        }
    }

}
