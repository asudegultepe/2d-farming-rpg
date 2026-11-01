using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField] float unSpawnTime = 10f;
    [SerializeField] float speed = 5f;
    [SerializeField] float pickUpDistance = 3f;
    [SerializeField] private GameObject player;

    public Item item;
    public int count = 1;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
    }

    public void Set(Item item, int count)
    {
        this.item = item;
        this.count = count;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = item.icon;
    }

    private void Update()
    {
        player = GameObject.FindWithTag("Player");

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if(distance > pickUpDistance)
        {
            return;
        }

        transform.position = Vector2.MoveTowards(
            transform.position,
            player.transform.position,
            speed * Time.deltaTime
            );

        unSpawnTime -= Time.deltaTime;
        if(unSpawnTime < 0) { Destroy(gameObject); }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        
        if(player)
        {
            if(item != null)
            {
                if(GameManager.instance.inventoryContainer != null)
                {
                    GameManager.instance.inventoryContainer.Add(item, count);
                }
                else
                {
                    Debug.LogWarning("No inventory container attached to the game manager");
                }

                Destroy(gameObject);
            }
        }
    }
}
