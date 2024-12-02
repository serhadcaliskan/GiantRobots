using Meta.WitAi.TTS.Utilities;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using Oculus.Voice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[System.Serializable]
public class GPTAction
{
    public string action;

    public GPTAction(string action)
    {
        this.action = action;
    }
}
[System.Serializable]
public class GPTReaction
{
    public string reaction;

    public GPTReaction(string reaction)
    {
        this.reaction = reaction;
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
    private Button highlightedButton;

    private enum Action { Load, Shoot, Shield, Dodge, Disarm }
    private Action playerAction;
    private int difficulty = 1;
    private float shieldSoundLength = 0f;
    private Action npcAction;
    private Color defaultColor;
    public Color highlightedColor = Color.green;
    public AppVoiceExperience voiceExperience;
    private bool isPlayerTurn = true; // gets edited in toggleButtons
    [SerializeField]
    /// <summary>
    /// a tupel is: (playerAction, npcAction)
    /// </summary>
    private List<(Action, Action)> gameHistory = new List<(Action, Action)>();
    private List<Action> npcCoolDowns = new List<Action>();
    Dictionary<Action, int> cooldownPeriods = new Dictionary<Action, int>
{
    { Action.Shoot, 1 },
    { Action.Shield, 2 },
    { Action.Disarm, 1 }
}; Dictionary<Action, int> actionCooldowns = new Dictionary<Action, int>
{
    { Action.Shoot, 0 },
    { Action.Shield, 0 },
    { Action.Disarm, 0 },
    { Action.Load, 0 },    // Load and Dodge usually won’t have cooldowns
    { Action.Dodge, 0 }
};
    APIKeys keys = APIKeys.Load();
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private List<Message> chatHistory = new List<Message>();
    private GPTAction gptAction;

    public string npcName = "Pirate";
    private void Start()
    {
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }
        if (loadButton != null)
        {
            defaultColor = loadButton.GetComponent<Image>().color;
        }
    }
    public void EnterGame(Collider other)
    {
        if (other.transform == playerObj)
        {
            // TODO: use rightShift as push-to-talk [WitAI command detection] to select an action and enter to submit
            loadButton.onClick.AddListener(() => SelectAction(Action.Load));
            shootButton.onClick.AddListener(() => SelectAction(Action.Shoot));
            shieldButton.onClick.AddListener(() => SelectAction(Action.Shield));
            dodgeButton.onClick.AddListener(() => SelectAction(Action.Dodge));
            disarmButton.onClick.AddListener(() => SelectAction(Action.Disarm));
            player.LoadGameSettings();
            npc.LoadGameSettings();
            ttsSpeakerCommentary.VoiceID = "WIT$DISAFFECTED";
            shieldSoundLength = Resources.Load<AudioClip>("Audio/deactivateShield").length;

            UpdateUI();
            gameCanvas.SetActive(true);
            actionLog.text = "Select an action!";
            //ttsSpeakerCommentary.Speak(actionLog.text);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            fpsController.canMove = false;
            npc.startFight();
            player.startFight();
        }
    }

    public void ExitGame(Collider other)
    {
        if (other.transform == playerObj)
        {
            gameCanvas.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    async void SelectAction(Action action)
    {
        toggleButtons(); // make them unclickable
        ResetAllButtons(); // reset their color
        playerAction = action;
        // TODO: remove comment for LLM-NPC
        //gptAction = await GetGptAction();
        if (gptAction != null)
        {
            if (Enum.TryParse(gptAction.action, true, out npcAction))
            {
                Debug.Log(gptAction.action);
            }
            else npcAction = getKiAction(difficulty);
        }
        else npcAction = getKiAction(difficulty);
        gameHistory.Add((playerAction, npcAction));
        EvaluateRound();

    }

    async Task<GPTAction> GetGptAction()
    {
        chatHistory.Add(new Message
        {
            role = "system",
            content = "Game Rules: You play as " + npcName + " against the user. On your turn, choose one action:\nLoad – Prepare your weapon to shoot. You can load multiple times, each allowing one shot.\nShoot – Fire at your opponent if you've loaded at least once. It deducts one load.\nShield – Block damage from a shot or disarm. Limited shields. Using Shield deducts one from your count.\nDodge – Avoid a shot. If successful, the shot misses. Failing takes full damage. Does not prevent disarm.\nDisarm – Reduce your opponent's load to zero. It works if they load, dodge, or try to disarm.\nTurn Mechanics:\nPlayers choose one action per turn. Actions are revealed simultaneously.\nShot: Hits if the opponent isn't shielding or dodging (or dodging fails), dealing damage based on weapon strength.\nDisarm: Zeroes the opponent’s load, preventing them from shooting until they reload.\nResponse Format: {\"action\": \"Action\"}"
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
        GPTAction gptAction = JsonConvert.DeserializeObject<GPTAction>(await GetOpenAIResponseAsync(jsonData));
        return gptAction;
    }

    Action getKiAction(int difficulty)
    {
        // Ensure difficulty is within bounds
        if (difficulty < 1 || difficulty > 3) difficulty = difficulty % 3 + 1;
        if (gameHistory?.Count == 0) return Action.Load; // AI loads on the first round


        // Decrement cooldowns each turn
        DecrementCooldowns();

        // Retrieve player and AI states
        int playerLife = player.lifePoints;
        int playerAmmo = player.loadCount;
        int playerShields = player.shieldCount;

        int aiLife = npc.lifePoints;
        int aiAmmo = npc.loadCount;
        int aiShields = npc.shieldCount;
        // Difficulty 1: Simple random action selection with minor tweaks for realism
        if (difficulty == 1)
        {
            List<Action> possibleActions = new List<Action>();

            if (aiAmmo > 0 && !IsOnCooldown(Action.Shoot)) possibleActions.Add(Action.Shoot);
            if (aiAmmo == 0 || Random.value < 0.3f) possibleActions.Add(Action.Load); // Slight preference for loading when low on ammo
            if (aiShields > 0 && !IsOnCooldown(Action.Shield)) possibleActions.Add(Action.Shield);
            possibleActions.Add(Action.Dodge);
            if (playerAmmo > 0 && !IsOnCooldown(Action.Disarm)) possibleActions.Add(Action.Disarm);

            var selectedAction = possibleActions[Random.Range(0, possibleActions.Count)];
            SetCooldown(selectedAction);
            return selectedAction;
        }

        // Difficulty 2: Slightly smarter; attempts to anticipate the player's moves
        else if (difficulty == 2)
        {
            var lastPlayerAction = gameHistory?.LastOrDefault().Item1;

            if (playerAmmo > 0)
            {
                if ((float)playerAmmo / player.loadCapacity >= 0.6f && aiShields > 0 && !IsOnCooldown(Action.Shield))
                {
                    SetCooldown(Action.Shield);
                    return Action.Shield;
                }
                if (Random.value < 0.5) return Action.Dodge;
            }
            if (aiAmmo == 0) return Action.Load;
            if (aiAmmo > 0 && lastPlayerAction != Action.Shield && !IsOnCooldown(Action.Shoot))
            {
                SetCooldown(Action.Shoot);
                return Action.Shoot;
            }
            if (lastPlayerAction == Action.Load && playerAmmo > 0 && !IsOnCooldown(Action.Disarm))
            {
                SetCooldown(Action.Disarm);
                return Action.Disarm;
            }

            var selectedAction = aiAmmo > 0 ? Action.Shoot : Action.Load;
            SetCooldown(selectedAction);
            return selectedAction;
        }

        // Difficulty 3: Advanced AI with dynamic weighting, endgame aggressiveness, and cooldown management
        else if (difficulty == 3)
        {
            var lastPlayerAction = gameHistory?.LastOrDefault().Item1;
            bool playerLikelyToShoot = playerAmmo > 0 && lastPlayerAction == Action.Load;
            bool playerLikelyToLoad = lastPlayerAction == Action.Disarm || playerAmmo == 0;

            // High likelihood of Shield or Dodge if AI anticipates a shot
            if (playerLikelyToShoot && !IsOnCooldown(Action.Shield))
            {
                SetCooldown(Action.Shield);
                return aiShields > 0 ? Action.Shield : Action.Dodge;
            }

            // Prefer Disarm if player is likely to load
            if (playerLikelyToLoad && Random.value < 0.8 && !IsOnCooldown(Action.Disarm))
            {
                SetCooldown(Action.Disarm);
                return Action.Disarm;
            }

            // Endgame strategy: more aggressive if AI has lower life
            if (aiLife < playerLife * 0.3 && aiAmmo > 0 && !IsOnCooldown(Action.Shoot))
            {
                SetCooldown(Action.Shoot);
                return Action.Shoot;
            }

            // Conditional action weights
            float healthFactor = aiLife < playerLife ? 1.2f : 0.8f;
            float ammoFactor = aiAmmo > playerAmmo ? 1.1f : 0.9f;
            float shieldFactor = aiShields > 0 ? 1.1f : 0.7f;

            // Weighted probabilities for each action
            float shootWeight = 0.4f * healthFactor * ammoFactor;
            float shieldWeight = 0.2f * shieldFactor;
            float dodgeWeight = 0.2f;
            float loadWeight = aiAmmo == 0 ? 0.5f : 0.1f;
            float disarmWeight = playerAmmo > 0 ? 0.3f : 0.1f;

            // Compile weighted actions, skipping actions on cooldown
            List<(Action, float)> weightedActions = new List<(Action, float)>
        {
            (Action.Shoot, shootWeight),
            (Action.Shield, shieldWeight),
            (Action.Dodge, dodgeWeight),
            (Action.Load, loadWeight),
            (Action.Disarm, disarmWeight)
        }.Where(a => !IsOnCooldown(a.Item1)).ToList();

            // Normalize weights and select action
            float totalWeight = weightedActions.Sum(a => a.Item2);
            float rand = Random.value * totalWeight;
            float cumulative = 0;

            foreach (var (action, weight) in weightedActions)
            {
                cumulative += weight;
                if (rand < cumulative)
                {
                    SetCooldown(action);
                    return action;
                }
            }

            // Fallback in case no actions are available (should rarely happen)
            var fallbackAction = aiAmmo > 0 ? Action.Shoot : Action.Load;
            SetCooldown(fallbackAction);
            return fallbackAction;
        }

        // Default fallback if all else fails
        var defaultAction = (Action)Random.Range(0, 5);
        SetCooldown(defaultAction);
        return defaultAction;
    }

    async Task<string> getKiReaction()
    {
        List<Message> chat = new List<Message>
        {
            new Message
            {
                role = "system",
                content = $"You are {npcName}, playing against the user. Your task is to send me a short reaction of {npcName} to the outcome of the current round. Keep it short. \"You\" is the user. Answer strictly in format \"{{ \"reaction\": \"your-reaction\"}}\""
            },
            new Message
            {
                role = "user",
                content = $"Actions:\nPlayer: {playerAction} - {npcName}: {npcAction}\nOutcome: {actionLog.text}"
            }
        };
        var requestData = new OpenAIRequest
        {
            model = "gpt-4o-mini",
            messages = chat
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        string response = await GetOpenAIResponseAsync(jsonData);
        GPTReaction gptReaction = JsonConvert.DeserializeObject<GPTReaction>(response);
        if (gptReaction != null)
            return gptReaction.reaction;
        return $"I {npcAction}";
    }
    void EvaluateRound()
    {
        // TODO when sending actionLog to GPT, tell it "You" is the other player
        actionLog.text = "";
        switch (playerAction)
        {
            case Action.Load:
                player.IncreaseLoad();
                switch (npcAction)
                {
                    case Action.Load:
                        npc.IncreaseLoad();
                        actionLog.text = "Both players loaded!";
                        break;

                    case Action.Shoot:
                        actionLog.text = "You loaded a round!";
                        if (npc.loadCount > 0)
                        {
                            player.TakeDamage(npc.shootDamage);
                            npc.loadCount--;
                            npc.Shoot(true);
                            actionLog.text += $"{npcName} shot you!";
                        }
                        else actionLog.text += $"{npcName} tried to shoot without ammo!";
                        break;

                    case Action.Shield:
                        actionLog.text = "You loaded a round!";
                        npc.UseShield();
                        actionLog.text += npc.isShielding ? $"{npcName} wasted a shield!" : $"{npcName} tried to shield without having shields!";
                        break;

                    case Action.Dodge:
                        actionLog.text = $"You loaded a round and {npcName} dodged!";
                        npc.isDodging = true;
                        break;

                    case Action.Disarm:
                        if (Random.value <= npc.disarmSuccessRate)
                        {
                            player.ResetLoad();
                            actionLog.text = $"{npcName} disarmed you, your loading failed!";
                        }
                        else
                        {
                            actionLog.text = $"{npcName}'s disarm failed, you loaded a round!";
                        }
                        break;
                }
                break;

            case Action.Shoot:
                if (player.loadCount <= 0) actionLog.text = "You tried to shoot without ammo!";

                switch (npcAction)
                {
                    case Action.Load:
                        npc.IncreaseLoad();
                        if (player.loadCount > 0)
                        {
                            player.loadCount--;
                            player.Shoot(true);
                            npc.TakeDamage(player.shootDamage);
                            actionLog.text = $"You shot {npcName}! {npcName} loaded a round!";
                        }
                        break;

                    case Action.Shoot:
                        if (player.loadCount > 0)
                        {
                            player.loadCount--;
                            player.Shoot(true);
                            npc.TakeDamage(player.shootDamage);
                            actionLog.text = $"You shot {npcName}!";
                        }

                        if (npc.loadCount > 0)
                        {
                            player.TakeDamage(npc.shootDamage);
                            npc.loadCount--;
                            npc.Shoot(true);
                            actionLog.text += $"{npcName} shot you!";
                        }
                        else actionLog.text += $"{npcName} tried to shoot without ammo!";
                        break;

                    case Action.Shield:
                        npc.UseShield();
                        if (player.loadCount > 0)
                        {
                            player.loadCount--;
                            player.Shoot(true);
                            if (npc.isShielding)
                            {
                                actionLog.text = $"{npcName} shielded your shot!";
                            }
                            else
                            {
                                npc.TakeDamage(player.shootDamage);
                                actionLog.text = $"{npcName} tried to shield without having shields. You shot {npcName}!";
                            }
                        }
                        else actionLog.text += npc.isShielding ? $"{npcName} wasted a shield!" : $"{npcName} tried to shield without having shields.";
                        break;

                    case Action.Dodge:
                        npc.isDodging = true;
                        if (player.loadCount > 0)
                        {
                            player.loadCount--;
                            if (Random.value <= npc.dodgeSuccessRate)
                            {
                                actionLog.text = $"{npcName} dodged your shot!";
                                player.Shoot(false);
                            }
                            else
                            {
                                npc.TakeDamage(player.shootDamage);
                                player.Shoot(true);
                                actionLog.text = $"{npcName}'s dodge failed! You shot {npcName}!";
                            }
                        }
                        break;

                    case Action.Disarm:
                        if (player.loadCount > 0)
                        {
                            player.loadCount--;
                            player.Shoot(true);
                            npc.TakeDamage(player.shootDamage);
                            actionLog.text = $"You shot {npcName}! {npcName} failed to disarm you!";
                        }
                        else actionLog.text += $"{npcName} tried to disarm you!";
                        break;
                }
                break;

            case Action.Shield:
                player.UseShield();
                switch (npcAction)
                {
                    case Action.Load:
                        npc.IncreaseLoad();
                        actionLog.text = $"{npcName} loaded! " + (player.isShielding ? "You wasted a shield!" : "You tried to shield without having shields!");
                        break;

                    case Action.Shoot:
                        if (npc.loadCount > 0)
                        {
                            npc.loadCount--;
                            npc.Shoot(true);
                            if (player.isShielding)
                            {
                                actionLog.text = $"You shielded {npcName}'s shot!";
                            }
                            else
                            {
                                player.TakeDamage(npc.shootDamage);
                                actionLog.text = $"You tried to shield without having shields! {npcName} shot you!";
                            }
                        }
                        else
                        {
                            actionLog.text = $"{(player.isShielding ? "You wasted a shield," : "You tried to shield without having shields!")} {npcName} tried to shoot without ammo!";
                        }
                        break;

                    case Action.Shield:
                        npc.UseShield();
                        actionLog.text = (player.isShielding ? "You wasted a shield!" : "You tried to shield without having shields!") +
                                         (npc.isShielding ? $"{npcName} wasted a shield!" : $"{npcName} tried to shield without having shields!");
                        break;

                    case Action.Dodge:
                        npc.isDodging = true;
                        actionLog.text = $"{npcName} dodged, " + (player.isShielding ? "You wasted a shield!" : "You tried to shield without having shields!");
                        break;

                    case Action.Disarm:
                        if (!player.isShielding)
                        {
                            actionLog.text = "You tried to shield without having shields!";
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
                        else
                        {
                            actionLog.text = $"You shielded {npcName}'s disarm attempt!";
                        }
                        break;
                }
                break;

            case Action.Dodge:
                //player.isDodging = true;
                switch (npcAction)
                {
                    case Action.Load:
                        npc.IncreaseLoad();
                        actionLog.text = $"{npcName} loaded a round! You dodged";
                        break;

                    case Action.Shoot:
                        if (npc.loadCount > 0)
                        {
                            npc.loadCount--;
                            if (Random.value <= player.dodgeSuccessRate)
                            {
                                actionLog.text = $"You dodged {npcName}'s shot!";
                                npc.Shoot(false);
                            }
                            else
                            {
                                player.TakeDamage(npc.shootDamage);
                                actionLog.text = $"You failed to dodge! {npcName} shot you!";
                                npc.Shoot(true);
                            }
                        }
                        else
                        {
                            actionLog.text = $"{npcName} tried to shoot without ammo, you dodged!";
                        }
                        break;

                    case Action.Shield:
                        npc.UseShield();
                        actionLog.text = "You dodged, " + (npc.isShielding ? $"{npcName} wasted a shield!" : $"{npcName} tried to shield without having shields!");
                        break;

                    case Action.Dodge:
                        actionLog.text = "Both players dodged!";
                        npc.isDodging = true;
                        break;

                    case Action.Disarm:
                        if (Random.value <= npc.disarmSuccessRate)
                        {
                            player.ResetLoad();
                            actionLog.text = $"You failed to dodge, {npcName} disarmed you!";
                        }
                        else
                        {
                            actionLog.text = $"You dodged, {npcName}'s disarm failed by probability!";
                        }
                        break;
                }
                break;

            case Action.Disarm:
                switch (npcAction)
                {
                    case Action.Load:
                        if (Random.value <= player.disarmSuccessRate)
                        {
                            actionLog.text = $"You disarmed {npcName} who tried to load!";
                            npc.ResetLoad();
                        }
                        else
                        {
                            npc.IncreaseLoad();
                            actionLog.text = $"Your disarm failed! {npcName} loaded 1 round";
                        }
                        break;

                    case Action.Shoot:
                        if (npc.loadCount > 0)
                        {
                            npc.loadCount--;
                            npc.Shoot(true);
                            player.TakeDamage(npc.shootDamage);
                            actionLog.text = $"Your disarm failed! {npcName} shot you!";
                        }
                        else
                        {
                            actionLog.text = $"{npcName} tried to shoot without ammo, you didnt need to disarm!";
                        }
                        break;

                    case Action.Shield:
                        npc.UseShield();
                        if (!npc.isShielding)
                        {
                            if (Random.value <= player.disarmSuccessRate)
                            {
                                actionLog.text = $"You disarmed {npcName}.";
                                npc.ResetLoad();
                            }
                            else actionLog.text = $"Your disarm failed by probability!";
                            actionLog.text += $"{npcName} tried to shield without having shields!";
                        }
                        else
                        {
                            actionLog.text = $"{npcName} shielded your disarm!";
                        }
                        break;

                    case Action.Dodge:
                        npc.isDodging = true;
                        if (Random.value <= player.disarmSuccessRate)
                        {
                            actionLog.text = $"You disarmed {npcName}!";
                            npc.ResetLoad();
                        }
                        else
                            actionLog.text = "Your disarm failed by probability!";
                        break;

                    case Action.Disarm:
                        actionLog.text = "Both players attempted to disarm!";
                        if (Random.value <= player.disarmSuccessRate)
                        {
                            actionLog.text += $"You disarmed {npcName}!";
                            npc.ResetLoad();
                        }
                        else actionLog.text += $"Your disarm failed by probability!";
                        if (Random.value <= npc.disarmSuccessRate)
                        {
                            actionLog.text += $"{npcName} disarmed you!";
                            player.ResetLoad();
                        }
                        else actionLog.text += $"{npcName}'s disarm failed by probability!";
                        break;
                }
                break;
        }
        Debug.Log($"Actions:\nPlayer: {playerAction} - {npcName}: {npcAction}\nOutcome: {actionLog.text}");

        SpeakAndWait();
    }

    public async Task SpeakAndWait()
    {
        // Wait until the speech is done
        UpdateUI();
        string reaction = $"I {npcAction}";
        // TODO: remove comment for LLM-NPC
        reaction = await getKiReaction();
        actionLog.text += "\n" + reaction;
        await ttsSpeakerNPC.SpeakTask(reaction);
        //await ttsSpeakerCommentary.SpeakTask(actionLog.text);

        // Reset states
        player.ResetDodge();
        npc.ResetDodge();
        float time = 0;
        if (player.isShielding)
        {
            player.ResetShield(npcAction != Action.Shoot);// if the action is shoot, the sound comes from projectile and the blink effect deactivates the shield
            time = shieldSoundLength;
        }

        if (npc.isShielding)
        {
            npc.ResetShield(playerAction != Action.Shoot);
            time = shieldSoundLength;
        }

        CheckGameEnd();
        StartCoroutine(DelayedToggle(time));
    }

    private IEnumerator DelayedToggle(float time = 0)
    {
        yield return new WaitForSeconds(time); // Pause to be sure shield is deactivated
        toggleButtons();
    }

    void toggleButtons()
    {
        loadButton.interactable = !loadButton.interactable;
        shootButton.interactable = !shootButton.interactable;
        shieldButton.interactable = !shieldButton.interactable;
        dodgeButton.interactable = !dodgeButton.interactable;
        disarmButton.interactable = !disarmButton.interactable;
        isPlayerTurn = !isPlayerTurn;
    }

    public void SelectButton(string action)
    {
        // Reset all buttons to default color
        ResetAllButtons();

        // Highlight the button corresponding to the action
        switch (action)
        {
            case "Load":
                HighlightButton(loadButton);
                break;
            case "Shoot":
                HighlightButton(shootButton);
                break;
            case "Shield":
                HighlightButton(shieldButton);
                break;
            case "Dodge":
                HighlightButton(dodgeButton);
                break;
            case "Disarm":
                HighlightButton(disarmButton);
                break;
            default:
                Debug.LogWarning("Unknown action: " + action);
                break;
        }
    }

    private void HighlightButton(Button button)
    {
        highlightedButton = button;
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = highlightedColor; // Set the highlight color
        }
    }
    private void ResetAllButtons()
    {
        // Reset all buttons to the default color
        loadButton.GetComponent<Image>().color = defaultColor;
        shootButton.GetComponent<Image>().color = defaultColor;
        shieldButton.GetComponent<Image>().color = defaultColor;
        dodgeButton.GetComponent<Image>().color = defaultColor;
        disarmButton.GetComponent<Image>().color = defaultColor;
        highlightedButton = null;
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
            gameHistory = new List<(Action, Action)>();
            npc.inFight = false;
            player.inFight = false;
        }
    }

    /// <summary>
    /// Gets OpenAI response from JSON data, TODO move to a helper class
    /// </summary>
    /// <param name="jsonData">Only the data as serialized OpenAIRequest, token will be added in method</param>
    /// <returns>a the GPT answer to your message</returns>
    async Task<string> GetOpenAIResponseAsync(string jsonData)
    {
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
                if (reply != null)
                {
                    return reply;
                }
            }
        }
        return null;
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

    bool IsOnCooldown(Action action) => actionCooldowns[action] > 0;

    // Helper function to set cooldown for an action after use
    void SetCooldown(Action action)
    {
        if (cooldownPeriods.ContainsKey(action))
            actionCooldowns[action] = cooldownPeriods[action];
    }

    // Decrement cooldowns at the beginning of each turn
    void DecrementCooldowns()
    {
        foreach (var action in actionCooldowns.Keys.ToList())
        {
            if (actionCooldowns[action] > 0)
                actionCooldowns[action]--;
        }
    }

    void Update()
    {
        if (gameCanvas.activeSelf && isPlayerTurn) // TODO: could also activate mic per default when its users turn?!
        {
            if (Input.GetKeyDown(KeyCode.Return) && highlightedButton != null) // TODO: replace with handgesture
            {
                highlightedButton.onClick.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.RightShift) && !voiceExperience.Active && !ttsSpeakerNPC.IsSpeaking && !ttsSpeakerCommentary.IsSpeaking) // TODO: replace with handgesture
            {
                voiceExperience.Activate();
            }

            if (Input.GetKeyUp(KeyCode.RightShift) && voiceExperience.Active)
            {
                voiceExperience.Deactivate();
            }
        }
    }
}
