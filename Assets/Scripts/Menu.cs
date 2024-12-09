using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public AudioClip clickSound;
    public AudioSource audioSource;
    public Canvas pauseCanvas;
    public GameObject noConnectionHint;

    private void Start()
    {
        Time.timeScale = 1f; // Unpause the game if it was paused
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        if (!IsConnectedToInternet())
        {
            StartCoroutine(ShowNoConnectionHint());
            return;
        }
        else
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
        StartCoroutine(PlaySoundAndExecute(() => { Debug.Log("TODO: implement save function!"); SceneManager.LoadScene(0); }));
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
        EventSystem.current.SetSelectedGameObject(null);
        callback?.Invoke();
    }

    private IEnumerator ShowNoConnectionHint()
    {
        if (noConnectionHint != null) noConnectionHint.SetActive(true);
        yield return new WaitForSeconds(3f);
        if (noConnectionHint != null) noConnectionHint.SetActive(false);
    }

    private bool IsConnectedToInternet()
    {
        // Check if there is an internet connection
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false; // No connection
        }

        return true; // Connection is available
    }

    private void OnDestroy()
    {
        OVRManager.HMDMounted -= PauseGame;
    }
}
