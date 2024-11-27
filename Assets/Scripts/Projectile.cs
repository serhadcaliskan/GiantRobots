using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 100f;
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
            // Move to target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * speed * Time.deltaTime;
            // Rotate so that projectile is facing the target
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, direction);
            transform.rotation = targetRotation;
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
            
            Destroy(gameObject);
        }
    }
}
