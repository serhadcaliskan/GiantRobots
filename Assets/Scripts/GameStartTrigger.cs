using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerChild : MonoBehaviour
{
    public GameManager gameManager;
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TriggerChild OnTriggerEnter");
        gameManager.EnterGame(other);
        gameObject.SetActive(false);
    }

    private void OnTriggerExit(Collider other)
    {
        gameManager.ExitGame(other);
    }
}
