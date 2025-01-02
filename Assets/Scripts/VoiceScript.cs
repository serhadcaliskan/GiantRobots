using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Oculus.Voice;
using Oculus.Voice.Dictation;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Integrations;
using System;
using Unity.VisualScripting;
using System.Linq;

[System.Serializable]
public class OpenAIResponse
{
    public List<Choice> choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class OpenAIRequest
{
    public string model = "gpt-4o";
    public List<Message> messages;
}
[System.Serializable]
public class Message
{
    public string role;
    public string content;
}

/// <summary>
/// Set the voce ID in the child TTS object of the gameobject
/// </summary>
public class VoiceScript : MonoBehaviour
{
    public AppDictationExperience dictationExperience;
    //private float silenceTimeout = 3f; // Timeout in seconds
    //private float lastSpokenTime;
    public TTSSpeaker tts;
    public Button ttsButton;
    public TMP_Text textField;
    public TMP_Text answerTextField;
    public TMP_Text buttonText;
    public GameObject canvas;
    public GameObject recordingHint;
    public string npcName;
    /// <summary>
    /// The name of the opponent this one has information about
    /// </summary>
    public string hasInfoAbout;
    APIKeys keys = APIKeys.Load();
    public float displayDistance = 10.0f;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";
    private bool updatedKarma = false;

    private List<Message> chatHistory = new List<Message>();
    public Transform player;
    public PlayerStats playerStats;
    private string instructions = "";
    private void Start()
    {
        PlayerPrefs.SetInt("karmaScore", 50);
        playerStats.LoadGameSettings();

        if (canvas != null)
            canvas.SetActive(false);
    }
    void OnTTSButtonClick()
    {
        if (textField.text?.Length > 0)
            StartCoroutine(CallOpenAI(textField.text));
    }

    public void displayUserUtterance(string message)
    {
        textField.text += message;
    }
    private IEnumerator CallOpenAI(string message)
    {
        buttonText.text = "Loading...";
        // Construct the message payload
        //chatHistory.Add(new Message { role = "system", content = instructions });
        chatHistory.Add(new Message { role = "user", content = message });

        var requestData = new OpenAIRequest
        {
            model = "gpt-4o",
            messages = chatHistory
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
                string reply = response.choices[0].message.content.Replace("```", "");
                reply = reply.Replace("json", "");
                if (reply != null && reply.Length > 0)
                {
                    buttonText.text = "Button";
                    answerTextField.text = reply;
                    chatHistory.Add(new Message { role = "assistant", content = reply });
                    if (reply.Length > 280)
                    {
                        List<string> textChunks = SplitTextIntoChunks(answerTextField.text, 275);
                        StartCoroutine(PlayChunksSequentially(textChunks));
                    }
                    else
                    {
                        tts.Speak(reply);
                    }
                    StartCoroutine(UpdateKarma(message));
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                }
                ttsButton.enabled = true;
            }
        }
    }
    private IEnumerator UpdateKarma(string lastMessage)
    { // Construct the message payload
        Message prompt = new Message { role = "system", content = PromptLibrary.EvalConversationKarma };
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
                    Debug.Log("Karma update: " + reply);
                    Debug.Log("Karma before: " + playerStats.KarmaScore);
                    if (reply.Contains("+"))
                    {
                        playerStats.KarmaScore += 5;
                    }
                    else if (reply.Contains("-"))
                    {
                        playerStats.KarmaScore -= 5;
                    }
                    Debug.Log("Karma after: " + playerStats.KarmaScore);
                }
                else
                {
                    Debug.LogError("Error: " + request.error);
                }
            }
            // TDOD: do we want to be able to talk further or just until the information got reveiled?
            //chatHistory.Clear();
            //chatHistory.Add(new Message { role = "system", content = string.Format(PromptLibrary.NPCConversation, npcName, playerStats.KarmaScoreNormalized, PromptLibrary.GetBehaviour(hasInfoAbout)) });
        }
    }
    // TODO refactor and use the TTSScript
    private List<string> SplitTextIntoChunks(string text, int chunkLength)
    {
        List<string> chunks = new List<string>();
        int currentIndex = 0;

        while (currentIndex < text.Length)
        {
            // Calculate the tentative end of the chunk
            int nextChunkEnd = Mathf.Min(currentIndex + chunkLength, text.Length);

            // Find the last space within this range, avoiding cutting words
            int lastSpaceIndex = text.LastIndexOf(' ', nextChunkEnd - 1, nextChunkEnd - currentIndex);

            // If there's no space within this range, cut at the chunk length
            if (lastSpaceIndex == -1 || lastSpaceIndex <= currentIndex)
            {
                lastSpaceIndex = nextChunkEnd;
            }

            // Extract the chunk safely
            string chunk = text.Substring(currentIndex, lastSpaceIndex - currentIndex).Trim();
            chunks.Add(chunk);

            // Move to the next chunk, skipping the space after the last word
            currentIndex = lastSpaceIndex + 1;
        }
        return chunks;
    }

    private string chatHistoryAsString(List<Message> messages)
    {
        string output = "";
        foreach (Message message in messages)
        {
            output += message.role + ": " + message.content + "\n";
        }
        return output;
    }

    private IEnumerator PlayChunksSequentially(List<string> chunks)
    {
        Debug.Log(chunks);
        foreach (string chunk in chunks)
        {
            tts.SpeakQueued(chunk); // Play the TTS for the current chunk

            // Wait for the current chunk to finish playing before proceeding
            yield return new WaitUntil(() => !tts.IsSpeaking);
        }
        textField.text = "";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            string helpfulness = PromptLibrary.HelpfulnessMid;
            if (playerStats.KarmaScore < 33)
                helpfulness = PromptLibrary.HelpfulnessLow;
            else if (playerStats.KarmaScore > 66)
                helpfulness = PromptLibrary.HelpfulnessHigh;

            instructions = string.Format(PromptLibrary.NPCConversation, npcName, helpfulness, PromptLibrary.GetBehaviour(hasInfoAbout));
            if (chatHistory.Count > 0)
            {
                List<Message> newHistory = new List<Message> { new Message { role = "system", content = instructions } };
                foreach (Message message in chatHistory)
                {
                    if (message.role != "system")
                        newHistory.Add(message);
                }
                chatHistory = newHistory;
            }
            else
                chatHistory.Add(new Message { role = "system", content = instructions });

            ttsButton.onClick.AddListener(OnTTSButtonClick);
            ShowCanvasInFrontOfPlayer();
        }
    }

    private void ShowCanvasInFrontOfPlayer()
    {
        if (canvas != null && player != null)
        {
            // Calculate the position for the canvas to appear directly in front of the player
            Vector3 playerForward = player.forward; // Direction player is facing
            Vector3 canvasPosition = player.position + playerForward * displayDistance;
            canvasPosition.y += 15.0f;

            // Position and face the canvas toward the player
            canvas.transform.position = canvasPosition;
            canvas.transform.LookAt(player); // Make the canvas face the player
            canvas.transform.rotation = Quaternion.LookRotation(canvas.transform.position - player.position); // Adjust for proper facing

            // Activate the canvas
            canvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            ttsButton.onClick.RemoveListener(OnTTSButtonClick);
            canvas.SetActive(false);
            stopRecording();
            tts.Stop();
        }
    }

    private void startRecording()
    {
        buttonText.text = "Listening...";
        //lastSpokenTime = Time.time;
        recordingHint.SetActive(true);
        dictationExperience.Activate();
    }

    private void stopRecording()
    {
        buttonText.text = "Button";
        recordingHint.SetActive(false);
        dictationExperience.Deactivate();
    }

    // Update is called once per frame
    void Update()
    {
        if (canvas.activeSelf)
        {
            if (textField.text?.Length > 0 && Input.GetKeyDown(KeyCode.Return) && !tts.IsSpeaking)
            {
                stopRecording();
                StartCoroutine(CallOpenAI(textField.text));
            }

            // TODO: replace with ingame button/handgesture
            if (Input.GetKeyDown(KeyCode.RightShift) && !dictationExperience.Active && !tts.IsSpeaking)
            {
                textField.text = "";
                startRecording();
            }

            // Stop voice dictation when the key is released & get the answer
            if (Input.GetKeyUp(KeyCode.RightShift) && dictationExperience.Active)
            {
                stopRecording();
            }
        }
    }
}
