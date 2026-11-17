using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Draw order on the World layer comes from where each object stands: the lower its
// feet are on screen, the higher its Order in Layer, so it draws in front.
//
// The feet are the bottom edge of the object's lowest sprite. Some sprites (the trees)
// have a skirt of grass under their trunk that the object doesn't stand on, so its
// height is added on top of the bottom edge.
//
// Static scene objects get their order baked by the Sort By Feet editor tool.
// This component keeps it right for things that are spawned, move or change sprite.
[DisallowMultipleComponent]
public class SortByFeet : MonoBehaviour
{
    // Order in Layer per world unit. Objects 1/100 of a unit apart still sort apart,
    // and the order stays inside its ±32767 range for heights within ±327 units.
    public const float OrderPerUnit = 100f;

    [Tooltip("Height of the grass at the bottom of the sprite, in units at scale 1. Measured by the Sort By Feet tool.")]
    [SerializeField] float groundSkirt;

    SortingGroup group;
    SpriteRenderer[] sprites;
    int appliedOrder = int.MinValue;

    private void Awake()
    {
        group = GetComponent<SortingGroup>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        if (TryGetOrder(sprites, groundSkirt * Mathf.Abs(transform.lossyScale.y), out int order) == false) { return; }

        // only touch the renderers when the order actually changes
        if (order == appliedOrder) { return; }
        appliedOrder = order;

        if (group != null)
        {
            group.sortingOrder = order;
            return;
        }

        foreach (SpriteRenderer sprite in sprites)
        {
            sprite.sortingOrder = order;
        }
    }

    // Order in Layer for an object made of these sprites, with the ground skirt in world units
    public static bool TryGetOrder(SpriteRenderer[] sprites, float groundSkirt, out int order)
    {
        order = 0;
        SpriteRenderer lowest = LowestSprite(sprites);
        if (lowest == null) { return false; }

        float feet = lowest.bounds.min.y + groundSkirt;
        order = Mathf.RoundToInt(-feet * OrderPerUnit);
        return true;
    }

    // Lowest visible sprite, the one the object stands on
    public static SpriteRenderer LowestSprite(SpriteRenderer[] sprites)
    {
        SpriteRenderer lowest = null;
        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite.sprite == null || sprite.enabled == false || sprite.gameObject.activeInHierarchy == false) { continue; }
            if (lowest == null || sprite.bounds.min.y < lowest.bounds.min.y) { lowest = sprite; }
        }
        return lowest;
    }
}
