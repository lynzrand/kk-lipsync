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
            this.baseFaceStore = new Dictionary<int, Dictionary<int, float>>();
            this.lastFaceStore = new Dictionary<int, Dictionary<int, float>>();
            this.activeFrames = new HashSet<int>();
            this.inactiveFrames = new List<int>();
        }

        public ManualLogSource logger;

        public bool enabled = true;

        public float overdriveFactor = 1f;

        /// <summary>
        /// Storage of frames, numbered by character hash
        /// </summary>
        public Dictionary<int, OVRLipSync.Frame> frameStore;

        /// <summary>
        /// Storage of face base status, numbered by character hash
        /// </summary>
        public Dictionary<int, Dictionary<int, float>> baseFaceStore;

        /// <summary>
        /// Storage of character face's last status to keep track of which has changed, numbered by character hash
        /// </summary>
        public Dictionary<int, Dictionary<int, float>> lastFaceStore;

        public HashSet<int> activeFrames;
        public List<int> inactiveFrames;
        public bool cleaned = true;


        private static LipsyncConfig? _instance;
        public static LipsyncConfig Instance { get => _instance is null ? (_instance = new LipsyncConfig()) : _instance; }
    }
}
