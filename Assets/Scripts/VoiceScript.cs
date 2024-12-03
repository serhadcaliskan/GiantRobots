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
public class VoiceScript : MonoBehaviour
{
    public AppVoiceExperience voiceExperience;
    public AppDictationExperience dictationExperience;
    public TTSSpeaker tts;
    public Button ttsButton;
    public TMP_Text textField;
    public TMP_Text answerTextField;
    public TMP_Text buttonText;
    public TMP_InputField nameTextField;
    public GameObject canvas;
    APIKeys keys = APIKeys.Load();
    public float displayDistance = 10.0f;
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    private List<Message> chatHistory = new List<Message>();
    public Transform player;

    //private string voicesList = "WIT$BRITISH BUTLER, WIT$CAEL, WIT$CAM, WIT$CARL, WIT$CARTOON BABY, WIT$CARTOON KID, WIT$CARTOON VILLAIN, WIT$CHARLIE, WIT$COCKNEY ACCENT, WIT$CODY, WIT$COLIN, WIT$CONNOR, WIT$COOPER, WIT$DISAFFECTED, WIT$HOLLYWOOD, WIT$KENYAN ACCENT, WIT$OVERCONFIDENT, WIT$PIRATE, WIT$PROSPECTOR, WIT$RAILEY, WIT$REBECCA, WIT$REMI, WIT$ROSIE, WIT$RUBIE, WIT$SOUTHERN ACCENT, WIT$SURFER, WIT$TRENDY, WIT$VAMPIRE, WIT$WHIMSICAL, WIT$WIZARD";
    //private string voiceSelectRequest = "";
    // TODO: dont hardcode the following 2 strings
    private string lastMessage = "Goodbye!";

    private void Start()
    {
        if (canvas != null)
            canvas.SetActive(false);
    }
    void OnTTSButtonClick()
    {
        //voiceSelectRequest = "I have a TTS app and the user wants to talk with \"" + nameTextField.text + "\".\r\n" +
        //    "This is a list of VoiceIDs i have:\"" + voicesList + "\", please choose the best suiting one from the list. " +
        //    "If nothing suits give me a random one from the list. Answer only with the one ID you selected.";
        //getChatGPTAnswer(voiceSelectRequest, (reply) =>
        //{
        //    Debug.Log("VoiceID: " + reply);
        //    if (voicesList.Contains(reply))
        //    {
        //        tts.VoiceID = reply;
        //    }
        //    else
        //    {
        //        tts.VoiceID = "WIT$PIRATE";
        //    }
        //});
        if (textField.text?.Length > 0)
            StartCoroutine(CallOpenAI(textField.text));
    }

    public void getChatGPTAnswer(string message, Action<string> onComplete)
    {
        StartCoroutine(getGPTAnswer(message, onComplete));
    }
    private IEnumerator getGPTAnswer(string message, Action<string> onComplete)
    {
        List<Message> messages = new() { new Message { role = "user", content = message } };

        var requestData = new OpenAIRequest
        {
            model = "gpt-4o",
            messages = messages
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

                // Call the callback with the reply
                onComplete(reply);
                if (!tts.IsSpeaking && textField.text?.Length > 0 && !voiceExperience.Active && !dictationExperience.Active)
                {
                    buttonText.text = "Loading...";
                    ttsButton.enabled = false;
                    yield return StartCoroutine(CallOpenAI(textField.text));
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error);
                onComplete("Error: " + request.error);
            }
        }
    }
    private IEnumerator CallOpenAI(string message)
    {
        if (nameTextField.text?.Length > 0)
        {
            chatHistory.Add(new Message { role = "system", content = "You are " + nameTextField.text + "." });
        }
        else
        {
            chatHistory.Add(new Message { role = "system", content = "You are a notorious Pirate. Keep your answers short. You dont like the one talking to you." });
        }
        // Construct the message payload
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
                string reply = response.choices[0].message.content;
                //Debug.Log("Response: " + reply);
                if (reply != null && reply.Length > 0)
                {
                    answerTextField.text = reply;
                    chatHistory.Add(new Message { role = "assistant", content = reply });
                    Debug.Log(chatHistoryAsString());
                    if (reply.Length > 280)
                    {
                        List<string> textChunks = SplitTextIntoChunks(reply, 275);
                        StartCoroutine(PlayChunksSequentially(textChunks));
                    }
                    else
                    {
                        tts.Speak(reply);
                    }
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
            ttsButton.enabled = true;
            buttonText.text = "Button";
        }
    }

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

    private string chatHistoryAsString()
    {
        string output = "";
        foreach (Message message in chatHistory)
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            ttsButton.onClick.AddListener(OnTTSButtonClick);
            tts.VoiceID = "WIT$PIRATE";
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
            canvasPosition.y += 35.0f;

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
            canvas.SetActive(false);
            tts.Speak(lastMessage);
            //chatHistory.Add(new Message { role = "assistant", content = lastMessage });
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (canvas.activeSelf)
        {
            if (textField.text?.Length > 0 && Input.GetKeyDown(KeyCode.Alpha0))
            {
                StartCoroutine(CallOpenAI(textField.text));
            }

            //// Start voice recording when the Return key is pressed
            //if (Input.GetKeyDown(KeyCode.Return) && !voiceExperience.Active && !tts.IsSpeaking)
            //{
            //    voiceExperience.Activate(); // Activates default mic for 20 seconds after the volume threshold is hit.
            //                                // If the user is quiet for 2 seconds, or if it reaches the 20 second mark, the mic will stop recording.
            //                                // (Can all be changed in runtime config)
            //}

            //// Stop voice recording when the Return key is released
            //if (Input.GetKeyUp(KeyCode.Return) && voiceExperience.Active)
            //{
            //    voiceExperience.Deactivate();
            //}

            // Start dictation when the arrow-up key is pressed
            if (Input.GetKeyDown(KeyCode.RightShift) && !dictationExperience.Active && !tts.IsSpeaking)
            {
                dictationExperience.Activate();
            }

            // Stop voice dictation when the arrow-up key is released
            if (Input.GetKeyUp(KeyCode.UpArrow) && dictationExperience.Active)
            {
                dictationExperience.Deactivate();
            }
        }
    }


}
