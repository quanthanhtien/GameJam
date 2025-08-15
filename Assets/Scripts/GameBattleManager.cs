using System;
using UnityEngine;

public class GameBattleManager : MonoBehaviour
{
    public GameObject menuUI;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            menuUI.SetActive(!menuUI.activeSelf);
        }
    }
}