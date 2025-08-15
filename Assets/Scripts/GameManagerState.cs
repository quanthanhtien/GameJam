using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerState : MonoBehaviour
{
    public static GameManagerState _instance;

    private void Start()
    {
        SoundManager.Play("bg");
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("BattleScene");
    }
    
    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}