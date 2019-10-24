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
    class LipsyncFaceMorpher : FBSCtrlMouth
    {
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
    class LipsyncBlendedFace : FaceBlendShape
    {

    }

    /// <summary>
    /// This class replaces <c>ChaControl.fbsaaVoice</c>.
    /// </summary>
    class LipDataCreator: FBSAssist.AudioAssist
    {
        float[] spectrumBuffer = new float[512];
        

        public LipDataCreator()
        {
          
        }

        public LipData GetLipData(AudioSource src)
        {
            src.GetSpectrumData(spectrumBuffer, 0, FFTWindow.BlackmanHarris);
            // TODO: implement fft
            return new LipData();
        }
    }
}
