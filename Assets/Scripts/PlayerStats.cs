using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public int lifePoints = 100;
    public int loadCount = 0;
    public int shieldCount = 3;
    public int shootDamage = 10;
    public int loadCapacity = 3;
    public float dodgeSuccessRate = 0.5f;
    public float disarmSuccessRate = 0.7f;
    public bool isDodging = false;
    public bool isShielding = false;

    public GameObject shield;
    public GameObject projectilePrefab;
    public Transform opponent;
    private AudioSource audioSource;
    private AudioClip shootSound;
    private AudioClip reloadSound;
    private void Start()
    {
        shield.SetActive(false);
        audioSource = GetComponentInChildren<AudioSource>();
        shootSound = Resources.Load<AudioClip>("Audio/shoot");
        reloadSound = Resources.Load<AudioClip>("Audio/reload");
    }

    // Save the settings to PlayerPrefs
    public void SaveGameSettings()
    {
        PlayerPrefs.SetInt("shieldCount", shieldCount);
        PlayerPrefs.SetInt("shootDamage", shootDamage);
        PlayerPrefs.SetInt("loadCapacity", loadCapacity);
        PlayerPrefs.SetFloat("dodgeSuccessRate", dodgeSuccessRate);
        PlayerPrefs.SetFloat("disarmSuccessRate", disarmSuccessRate);
        //PlayerPrefs.SetInt("shootDamage", 20);
        //PlayerPrefs.SetFloat("dodgeSuccessRate", 0.5f);
        //PlayerPrefs.SetFloat("disarmSuccessRate", 0.4f);

        PlayerPrefs.Save();
        Debug.Log("Game settings saved!");
    }
    // Load the settings from PlayerPrefs
    public void LoadGameSettings()
    {
        if (PlayerPrefs.HasKey("shieldCount"))
            shieldCount = PlayerPrefs.GetInt("shieldCount");
        else shieldCount = 3;

        if (PlayerPrefs.HasKey("shootDamage"))
            shootDamage = PlayerPrefs.GetInt("shootDamage");
        else shootDamage = 20;

        if (PlayerPrefs.HasKey("loadCapacity"))
            loadCapacity = PlayerPrefs.GetInt("loadCapacity");
        else loadCapacity = 3;

        if (PlayerPrefs.HasKey("dodgeSuccessRate"))
            dodgeSuccessRate = PlayerPrefs.GetFloat("dodgeSuccessRate");
        else dodgeSuccessRate = 0.5f;

        if (PlayerPrefs.HasKey("disarmSuccessRate"))
            disarmSuccessRate = PlayerPrefs.GetFloat("disarmSuccessRate");
        else disarmSuccessRate = 0.4f;

        Debug.Log($"Settings loaded: shieldCount={shieldCount}, shootDamage={shootDamage}, loadCapacity={loadCapacity}, dodgeSuccessRate={dodgeSuccessRate}, disarmSuccessRate={disarmSuccessRate}");
    }
    /// <summary>
    /// Adds damage to player's life points.
    /// </summary>
    /// <param name="damage"> How much damage to give. </param>
    public void TakeDamage(int damage)
    {
        lifePoints -= damage;
        if (lifePoints < 0)
            lifePoints = 0;
    }
    //  TODO add sound effect
    public void Shoot()
    {
        GameObject projectile = Instantiate(projectilePrefab, gameObject.transform.position, Quaternion.identity);

        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            projScript.SetTarget(opponent);
        }
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    /// <summary>
    /// If player has shields it will be used and the flag set.
    /// </summary>
    /// <remarks>Dont forget to ResetShield() after round</remarks>
    public void UseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            isShielding = true;
            shield.SetActive(true);
            shield.GetComponent<ShieldCollision>().Activate();
        }
        else
        {
            isShielding = false;
        }
    }

    /// <summary>
    /// Adds one round to the gun until max is reached.
    /// </summary>
    public void IncreaseLoad()
    {
        if (loadCount < loadCapacity)
        {
            loadCount++;
            if (audioSource != null && reloadSound != null)
                audioSource.PlayOneShot(reloadSound);
        }
    }

    /// <summary>
    /// Resets loads to 0.
    /// </summary>
    public void ResetLoad()
    {
        loadCount = 0;
    }

    /// <summary>
    /// Undoges the player
    /// </summary>
    public void ResetDodge()
    {
        isDodging = false;
    }
    /// <summary>
    /// Removes the shield from the player.
    /// </summary>
    /// <param name="deactivateSound"> If true, the deactivate sound will be played. Set to false if the other action is shoot because the sound comes from projectile and the blink effect deactivates the shield </param>
    public void ResetShield(bool deactivateSound = true)
    {
        isShielding = false;
        if (deactivateSound)
            shield.GetComponent<ShieldCollision>().Deactivate();
    }
}
