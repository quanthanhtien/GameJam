using System;
using UnityEngine;
using UnityEngine.UI;

public class GoMenu : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClickGoMenu);
    }

    public void OnClickGoMenu()
    {
        GameManagerState._instance.LoadMenu();
    }
}
