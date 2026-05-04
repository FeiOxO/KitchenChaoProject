using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI numText;
    [SerializeField] private GameObject uiParent;
    [SerializeField] private Button meunBtn;
    private void Start()
    {
        Hide();
        meunBtn.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameMenuScene);
        });
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsGameOverState())
        {
            Show();
            numText.text = OrderManager.Instance.GetSuccessDeliveryCount().ToString();
        }
    }

    private void Show()
    {
        uiParent.SetActive(true);
        meunBtn.gameObject.SetActive(true);
    }

    private void Hide()
    {
        uiParent.SetActive(false);
        meunBtn.gameObject.SetActive(false);
    }
}
