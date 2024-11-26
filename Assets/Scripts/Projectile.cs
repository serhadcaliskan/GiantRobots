using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 50f;
    public GameObject impactEffect;

    private Transform target; // Opponent's transform

    // Set the target for the projectile
    public void SetTarget(Transform opponent)
    {
        target = opponent;
    }

    void Update()
    {
        if (target != null)
        {
            // Move the projectile toward the target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == target || other.CompareTag("Shield"))
        {
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }
            // TODO Destroy also if missed the target
            // TODO add sound effect
            Destroy(gameObject);
        }
    }
}
