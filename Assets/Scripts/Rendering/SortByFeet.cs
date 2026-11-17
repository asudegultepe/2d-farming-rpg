using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The 2D renderer sorts the World layer along the axis (0, 1, 1): lower on screen
// draws in front, and z shifts an object's place in that order without any visible
// effect in 2D. All sprites have centered pivots, so a tall sprite would otherwise
// sort by its middle; pushing z down by the distance from that sort point to the
// sprite's bottom edge makes everything sort by where it stands instead.
//
// Static scene objects get their z baked by the Sort By Feet editor tool.
// This component keeps it right for things that are spawned, move or change sprite.
[DisallowMultipleComponent]
public class SortByFeet : MonoBehaviour
{
    SortingGroup group;
    SpriteRenderer[] sprites;

    private void Awake()
    {
        group = GetComponent<SortingGroup>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void LateUpdate()
    {
        float z = FeetZ(transform, group, sprites);
        Vector3 position = transform.position;

        // only touch the transform when something actually changed
        if (Mathf.Approximately(position.z, z) == false)
        {
            position.z = z;
            transform.position = position;
        }
    }

    // World z that makes this object sort by the bottom of its sprites
    public static float FeetZ(Transform target, SortingGroup group, SpriteRenderer[] sprites)
    {
        bool found = false;
        float bottom = 0f;
        float sortPoint = 0f;

        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite.sprite == null || sprite.enabled == false || sprite.gameObject.activeInHierarchy == false) { continue; }

            Bounds bounds = sprite.bounds;
            if (found == false || bounds.min.y < bottom)
            {
                bottom = bounds.min.y;
            }

            if (found == false)
            {
                // a sorting group sorts at its own position, a lone sprite at its sort point
                sortPoint = group != null || sprite.spriteSortPoint == SpriteSortPoint.Pivot
                    ? sprite.transform.position.y
                    : bounds.center.y;
            }
            found = true;
        }

        if (found == false) { return target.position.z; }
        if (group != null) { sortPoint = target.position.y; }

        return bottom - sortPoint;
    }
}
