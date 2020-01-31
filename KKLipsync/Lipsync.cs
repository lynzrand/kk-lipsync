using System;
using System.Collections.Generic;
using System.Text;
using WavInfoControl;
using UnityEngine;
using KKAPI;
using HarmonyLib;
using System.Linq;

namespace KKLipsync
{
    /// <summary>
    /// Data structure for lip data construction.
    /// <para>
    /// Instead of a single value (mouth open-ness) in the original game, a struct of 
    /// </para>
    /// </summary>
    [Serializable]
    struct LipData
    {
        public int before, after;
        public float animationProgress;
        public float openPercent;
        public float originOpenPercent;
    }

    // we don't need this class for now
    //class LipsyncController : KKAPI.Chara.CharaCustomFunctionController
    //{

    //    Coroutine c = null;

    //    void AnalyzeMouth()
    //    {
    //        Dictionary<int, float> val = (Dictionary<int, float>)(typeof(FBSCtrlMouth).GetField("dictNowFace").GetValue(ChaControl.mouthCtrl));

    //        foreach (var kvp in val)
    //        {
    //            print(kvp);
    //        }
    //    }


    //    protected override void Update()
    //    {
    //        base.Update();
    //        if (Input.GetKeyDown(KeyCode.J)) AnalyzeMouth();
    //    }

    //    protected override void OnCardBeingSaved(GameMode currentGameMode)
    //    {
    //        throw new NotImplementedException();
    //    }
    //    protected override void Awake()
    //    {
    //        base.Awake();
    //    }
    //}

    /// <summary>
    /// This class replaces <c>FaceBlendShape.MouthCtrl</c>.
    /// </summary>
    [Serializable]
    class LipsyncMouthController : FBSCtrlMouth
    {
        /// <summary>
        /// This method transfers data and replaces the original mouth controller with
        /// this one.
        /// </summary>
        /// <param name="_base"></param>
        public LipsyncMouthController(FBSCtrlMouth _base)
        {
            var trav = Traverse.Create(_base);
            var trav_this = Traverse.Create(this);
            trav.Fields().ForEach((field) =>
            {
                trav_this.Field(field).SetValue(trav.Field(field).GetValue(_base));
            });

            // properties should already be synced with their backing fields?
            //trav.Properties().ForEach((field) =>
            //{
            //    trav_this.Field(field).SetValue(trav.Field(field).GetValue());
            //});

            this.Init();
        }

        public new void Init()
        {
            base.Init();
        }
        public LipsyncMouthController()
        {
            // TODO: This is not used, -10023 is a placeholder
            creator = new LipDataCreator(-10023);
        }

        public LipData LipData { get; set; }
        public LipDataCreator creator { get; set; }

        public void CalculateBlendShapeNew()
        {
            // TODO: Calculate blend shape according to lip data

            foreach (var fbsTarget in this.FBSTarget)
            {
                // TODO: do the blending
            }
        }

    }

    /// <summary>
    /// This class replaces <c>ChaControl.fbsCtrl</c>
    /// </summary>
    class LipsyncFaceBlender : FaceBlendShape
    {

    }

    public struct PeakInfo
    {
        public float intensity;
        public int id;

        public override string ToString()
        {
            return $"{{{id}, {intensity}}}";
        }
    }

    /// <summary>
    /// This class replaces <c>ChaControl.fbsaaVoice</c>.
    /// </summary>
    class LipDataCreator : FBSAssist.AudioAssist
    {
        private int characterId;
        public const int bufferSize = 512;
        public float[] audioBuffer = new float[bufferSize];
        public double[] doubleBuffer = new double[bufferSize];

        private uint contextId = 299;
        //public int verticalScale = 500;
        //MathNet.Filtering.OnlineFilter filter;

        public LipDataCreator(int characterId)
        {
            //filter = MathNet.Filtering.OnlineFilter.CreateLowpass(MathNet.Filtering.ImpulseResponse.Infinite, 1, 0.02);

            OVRLipSync.Initialize(sampleRate, bufferSize);

            var ctx_result = OVRLipSync.CreateContext(ref contextId, OVRLipSync.ContextProviders.Enhanced_with_Laughter, sampleRate, true);

            if (ctx_result != 0) LipsyncConfig.Instance.logger.LogError($"Failed to create context: {contextId}");

            this.characterId = characterId;
        }

        public int sampleRate { get => UnityEngine.AudioSettings.outputSampleRate; }
        public float hzPerBin { get => (float)sampleRate / 2 / bufferSize; }

        public bool isPlaying = false;

        public OVRLipSync.Frame GetLipData(AudioSource src)
        {
            if (src == null) return new OVRLipSync.Frame();
            src.GetOutputData(audioBuffer, 0);
            isPlaying = true;
            //doubleBuffer = Array.ConvertAll(spectrumBuffer, x => (double)x);

            var framedata = new OVRLipSync.Frame();

            OVRLipSync.ProcessFrame(contextId, audioBuffer, framedata, false);

            return framedata;
        }

        string ListToString<T>(IList<T> list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            foreach (var i in list)
            {
                sb.AppendLine($"  {i},");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        string ListToString(IList<float> list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            foreach (var i in list)
            {
                sb.AppendLine($"  {i.ToString("0.0000000")},");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }


        static float threshold = 0.005f;

        public static List<PeakInfo> findPeaks(float[] buf, int numPeaks = 12)
        {
            var peaks = new List<PeakInfo>();
            // delete ultra-low items
            for (int i = 5; i < buf.Length / 2; i++)
            {
                if (buf[i] > buf[i - 1] && buf[i] > buf[i + 1] && buf[i] > threshold)
                {
                    peaks.Add(new PeakInfo() { id = i, intensity = buf[i] });
                    peaks.Sort((a, b) => a.intensity > b.intensity ? -1 : a.intensity < b.intensity ? 1 : 0);
                    if (peaks.Count > numPeaks)
                    {
                        peaks.RemoveAt(peaks.Count - 1);
                    }
                }
            }
            //peaks.Sort((a, b) => a.id - b.id);
            return peaks;
        }

        public float GetLegacyAudioWaveValue(AudioSource src, float correct = 2)
        {
            this.GetLipData(src);
            if (src == null) return 0;
            return this.GetAudioWaveValue(src, correct);
        }

    }

    class LipsyncDebugGui : MonoBehaviour
    {
        public LipDataCreator? audioAssist;
        public LipsyncDebugGui()
        {
            for (int i = 0; i < graphWidth * graphHeight; i++) graphBuffer[i] = new Color(0, 0, 0, 1);
        }

        const int graphWidth = 512;
        const int graphHeight = 512;
        private Texture2D graph = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);

        readonly Color[] graphBuffer = new Color[graphWidth * graphHeight];

        float yScale = 140f / 512;
        float yOffset = 4f;
        const float xScale = (float)graphWidth / LipDataCreator.bufferSize;

        List<PeakInfo> peaks = new List<PeakInfo>();

        public static void DrawLine(Texture2D tex, Vector2 p1, Vector2 p2, Color col)
        {
            Vector2 t = p1;
            float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
            float ctr = 0;

            while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
            {
                t = Vector2.Lerp(p1, p2, ctr);
                ctr += frac;
                tex.SetPixel((int)t.x, (int)t.y, col);
            }
        }

        private Color intensityToColor(float intensity)
        {
            var val = Mathf.Clamp((Mathf.Log10(intensity) + yOffset) * yScale, 0, 1);
            return new Color(val, val, val);
        }

        private void DrawSpectrogram()
        {
            if (audioAssist is null) return;
            //graph.SetPixels(reset);
            for (int i = 0; i < graphHeight - 1; i++)
            {
                for (int j = 0; j < graphWidth; j++)
                {
                    graphBuffer[i * graphWidth + j] = graphBuffer[(i + 1) * graphWidth + j];
                }
            }
            for (int i = 1; i < LipDataCreator.bufferSize; i++)
            {
                var val = audioAssist.audioBuffer[i];
                graphBuffer[graphWidth * (graphHeight - 1) + Mathf.FloorToInt(i * xScale)] = intensityToColor(val);
            }
            peaks = LipDataCreator.findPeaks(audioAssist.audioBuffer, 7);
            //LipsyncConfig.Instance.logger.LogInfo(peaks[0]);
            for (int i = 0; i < peaks.Count; i++)
            {
                var item = peaks[i];
                graphBuffer[graphWidth * (graphHeight - 1) + Mathf.FloorToInt(item.id * xScale)] = new Color(.3f / i + .7f, .7f / i + .3f, 0);
            }
            graph.SetPixels(graphBuffer);
            graph.Apply();
        }

        private float intensityToYDIsplayCoord(float val)
        {
            return Mathf.Clamp((Mathf.Log10(val) + yOffset) * yScale, 0, graphHeight);
        }

        private static float binToXDisplayCoord(int i)
        {
            return Mathf.Clamp(Mathf.Log(i, 2) * 102.4f * xScale, 0, graphWidth);
        }

        Rect windowRect = new Rect(0, 0, 600, 800);

        public void OnGUI()
        {
            if (audioAssist != null && audioAssist.isPlaying)
                DrawSpectrogram();
            int windowId = 1000000;
            windowRect = GUILayout.Window(windowId, windowRect, (id) =>
            {
                //GUILayout.BeginArea(new Rect(0, 0, 600, 800));
                //GUILayout.Label("Spectrum");
                GUILayout.BeginVertical();
                GUILayout.Box(new GUIContent(graph));
                {
                    // y scale slider
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label($"Y scale: {yScale}");
                    yScale = GUILayout.HorizontalSlider(yScale, 0, 3, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
                {
                    // y offset slider
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label($"Y offset: {yOffset}");
                    yOffset = GUILayout.HorizontalSlider(yOffset, -1, 5, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
                {
                    GUILayout.Label("Peaks:");
                    foreach (var i in peaks)
                    {
                        GUILayout.Label($"{i.id * audioAssist?.hzPerBin}Hz: {i.intensity}");
                    }
                }
                GUILayout.EndVertical();
                //GUILayout.EndArea();
            }, "Spectrum");

            if (audioAssist != null)
                audioAssist.isPlaying = false;
        }
    }
}
