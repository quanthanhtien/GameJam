using System;
using UnityEngine;

// ReSharper disable CheckNamespace



[Serializable]
public class SoundConfig
{
    //
    public static string ClickEft = "click";
    public static string SelectEft = "select";
    //
    [field: SerializeField] public string Name { get; set; }
    [field: SerializeField] public AudioClip Clip { get; set; }
    [field: SerializeField] public TrackHelper Track { get; set; }
    [field: SerializeField, Range(0f, 1f)] public float Volume { get; set; }
    [field: SerializeField, Range(0f, 3f)] public float Pitch { get; set; }
    [field: SerializeField] public bool Loop { get; set; }
    [field: SerializeField] public bool FadeIn { get; set; }
    [field: SerializeField] public float FadeInTime { get; set; }
    [field: SerializeField] public bool FadeOut { get; set; }
    [field: SerializeField] public float FadeOutTime { get; set; }

    //
    public SoundConfig(string sName, AudioClip sClip, TrackHelper sTrack, float sVolume, float sPitch, bool sLoop, bool sFadeIn, float sFadeInTime, bool sFadeOut, float sFadeOutTime)
    {
        Name = sName;
        Clip = sClip;
        Track = sTrack;
        Volume = sVolume;
        Pitch = sPitch;
        Loop = sLoop;
        FadeIn = sFadeIn;
        FadeInTime = sFadeInTime;
        FadeOut = sFadeOut;
        FadeOutTime = sFadeOutTime;
    }
}