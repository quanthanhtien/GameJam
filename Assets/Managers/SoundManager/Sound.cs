using DG.Tweening;
using UnityEngine;

// ReSharper disable CheckNamespace

public class Sound : MonoBehaviour
{
    #region Variables

    //
    public string Name { get; set; }
    public AudioSource AudioSource { get; set; }
    public SoundConfig SoundConfig { get; set; }
    public float CurrentSystemVolume { get; set; }
    public bool OnFadeVolume { get; set; }

    //
    private Coroutine _coroutineEndClip;
    private Coroutine _coroutineFadeoutEndClip;
    private Tweener _tweenFadeVolume;

    #endregion

    #region Initialize

    //
    public void Initialize(SoundConfig soundConfig, float currentSystemVolume)
    {
        Name = soundConfig.Name;
        SoundConfig = soundConfig;
        AudioSource = gameObject.GetOrAddComponent<AudioSource>();
        AudioSource.playOnAwake = false;
        AudioSource.clip = soundConfig.Clip;
        AudioSource.volume = soundConfig.Volume;
        AudioSource.pitch = soundConfig.Pitch;
        AudioSource.loop = soundConfig.Loop;
        CurrentSystemVolume = currentSystemVolume;
        OnFadeVolume = false;
    }

    #endregion

    #region Control functions

    //
    public void Play()
    {
        if (IsPlaying() || OnFadeVolume) return;

        // Play audio
        if (SoundConfig.FadeIn)
        {
            _tweenFadeVolume?.Kill();
            _tweenFadeVolume = SoundHelper.AddDOTween(SoundConfig.FadeInTime, process =>
            {
                SetVolume(Mathf.Lerp(0f, CurrentSystemVolume, process));
            }).OnStart(() =>
            {
                SetVolume(0f);
                AudioSource.Play();
                OnFadeVolume = true;
            }).OnComplete(() =>
            {
                OnFadeVolume = false;
            }).SetEase(Ease.Linear);
        }
        else
        {
            AudioSource.Play();
            OnFadeVolume = false;
        }

        // Check loop
        if (!SoundConfig.Loop)
        {
            // Event complete playing sound
            if (_coroutineEndClip != null) StopCoroutine(_coroutineEndClip);
            _coroutineEndClip = StartCoroutine(SoundHelper.DelayCoroutine(() =>
            {
                SoundManager.OnCompleteSound.Invoke(this);
            }, Duration()));

            // Check fade out
            if (SoundConfig.FadeOut)
            {
                if (_coroutineFadeoutEndClip != null) StopCoroutine(_coroutineFadeoutEndClip);
                _coroutineFadeoutEndClip = StartCoroutine(SoundHelper.DelayCoroutine(() =>
                {
                    var currentVolume = AudioSource.volume;
                    _tweenFadeVolume?.Kill();
                    _tweenFadeVolume = SoundHelper.AddDOTween(SoundConfig.FadeOutTime, process =>
                    {
                        SetVolume(Mathf.Lerp(currentVolume, 0f, process));
                    }).OnStart(() =>
                    {
                        OnFadeVolume = true;
                    }).OnComplete(() =>
                    {
                        AudioSource.Pause(); 
                        OnFadeVolume = false;
                    }).SetEase(Ease.Linear);
                }, Duration() - SoundConfig.FadeOutTime));
            }
        }
    }

    //
    public void Pause()
    {
        if (!IsPlaying()) return;
        if (SoundConfig.FadeOut)
        {
            var currentVolume = AudioSource.volume;
            _tweenFadeVolume?.Kill();
            _tweenFadeVolume = SoundHelper.AddDOTween(SoundConfig.FadeOutTime, process =>
            {
                SetVolume(Mathf.Lerp(currentVolume, 0f, process));
            }).OnStart(() =>
            {
                OnFadeVolume = true;
            }).OnComplete(() =>
            {
                AudioSource.Pause(); 
                OnFadeVolume = false;
            }).SetEase(Ease.Linear);
        }
        else
        {
            AudioSource.Pause();
            OnFadeVolume = false;
        }

        if (!SoundConfig.Loop && _coroutineEndClip != null)
        {
            StopCoroutine(_coroutineEndClip);
        }
    }

    //
    public void Stop()
    {
        if (!IsPlaying()) return;
        if (SoundConfig.FadeOut)
        {
            var currentVolume = AudioSource.volume;
            _tweenFadeVolume?.Kill();
            _tweenFadeVolume = SoundHelper.AddDOTween(SoundConfig.FadeOutTime, process =>
            {
                SetVolume(Mathf.Lerp(currentVolume, 0f, process));
            }).OnStart(() =>
            {
                OnFadeVolume = true;
            }).OnComplete(() =>
            {
                AudioSource.Pause(); 
                OnFadeVolume = false; 
                SoundManager.OnCompleteSound.Invoke(this);
            }).SetEase(Ease.Linear);
        }
        else
        {
            AudioSource.Stop();
            OnFadeVolume = false;
            SoundManager.OnCompleteSound.Invoke(this);
        }

        if (!SoundConfig.Loop && _coroutineEndClip != null)
        {
            StopCoroutine(_coroutineEndClip);
        }
    }

    #endregion

    #region Audiosource functions

    //
    public void SetVolume(float volume)
    {
        AudioSource.volume = volume <= SoundConfig.Volume ? volume : SoundConfig.Volume;
    }

    //
    public void SetVolume(bool volume)
    {
        AudioSource.volume = volume ? SoundConfig.Volume : 0f;
    }

    //
    public void SetPitch(float pitch)
    {
        AudioSource.pitch = pitch;
    }

    //
    public float Duration()
    {
        return (AudioSource.clip != null ? AudioSource.clip.length * (1 / AudioSource.pitch) : 0f) - AudioSource.time;
    }

    //
    public bool IsPlaying()
    {
        return AudioSource.isPlaying;
    }

    #endregion
}