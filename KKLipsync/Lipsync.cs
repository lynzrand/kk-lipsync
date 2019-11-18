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
            creator = new LipDataCreator();
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
        public const int bufferSize = 1024;
        public float[] spectrumBuffer = new float[bufferSize];

        //public int verticalScale = 500;

        public LipDataCreator()
        {

        }

        public int sampleRate { get => UnityEngine.AudioSettings.outputSampleRate; }
        public float hzPerBin { get => (float)sampleRate / 2 / bufferSize; }

        public LipData GetLipData(AudioSource src)
        {
            if (src == null) return new LipData();
            src.GetSpectrumData(spectrumBuffer, 0, FFTWindow.BlackmanHarris);
            //MathNet.Numerics.IntegralTransforms.Fourier.ForwardReal(spectrumBuffer, bufferSize - 2);
            //for (int i = 0; i < bufferSize / 3; i++)
            //{
            //    spectrumBuffer[i] = Mathf.Log(Mathf.Abs(spectrumBuffer[i]));
            //}
            //for (int i = bufferSize / 3; i < bufferSize; i++)
            //{
            //    spectrumBuffer[i] = 0;
            //}
            //MathNet.Numerics.IntegralTransforms.Fourier.InverseReal(spectrumBuffer, bufferSize - 2);
            //LipsyncConfig.Instance.logger.LogInfo(ListToString(findPeaks(spectrumBuffer)));
            CheckLogData(src);
            return new LipData();
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

        void CheckLogData(AudioSource src)
        {
            if (!Input.GetKeyDown(KeyCode.Comma)) return;

            LipsyncConfig.Instance.logger.LogInfo($"Cepstrum := {ListToString(spectrumBuffer)}");
            LipsyncConfig.Instance.logger.LogInfo($"Original := {ListToString(src.GetOutputData(1024, 0))}");

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
        public LipDataCreator reference;
        public LipsyncDebugGui()
        {
            for (int i = 0; i < graphWidth * graphHeight; i++) resetArray[i] = new Color(0, 0, 0, 1);
        }

        const int graphWidth = 512;
        const int graphHeight = 512;
        private Texture2D graph = new Texture2D(graphWidth, graphHeight, TextureFormat.RGBA32, false);

        readonly Color[] resetArray = new Color[graphWidth * graphHeight];

        float yScale = 70f;
        float yOffset = 4f;
        const float xScale = (float)graphWidth / LipDataCreator.bufferSize;

        List<PeakInfo> peaks;

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

        private void DrawTexture()
        {
            graph.SetPixels(resetArray);
            float val = 0, lastX = 0, lastY = 0;
            for (int i = 1; i < LipDataCreator.bufferSize; i++)
            {
                val = reference.spectrumBuffer[i];
                float xval = binToXDisplayCoord(i);
                float yval = intensityToYDIsplayCoord(val);

                DrawLine(graph, new Vector2(xval, yval), new Vector2(lastX, lastY), new Color(1, 1, 1, 1));
                //graph.SetPixel((int)xval, (int)yval, new Color(1, 1, 1, 1));


                lastX = xval;
                lastY = yval;
            }
            peaks = LipDataCreator.findPeaks(reference.spectrumBuffer, 7);
            //LipsyncConfig.Instance.logger.LogInfo(peaks[0]);
            for (int i = 0; i < peaks.Count; i++)
            {
                var item = peaks[i];
                var x = binToXDisplayCoord(item.id);
                var y = intensityToYDIsplayCoord(item.intensity);
                for (int j = 0; j < 10; j++)
                    graph.SetPixel((int)x, j, new Color(1f / i, 1f / i, 0));
            }
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

        public void OnGUI()
        {
            DrawTexture();
            int windowId = 1000000;
            GUILayout.Window(windowId, new Rect(0, 0, 600, 800), (id) =>
            {
                //GUILayout.BeginArea(new Rect(0, 0, 600, 800));
                //GUILayout.Label("Spectrum");
                GUILayout.BeginVertical();
                GUILayout.Box(new GUIContent(graph));
                {
                    // y scale slider
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label("Y scale");
                    yScale = GUILayout.HorizontalSlider(yScale, 1, 100, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
                {
                    // y offset slider
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                    GUILayout.Label("Y offset");
                    yOffset = GUILayout.HorizontalSlider(yOffset, -3, 10, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }
                {
                    GUILayout.Label("Peaks:");
                    foreach (var i in peaks)
                    {
                        GUILayout.Label($"{i.id * reference.hzPerBin}Hz: {i.intensity}");
                    }
                }
                GUILayout.EndVertical();
                //GUILayout.EndArea();
            }, "Spectrum");
        }   
    }
}
