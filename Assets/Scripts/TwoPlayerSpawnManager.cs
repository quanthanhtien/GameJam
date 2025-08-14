using UnityEngine;
using Alteruna;

public class TwoPlayerSpawnManager : MonoBehaviour
{
    [Header("Spawn Configuration")]
    public Transform position1; // Vị trí 1
    public Transform position2; // Vị trí 2
    public GameObject playerPrefab;
    
    private Multiplayer multiplayer;

    void Start()
    {
        multiplayer = FindObjectOfType<Multiplayer>();
        
        // Spawn ngay khi start
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (multiplayer == null)
        {
            Debug.LogError("Multiplayer not found!");
            return;
        }
        
        // Xác định vị trí dựa trên user index
        Vector3 spawnPos = (multiplayer.Me.Index == 0) ? position1.position : position2.position;
        
        // Spawn player đơn giản
        GameObject myPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        myPlayer.name = $"Player_{multiplayer.Me.Index}";
        
        Debug.Log($"Player spawned at {spawnPos} (User Index: {multiplayer.Me.Index})");
    }
}