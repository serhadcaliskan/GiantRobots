using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerChild : MonoBehaviour
{
    private GameManagerFive gameManager;
    private void Start()
    {
        gameManager = GetComponentInChildren<GameManagerFive>();
    }
    private void OnTriggerEnter(Collider other)
    {
        gameManager.EnterGame(other);
    }

    private void OnTriggerExit(Collider other)
    {
        gameManager.ExitGame(other);
    }
}
