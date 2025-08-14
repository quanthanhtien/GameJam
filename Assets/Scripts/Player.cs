using System;
using UnityEngine;
using Alteruna;
public class Player : AttributesSync
{
    public float speed = 5f;
    public float distance = 5f;
    public GameObject boxPrefab;
    public Transform boxSpawnPoint;
    SpriteRenderer spriteRenderer;
    Vector3 currentPosition;
    Vector3 startPosition;
    Vector3 targetPosition;
    bool movingRight = true;
    public Alteruna.Avatar avatar;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int playerSelfLayer;
    private void Start()
    {
        if (avatar.IsMe)
            avatar.gameObject.layer = playerSelfLayer;
        startPosition = transform.position;
        currentPosition = startPosition;
        targetPosition = startPosition + Vector3.right * distance;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    // Move
    public void Update()
    {
        if (!avatar.IsMe)
            return;
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

        if (Input.GetMouseButtonDown(0))
        {
            BroadcastRemoteMethod("SpawnBox", transform.position);
        }
    }
    
    [SynchronizableMethod]
    void SpawnBox()
    {
        Instantiate(boxPrefab, boxSpawnPoint.position, Quaternion.identity);
    }
}