using System.Collections;
using UnityEngine;

public class Box : MonoBehaviour
{
    public Player ownerPlayer;
    public bool hasLanded = false;

    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (sr != null)
            originalColor = sr.color;
    }

    public void Initialize(Player player)
    {
        ownerPlayer = player;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLanded) return;
        SoundManager.Play("drop");
        hasLanded = true;
        if (ownerPlayer != null)
            ownerPlayer.AddBlockToTower(gameObject);
    }

    public void StartFreezeEffect(float duration)
    {
        StartCoroutine(FreezeEffectRoutine(duration));
    }

    private IEnumerator FreezeEffectRoutine(float duration)
    {
        // Tạm dừng physics trong khi freeze
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (sr != null)
                sr.color = Color.Lerp(originalColor, Color.cyan, Mathf.PingPong(elapsed * 5f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Khôi phục physics
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (sr != null) sr.color = originalColor;
    }

    public void StartEarthquakeEffect(float duration)
    {
        StartCoroutine(EarthquakeEffectRoutine(duration));
    }

    private IEnumerator EarthquakeEffectRoutine(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        // Hiệu ứng màu đỏ để báo hiệu sẽ bị phá hủy
        if (sr != null)
        {
            StartCoroutine(FlashRed(duration));
        }

        while (elapsed < duration)
        {
            // Shake mạnh hơn để thể hiện earthquake
            transform.position = startPos + (Vector3)Random.insideUnitCircle * 0.2f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = startPos;
    }

    private IEnumerator FlashRed(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (sr != null)
                sr.color = Color.Lerp(originalColor, Color.red, Mathf.PingPong(elapsed * 8f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = originalColor;
    }

    public void StartTornadoShake(float duration)
    {
        StartCoroutine(TornadoShakeRoutine(duration));
    }

    private IEnumerator TornadoShakeRoutine(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        
        // Hiệu ứng màu vàng cho tornado
        if (sr != null)
        {
            StartCoroutine(FlashYellow(duration));
        }

        while (elapsed < duration)
        {
            // Chuyển động xoắn ốc như tornado
            float angle = elapsed * 15f; // Tốc độ xoay
            float radius = 0.15f * Mathf.Sin(elapsed * 10f); // Bán kính dao động
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle * 0.5f) * 0.1f, 0);
            transform.position = startPos + offset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = startPos;
    }

    private IEnumerator FlashYellow(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (sr != null)
                sr.color = Color.Lerp(originalColor, Color.yellow, Mathf.PingPong(elapsed * 6f, 1f));
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = originalColor;
    }

    private void OnDestroy()
    {
        // Cleanup khi block bị phá hủy
        if (ownerPlayer != null && ownerPlayer.towerBlocks != null)
        {
            ownerPlayer.towerBlocks.Remove(gameObject);
        }
    }
}