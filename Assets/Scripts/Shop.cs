using Meta.WitAi.TTS.Utilities;
using Newtonsoft.Json;
using Oculus.Voice.Dictation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class ShopItem
{
    public string name;
    public string description;
    public int price;
}
[System.Serializable]
public class NPCShopAnswer
{
    public string answer;
    public List<string> boughtItems;
    public int price;
}
public class Shop : MonoBehaviour
{
    public GameObject parent;
    public AppDictationExperience dictationExperience;
    public TTSSpeaker tts;
    public TMP_Text textField;
    public TMP_Text answerTextField;
    public TMP_Text buttonText;
    public GameObject canvas;
    public GameObject recordingHint;
    public GameObject player;
    private PlayerStats playerStats;
    public float displayDistance = 10.0f;
    private List<ShopItem> shopItems;
    APIKeys keys = APIKeys.Load();
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private string prompt = "";
    private List<Message> chatHistory = new List<Message>();
    // Start is called before the first frame update
    void Start()
    {
        if (canvas != null)
            canvas.SetActive(false);
        playerStats = player.GetComponent<PlayerStats>();
        playerStats.LoadGameSettings();
        int itemPrice = playerStats.itemPrice();
        shopItems = new List<ShopItem>()
        {
            new ShopItem() { name = "Chainreaction", description = "Chainweapon does more damage", price = itemPrice},
            new ShopItem() { name = "Shield", description = "One more shield per fight", price = itemPrice },
            new ShopItem() { name = "Dodgedrink", description = "Increases your dodgingstrength by 10%", price = itemPrice },
            new ShopItem() { name = "Disarmpotion", description = "Increases your disarmstrength by 10%", price = itemPrice }
        };
    }

    private string inventoryString()
    {
        string inventory = "";
        foreach (ShopItem item in shopItems)
        {
            inventory += "Item: \"" + item.name + "\" Description: " + item.description + " Price: " + item.price + " coins\n";
        }
        return inventory;
    }

    private IEnumerator CallOpenAI(string message)
    {
        buttonText.text = "Loading...";
        chatHistory.Add(new Message { role = "user", content = $"Prisoners balance {PlayerPrefs.GetInt("Money", 0)} coins." });
        chatHistory.Add(new Message { role = "user", content = message });

        var requestData = new OpenAIRequest
        {
            model = "gpt-4o",
            messages = chatHistory
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        StartCoroutine(UpdateKarma(message));
        // Set up the UnityWebRequest
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

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAIResponse response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                string reply = response.choices[0].message.content.Replace("```", "");
                reply = reply.Replace("json", "");
                if (reply != null && reply.Length > 0)
                {
                    Debug.Log(reply);
                    NPCShopAnswer npcShopAnswer;
                    try
                    {
                        GameObject goToDestroy = new GameObject();
                        npcShopAnswer = JsonConvert.DeserializeObject<NPCShopAnswer>(reply);
                        chatHistory.Add(new Message { role = "assistant", content = reply });
                        answerTextField.text = npcShopAnswer.answer;
                        if (npcShopAnswer.boughtItems != null && npcShopAnswer.boughtItems.Count > 0)
                        {
                            goToDestroy = parent;
                            foreach (string item in npcShopAnswer.boughtItems)
                            {
                                switch (item)
                                {
                                    case "Chainreaction":
                                        PlayerPrefs.SetInt("shootDamage", PlayerPrefs.GetInt("shootDamage", 10) + 7);
                                        shopItems.RemoveAll(x => x.name == "Chainreaction");
                                        break;
                                    case "Shield":
                                        PlayerPrefs.SetInt("shieldCount", PlayerPrefs.GetInt("shieldCount", 3) + 1);
                                        shopItems.RemoveAll(x => x.name == "Shield");
                                        break;
                                    case "Dodgedrink":
                                        PlayerPrefs.SetFloat("dodgeSuccessRate", PlayerPrefs.GetFloat("dodgeSuccessRate", 0.5f) + 0.1f);
                                        shopItems.RemoveAll(x => x.name == "Dodgedrink");
                                        break;
                                    case "Disarmpotion":
                                        PlayerPrefs.SetFloat("disarmSuccessRate", PlayerPrefs.GetFloat("disarmSuccessRate", 0.5f) + 0.1f);
                                        shopItems.RemoveAll(x => x.name == "Disarmpotion");
                                        break;
                                    default:
                                        break;
                                }
                            }
                            // wo dont check price, if user manages to convince LLM, then its what we want
                            PlayerPrefs.SetInt("Money", Mathf.Max(0, PlayerPrefs.GetInt("Money", 0) - npcShopAnswer.price));
                            PlayerPrefs.Save();
                            playerStats.LoadGameSettings();
                        }
                        buttonText.text = PlayerPrefs.GetInt("Money", -1) + " Coins";
                        if (npcShopAnswer.answer.Length > 280)
                        {
                            List<string> textChunks = PromptLibrary.SplitTextIntoChunks(npcShopAnswer.answer, 275);
                            StartCoroutine(WaitForTTSToFinishAndDestroy(goToDestroy));
                            StartCoroutine(PromptLibrary.PlayChunksSequentially(textChunks, tts, textField));
                        }
                        else
                        {
                            StartCoroutine(WaitForTTSToFinishAndDestroy(goToDestroy));
                            tts.Speak(npcShopAnswer.answer);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error: " + e.Message);
                        Debug.LogError("Error: " + reply);
                        textField.text = "Transaction failed";
                        tts.Speak("Transaction Failed!");
                    }
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                }
            }
        }
    }

    private IEnumerator WaitForTTSToFinishAndDestroy(GameObject gameObjectToDestroy)
    {
        while (!tts.IsSpeaking)
        {
            yield return null; // Wait until the next frame.
        }
        while (tts.IsSpeaking)
        {
            yield return null; // Wait until the next frame.
        }
        Destroy(gameObjectToDestroy);
    }
    private void updatePrompt()
    {
        string negotiationLvl = PromptLibrary.NegotiationMid;
        if (playerStats.KarmaScore < 33)
            negotiationLvl = PromptLibrary.NegotiationLow;
        else if (playerStats.KarmaScore > 66)
            negotiationLvl = PromptLibrary.NegotiationHigh;
        prompt = string.Format(PromptLibrary.shop, inventoryString().Length > 0 ? inventoryString() : "Inventory is empty!", negotiationLvl);
        Debug.Log(prompt);
        if (chatHistory.Count > 0)
        {
            List<Message> newHistory = new List<Message> { new Message { role = "system", content = prompt } };
            foreach (Message message in chatHistory)
            {
                if (message.role != "system")
                    newHistory.Add(message);
            }
            chatHistory = newHistory;
        }
        else
            chatHistory.Add(new Message { role = "system", content = prompt });
    }
    private IEnumerator UpdateKarma(string lastMessage)
    {
        Message prompt = new Message { role = "system", content = PromptLibrary.EvalShopKarma };
        Message userMessage = new Message { role = "user", content = lastMessage };
        var requestData = new OpenAIRequest
        {
            model = "gpt-4o",
            messages = new List<Message> { prompt, userMessage }
        };

        string jsonData = JsonConvert.SerializeObject(requestData);

        // Set up the UnityWebRequest
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

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                OpenAIResponse response = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                string reply = response.choices[0].message.content;
                if (reply != null && reply.Length > 0)
                {
                    if (reply.Contains("+"))
                    {
                        playerStats.KarmaScore += 5;
                    }
                    else if (reply.Contains("-"))
                    {
                        playerStats.KarmaScore -= 5;
                    }
                    updatePrompt();
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player.transform)
        {
            updatePrompt();
            ShowCanvasInFrontOfPlayer();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.transform == player.transform)
        {
            canvas.SetActive(false);
            stopRecording();
            tts.Stop();
        }
    }

    private void ShowCanvasInFrontOfPlayer()
    {
        if (canvas != null && player != null)
        {
            // Calculate the position for the canvas to appear directly in front of the player
            Vector3 playerForward = player.transform.forward; // Direction player is facing
            Vector3 canvasPosition = player.transform.position + playerForward * displayDistance;
            canvasPosition.y += 15.0f;

            // Position and face the canvas toward the player
            canvas.transform.position = canvasPosition;
            canvas.transform.LookAt(player.transform); // Make the canvas face the player
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - player.transform.position); // Adjust for proper facing

            // Activate the canvas
            canvas.SetActive(true);
        }
    }
    private void startRecording()
    {
        //buttonText.text = "Listening...";
        //recordingHint.SetActive(true);
        //dictationExperience.Activate();
    }

    private void stopRecording()
    {
        buttonText.text = PlayerPrefs.GetInt("Money", -1) + " Coins";
        recordingHint.SetActive(false);
        dictationExperience.Deactivate();
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.activeSelf)
        {
            if (textField.text?.Length > 0 && Input.GetKeyDown(KeyCode.Return) && !tts.IsSpeaking && !dictationExperience.Active)
            {
                stopRecording();
                StartCoroutine(CallOpenAI(textField.text));
            }

            // TODO: replace with ingame button/handgesture
            if ((Input.GetKeyUp(KeyCode.RightShift) || OVRInput.GetUp(OVRInput.Button.One)) && !dictationExperience.Active && !tts.IsSpeaking)
            {
                dictationExperience.Activate();
            }
        }
    }

    public void AutomaticGPTAnswer()
    {
        Debug.Log("AutomaticGPTAnswer");
        StartCoroutine(CallOpenAI(textField.text));
    }
}
