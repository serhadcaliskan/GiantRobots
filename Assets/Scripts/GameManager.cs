using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public TMP_Text gameStatusText; // UI Text to display game messages
    private string playerAction = "";
    private string aiAction = "";

    private int playerLoads = 0; // Track player loads (ammo count)
    private int aiLoads = 0;     // Track AI loads (ammo count)

    public GameObject gameCanvas;
    public Transform player;
    public FPSController fpsController;

    // UI Buttons
    public Button loadButton;
    public Button shootButton;
    public Button shieldButton;

    public TTSSpeaker ttsSpeaker;


    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            gameCanvas.SetActive(true);
            ttsSpeaker.Speak(gameStatusText.text);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            gameCanvas.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }
    }
    void Start()
    {
        // Attach button listeners
        loadButton.onClick.AddListener(() => PlayerChooseAction("Load"));
        shootButton.onClick.AddListener(() => PlayerChooseAction("Shoot"));
        shieldButton.onClick.AddListener(() => PlayerChooseAction("Shield"));
        ttsSpeaker.VoiceID = "WIT$DISAFFECTED";
        ResetGame();
        if (gameCanvas != null)
        {
            gameCanvas.SetActive(false);
        }
    }

    void ResetGame()
    {
        gameStatusText.text = "Choose an action!";
        playerLoads = 0;
        aiLoads = 0;
    }

    void PlayerChooseAction(string action)
    {
        playerAction = action;

        // Handle player actions
        if (playerAction == "Load")
        {
            playerLoads++; // Increase load count
            gameStatusText.text = $"You loaded! Ammo: {playerLoads}";
            ttsSpeaker.Speak($"You loaded! Ammo: {playerLoads}");
        }
        else if (playerAction == "Shoot")
        {
            if (playerLoads > 0)
            {
                playerLoads--; // Use one load for each shot
                gameStatusText.text = $"You shot! Ammo left: {playerLoads}";
                ttsSpeaker.Speak($"You shot! Ammo left: {playerLoads}");
            }
            else
            {
                gameStatusText.text = "You need to load first!";
                ttsSpeaker.Speak("You need to load first!");
                return;
            }
        }

        // AI chooses its action
        AITurnRandom();

        // Resolve game outcome
        ResolveTurn();
    }

    void AITurnRandom()
    {
        // Simple AI with random action choice
        int choice = Random.Range(0, 3); // 0 = Load, 1 = Shoot, 2 = Shield

        if (choice == 0)
        {
            aiAction = "Load";
            aiLoads++; // Increase AI load count
        }
        else if (choice == 1 && aiLoads > 0)
        {
            aiAction = "Shoot";
            aiLoads--; // Use one load for each shot
        }
        else
        {
            aiAction = "Shield";
        }
    }

    // TODO Implement gpt calls
        void AITurnGPT()
    {
        // Simple AI with random action choice
        int choice = Random.Range(0, 3); // 0 = Load, 1 = Shoot, 2 = Shield

        if (choice == 0)
        {
            aiAction = "Load";
            aiLoads++; // Increase AI load count
        }
        else if (choice == 1 && aiLoads > 0)
        {
            aiAction = "Shoot";
            aiLoads--; // Use one load for each shot
        }
        else
        {
            aiAction = "Shield";
        }
    }

    void ResolveTurn()
    {
        if (playerAction == "Shoot" && aiAction != "Shield")
        {
            gameStatusText.text = "Player wins! AI was shot!";
            ttsSpeaker.Speak("Player wins! AI was shot!");
            Invoke("ResetGame", 5f);
        }
        else if (aiAction == "Shoot" && playerAction != "Shield")
        {
            gameStatusText.text = "AI wins! You were shot!";
            ttsSpeaker.Speak("AI wins! You were shot!");
            Invoke("ResetGame", 5f);
        }
        else if (playerAction == "Shoot" && aiAction == "Shield")
        {
            gameStatusText.text = "AI blocked your shot!";
            ttsSpeaker.Speak("AI blocked your shot!");
        }
        else if (aiAction == "Shoot" && playerAction == "Shield")
        {
            gameStatusText.text = "You blocked AI's shot!";
            ttsSpeaker.Speak("You blocked AI's shot!");
        }
        else if (playerAction == "Shoot" && aiAction == "Shoot")
        {
            gameStatusText.text = "Both shot! This is a draw!";
            ttsSpeaker.Speak("Both shot! This is a draw!");
            Invoke("ResetGame", 5f);
        }
        else
        {
            gameStatusText.text = $"Turn ends. Player ammo: {playerLoads}, AI ammo: {aiLoads}. Choose again!";
            ttsSpeaker.Speak($"Turn ends. Player ammo: {playerLoads}, AI ammo: {aiLoads}. Choose again!");
        }

        // Reset choices after each round
        playerAction = "";
        aiAction = "";
    }
}
