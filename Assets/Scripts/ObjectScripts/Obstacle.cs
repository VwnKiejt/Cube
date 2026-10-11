using System;
using UnityEngine;

public class Obstacles : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 5f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.tag == "Gear")
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            player.Die();
        }       
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
            player.Die();
    }
}
