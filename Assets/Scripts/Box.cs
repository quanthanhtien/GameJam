// Box.cs - Đơn giản và hoàn chỉnh

using System.Collections;
using UnityEngine;

public class Box : MonoBehaviour
{
    [Header("Box Settings")]
    public float perfectThreshold = 0.3f;
    
    [HideInInspector] public bool hasLanded = false;
    private Player ownerPlayer;
    private Rigidbody2D rb;
    private bool isSwaying = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    public void Initialize(Player owner)
    {
        ownerPlayer = owner;
        
        // Add slight random force để tránh box overlap
        if (rb != null)
        {
            float randomForce = Random.Range(-0.3f, 0.3f);
            rb.AddForce(Vector2.right * randomForce, ForceMode2D.Impulse);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;
        
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Box"))
        {
            hasLanded = true;
            
            // Stop physics movement
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            // Add to tower
            if (ownerPlayer != null)
            {
                ownerPlayer.AddBlockToTower(gameObject);
            }
            
            // Check for perfect placement
            if (collision.gameObject.CompareTag("Box"))
            {
                CheckPerfectPlacement(collision.gameObject);
            }
            
            Debug.Log($"Box from {ownerPlayer?.gameObject.name} landed!");
        }
    }
    
    private void CheckPerfectPlacement(GameObject otherBox)
    {
        Box otherBoxScript = otherBox.GetComponent<Box>();
        if (otherBoxScript != null && otherBoxScript.ownerPlayer == ownerPlayer)
        {
            // Check horizontal alignment
            float horizontalDistance = Mathf.Abs(transform.position.x - otherBox.transform.position.x);
            
            // Check if placement is perfect
            if (horizontalDistance <= perfectThreshold)
            {
                // Perfect placement!
                if (ownerPlayer != null)
                {
                    ownerPlayer.OnPerfectStack();
                }
                
                // Visual feedback
                StartCoroutine(PerfectEffect());
                Debug.Log($"PERFECT STACK! {ownerPlayer?.gameObject.name}");
            }
            else
            {
                // Imperfect placement
                if (ownerPlayer != null)
                {
                    ownerPlayer.OnImperfectStack();
                }
                Debug.Log($"Imperfect stack. Distance: {horizontalDistance:F2}");
            }
        }
    }
    
    private IEnumerator PerfectEffect()
    {
        // Simple visual effect for perfect placement
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = Color.green;
            yield return new WaitForSeconds(0.5f);
            sr.color = originalColor;
        }
    }
    
    public void StartSway(float duration)
    {
        if (!hasLanded && !isSwaying)
        {
            StartCoroutine(SwayEffect(duration));
        }
    }
    
    private IEnumerator SwayEffect(float duration)
    {
        isSwaying = true;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        while (elapsed < duration && !hasLanded)
        {
            float swayAmount = Mathf.Sin(elapsed * 8f) * 0.8f;
            transform.position = startPos + Vector3.right * swayAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        isSwaying = false;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Destroy box if it falls out of bounds
        if (other.CompareTag("DestroyZone"))
        {
            if (ownerPlayer != null && ownerPlayer.avatar.IsMe)
            {
                ownerPlayer.OnImperfectStack(); // Reset perfect streak
            }
            Destroy(gameObject);
        }
    }
}