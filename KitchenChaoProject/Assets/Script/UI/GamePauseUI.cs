using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private GameObject uiParent;
    [SerializeField] private Button resumeBtn;
    [SerializeField] private Button meunBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private GameObject settingPanel;
    private void Start()
    {
        Hide();
        GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        GameManager.Instance.OnGameUnpaused += GameManager_OnGameUnpaused;
        resumeBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.ToggleGame();
        });

        meunBtn.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameMenuScene);
        });

        settingBtn.onClick.AddListener(() =>
        {
            settingPanel.SetActive(!settingPanel.activeInHierarchy);
        });
    }

    private void GameManager_OnGameUnpaused(object sender, EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGamePaused(object sender, EventArgs e)
    {
        Show();
    }

    private void Show(){
        uiParent.SetActive(true);
    }

    private void Hide(){
        uiParent.SetActive(false);
    }
}
