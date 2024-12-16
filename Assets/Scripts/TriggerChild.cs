using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerChild : MonoBehaviour
{
    private GameManager gameManager;
    private void Start()
    {
        gameManager = GetComponentInChildren<GameManager>();
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
