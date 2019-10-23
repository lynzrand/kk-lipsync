using System;
using System.Collections.Generic;
using System.Text;

using WavInfoControl;
using UnityEngine;
using KKAPI;

namespace KKLipsync
{
    /// <summary>
    /// Data structure for lip data construction.
    /// <para>
    /// Instead of a single value (mouth open-ness) in the original game, a struct of 
    /// </para>
    /// </summary>
    struct LipData
    {
        int before, after;
        float animationProgress;
        float openPercent;
        float originOpenPercent;
    }

    class LipsyncController : KKAPI.Chara.CharaCustomFunctionController
    {

        Coroutine c = null;

        void AnalyzeMouth()
        {
            Dictionary<int, float> val = (Dictionary<int, float>)(typeof(FBSCtrlMouth).GetField("dictNowFace").GetValue(ChaControl.mouthCtrl));

            foreach(var kvp in val)
            {
                print(kvp);
            }
        }


        protected override void Update()
        {
            base.Update();
            if (Input.GetKeyDown(KeyCode.J)) AnalyzeMouth();
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            throw new NotImplementedException();
        }
        protected override void Awake()
        {
            base.Awake();
        }
    }

    class LipDataCreator
    {
        float[] spectrumBuffer = new float[512];
        AudioSource src;
        ChaControl ctrl;

        public LipDataCreator(ChaControl ctrl, AudioSource src)
        {
            this.ctrl = ctrl;
            this.src = src;
        }

        public LipData GetLipData()
        {
            src.GetSpectrumData(spectrumBuffer, 0, FFTWindow.BlackmanHarris);
            // TODO: implement fft
            return new LipData();
        }
    }
}
