using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace KKLipsync
{
    [BepInPlugin(Guid, PluginName, PluginVersion)]
    public class LipsyncPlugin : BaseUnityPlugin
    {
        const string Guid = "me.rynco.kk-lipsync";
        const string PluginName = "KKLipsync";
        const string PluginVersion = "0.1.3";

        private const string _enablePluginStr = "Enable plugin";


        public LipsyncPlugin()
        {
            Logger.Log(BepInEx.Logging.LogLevel.Info, $"Loaded {PluginName} {PluginVersion}");
            var harmony = new Harmony(Guid);

            harmony.PatchAll(typeof(Hooks.UpdateBlendShapeHook));
            harmony.PatchAll(typeof(Hooks.AssistHook));
            harmony.PatchAll(typeof(Hooks.BlendShapeHook));

            AddConfigs();
        }

        private void AddConfigs()
        {
            {
                var enabledEntry = Config.AddSetting(new ConfigDefinition("KKLipsync", _enablePluginStr), true);
                enabledEntry.SettingChanged += (sender, newEntry) =>
                {
                    LipsyncConfig.Instance.enabled = enabledEntry.Value;
                };
                enabledEntry.ConfigFile.ConfigReloaded += (sender, cfg) =>
                {
                    LipsyncConfig.Instance.enabled = enabledEntry.Value;
                };
            }
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
                var enabled = LipsyncConfig.Instance.enabled;
                if (!enabled) return;


                var voice = AccessTools.PropertyGetter(typeof(ChaControl), "fbsaaVoice").Invoke(__instance, new object[] { }) as LipDataCreator;
                AudioSource asVoice = __instance.asVoice;
                if (asVoice != null && asVoice.isPlaying && asVoice.time <= asVoice.clip.length && voice != null)
                {
                    if (!LipsyncConfig.Instance.frameStore.TryGetValue(__instance.fbsCtrl.MouthCtrl.GetHashCode(), out var frame))
                    {
                        frame = new OVRLipSync.Frame();
                    }
                    frame = voice.GetLipData(asVoice, frame);
                    //! This method relies on the fact that GetHashCode() is _not_ overridden.
                    // Thus it returns the same value for every run, and we can safely use this value 
                    // to separate between different objects
                    LipsyncConfig.Instance.frameStore[__instance.fbsCtrl.MouthCtrl.GetHashCode()] = frame;
                    LipsyncConfig.Instance.cleaned = false;
                }
                else if (asVoice == null || voice == null)
                {
                }
                else
                {
                    LipsyncConfig.Instance.frameStore.Remove(__instance.fbsCtrl.MouthCtrl.GetHashCode());

                }


                if (voice == null) LipsyncConfig.Instance.logger.LogWarning("LipDataCreator is null");

                return;
            }

            [HarmonyPatch(typeof(CameraControl), "LateUpdate")]
            [HarmonyPostfix]
            public static void FrameCleanup(CameraControl __instance)
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
                    instance.baseFaceStore.Remove(inactiveFrame);
                    instance.lastFaceStore.Remove(inactiveFrame);
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
            }

        }

        public static class BlendShapeHook
        {
            //static float progress = 0f;

            [HarmonyPatch(typeof(FBSCtrlMouth), "CalcBlend")]
            [HarmonyPrefix]
            public static bool NewCalcBlendShape(FBSBase __instance)
            {
                var enabled = LipsyncConfig.Instance.enabled;
                if (!enabled) return true;

                var nowFace = AccessTools.Field(typeof(FBSCtrlMouth), "dictNowFace").GetValue(__instance) as Dictionary<int, float>;

                if (nowFace is null) return true;

                LipsyncConfig cfg = LipsyncConfig.Instance;

                if (cfg.frameStore.TryGetValue(__instance.GetHashCode(), out var targetFrame))
                {

                    var openness = (float)AccessTools.Field(typeof(FBSCtrlMouth), "openRate").GetValue(__instance);

                    if (!cfg.baseFaceStore.TryGetValue(__instance.GetHashCode(), out var baseFace))
                    {
                        baseFace = new Dictionary<int, float>();
                        cfg.baseFaceStore[__instance.GetHashCode()] = baseFace;
                    }

                    if (!cfg.lastFaceStore.TryGetValue(__instance.GetHashCode(), out var lastFace))
                    {
                        lastFace = new Dictionary<int, float>();
                        cfg.lastFaceStore[__instance.GetHashCode()] = lastFace;
                    }

                    MapFrame(targetFrame, ref baseFace, ref lastFace, ref nowFace, ref openness);
                    AccessTools.Field(typeof(FBSCtrlMouth), "openRate").SetValue(__instance, openness);
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
                [(int)OVRLipSync.Viseme.aa] = (int)KKLips.BigA,
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
                [(int)OVRLipSync.Viseme.aa] = 1.1f,
                [(int)OVRLipSync.Viseme.CH] = 1.1f,
                [(int)OVRLipSync.Viseme.DD] = .2f,
                [(int)OVRLipSync.Viseme.E] = .8f,
                [(int)OVRLipSync.Viseme.FF] = .2f,
                [(int)OVRLipSync.Viseme.ih] = 2f,
                [(int)OVRLipSync.Viseme.kk] = 1.1f,
                [(int)OVRLipSync.Viseme.nn] = 0f,       // /nn/ should not produce visible mouth actions
                [(int)OVRLipSync.Viseme.oh] = .7f,
                [(int)OVRLipSync.Viseme.ou] = 1.1f,
                [(int)OVRLipSync.Viseme.PP] = 0f,
                [(int)OVRLipSync.Viseme.RR] = .6f,
                [(int)OVRLipSync.Viseme.sil] = 0f,       // /sil/ also shouldn't
                [(int)OVRLipSync.Viseme.SS] = .2f,
                [(int)OVRLipSync.Viseme.TH] = .6f,
            };


            /// <summary>
            /// Coeffecient of OVR visemes on face morphing.
            /// 
            /// <para>
            ///     Vowels always have a .9f contribution, while consonants contributions are 
            ///     based on their relationship with mouth actions.
            /// </para>
            /// </summary>
            static readonly Dictionary<int, float> VisemeOverdriveFactor = new Dictionary<int, float>()
            {
                [(int)OVRLipSync.Viseme.aa] = 1f,
                [(int)OVRLipSync.Viseme.CH] = 1f,
                [(int)OVRLipSync.Viseme.DD] = 1f,
                [(int)OVRLipSync.Viseme.E] = 1f,
                [(int)OVRLipSync.Viseme.FF] = .9f,
                [(int)OVRLipSync.Viseme.ih] = 2f,
                [(int)OVRLipSync.Viseme.kk] = 1f,
                [(int)OVRLipSync.Viseme.nn] = 0f,
                [(int)OVRLipSync.Viseme.oh] = 1f,
                [(int)OVRLipSync.Viseme.ou] = 2f,
                [(int)OVRLipSync.Viseme.PP] = 0f,
                [(int)OVRLipSync.Viseme.RR] = 1f,
                [(int)OVRLipSync.Viseme.sil] = 0f,
                [(int)OVRLipSync.Viseme.SS] = .8f,
                [(int)OVRLipSync.Viseme.TH] = 1f,
            };

            static readonly HashSet<int> DisabledFaces = new HashSet<int>()
            {
                (int) KKLips.Playful,
                // (int) KKLips.Eating,
                (int) KKLips.Kiss,
                (int) KKLips.TongueOut,
                (int) KKLips.HoldInMouth,
                (int) KKLips.Eating,
                (int) KKLips.CatLike,
                (int) KKLips.Triangle,
                (int) KKLips.CartoonySmile,
            };

            private static string PrettyPrintDictionary<TK, TV>(Dictionary<TK, TV> dict)
            {
                var sb = new StringBuilder();
                sb.Append("{");
                foreach (var kv in dict)
                {
                    sb.AppendFormat(" '{0}': {1}", kv.Key, kv.Value);
                }
                sb.AppendLine(" }");
                return sb.ToString();
            }

            private static bool CompareDictionary<TK, TV>(Dictionary<TK, TV> lhs, Dictionary<TK, TV> rhs)
                where TV : IEquatable<TV>
            {
                if (lhs.Count != rhs.Count) return false;
                foreach (var kv in lhs)
                {
                    if (rhs.TryGetValue(kv.Key, out var rv))
                    {
                        if (EqualityComparer<TV>.Default.Equals(kv.Value, rv)) continue;
                        else return false;
                    }
                    else return false;
                }
                return true;
            }

            private const int KKLipCount = 39;
            private static List<int> scratchpad = new List<int>();
            /// <summary>
            /// Maps an OVR frame data output by OVR to KoiKatsu face
            /// </summary>
            /// <param name="frame">OVR Frame input</param>
            /// <param name="nowFace">KoiKatsu face blending dictionary output</param>
            /// <param name="openness">KoiKatsu mouth openness</param>
            private static void MapFrame(
                in OVRLipSync.Frame frame,
                ref Dictionary<int, float> baseFace,
                ref Dictionary<int, float> lastFace,
                ref Dictionary<int, float> nowFace,
                ref float openness)
            {
                if (CompareDictionary(lastFace, nowFace))
                {
                    // There's no other code that changed this variable
                    nowFace.Clear();
                    foreach (var refItem in baseFace)
                    {
                        nowFace.Add(refItem.Key, refItem.Value);
                    }
                }
                else
                {
                    // Someone has updated this variable. Update to latest
                    baseFace.Clear();
                    foreach (var refItem in nowFace)
                    {
                        baseFace.Add(refItem.Key, refItem.Value);
                    }

#if DEBUG
                    if (nowFace.TryGetValue(23, out var val23) && val23 > 0.1) { LipsyncConfig.Instance.logger.LogMessage($"Kissing detected: {val23}"); }
                    LipsyncConfig.Instance.logger.LogInfo($"Detected update at {nowFace.GetHashCode()}:\nnew:{PrettyPrintDictionary(nowFace)}");
#endif
                }

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
                    if (nowFace.TryGetValue(faceId, out float val))
                        morphingCoeff -= val * morphingCoeff;


                // I used a explicit for loop here because the index is needed
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

                newOpenness = Mathf.Clamp(newOpenness * 1.5f, 0f, 1f);
                morphingCoeff *= Mathf.Clamp(newOpenness * 3f, 0f, .95f);

                // Rectify old face data
                {
                    scratchpad.AddRange(nowFace.Keys);
                    foreach (var key in scratchpad)
                        nowFace[key] *= (1 - morphingCoeff);
                    scratchpad.Clear();
                }
                // Add new face data
                for (var i = 0; i < frame.Visemes.Length; i++)
                {
                    var x = frame.Visemes[i];
                    x = Mathf.Clamp(Mathf.Pow(x * 1.5f, 1.2f), 0f, 1.1f);
                    var faceId = VisemeKKFaceId[i];
                    if (nowFace.TryGetValue(faceId, out var val))
                    {
                        nowFace[faceId] = val + x * morphingCoeff * VisemeOverdriveFactor[i];
                    }
                    else
                    {
                        nowFace[faceId] = x * morphingCoeff * VisemeOverdriveFactor[i];
                    }
                }
                {
                    const int laughId = (int)KKLips.HappyBroad;
                    if (nowFace.TryGetValue(laughId, out var val))
                    {
                        nowFace[laughId] = val + frame.laughterScore * morphingCoeff;
                    }
                    else
                    {
                        nowFace[laughId] = frame.laughterScore * morphingCoeff;
                    }
                }

                //{
                //    var sum = nowFace.Sum(val => val.Value);
                //    if (sum > 1.2)
                //    {
                //        scratchpad.AddRange(nowFace.Keys);
                //        foreach (var key in scratchpad)
                //            nowFace[key] /= sum / 1.2f;
                //        scratchpad.Clear();
                //    }
                //}

                openness = newOpenness * morphingCoeff + openness * (1 - morphingCoeff);

                {
                    // update lastFace to be the same as this face
                    lastFace.Clear();
                    foreach (var entry in nowFace)
                    {
                        lastFace.Add(entry.Key, entry.Value);
                    }

#if DEBUG
                    LipsyncConfig.Instance.logger.LogInfo($"Face updated:\nnew:{PrettyPrintDictionary(nowFace)}");
#endif
                }
            }
        }
    }
}
