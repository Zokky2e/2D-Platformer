using System;
using UnityEngine;
using UnityEngine.UI;


public class PauseMenu : Singleton<PauseMenu>
{
    public static bool GameIsPaused = true;
    public GameObject pauseMenuUI;

    protected override void Awake()
    {
        base.Awake();
    }

    public void Start()
    {
        Resume();
    }

    private void Update()
    {
        if (!CoreUI.IsUIOpen && GameInput.CancelPressed)
        {
            CheckForPause();
        }
    }

    public void CheckForPause(bool forcePause = false)
    {
        if (forcePause)
        {
            Pause();
            return;
        }
        if (GameIsPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
    }

    public void OnRespawnClicked()
    {
        if (GameRespawn.Instance != null)
        {
            Resume();
            GameRespawn.Instance.RespawnPlayer();
        }
    }

    // Hook a pause-menu button's OnClick to this
    public void OnNewGameClicked()
    {
        SaveSystem.Instance.StartNewGame();
    }

    public void OnQuitGameClicked()
    {
        Resume();
        SaveSystem.Instance.Save();
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
