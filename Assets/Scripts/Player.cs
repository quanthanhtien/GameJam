using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class Player : AttributesSync
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float distance = 5f;

    [Header("Box Settings")]
    public GameObject boxPrefab;
    public Transform boxSpawnPoint;

    [Header("Game Settings")]
    public int targetBlocks = 20;
    public float targetHeight = 50f;

    [Header("Mana System")]
    public float maxMana = 20f;
    public float manaRegenRate = 1f;
    [Header("Game Result UI")]
    public GameObject winUI; // GameObject hiển thị khi win
    public GameObject loseUI; // GameObject hiển thị khi lose

    [Header("Skills")]
    public float freezeCost = 15f;
    public float earthquakeCost = 20f;
    public float tornadoCost = 10f;

    [Header("Multiplayer")]
    public Alteruna.Avatar avatar;
    
    [Header("Player ID")]
    public int playerIndex = 0; // Set này trong Inspector: Player 1 = 0, Player 2 = 1

    [SynchronizableField] public Vector3 syncPosition;
    [SynchronizableField] public bool syncFlipX = false;
    [SynchronizableField] public float syncCurrentMana = 20f;
    [SynchronizableField] public int syncTowerBlocks = 0;
    [SynchronizableField] public float syncTowerHeight = 0f;
    [SynchronizableField] public bool syncGameEnded = false;
    [SynchronizableField] public string syncWinMessage = "";
    [SynchronizableField] public float sharedTime = 300f; // 5 phút

    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingRight = true;
    private bool isFrozen = false;
    private bool canSpawn = true;
    private Color originalPlayerColor;
    private bool isPlayerFlashing = false;

    private float currentMana;
    private int consecutivePerfects = 0;
    public List<GameObject> towerBlocks = new List<GameObject>();
    private bool gameEnded = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentMana = maxMana;
        syncCurrentMana = maxMana;
        originalPlayerColor = spriteRenderer.color;

        startPosition = transform.position;
        targetPosition = startPosition + Vector3.right * distance;
        syncPosition = startPosition;

        // Tự động set playerIndex dựa trên thứ tự tạo object (nếu chưa set trong Inspector)
        if (playerIndex == 0)
        {
            Player[] allPlayers = FindObjectsOfType<Player>();
            playerIndex = allPlayers.Length - 1; // 0-based index
        }

        if (avatar.IsMe)
        {
            GameUIManager.Instance.RegisterLocalPlayer(this);
            StartCoroutine(GameMasterLoop());
            SoundManager.Play("fly");
        }
    }

    private IEnumerator GameMasterLoop()
    {
        while (!gameEnded)
        {
            if (IsHost())
            {
                sharedTime -= Time.deltaTime;
                if (sharedTime <= 0) 
                {
                    sharedTime = 0;
                    if (!gameEnded)
                    {
                        EndGame("Time's up! It's a draw!");
                    }
                }
            }

            // Cập nhật UI cho tất cả
            GameUIManager.Instance.UpdateTime(sharedTime);
            GameUIManager.Instance.UpdateAllPlayerMana();
            
            CheckWinConditions();
            UpdateMana();
            UpdateTowerStats();
            yield return null;
        }
    }

    private void Update()
    {
        if (avatar.IsMe)
        {
            if (!isFrozen && !gameEnded)
            {
                HandleMovement();
            }
            SyncData();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 10f);
            spriteRenderer.flipX = syncFlipX;
            currentMana = syncCurrentMana;
            gameEnded = syncGameEnded;
        }

        // Cập nhật UI mana realtime cho tất cả players
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.UpdateMana(currentMana, playerIndex, maxMana);
        }
    }

    private void HandleMovement()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            if (movingRight)
            {
                targetPosition = startPosition - Vector3.right * distance;
                movingRight = false;
                spriteRenderer.flipX = false;
            }
            else
            {
                targetPosition = startPosition + Vector3.right * distance;
                movingRight = true;
                spriteRenderer.flipX = true;
            }
        }
    }

    public void OnClickSpawnBox()
    {
        if (canSpawn && !isFrozen && !gameEnded)
        {
            StartCoroutine(SpawnBoxDelay());
        }
    }

    private IEnumerator SpawnBoxDelay()
    {
        canSpawn = false;
        Vector3 spawnPos = boxSpawnPoint != null ? boxSpawnPoint.position : transform.position + Vector3.up * 2f;
        BroadcastRemoteMethod("NetworkSpawnBox", spawnPos);
        yield return new WaitForSeconds(1f);
        canSpawn = true;
    }

    [SynchronizableMethod]
    void NetworkSpawnBox(Vector3 spawnPos)
    {
        GameObject newBox = Instantiate(boxPrefab, spawnPos, Quaternion.identity);
        Box boxScript = newBox.GetComponent<Box>();
        if (boxScript != null)
        {
            boxScript.Initialize(this);
        }
    }

    public void OnClickSkill(string skillName)
    {
        float cost = GetSkillCost(skillName);
        if (currentMana >= cost)
        {
            currentMana -= cost;
            syncCurrentMana = currentMana;
            StartPlayerSkillEffect(skillName);
            BroadcastRemoteMethod("ExecuteSkill", skillName);
        }
    }

    private float GetSkillCost(string skillName)
    {
        switch (skillName)
        {
            case "Freeze": return freezeCost;
            case "Earthquake": return earthquakeCost;
            case "Tornado": return tornadoCost;
        }
        return 0f;
    }

    private void StartPlayerSkillEffect(string skillName)
    {
        if (!isPlayerFlashing)
        {
            switch (skillName)
            {
                case "Freeze": StartCoroutine(PlayerSkillFlash(Color.cyan, 1f)); break;
                case "Earthquake": StartCoroutine(PlayerSkillFlash(new Color(0.8f, 0.4f, 0.2f), 1f)); break;
                case "Tornado": StartCoroutine(PlayerSkillFlash(Color.yellow, 1f)); break;
            }
        }
    }

    private IEnumerator PlayerSkillFlash(Color skillColor, float duration)
    {
        isPlayerFlashing = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            spriteRenderer.color = Color.Lerp(originalPlayerColor, skillColor, Mathf.PingPong(elapsed * 6f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = originalPlayerColor;
        isPlayerFlashing = false;
    }

    [SynchronizableMethod]
    void ExecuteSkill(string skillName)
    {
        switch (skillName)
        {
            case "Freeze": StartCoroutine(FreezeAllOpponents()); break;
            case "Earthquake": StartCoroutine(EarthquakeAllOpponents()); break;
            case "Tornado": StartCoroutine(TornadoEffect()); break;
        }
    }

    private IEnumerator FreezeAllOpponents()
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player != this)
            {
                player.isFrozen = true;
                foreach (GameObject block in player.towerBlocks)
                    block?.GetComponent<Box>()?.StartFreezeEffect(2f);
            }
        }
        yield return new WaitForSeconds(2f);
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player != this) player.isFrozen = false;
        }
    }

    // Earthquake skill đã được cập nhật để phá hủy 2 blocks ngẫu nhiên
    private IEnumerator EarthquakeAllOpponents()
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player != this && player.towerBlocks.Count > 0)
            {
                // Phá hủy tối đa 2 blocks ngẫu nhiên
                int blocksToDestroy = Mathf.Min(2, player.towerBlocks.Count);
                List<int> indicesToRemove = new List<int>();
                
                for (int i = 0; i < blocksToDestroy; i++)
                {
                    int idx;
                    do
                    {
                        idx = Random.Range(0, player.towerBlocks.Count);
                    } 
                    while (indicesToRemove.Contains(idx));
                    
                    indicesToRemove.Add(idx);
                    
                    var block = player.towerBlocks[idx];
                    if (block != null)
                    {
                        block.GetComponent<Box>()?.StartEarthquakeEffect(1f);
                        yield return new WaitForSeconds(0.5f); // Delay ngắn giữa các blocks
                    }
                }
                
                yield return new WaitForSeconds(1f);
                
                // Xóa các blocks từ cao xuống thấp để tránh lỗi index
                indicesToRemove.Sort((a, b) => b.CompareTo(a));
                
                foreach (int idx in indicesToRemove)
                {
                    if (idx < player.towerBlocks.Count && player.towerBlocks[idx] != null)
                    {
                        Destroy(player.towerBlocks[idx]);
                        player.towerBlocks.RemoveAt(idx);
                    }
                }
            }
        }
    }

    private IEnumerator TornadoEffect()
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player != this)
            {
                foreach (GameObject block in player.towerBlocks)
                    block?.GetComponent<Box>()?.StartTornadoShake(2f);
            }
        }
        yield return null;
    }

    private void UpdateMana()
    {
        if (currentMana < maxMana)
        {
            currentMana += manaRegenRate * Time.deltaTime;
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            syncCurrentMana = currentMana;
        }
    }

    private void UpdateTowerStats()
    {
        towerBlocks.RemoveAll(b => b == null);
        float towerHeight = 0f;
        if (towerBlocks.Count > 0)
        {
            float maxY = float.MinValue;
            foreach (GameObject block in towerBlocks)
                if (block != null && block.transform.position.y > maxY) maxY = block.transform.position.y;
            towerHeight = maxY - startPosition.y;
        }
        syncTowerBlocks = towerBlocks.Count;
        syncTowerHeight = towerHeight;
    }

    private void CheckWinConditions()
    {
        if (gameEnded) return;
        if (towerBlocks.Count >= targetBlocks)
        {
            EndGame($"{gameObject.name} wins by blocks!");
            winUI.SetActive(true);
            return;
        }
        if (syncTowerHeight >= targetHeight)
        {
            winUI.SetActive(true);
            EndGame($"{gameObject.name} wins by height!");
            return;
        }
    }

    private void EndGame(string msg)
    {
        gameEnded = true;
        syncGameEnded = true;
        syncWinMessage = msg;
        BroadcastRemoteMethod("GameEnded", msg);
    }

    [SynchronizableMethod]
    void GameEnded(string msg) 
    {
        gameEnded = true;
        syncWinMessage = msg;
        Debug.Log(msg);
    }

    public void OnPerfectStack()
    {
        consecutivePerfects++;
        currentMana = Mathf.Clamp(currentMana + Mathf.Min(consecutivePerfects, 3), 0, maxMana);
        syncCurrentMana = currentMana;
    }

    public void OnImperfectStack() => consecutivePerfects = 0;

    public void AddBlockToTower(GameObject block)
    {
        if (!towerBlocks.Contains(block))
            towerBlocks.Add(block);
    }

    // Getter cho UI Manager
    public float GetCurrentMana()
    {
        return currentMana;
    }

    private void SyncData()
    {
        syncPosition = transform.position;
        syncFlipX = spriteRenderer.flipX;
        syncCurrentMana = currentMana;
    }

    private bool IsHost()
    {
        return playerIndex == 0; // Player đầu tiên (index 0) làm host
    }
}