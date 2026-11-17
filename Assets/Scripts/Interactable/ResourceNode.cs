using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ResourceNode : ToolHit
{
    [SerializeField] GameObject pickUpDrop;
    [SerializeField] float spread = 0.7f;

    [SerializeField] Item item;
    [SerializeField] int itemCountInOneDrop;
    [SerializeField] int dropCount = 10;
    [SerializeField] ResourceNodeType nodeType;

    [Header("Health")]
    [SerializeField] int hitsToBreak = 3;

    [Header("Hit Animation")]
    [SerializeField] float shakeDuration = 0.2f;
    [SerializeField] float shakeStrength = 0.08f;

    [Header("Break Animation")]
    [SerializeField] float breakDuration = 0.6f;

    int hitsTaken;
    bool isBroken;
    Vector3 originalPosition;
    SpriteRenderer[] spriteRenderers;
    Color[] originalColors;
    Coroutine shakeRoutine;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    private void Start()
    {
        if (SaveManager.instance != null && SaveManager.instance.IsNodeDestroyed(NodeId))
        {
            Destroy(gameObject);
        }
    }

    // Nodes are placed by hand in scenes, so scene + position is a stable id
    string NodeId
    {
        get
        {
            Vector3 p = transform.position;
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.InvariantCulture;
            return gameObject.scene.name + ":" + p.x.ToString("F2", culture) + "," + p.y.ToString("F2", culture);
        }
    }

    public override void Hit()
    {
        if (isBroken) { return; }

        if (shakeRoutine == null)
        {
            originalPosition = transform.position;
        }

        hitsTaken += 1;

        if (hitsTaken >= hitsToBreak)
        {
            Break();
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            transform.position = originalPosition;
        }
        shakeRoutine = StartCoroutine(HitRoutine());
    }

    public override bool CanBeHit(List<ResourceNodeType> canBeHit)
    {
        return isBroken == false && canBeHit.Contains(nodeType);
    }

    private void Break()
    {
        isBroken = true;

        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        {
            c.enabled = false;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }
        transform.position = originalPosition;

        if (SaveManager.instance != null)
        {
            SaveManager.instance.RegisterDestroyedNode(NodeId);
        }

        DropItems();
        StartCoroutine(BreakRoutine());
    }

    private void DropItems()
    {
        while (dropCount > 0)
        {
            dropCount -= 1;

            Vector3 position = transform.position;

            position.x += spread * UnityEngine.Random.value - spread / 2;
            position.y += spread * UnityEngine.Random.value - spread / 2;

            ItemSpawnerManager.instance.SpawnItem(position, item, itemCountInOneDrop);
        }
    }

    IEnumerator HitRoutine()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - elapsed / shakeDuration;
            float offsetX = Mathf.Sin(elapsed * 60f) * shakeStrength * damper;
            transform.position = originalPosition + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        transform.position = originalPosition;
        shakeRoutine = null;
    }

    IEnumerator BreakRoutine()
    {
        // Trees tip over to the side, rocks crumble down
        float fallDirection = Random.value < 0.5f ? 1f : -1f;
        Vector3 startScale = transform.localScale;

        // Sprite pivots are centered, so rotate around the bottom of the trunk instead
        Vector3 basePoint = originalPosition;
        if (spriteRenderers.Length > 0)
        {
            Bounds bounds = spriteRenderers[0].bounds;
            basePoint = new Vector3(bounds.center.x, bounds.min.y, originalPosition.z);
        }

        float elapsed = 0f;
        while (elapsed < breakDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / breakDuration);

            if (nodeType == ResourceNodeType.Tree)
            {
                float eased = t * t;
                Quaternion rotation = Quaternion.Euler(0f, 0f, -90f * fallDirection * eased);
                transform.rotation = rotation;
                transform.position = basePoint + rotation * (originalPosition - basePoint);
            }
            else
            {
                transform.localScale = new Vector3(
                    startScale.x * (1f + 0.2f * t),
                    startScale.y * (1f - t),
                    startScale.z
                    );
            }

            SetAlpha(1f - t * t);

            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color c = originalColors[i];
            c.a = originalColors[i].a * alpha;
            spriteRenderers[i].color = c;
        }
    }
}
