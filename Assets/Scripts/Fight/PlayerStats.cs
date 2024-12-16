using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : Stats
{
    // Save the settings to PlayerPrefs
    public override void SaveGameSettings()
    {
        PlayerPrefs.SetInt("lifePoints", lifePoints);
        PlayerPrefs.SetInt("shieldCount", shieldCount);
        PlayerPrefs.SetInt("shootDamage", shootDamage);
        PlayerPrefs.SetInt("loadCapacity", loadCapacity);
        PlayerPrefs.SetFloat("dodgeSuccessRate", dodgeSuccessRate);
        PlayerPrefs.SetFloat("disarmSuccessRate", disarmSuccessRate);

        PlayerPrefs.Save();
        Debug.Log("Game settings saved!");
    }
    // Load the settings from PlayerPrefs
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
    }
}
