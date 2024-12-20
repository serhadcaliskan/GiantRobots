using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrologEpilogHandler : MonoBehaviour
{
    public TextMeshProUGUI uiText;
    public TTSSpeaker speaker;
    public Canvas canvas;
    private TTSScript ttsScript;
    private bool addBlink = false;

    public virtual void Start()
    {
        ttsScript = GetComponent<TTSScript>();
        canvas.gameObject.SetActive(true);
        uiText.text = "";
    }

    protected IEnumerator TypeTextWithBlink(string prologText, TextMeshProUGUI uiText, System.Action callback)
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
