using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableManager : MonoBehaviour
{
    Movement playerController;
    Rigidbody2D rgbd2d;
    Player player;

    [SerializeField] float offsetDistance = 1f;
    [SerializeField] float sizeOfInteractableArea = 1.2f;

    [SerializeField] HighlightManager highlightManager;

    private void Awake()
    {
        player = GetComponent<Player>();
        playerController = GetComponent<Movement>();
        rgbd2d = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) { return; }

        Check();

        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return; }
            Interact();
        }
    }

    private void Check()
    {
        Vector2 position = rgbd2d.position + playerController.lastMotionVector * offsetDistance;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, sizeOfInteractableArea);

        foreach (Collider2D c in colliders)
        {
            Interactable hit = c.GetComponent<Interactable>();
            if (hit != null)
            {
                highlightManager.Highlight(hit.gameObject);
                return;
            }
        }
        
        highlightManager.Hide();
    }

    private void Interact()
    {
        Vector2 position = rgbd2d.position + playerController.lastMotionVector * offsetDistance;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, sizeOfInteractableArea);

        foreach (Collider2D c in colliders)
        {
            Interactable interact = c.GetComponent<Interactable>();
            if (interact != null)
            {
                interact.Interact(player);
                break;
            }
        }
    }
}
