using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "VideoInstructionTextLibrary", menuName = "XR Drone/Video Instruction Text Library")]
public class VideoInstructionTextLibrary : ScriptableObject
{
    [Serializable]
    public class InstructionEntry
    {
        public VideoPanelPlayer.VideoOption option;
        public VideoClip clip;
        public string header;
        [TextArea(2, 6)] public string body;
    }

    [SerializeField] private List<InstructionEntry> entries = new List<InstructionEntry>();

    public bool TryGet(VideoPanelPlayer.VideoOption option, out VideoClip clip, out string header, out string body)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            InstructionEntry entry = entries[i];
            if (entry != null && entry.option == option)
            {
                clip = entry.clip;
                header = entry.header;
                body = entry.body;
                return true;
            }
        }

        clip = null;
        header = string.Empty;
        body = string.Empty;
        return false;
    }
}
