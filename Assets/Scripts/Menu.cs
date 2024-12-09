using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioSource audioSource;
    public Canvas pauseCanvas;

    private void Start()
    {
        Time.timeScale = 1f; // Unpause the game if it was paused
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(false);
        OVRManager.HMDMounted += PauseGame;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // TODO: add some handgesture to pause the game
        {
            if (SceneManager.GetActiveScene().buildIndex == 0) // when in main menu just quit the game
                OnQuitButton();
            else
                ToggleGame();
        }
    }
    public void OnPlayButton()
    {
        StartCoroutine(PlaySoundAndExecute(() => SceneManager.LoadScene(1)));
    }

    public void OnQuitButton()
    {
        StartCoroutine(PlaySoundAndExecute(() =>
        {
            Time.timeScale = 1f; // Unpause the game if it was paused  

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
           Application.Quit();  
#endif
        }));
    }

    public void OnPauseButton()
    {
        StartCoroutine(PlaySoundAndExecute(() => ToggleGame()));
    }

    public void OnSaveButton()
    {
        StartCoroutine(PlaySoundAndExecute(() => Debug.Log("TODO: implement save function!")));
    }

    private void ToggleGame()
    {
        Time.timeScale = Time.timeScale == 0f ? 1f : 0f; // Pause or unpause the game
        pauseCanvas.gameObject.SetActive(!pauseCanvas.gameObject.activeSelf);

        Debug.Log("Game is " + (Time.timeScale == 0f ? "paused" : "unpaused"));
    }
    private void PauseGame()
    {
        Time.timeScale = 0f;
        if (pauseCanvas != null) pauseCanvas.gameObject.SetActive(true); ;
    }

    private IEnumerator PlaySoundAndExecute(System.Action callback)
    {
        audioSource.PlayOneShot(clickSound);
        yield return new WaitForSecondsRealtime(clickSound.length); // need realtime otherwise callback isnt called in PauseGame
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        OVRManager.HMDMounted -= PauseGame;
    }
}
