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

    // Save the settings to PlayerPrefs
    public void SaveGameSettings()
    {
        PlayerPrefs.SetInt("shieldCount", shieldCount);
        PlayerPrefs.SetInt("shootDamage", shootDamage);
        PlayerPrefs.SetInt("loadCapacity", loadCapacity);
        PlayerPrefs.SetFloat("dodgeSuccessRate", dodgeSuccessRate);
        PlayerPrefs.SetFloat("disarmSuccessRate", disarmSuccessRate);

        // Save changes
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
        else shootDamage = 10;

        if (PlayerPrefs.HasKey("loadCapacity"))
            loadCapacity = PlayerPrefs.GetInt("loadCapacity");
        else loadCapacity = 3;

        if (PlayerPrefs.HasKey("dodgeSuccessRate"))
            dodgeSuccessRate = PlayerPrefs.GetFloat("dodgeSuccessRate");
        else dodgeSuccessRate = 0.5f;

        if (PlayerPrefs.HasKey("disarmSuccessRate"))
            disarmSuccessRate = PlayerPrefs.GetFloat("disarmSuccessRate");
        else disarmSuccessRate = 0.7f;
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

    /// <summary>
    /// If player has shields it will be used and the flag set.
    /// </summary>
    /// <remarks>Dont forget to ResetShield() after round</remarks>
    public void UseShield()
    {
        if (shieldCount > 0)
        {
            shieldCount--;
            this.isShielding = true;
        } else this.isShielding = false;
    }

    /// <summary>
    /// Adds one round to the gun until max is reached.
    /// </summary>
    public void IncreaseLoad()
    {
        if (loadCount < loadCapacity)
            loadCount++;
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
    public void ResetShield()
    {
        isShielding = false;
    }
}
