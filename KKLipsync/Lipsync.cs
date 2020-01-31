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
        public const int bufferSize = 1024;
        public float[] audioBuffer = new float[bufferSize];

        private uint contextId = 299;

        public LipDataCreator(int characterId)
        {
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


        public float GetLegacyAudioWaveValue(AudioSource src, float correct = 2)
        {
            this.GetLipData(src);
            if (src == null) return 0;
            return this.GetAudioWaveValue(src, correct);
        }

    }
}