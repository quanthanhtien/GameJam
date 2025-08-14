using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

// ReSharper disable CheckNamespace

public enum TrackHelper
{
    Background = 0, 
    Effect = 1, 
    UInterface = 2
}

public static class SoundHelper
{
    #region Static functions
    
    //
    public static T GetOrAddComponent<T>(this GameObject child) where T : Component
    {
        var result = child.GetComponent<T>();
        if (result == null) result = child.AddComponent<T>();
        return result;
    }
    
    //
    public static Tweener AddDOTween(float from, float to, float duration, Action<float> onUpdate)
    {
        var cValue = from;
        return DOTween.To(() => cValue, setter => cValue = setter, to, duration)
            .OnUpdate(() => onUpdate?.Invoke(cValue));
    }
    
    //
    public static Tweener AddDOTween(float duration, Action<float> onUpdate)
    {
        return AddDOTween(0f, 1f, duration, onUpdate);
    }
    
    //
    public static IEnumerator DelayCoroutine(Action action, float delay)
    {
        yield return new WaitForSeconds(delay);
        action.Invoke();
    }

    #endregion
}