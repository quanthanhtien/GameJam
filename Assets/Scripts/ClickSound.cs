using System;
using UnityEngine;
using UnityEngine.UI;

public class ClickSound : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => PlayClickSound());
    }
    public void PlayClickSound()
    {
        SoundManager.Play("click");
    }
}