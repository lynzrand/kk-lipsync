using UnityEngine;
using System;
using BepInEx.Logging;
using System.Collections.Generic;

namespace KKLipsync
{
    public class LipsyncConfig
    {
        private LipsyncConfig()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("LipSync");
            this.frameStore = new Dictionary<int, OVRLipSync.Frame>();
            this.activeFrames = new HashSet<int>();
            this.inactiveFrames = new List<int>();
        }

        public ManualLogSource logger;

        public bool enabled;

        /// <summary>
        /// Storage of frames, numbered by character ID
        /// </summary>
        public Dictionary<int, OVRLipSync.Frame> frameStore;

        public HashSet<int> activeFrames;
        public List<int> inactiveFrames;
        public bool cleaned = true;


        private static LipsyncConfig? _instance;
        public static LipsyncConfig Instance { get => _instance is null ? (_instance = new LipsyncConfig()) : _instance; }
    }
}
