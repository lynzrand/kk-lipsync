using System;
using System.Collections.Generic;
using System.Text;
using Karenia.LipsyncCore;
using UnityEngine;

namespace Karenia.Hs2Lipsync
{
    class LipDataCreatorHack : FBSAssist.AudioAssist
    {
        LipDataCreator creator;

        public LipDataCreatorHack(int charaId)
        {
            this.creator = new LipDataCreator(charaId);
        }

        public OVRLipSync.Frame GetLipData(AudioSource src)
        {
            return this.creator.GetLipData(src);
        }


    }
}
