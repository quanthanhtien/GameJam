using System;
using UnityEngine;

public class GameManager : MonoBehaviour
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
