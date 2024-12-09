using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologHandler : MonoBehaviour
{
    private string prologText = "YOU FOOL! You put on the forbidden mask of unbearable truths." +
        " As punishment for your disobedience you are teleported to the prison planet Mars." +
        " To earn one of the few tickets back to earth you will have to prove yourself " +
        "in a series of fights. Good luck";

    public TextMeshProUGUI uiText;
    public TTSSpeaker speaker;
    private TTSScript ttsScript;
    public void Start()
    {
        ttsScript = GetComponent<TTSScript>();
        uiText.text = "|";
        StartCoroutine(TypeTextWithBlink(prologText, uiText, () => { SceneManager.LoadScene(2); }));
    }

    IEnumerator TypeTextWithBlink(string prologText, TextMeshProUGUI uiText, System.Action callback)
    {
        ttsScript.Speak(prologText.Replace("\n", " "), speaker);
        prologText += "...";
        bool addBlink = false;
        for (int i = 0; i < 6; i++)
        {
            while (Time.timeScale == 0f) yield return null; // Wait for the game to be unpaused

            string baseText = uiText.text.TrimEnd('|');
            uiText.text = addBlink ? baseText + "|" : baseText;

            addBlink = !addBlink;
            yield return new WaitForSeconds(0.3f);
        }

        foreach (char c in prologText)
        {
            while (Time.timeScale == 0f) yield return null; // Wait for the game to be unpaused

            string baseText = uiText.text.TrimEnd('|');
            uiText.text = baseText + c;

            if (addBlink)
            {
                uiText.text += "|";
            }

            addBlink = !addBlink;
            yield return new WaitForSeconds(0.055f);
        }

        for (int i = 0; i < 7; i++) // Blink 10 times
        {
            while (Time.timeScale == 0f) yield return null; // Wait for the game to be unpaused

            string baseText = uiText.text.TrimEnd('|');
            uiText.text = addBlink ? baseText + "|" : baseText;

            addBlink = !addBlink;
            yield return new WaitForSeconds(0.3f);
        }
        callback?.Invoke();
    }
}
