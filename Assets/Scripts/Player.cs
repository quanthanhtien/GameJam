using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

// Player.cs - Với hiệu ứng skill
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
    
    [Header("Skills")]
    public float freezeCost = 15f;
    public float earthquakeCost = 20f;
    public float tornadoCost = 10f;
    
    [Header("Multiplayer")]
    public Alteruna.Avatar avatar;
    
    // Synchronizable fields
    [SynchronizableField] public Vector3 syncPosition;
    [SynchronizableField] public bool syncMovingRight = true;
    [SynchronizableField] public bool syncFlipX = false;
    [SynchronizableField] public float syncCurrentMana = 20f;
    [SynchronizableField] public int syncTowerBlocks = 0;
    [SynchronizableField] public float syncTowerHeight = 0f;
    [SynchronizableField] public bool syncGameEnded = false;
    [SynchronizableField] public string syncWinMessage = "";
    
    // Private variables
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingRight = true;
    private bool isFrozen = false;
    private Color originalPlayerColor;
    private bool isPlayerFlashing = false;
    
    // Game state
    private float currentMana;
    private int consecutivePerfects = 0;
    private List<GameObject> towerBlocks = new List<GameObject>();
    private bool gameEnded = false;
    
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentMana = maxMana;
        originalPlayerColor = spriteRenderer.color;
        
        // Sử dụng vị trí hiện tại làm startPosition
        startPosition = transform.position;
        targetPosition = startPosition + Vector3.right * distance;
        
        syncPosition = startPosition;
        
        if (avatar.IsMe)
        {
            StartCoroutine(GameMasterLoop());
        }
    }
    
    private IEnumerator GameMasterLoop()
    {
        while (!gameEnded)
        {
            // Check win conditions
            CheckWinConditions();
            
            // Update mana
            UpdateMana();
            
            // Update tower stats
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
                HandleInput();
            }
            SyncData();
        }
        else
        {
            // Sync remote player
            transform.position = Vector3.Lerp(transform.position, syncPosition, Time.deltaTime * 10f);
            spriteRenderer.flipX = syncFlipX;
            currentMana = syncCurrentMana;
            gameEnded = syncGameEnded;
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
    
    private void HandleInput()
    {
        // Spawn box
        if (Input.GetMouseButtonDown(0))
        {
            SpawnBox();
        }
        
        // Skills
        if (Input.GetKeyDown(KeyCode.Q) && currentMana >= freezeCost)
        {
            UseSkill("Freeze");
        }
        if (Input.GetKeyDown(KeyCode.W) && currentMana >= earthquakeCost)
        {
            UseSkill("Earthquake");
        }
        if (Input.GetKeyDown(KeyCode.E) && currentMana >= tornadoCost)
        {
            UseSkill("Tornado");
        }
    }
    
    private void SpawnBox()
    {
        Vector3 spawnPos = boxSpawnPoint != null ? boxSpawnPoint.position : transform.position + Vector3.up * 2f;
        BroadcastRemoteMethod("NetworkSpawnBox", spawnPos);
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
    
    private void UseSkill(string skillName)
    {
        float cost = GetSkillCost(skillName);
        currentMana -= cost;
        
        // Start player flash effect when using skill
        StartPlayerSkillEffect(skillName);
        
        BroadcastRemoteMethod("ExecuteSkill", skillName);
    }
    
    private void StartPlayerSkillEffect(string skillName)
    {
        if (!isPlayerFlashing)
        {
            switch (skillName)
            {
                case "Freeze":
                    StartCoroutine(PlayerSkillFlash(Color.cyan, 1f));
                    break;
                case "Earthquake":
                    StartCoroutine(PlayerSkillFlash(new Color(0.8f, 0.4f, 0.2f), 1f));
                    break;
                case "Tornado":
                    StartCoroutine(PlayerSkillFlash(Color.yellow, 1f));
                    break;
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
    
    private float GetSkillCost(string skillName)
    {
        switch (skillName)
        {
            case "Freeze": return freezeCost;
            case "Earthquake": return earthquakeCost;
            case "Tornado": return tornadoCost;
            default: return 0f;
        }
    }
    
    [SynchronizableMethod]
    void ExecuteSkill(string skillName)
    {
        switch (skillName)
        {
            case "Freeze":
                StartCoroutine(FreezeAllOpponents());
                break;
            case "Earthquake":
                StartCoroutine(EarthquakeAllOpponents());
                break;
            case "Tornado":
                StartCoroutine(TornadoEffect());
                break;
        }
    }
    
    private IEnumerator FreezeAllOpponents()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        // Freeze opponents only
        foreach (Player player in allPlayers)
        {
            if (player != this)
            {
                player.isFrozen = true;
                if (!player.isPlayerFlashing)
                {
                    player.StartCoroutine(player.PlayerSkillFlash(Color.cyan, 2f));
                }
                
                // Flash effect on opponent's boxes only
                foreach (GameObject block in player.towerBlocks)
                {
                    if (block != null)
                    {
                        Box boxScript = block.GetComponent<Box>();
                        if (boxScript != null)
                        {
                            boxScript.StartFreezeEffect(2f);
                        }
                    }
                }
            }
        }
        
        Debug.Log("Freeze activated!");
        yield return new WaitForSeconds(2f);
        
        // Unfreeze opponents
        foreach (Player player in allPlayers)
        {
            if (player != this)
            {
                player.isFrozen = false;
            }
        }
    }
    
    private IEnumerator EarthquakeAllOpponents()
    {
        Debug.Log("Earthquake activated!");
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        // Apply earthquake to opponents only
        foreach (Player player in allPlayers)
        {
            if (player != this && player.towerBlocks.Count > 0)
            {
                // Flash effect on opponent
                if (!player.isPlayerFlashing)
                {
                    player.StartCoroutine(player.PlayerSkillFlash(new Color(0.8f, 0.4f, 0.2f), 2f));
                }
                
                // Randomly destroy 1 block from opponent's tower
                int randomIndex = Random.Range(0, player.towerBlocks.Count);
                GameObject blockToDestroy = player.towerBlocks[randomIndex];
                
                if (blockToDestroy != null)
                {
                    // Flash effect on the block before destroying
                    Box boxScript = blockToDestroy.GetComponent<Box>();
                    if (boxScript != null)
                    {
                        boxScript.StartEarthquakeEffect(1f);
                    }
                    
                    // Destroy after a short delay
                    StartCoroutine(DestroyBlockAfterDelay(player, blockToDestroy, randomIndex, 1f));
                }
                
                // Flash effect on remaining opponent's boxes
                foreach (GameObject block in player.towerBlocks)
                {
                    if (block != null && block != blockToDestroy)
                    {
                        Box boxScript = block.GetComponent<Box>();
                        if (boxScript != null)
                        {
                            boxScript.StartEarthquakeEffect(2f);
                        }
                    }
                }
            }
        }
        yield return null;
    }
    
    private IEnumerator DestroyBlockAfterDelay(Player player, GameObject block, int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (block != null && player.towerBlocks.Contains(block))
        {
            Destroy(block);
            player.towerBlocks.RemoveAt(index);
            Debug.Log($"Earthquake destroyed a block from {player.gameObject.name}!");
        }
    }
    
    private IEnumerator TornadoEffect()
    {
        Debug.Log("Tornado activated!");
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        // Apply tornado to opponents only
        foreach (Player player in allPlayers)
        {
            if (player != this)
            {
                // Flash effect on opponent
                if (!player.isPlayerFlashing)
                {
                    player.StartCoroutine(player.PlayerSkillFlash(Color.yellow, 2f));
                }
                
                // Shake all opponent's boxes (both in tower and falling)
                foreach (GameObject block in player.towerBlocks)
                {
                    if (block != null)
                    {
                        Box boxScript = block.GetComponent<Box>();
                        if (boxScript != null)
                        {
                            boxScript.StartTornadoShake(2f);
                            boxScript.StartTornadoEffect(2f);
                        }
                    }
                }
            }
        }
        
        // Also affect any falling boxes from opponents
        Box[] allBoxes = FindObjectsOfType<Box>();
        foreach (Box box in allBoxes)
        {
            // Check if this box belongs to an opponent
            if (box.GetComponent<Box>().ownerPlayer != this && box.GetComponent<Box>().ownerPlayer != null)
            {
                if (!box.hasLanded)
                {
                    box.StartSway(2f); // Only falling boxes sway
                }
                box.StartTornadoShake(2f); // All opponent boxes shake
                box.StartTornadoEffect(2f); // Visual effect
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
        }
    }
    
    private void UpdateTowerStats()
    {
        // Remove destroyed blocks
        towerBlocks.RemoveAll(block => block == null);
        
        // Calculate tower height
        float towerHeight = 0f;
        if (towerBlocks.Count > 0)
        {
            float minY = startPosition.y;
            float maxY = float.MinValue;
            
            foreach (GameObject block in towerBlocks)
            {
                if (block != null)
                {
                    float blockY = block.transform.position.y;
                    if (blockY > maxY) maxY = blockY;
                }
            }
            towerHeight = Mathf.Max(0f, maxY - minY);
        }
        
        syncTowerBlocks = towerBlocks.Count;
        syncTowerHeight = towerHeight;
    }
    
    private void CheckWinConditions()
    {
        if (gameEnded) return;
        
        // Check block count win
        if (towerBlocks.Count >= targetBlocks)
        {
            EndGame($"{gameObject.name} wins by reaching {targetBlocks} blocks!");
            return;
        }
        
        // Check height win
        if (syncTowerHeight >= targetHeight)
        {
            EndGame($"{gameObject.name} wins by reaching {targetHeight}m height!");
            return;
        }
    }
    
    private void EndGame(string winMessage)
    {
        gameEnded = true;
        syncGameEnded = true;
        syncWinMessage = winMessage;
        BroadcastRemoteMethod("GameEnded", winMessage);
    }
    
    [SynchronizableMethod]
    void GameEnded(string winMessage)
    {
        gameEnded = true;
        syncWinMessage = winMessage;
        Debug.Log(winMessage);
    }
    
    public void OnPerfectStack()
    {
        if (!avatar.IsMe) return;
        
        consecutivePerfects++;
        
        float bonusMana = 0f;
        if (consecutivePerfects == 1) bonusMana = 1f;
        else if (consecutivePerfects == 2) bonusMana = 2f;
        else if (consecutivePerfects >= 3) bonusMana = 3f;
        
        currentMana = Mathf.Clamp(currentMana + bonusMana, 0, maxMana);
        Debug.Log($"{gameObject.name}: Perfect x{consecutivePerfects}! +{bonusMana} mana");
    }
    
    public void OnImperfectStack()
    {
        if (!avatar.IsMe) return;
        consecutivePerfects = 0;
    }
    
    public void AddBlockToTower(GameObject block)
    {
        if (!towerBlocks.Contains(block))
        {
            towerBlocks.Add(block);
        }
    }
    
    private void SyncData()
    {
        syncPosition = transform.position;
        syncMovingRight = movingRight;
        syncFlipX = spriteRenderer.flipX;
        syncCurrentMana = currentMana;
    }
}