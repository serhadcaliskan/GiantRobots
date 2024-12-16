using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : Stats
{
    /// <summary>
    /// Load the settings from PlayerPrefs, if not found, gets the default values
    /// </summary>
    public override void LoadGameSettings()
    {
        if(PlayerPrefs.HasKey("lifePoints"))
            lifePoints = PlayerPrefs.GetInt("lifePoints");
        else lifePoints = 100;

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
        else disarmSuccessRate = 0.5f;
    }
}
