using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerFive : MonoBehaviour
{
    public PlayerStats player;
    public PlayerStats npc;
    public Transform playerObj;
    public FPSController fpsController;
    public TTSSpeaker ttsSpeaker;
    public GameObject gameCanvas;
    public TMP_Text playerLifeText;
    public TMP_Text npcLifeText;
    public TMP_Text actionLog;

    public Button loadButton;
    public Button shootButton;
    public Button shieldButton;
    public Button dodgeButton;
    public Button disarmButton;

    private enum Action { Load, Shoot, Shield, Dodge, Disarm }
    private Action playerAction;
    private Action npcAction;

    private int basicShotDamage = 10;
    private string npcName = "Napoleon";

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == playerObj)
        {
            gameCanvas.SetActive(true);
            actionLog.text = "Select an action!";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            fpsController.canMove = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == playerObj)
        {
            gameCanvas.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Start()
    {
        loadButton.onClick.AddListener(() => SelectAction(Action.Load));
        shootButton.onClick.AddListener(() => SelectAction(Action.Shoot));
        shieldButton.onClick.AddListener(() => SelectAction(Action.Shield));
        dodgeButton.onClick.AddListener(() => SelectAction(Action.Dodge));
        disarmButton.onClick.AddListener(() => SelectAction(Action.Disarm));
        player.LoadGameSettings();
        ttsSpeaker.VoiceID = "WIT$DISAFFECTED";

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        UpdateUI();
    }

    void SelectAction(Action action)
    {
        toggleButtons(); // make them unclickable
        playerAction = action;
        // Todo, implement a more intelligent NPC with LLM who also gives backl an attacksentence
        npcAction = (Action)Random.Range(0, 5); // NPC randomly selects an action

        EvaluateRound();
    }

    void EvaluateRound()
    {
        if (playerAction == Action.Load && npcAction == Action.Load)
        {
            player.IncreaseLoad();
            npc.IncreaseLoad();
            actionLog.text = "Both players loaded!";
        }
        else if (playerAction == Action.Load && npcAction == Action.Shoot)
        {
            player.IncreaseLoad();
            actionLog.text = "You loaded a round!";
            if (npc.loadCount > 0)
            {
                player.TakeDamage(basicShotDamage);
                npc.loadCount--;
                actionLog.text += $"{npcName} shot you!";
            }

        }
        else if (playerAction == Action.Load && npcAction == Action.Shield)
        {
            player.IncreaseLoad();
            actionLog.text = "You loaded a round!";
            npc.UseShield();
            if (npc.isShielding) actionLog.text += $"{npcName} shielded!"; else actionLog.text += $"{npcName} is out of shields!";
        }
        else if (playerAction == Action.Load && npcAction == Action.Dodge)
        {
            player.IncreaseLoad();
            actionLog.text = "You loaded a round!";
        }
        else if (playerAction == Action.Load && npcAction == Action.Disarm)
        {
            if (Random.value <= npc.disarmSuccessRate)
            {
                player.ResetLoad();
                actionLog.text = $"{npcName} disarmed you!";
            }
            else
            {
                player.IncreaseLoad();
                actionLog.text = $"{npcName}'s disarm failed and you loaded one round!";
            }
        }
        else if (playerAction == Action.Shoot && npcAction == Action.Load)
        {
            npc.IncreaseLoad();
            if (player.loadCount > 0)
            {
                npc.TakeDamage(basicShotDamage);
                player.loadCount--;
                actionLog.text = $"You shot {npcName}!";
            }
            else
            {
                actionLog.text = "You are out of ammo!";
            }
        }
        else if (playerAction == Action.Shoot && npcAction == Action.Shoot)
        {
            if (player.loadCount > 0)
            {
                npc.TakeDamage(basicShotDamage);
                player.loadCount--;
                actionLog.text = $"You shot {npcName}!";
            }
            else
            {
                actionLog.text = "You are out of ammo!";
            }
            if (npc.loadCount > 0)
            {
                player.TakeDamage(basicShotDamage);
                npc.loadCount--;
                actionLog.text += $"{npcName} shot you!";
            }

        }
        else if (playerAction == Action.Shoot && npcAction == Action.Shield)
        {
            npc.UseShield();
            if (player.loadCount > 0)
            {
                player.loadCount--;
                if (npc.isShielding)
                {
                    actionLog.text = $"{npcName} shielded!";
                }
                else
                {
                    npc.TakeDamage(basicShotDamage);
                    actionLog.text = $"You shot {npcName}!";
                }
            }
            else
            {
                actionLog.text = "You are out of ammo!";
            }
        }
        else if (playerAction == Action.Shoot && npcAction == Action.Dodge)
        {
            if (player.loadCount > 0)
            {
                player.loadCount--;
                if (Random.value <= npc.dodgeSuccessRate)
                {
                    actionLog.text = $"{npcName} dodged!";
                }
                else
                {
                    npc.TakeDamage(basicShotDamage);
                    actionLog.text = $"You shot {npcName}!";
                }
            }
            else
            {
                actionLog.text = "You are out of ammo!";
            }

        }
        else if (playerAction == Action.Shoot && npcAction == Action.Disarm)
        {
            if (player.loadCount > 0)
            {
                player.loadCount--;
                npc.TakeDamage(basicShotDamage);
                actionLog.text = $"You shot {npcName}!";
            }
            else
            {
                actionLog.text = "You are out of ammo!";
            }
        }
        else if (playerAction == Action.Shield && npcAction == Action.Load)
        {
            npc.IncreaseLoad();
            player.UseShield();
            if (player.isShielding)
            {
                actionLog.text = "You wasted a shield!";
            }
            else
            {
                actionLog.text = "You are out of shields!";
            }
        }
        else if (playerAction == Action.Shield && npcAction == Action.Shoot)
        {
            player.UseShield();
            if (npc.loadCount > 0)
            {
                npc.loadCount--;
                if (player.isShielding)
                {
                    actionLog.text = "You shielded successfully!";
                }
                else
                {
                    player.TakeDamage(basicShotDamage);
                    actionLog.text = "You are out of shields!";
                    actionLog.text += $"{npcName} shot you!";
                }
            }
            else
            {
                actionLog.text = $"{npcName} is out of ammo!";
            }
        }
        else if (playerAction == Action.Shield && npcAction == Action.Shield)
        {
            player.UseShield();
            npc.UseShield();

            if (player.isShielding)
            {
                actionLog.text = "You wasted a shield!";
            }
            else
            {
                actionLog.text = "You are out of shields!";
            }
            if (npc.isShielding)
            {
                actionLog.text += $"{npcName} wasted a shield!";
            }
            else
            {
                actionLog.text += $"{npcName} is out of shields!";
            }
        }
        else if (playerAction == Action.Shield && npcAction == Action.Dodge)
        {
            player.UseShield();
            if (player.isShielding)
            {
                actionLog.text = "You wasted a shield!";
            }
            else
            {
                actionLog.text = "You are out of shields!";
            }
        }
        else if (playerAction == Action.Shield && npcAction == Action.Disarm)
        {
            player.UseShield();
            if (!player.isShielding)
            {
                actionLog.text = "You are out of shields!";
                if (Random.value <= npc.disarmSuccessRate)
                {
                    player.ResetLoad();
                    actionLog.text += $"{npcName} disarmed you!";
                }
                else
                {
                    actionLog.text += $"{npcName}'s disarm failed!";
                }
            }
            else actionLog.text = "You shielded successfully!";
        }
        else if (playerAction == Action.Dodge && npcAction == Action.Load)
        {
            npc.IncreaseLoad();
            actionLog.text = $"{npcName} loaded a round!";
        }
        else if (playerAction == Action.Dodge && npcAction == Action.Shoot)
        {
            if (npc.loadCount > 0)
            {
                npc.loadCount--;
                if (Random.value <= player.dodgeSuccessRate)
                {
                    actionLog.text = "You dodged successfully!";
                }
                else
                {
                    player.TakeDamage(basicShotDamage);
                    actionLog.text = "You failed to dodge!";
                    actionLog.text += $"{npcName} shot you!";
                }
            }
            else
            {
                actionLog.text = $"{npcName} is out of ammo!";
            }
        }
        else if (playerAction == Action.Dodge && npcAction == Action.Shield)
        {
            npc.UseShield();
            if (npc.isShielding)
            {
                actionLog.text = $"{npcName} wasted a shield!";
            }
            else
            {
                actionLog.text = $"{npcName} is out of shields!";
            }
        }
        else if (playerAction == Action.Dodge && npcAction == Action.Dodge)
        {
            actionLog.text = "Both players dodged!";
        }
        else if (playerAction == Action.Dodge && npcAction == Action.Disarm)
        {
            if (Random.value <= npc.disarmSuccessRate)
            {
                player.ResetLoad();
                actionLog.text = $"{npcName} disarmed you!";
            }
            else
            {
                actionLog.text = $"{npcName}'s disarm failed!";
            }
        }
        else if (playerAction == Action.Disarm && npcAction == Action.Load)
        {
            if (Random.value <= player.disarmSuccessRate)
            {
                npc.ResetLoad();
                actionLog.text = $"You disarmed {npcName}!";
            }
            else
            {
                actionLog.text = "Your disarm failed!";
                npc.IncreaseLoad();
            }
        }
        else if (playerAction == Action.Disarm && npcAction == Action.Shoot)
        {
            if (npc.loadCount > 0)
            {
                npc.loadCount--;
                player.TakeDamage(basicShotDamage);
                actionLog.text = "Your disarm failed!";
                actionLog.text += $"{npcName} shot you!";
            }
            else
            {
                actionLog.text = $"{npcName} is out of ammo!";
            }
        }
        else if (playerAction == Action.Disarm && npcAction == Action.Shield)
        {
            npc.UseShield();
            if (Random.value <= player.disarmSuccessRate)
            {
                if (!npc.isShielding)
                {
                    actionLog.text = $"You disarmed {npcName}!";
                    npc.ResetLoad();
                }
                else
                {
                    actionLog.text = $"Your disarm failed because {npcName} used a shield!";
                }
            }
            else
            {
                actionLog.text = "Your disarm failed!";
            }
        }
        else if (playerAction == Action.Disarm && npcAction == Action.Dodge)
        {
            if (Random.value <= player.disarmSuccessRate)
            {
                npc.ResetLoad();
                actionLog.text = $"You disarmed {npcName}!";
            }
            else actionLog.text = "Your disarm failed!";
        }
        else if (playerAction == Action.Disarm && npcAction == Action.Disarm)
        {
            actionLog.text ="Both players attemted to disarm!";
            if (Random.value <= player.disarmSuccessRate)
            {
                npc.ResetLoad();
                actionLog.text += $"You disarmed {npcName}!";
            }
            if (Random.value <= npc.disarmSuccessRate)
            {
                player.ResetLoad();
                actionLog.text += $"{npcName} disarmed you!";
            }
        }

        // Reset states
        player.ResetDodge(); player.ResetShield();
        npc.ResetDodge(); npc.ResetShield();

        SpeakAndWait();
    }

    public async Task SpeakAndWait()
    {
        // Wait until the speech is done
        await ttsSpeaker.SpeakTask(actionLog.text);

        UpdateUI();
        CheckGameEnd();
        toggleButtons();
    }

    void toggleButtons()
    {
        loadButton.interactable = !loadButton.interactable;
        shootButton.interactable = !shootButton.interactable;
        shieldButton.interactable = !shieldButton.interactable;
        dodgeButton.interactable = !dodgeButton.interactable;
        disarmButton.interactable = !disarmButton.interactable;
    }

    //void ExecuteTurn()
    //{
    //    //Check dodging
    //    player.isDodging = playerAction == Action.Dodge && Random.value <= player.dodgeSuccessRate;
    //    npc.isDodging = npcAction == Action.Dodge && Random.value <= npc.dodgeSuccessRate;

    //    // Process actions for both players
    //    if (playerAction == Action.Shoot && player.loadCount > 0)
    //    {
    //        if (npcAction != Action.Shield && !npc.isDodging)
    //        {
    //            npc.TakeDamage(basicShotDamage);
    //            actionLog.text = $"You shot {npcName}!";
    //        }
    //        else
    //        {
    //            actionLog.text = $"You shot but {npcName} successfully";
    //            if (npcAction == Action.Shield)
    //                actionLog.text += " shielded!";
    //            else if (npc.isDodging)
    //                actionLog.text += " dodged!";
    //        }
    //        player.loadCount--; // Decrease the load count after shooting
    //    }
    //    else if (playerAction == Action.Disarm)
    //    {
    //        if (Random.value <= player.disarmSuccessRate && npcAction != Action.Shield && npcAction != Action.Shoot) // 70% chance to disarm NPC
    //        {
    //            npc.ResetLoad();
    //            actionLog.text = $"You disarmed {npcName}!";
    //        }
    //        else
    //        {
    //            actionLog.text = "Your disarm failed!";
    //        }
    //    }
    //    else if (playerAction == Action.Shield)
    //    {
    //        player.UseShield();
    //        actionLog.text = "You shielded!";
    //    }
    //    else if (playerAction == Action.Dodge)
    //    {
    //        if (Random.value <= player.dodgeSuccessRate) // 50% chance to dodge
    //        {
    //            player.isDodging = true;
    //            actionLog.text = "Player successfully dodges!";
    //        }
    //        else
    //        {
    //            player.isDodging = false;
    //            actionLog.text = "Player fails to dodge!";
    //        }
    //    }

    //    // NPC action
    //    if (npcAction == Action.Shoot && npc.loadCount > 0)
    //    {
    //        if (playerAction != Action.Shield && !player.isDodging)
    //        {
    //            player.TakeDamage(basicShotDamage);
    //            actionLog.text = $"{npcName} shot you!";
    //        }
    //        else
    //        {
    //            actionLog.text = "NPC shoots but Player dodged or shielded!";
    //        }
    //        npc.loadCount--;
    //    }
    //    else if (npcAction == Action.Disarm)
    //    {
    //        if (Random.value <= npc.disarmSuccessRate) // 70% chance to disarm Player
    //        {
    //            player.ResetLoad();
    //            actionLog.text = "NPC disarms Player!";
    //        }
    //        else
    //        {
    //            actionLog.text = "NPC's disarm failed!";
    //        }
    //    }
    //    else if (npcAction == Action.Shield)
    //    {
    //        npc.UseShield();
    //        actionLog.text = "NPC shields!";
    //    }
    //    else if (npcAction == Action.Dodge)
    //    {
    //        if (Random.value <= npc.dodgeSuccessRate) // 50% chance to dodge
    //        {
    //            npc.isDodging = true;
    //            actionLog.text = "NPC successfully dodges!";
    //        }
    //        else
    //        {
    //            npc.isDodging = false;
    //            actionLog.text = "NPC fails to dodge!";
    //        }
    //    }

    //    // Reset dodging state
    //    player.ResetDodge();
    //    npc.ResetDodge();

    //    UpdateUI();
    //    CheckGameEnd();
    //}

    void UpdateUI()
    {
        playerLifeText.text = "Player Life: " + player.lifePoints + " Ammo: " + player.loadCount + "/" + player.loadCapacity + " Shields: " + player.shieldCount;
        npcLifeText.text = "NPC Life: " + npc.lifePoints + " Ammo: " + npc.loadCount + "/" + npc.loadCapacity + " Shields: " + npc.shieldCount;
    }

    void CheckGameEnd()
    {
        if (player.lifePoints <= 0 && npc.lifePoints <= 0)
        {
            actionLog.text = "It's a draw!";
        }
        else if (player.lifePoints <= 0)
        {
            actionLog.text = "NPC Wins!";
        }
        else if (npc.lifePoints <= 0)
        {
            actionLog.text = "You Win!";
        }
        if (player.lifePoints <= 0 || npc.lifePoints <= 0)
        {
            ttsSpeaker.Speak(actionLog.text);
            fpsController.canMove = true;
            npc.LoadGameSettings();
            player.LoadGameSettings();
        }

    }
}
