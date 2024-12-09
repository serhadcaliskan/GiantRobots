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
    private string epilogTextWon = "Against all odds, you emerged victorious, earning your place back on Earth. " +
        "The forbidden mask's burden is lifted, and your story becomes a testament to perseverance and triumph in the face of despair. " +
        "Welcome home champion. Please take off the mask";
    private string epilogTextLost = "Defeated, the weight of the forbidden mask crushes your spirit. " +
        "Mars becomes your eternal prison, a desolate monument to failure. Yet in your loss, whispers of " +
        "your struggle inspire others to defy the odds you could not";



    public TextMeshProUGUI uiText;
    public TTSSpeaker speaker;
    private TTSScript ttsScript;
    public void Start()
    {
        ttsScript = GetComponent<TTSScript>();
        uiText.text = "";
        StartCoroutine(TypeTextWithBlink(prologText, uiText, () => { SceneManager.LoadScene(2); }));
    }

    private bool addBlink = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowEndScreen(Random.value > 0.5);
        }
    }

    public void ShowEndScreen(bool won) // Call this method when the player wins or loses
    {
        StartCoroutine(TypeTextWithBlink(won ? epilogTextWon : epilogTextLost, uiText, () => { SceneManager.LoadScene(0); }));
    }

    IEnumerator TypeTextWithBlink(string prologText, TextMeshProUGUI uiText, System.Action callback)
    {
        ttsScript.Speak(prologText.Replace("\n", " "), speaker);
        prologText += "...";
        uiText.text = "|";
        yield return Blink(uiText, 6, 0.3f);

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

        yield return Blink(uiText, 7, 0.3f);

        callback?.Invoke();
    }

    private IEnumerator Blink(TextMeshProUGUI uiText, int blinkCount, float blinkInterval)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            while (Time.timeScale == 0f) yield return null; // Wait for the game to be unpaused

            string baseText = uiText.text.TrimEnd('|');
            uiText.text = addBlink ? baseText + "|" : baseText;

            addBlink = !addBlink;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
