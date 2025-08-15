// Box.cs - Với hiệu ứng skill

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
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
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
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.green;
            yield return new WaitForSeconds(0.5f);
            spriteRenderer.color = originalColor;
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
    
    // Skill Effects
    public void StartFreezeEffect(float duration)
    {
        if (!isFlashing)
        {
            StartCoroutine(FreezeFlashEffect(duration));
        }
    }
    
    public void StartEarthquakeEffect(float duration)
    {
        if (!isFlashing)
        {
            StartCoroutine(EarthquakeFlashEffect(duration));
        }
    }
    
    public void StartTornadoEffect(float duration)
    {
        if (!isFlashing)
        {
            StartCoroutine(TornadoFlashEffect(duration));
        }
    }
    
    private IEnumerator FreezeFlashEffect(float duration)
    {
        isFlashing = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Flash between original color and cyan (ice blue)
            spriteRenderer.color = Color.Lerp(originalColor, Color.cyan, Mathf.PingPong(elapsed * 8f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
    
    private IEnumerator EarthquakeFlashEffect(float duration)
    {
        isFlashing = true;
        float elapsed = 0f;
        Vector3 originalPosition = transform.position;
        
        while (elapsed < duration)
        {
            // Flash between original color and red/brown
            spriteRenderer.color = Color.Lerp(originalColor, new Color(0.8f, 0.4f, 0.2f), Mathf.PingPong(elapsed * 6f, 1f));
            
            // Add small shake effect if landed
            if (hasLanded)
            {
                float shakeAmount = 0.1f;
                transform.position = originalPosition + new Vector3(
                    Random.Range(-shakeAmount, shakeAmount),
                    Random.Range(-shakeAmount, shakeAmount),
                    0f
                );
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        if (hasLanded)
        {
            transform.position = originalPosition;
        }
        isFlashing = false;
    }
    
    private IEnumerator TornadoFlashEffect(float duration)
    {
        isFlashing = true;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Flash between original color and yellow/white (wind effect)
            Color windColor = Color.Lerp(Color.yellow, Color.white, Mathf.PingPong(elapsed * 4f, 1f));
            spriteRenderer.color = Color.Lerp(originalColor, windColor, Mathf.PingPong(elapsed * 10f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
        isFlashing = false;
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