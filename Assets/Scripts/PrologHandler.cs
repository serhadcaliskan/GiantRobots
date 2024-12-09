using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologHandler : MonoBehaviour // TODO add to gamobject in lvl to handle game start and end
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
    public Canvas canvas;
    private TTSScript ttsScript;

    public void Start()
    {
        Debug.Log("PrologHandler Start");
        ttsScript = GetComponent<TTSScript>();
        canvas.gameObject.SetActive(true);
        uiText.text = "";
        StartCoroutine(TypeTextWithBlink(prologText, uiText, () => { SceneManager.LoadScene(2); }));
    }

    private bool addBlink = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // TODO: remove when we have gameplay with lvls
        {
            ShowEndScreen(Random.value > 0.5);
        }
    }

    public void ShowEndScreen(bool won) // Call this method when the player wins or loses
    {
        canvas.gameObject.SetActive(true);
        StartCoroutine(TypeTextWithBlink(won ? epilogTextWon : epilogTextLost, uiText, () => { 
            SceneManager.LoadScene(0); 
        }));
    }

    IEnumerator TypeTextWithBlink(string prologText, TextMeshProUGUI uiText, System.Action callback)
    {
        ttsScript.Speak(prologText.Replace("\n", " "), speaker);
        prologText += "...";
        uiText.text = "|";
        yield return Blink(uiText, 5, 0.3f);

        foreach (char c in prologText)
        {
            string baseText = uiText.text.TrimEnd('|');
            uiText.text = baseText + c;

            if (addBlink)
            {
                uiText.text += "|";
            }

            addBlink = !addBlink;
            yield return new WaitForSeconds(0.045f);
        }

        yield return Blink(uiText, 10, 0.3f);
        canvas.gameObject.SetActive(false);
        callback?.Invoke();
    }

    private IEnumerator Blink(TextMeshProUGUI uiText, int blinkCount, float blinkInterval)
    {
        for (int i = 0; i < blinkCount; i++)
        {
            string baseText = uiText.text.TrimEnd('|');
            uiText.text = addBlink ? baseText + "|" : baseText;

            addBlink = !addBlink;
            yield return new WaitForSeconds(blinkInterval);
        }
    }
}
