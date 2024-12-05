using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldCollision : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip shieldBreakSound;
    public AudioClip deactivateShield;
    public AudioClip activateShield;

    public ShieldBlinkEffect blinkEffect;

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
        Debug.Log("ShieldCollision audioSource: " + audioSource);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            blinkEffect.BreakShield();
        }
    }

    public void Activate()
    {
        audioSource.PlayOneShot(activateShield);
    }

    public void Deactivate()
    {
        StartCoroutine(DeactivateAfterSound());
    }

    private IEnumerator DeactivateAfterSound()
    {
        audioSource.PlayOneShot(deactivateShield);
        yield return new WaitForSeconds(deactivateShield.length);
        gameObject.SetActive(false);
    }

}
