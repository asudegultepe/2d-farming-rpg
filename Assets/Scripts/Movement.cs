using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    Rigidbody2D rigidbody2d;
    float speed;
    [SerializeField] float walkSpeed = 4;
    [SerializeField] float sprintSpeed = 6;

    Vector2 motionVector;
    public Vector2 lastMotionVector;

    public Animator animator;
    public bool isMoving;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // normalized so walking diagonally isn't faster than walking straight
        motionVector = new Vector2(
            horizontal,
            vertical
            ).normalized;
        animator.SetFloat("horizontal", horizontal);
        animator.SetFloat("vertical", vertical);

        isMoving = horizontal != 0 || vertical != 0;
        animator.SetBool("isMoving", isMoving);

        if (horizontal != 0 || vertical != 0)
        {
            lastMotionVector = new Vector2(
                horizontal,
                vertical
                ).normalized;

            animator.SetFloat("lastHorizontal", horizontal);
            animator.SetFloat("lastVertical", vertical);
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = sprintSpeed;
        } else
        {
            speed = walkSpeed;
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move()
    {
        rigidbody2d.velocity = motionVector * speed;
    }
}
