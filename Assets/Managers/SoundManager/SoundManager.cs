using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

public class SoundManager : MonoBehaviour
{
    #region Singleton

    //
    [field: SerializeField] public bool DontDestroy { get; set; } = true;

    //
    private static SoundManager _instance;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();

                if (_instance == null)
                {
                    var singletonObject = new GameObject($"Singleton - {nameof(SoundManager)}");
                    _instance = singletonObject.AddComponent<SoundManager>();
                }
            }

            return _instance;
        }
    }

    //
    private void CheckSingleton()
    {
        if (_instance == null)
        {
            _instance = this;
            if (DontDestroy) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"Another instance of {nameof(SoundManager)} is already exist! Destroying self...");
            Destroy(gameObject);
        }
    }

    #endregion

    #region Sound pooler

    //
    [field: SerializeField] public int DefaultSoundCount { get; set; } = 5;

    //
    public Queue<Sound> SoundPooler { get; private set; }

    //
    private void CheckPooler()
    {
        SoundPooler = new Queue<Sound>();

        for (var i = 0; i < DefaultSoundCount; i++)
        {
            var sound = CreateSound();
            SoundPooler.Enqueue(sound);
        }
    }

    //
    private Sound CreateSound()
    {
        var newSound = new GameObject("Sound").AddComponent<Sound>();
        newSound.transform.SetParent(transform);
        newSound.gameObject.SetActive(false);
        return newSound;
    }

    //
    private Sound GetSound()
    {
        if (SoundPooler.Count == 0)
        {
            var newSound = CreateSound();
            SoundPooler.Enqueue(newSound);
        }
        var soundGet = SoundPooler.Dequeue();
        soundGet.gameObject.SetActive(true);
        return soundGet;
    }

    //
    private void ReleaseSound(Sound sound)
    {
        sound.Name = string.Empty;
        sound.gameObject.SetActive(false);
        SoundPooler.Enqueue(sound);
        AllSoundPlaying.Remove(sound);
        if (sound.SoundConfig.Track == TrackHelper.Effect) _countSoundEffect--;
    }

    #endregion

    #region Variables

    //
    [field: SerializeField]
    public List<SoundConfig> ConfigSounds = new List<SoundConfig>();

    //
    public static float BackgroundPref
    {
        get => PlayerPrefs.GetFloat("BackgroundPref", 1f);
        set => PlayerPrefs.SetFloat("BackgroundPref", value);
    }
    public static float EffectPref
    {
        get => PlayerPrefs.GetFloat("EffectPref", 1f);
        set => PlayerPrefs.SetFloat("EffectPref", value);
    }
    public static float UInterfacePref
    {
        get => PlayerPrefs.GetFloat("UInterfacePref", 1f);
        set => PlayerPrefs.SetFloat("UInterfacePref", value);
    }

    //
    public static Action<Sound> OnCompleteSound = sender => { };

    //
    public List<Sound> AllSoundPlaying { get; private set; }
    public List<Sound> AllSoundPause { get; private set; }

    //
    private static int _countSoundEffect;

    //
    private void CheckVariables()
    {
        AllSoundPlaying = new List<Sound>();
        AllSoundPause = new List<Sound>();
        if (!PlayerPrefs.HasKey("BackgroundPref")) PlayerPrefs.SetFloat("BackgroundPref", 1f);
        if (!PlayerPrefs.HasKey("EffectPref")) PlayerPrefs.SetFloat("EffectPref", 1f);
        if (!PlayerPrefs.HasKey("UInterfacePref")) PlayerPrefs.SetFloat("UInterfacePref", 1f);
        OnCompleteSound += ReleaseSound;
    }

    #endregion

    #region Unity callback functions

    //
    private void Awake()
    {
        CheckSingleton();
        CheckPooler();
        CheckVariables();
    }

    #endregion

    #region Volume functions

    //
    public static void Volume(float volume, TrackHelper trackCompare)
    {
        volume = Mathf.Clamp01(volume);
        switch (trackCompare)
        {
            case TrackHelper.Background:
                BackgroundPref = volume;
                break;
            case TrackHelper.Effect:
                EffectPref = volume;
                break;
            case TrackHelper.UInterface:
                UInterfacePref = volume;
                break;
        }

        InternalVolume(volume, trackCompare);
    }

    //
    public static void Mute(bool mute, TrackHelper trackCompare)
    {
        switch (trackCompare)
        {
            case TrackHelper.Background:
                BackgroundPref = mute ? 0f : 1f;
                break;
            case TrackHelper.Effect:
                EffectPref = mute ? 0f : 1f;
                break;
            case TrackHelper.UInterface:
                UInterfacePref = mute ? 0f : 1f;
                break;
        }

        InternalVolume(mute ? 0f : 1f, trackCompare);
    }

    //
    private static float GetVolume(TrackHelper track)
    {
        switch (track)
        {
            case TrackHelper.Background:
                return BackgroundPref;
            case TrackHelper.Effect:
                return EffectPref;
            case TrackHelper.UInterface:
                return UInterfacePref;
        }

        return 0f;
    }

    //
    private static void InternalVolume(float volume, TrackHelper trackCompare)
    {
        volume = Mathf.Clamp01(volume);
        Instance.AllSoundPlaying.ForEach(sound =>
        {
            if (string.Equals(sound.SoundConfig.Track.ToString(), trackCompare.ToString()))
            {
                sound.CurrentSystemVolume = volume;
                sound.SetVolume(volume);
            }
        });
        Instance.AllSoundPause.ForEach(sound =>
        {
            if (string.Equals(sound.SoundConfig.Track.ToString(), trackCompare.ToString()))
            {
                sound.CurrentSystemVolume = volume;
                sound.SetVolume(volume);
            }
        });
    }


    //
    private static void InternalVolume(float volume, TrackHelper trackCompare, bool stopWhenMute)
    {
        volume = Mathf.Clamp01(volume);
        Instance.AllSoundPlaying.ForEach(sound =>
        {
            if (string.Equals(sound.SoundConfig.Track.ToString(), trackCompare.ToString()))
            {
                sound.CurrentSystemVolume = volume;
                sound.SetVolume(volume);
            }
        });
        Instance.AllSoundPause.ForEach(sound =>
        {
            if (string.Equals(sound.SoundConfig.Track.ToString(), trackCompare.ToString()))
            {
                sound.CurrentSystemVolume = volume;
                sound.SetVolume(volume);
            }
        });
        for (int i = 0; i < Instance.AllSoundPlaying.Count; i++)
        {
            if (stopWhenMute)
            {
                Instance.AllSoundPlaying[i].Stop();
            }
        }
    }

    #endregion

    #region Sound functions

    //
    public static void Play(string name)
    {
        var playingConfigSound = GetConfigSound(name);
        if (playingConfigSound == null)
        {
            Debug.LogWarning($"Wrong name, there's none: {name} Sound.");
            return;
        }
        if (playingConfigSound.Track == TrackHelper.Effect)
        {
            if (_countSoundEffect >= 60)
            {
                return;
            }
            else
            {
                _countSoundEffect++;
            }
        }

        var currentVolume = GetVolume(playingConfigSound.Track);
        if (currentVolume == 0 && playingConfigSound.Track == TrackHelper.Effect) { return; }
        var sunSound = Instance.GetSound();
        sunSound.Initialize(playingConfigSound, currentVolume);
        sunSound.Play();
        Instance.AllSoundPlaying.Add(sunSound);
        Volume(currentVolume, playingConfigSound.Track);
    }
  

    //
    public static void PlayContinue(string name)
    {
        var playingSound = Instance.AllSoundPause.Find(o => o.Name == name);
        if (playingSound != null)
        {
            playingSound.Play();
            Instance.AllSoundPause.Remove(playingSound);
            Instance.AllSoundPlaying.Add(playingSound);
        }
        else
        {
            Play(name);
        }
    }

    //
    public static void Pause(string name)
    {
        var playingSound = Instance.AllSoundPlaying.Find(o => o.Name == name);
        if (playingSound == null)
        {
            Debug.LogWarning($"Wrong name, there's none: {name} Sound.");
            return;
        }

        playingSound.Pause();
        Instance.AllSoundPause.Add(playingSound);
        Instance.AllSoundPlaying.Remove(playingSound);
    }

    //
    public static void Stop(string name)
    {
        var playingSound = Instance.AllSoundPlaying.Find(o => o.Name == name);
        if (playingSound == null)
        {
            Debug.LogWarning($"Wrong name, there's none: {name} Sound.");
            return;
        }

        playingSound.Stop();
        Instance.AllSoundPlaying.Remove(playingSound);
    }

    //
    public static bool IsPlayingSound(string name)
    {
        var result = Instance.AllSoundPlaying.Find(o => o.Name.Contains(name));
        if (result == null) return false;
        return true;
    }

    //
    private static SoundConfig GetConfigSound(string name)
    {
        var soundsFound = new List<SoundConfig>();
        foreach (var configSound in Instance.ConfigSounds)
        {
            if (string.Equals(configSound.Name, name))
                soundsFound.Add(configSound);
        }

        if (soundsFound.Count == 1) return soundsFound[0];
        if (soundsFound.Count > 1) return soundsFound[UnityEngine.Random.Range(0, soundsFound.Count)];
        return null;
    }

    #endregion

    /*    [ContextMenu("asd")]
        public void asd()
        {
            foreach (var sound in ConfigSounds) {
                sound.Volume = .6f;
            }
        }*/
}