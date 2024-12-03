using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldCollision : MonoBehaviour // Todo: Make player shield visible, currently it is invisible bc we are inside the spehere
{
    private AudioSource audioSource;
    public AudioClip shieldBreakSound;
    public AudioClip deactivateShield;
    public AudioClip activateShield;

    public ShieldBlinkEffect blinkEffect;

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            //audioSource.PlayOneShot(shieldBreakSound);
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
