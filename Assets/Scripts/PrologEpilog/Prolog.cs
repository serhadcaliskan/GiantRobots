using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Prolog : PrologEpilogHandler
{
    private string prologText = "YOU FOOL! You put on the forbidden mask of unbearable truths." +
        " As punishment for your disobedience you are teleported to the prison planet Mars." +
        " To earn one of the few tickets back to earth you will have to prove yourself " +
        "in a series of fights. Good luck";

    public override void Start()
    {
        base.Start();
        StartCoroutine(TypeTextWithBlink(prologText, uiText, () => { SceneManager.LoadScene(2); }));
    }
}

