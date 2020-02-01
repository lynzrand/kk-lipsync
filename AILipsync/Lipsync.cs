using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.Linq;

namespace AILipsync
{
    /// <summary>
    /// This class replaces <c>ChaControl.fbsaaVoice</c>.
    /// </summary>
    class LipDataCreator : FBSAssist.AudioAssist
    {
        private readonly int characterId;
        public const int bufferSize = 1024;
        public float[] audioBuffer = new float[bufferSize];

        private readonly uint contextId;

        public LipDataCreator(int characterId)
        {
            this.contextId = (uint)characterId;
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

        ~LipDataCreator()
        {
            OVRLipSync.DestroyContext(contextId);
        }
    }
}