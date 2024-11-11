using Meta.WitAi.TTS.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class GPTAction
{
    public string action;
    public string comment;

    public GPTAction(string action, string comment)
    {
        this.action = action;
        this.comment = comment;
    }
}

public class GameManagerFive : MonoBehaviour
{
    public PlayerStats player;
    public PlayerStats npc;
    public Transform playerObj;
    public FPSController fpsController;
    public TTSSpeaker ttsSpeakerCommentary;
    public TTSSpeaker ttsSpeakerNPC;
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
    /// <summary>
    /// a tupel is: (playerAction, npcAction)
    /// </summary>
    private List<(string, string)> gameHistory = new List<(string, string)>();
    APIKeys keys = APIKeys.Load();
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private List<Message> chatHistory = new List<Message>();
    private GPTAction gptAction;

    private int basicShotDamage = 10;
    private string npcName = "Napoleon";

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == playerObj)
        {
            gameCanvas.SetActive(true);
            actionLog.text = "Select an action!";
            ttsSpeakerCommentary.Speak(actionLog.text);
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
        ttsSpeakerCommentary.VoiceID = "WIT$DISAFFECTED";

        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }

        UpdateUI();
    }

    async void SelectAction(Action action)
    {
        toggleButtons(); // make them unclickable
        playerAction = action;
        // TODO: remove comment for LLM-NPC
        //gptAction = await GetGptAction();
        if (gptAction != null)
        {
            if (Enum.TryParse(gptAction.action, true, out npcAction))
            {
                Debug.Log(gptAction.action);
            }
            else npcAction = (Action)Random.Range(0, 5); // NPC randomly selects an action
        }
        else npcAction = (Action)Random.Range(0, 5); // NPC randomly selects an action
        gameHistory.Add((playerAction.ToString(), npcAction.ToString()));
        EvaluateRound();

    }

    async Task<GPTAction> GetGptAction()
    {
        chatHistory.Add(new Message
        {
            role = "system",
            content = "Game Rules: You play as " + npcName + " against the user. On your turn, choose one action:\nLoad – Prepare your weapon to shoot. You can load multiple times, each allowing one shot.\nShoot – Fire at your opponent if you've loaded at least once. It deducts one load.\nShield – Block damage from a shot or disarm. Limited shields. Using Shield deducts one from your count.\nDodge – Avoid a shot. If successful, the shot misses. Failing takes full damage. Does not prevent disarm.\nDisarm – Reduce your opponent's load to zero. It works if they load, dodge, or try to disarm.\nTurn Mechanics:\nPlayers choose one action per turn. Actions are revealed simultaneously.\nShot: Hits if the opponent isn't shielding or dodging (or dodging fails), dealing damage based on weapon strength.\nDisarm: Zeroes the opponent’s load, preventing them from shooting until they reload.\nResponse Format: {\"action\": \"Action\", \"comment\": \"Your message to the opponent\"}"
        });
        string userMessage = $"Histroy: {GameHistoryAsString()}\n Stats: {StatsString()}";
        Debug.Log(userMessage);
        chatHistory.Add(new Message { role = "user", content = userMessage });
        var requestData = new OpenAIRequest
        {
            model = "gpt-4o",
            messages = chatHistory
        };
        string jsonData = JsonConvert.SerializeObject(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (keys != null)
            {
                request.SetRequestHeader("Authorization", "Bearer " + keys.OpenAIKey);
            }
            var operation = request.SendWebRequest();

            // Await the completion of the request
            while (!operation.isDone)
            {
                await Task.Yield(); // Let the engine continue to update other tasks
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAIResponse response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                string reply = response.choices[0].message.content;
                Debug.Log(reply);
                GPTAction gptAction = JsonConvert.DeserializeObject<GPTAction>(reply);
                return gptAction;
            }
        }
        return null;
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
            actionLog.text = "Both players attemted to disarm!";
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
        if (gptAction?.comment != null) await ttsSpeakerNPC.SpeakTask(gptAction.comment);
        await ttsSpeakerCommentary.SpeakTask(actionLog.text);


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
            ttsSpeakerCommentary.Speak(actionLog.text);
            fpsController.canMove = true;
            npc.LoadGameSettings();
            player.LoadGameSettings();
            chatHistory = new List<Message>();
            gameHistory = new List<(string, string)>();
        }
    }
    string GameHistoryAsString()
    {
        string history = "";
        foreach (var round in gameHistory)
        {
            history += $"Player: {round.Item1} - {npcName}: {round.Item2}\n";
        }
        return history;
    }

    string StatsString()
    {
        return $"Player: Life: {player.lifePoints} Ammo: {player.loadCount}/{player.loadCapacity} Shields: {player.shieldCount}\n" +
            $"{npcName}: Life: {npc.lifePoints} Ammo: {npc.loadCount}/{npc.loadCapacity} Shields: {npc.shieldCount}";
    }
}
