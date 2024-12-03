using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 100f;
    public GameObject impactEffect;


    private Transform target; // Opponent's transform
    private Vector3 initTargetPosition;
    private bool hit = true;

    // Set the target for the projectile
    public void SetTarget(Transform opponent, bool hit)
    {
        target = opponent;
        this.hit = hit;
        initTargetPosition = opponent.position;
    }

    void Update()
    {
        //if (target != null)
        //{
        //    // Move to target
        //    Vector3 direction = hit ? (target.position - transform.position).normalized : (initTargetPosition - transform.position).normalized;
        //    transform.position += direction * speed * Time.deltaTime;
        //    // Rotate so that projectile is facing the target
        //    Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, direction);
        //    transform.rotation = targetRotation;
        //}
        if (target != null)
        {
            Vector3 direction;
            if (hit)
            {
                // If hit is true, move towards the target
                direction = (target.position - transform.position).normalized;
            }
            else
            {
                direction = (initTargetPosition - transform.position);
                direction.y -= 2f;
                direction.Normalize();
            }

            // Move the projectile
            transform.position += direction * speed * Time.deltaTime;

            // Rotate so that the projectile is facing the movement direction
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
            Debug.Log(other.name);
            if (impactEffect != null)
            {
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
