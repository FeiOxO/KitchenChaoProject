using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIconUI : MonoBehaviour
{
    [SerializeField] private List<Sprite> spriteList;
    [SerializeField] private Image playerIcon;
    [SerializeField] private TextMeshProUGUI playerName;
    void Start()
    {
        playerIcon.sprite = spriteList[UnityEngine.Random.Range(0,spriteList.Count)];
        playerName.text = GameData.playerName;
        ShowIcon(GameData.showIcon);
        ShowName(GameData.showName);
        GameData.OnShowIconValueChanged += ShowIcon;
        GameData.OnShowNameValueChanged += ShowName;
        GameData.OnPlayerNameValueChanged += SetPlayerName;
    }

    private void SetPlayerName(string value)
    {
        playerName.text = value;
    }

    private void ShowIcon(bool value)
    {
        playerIcon.gameObject.SetActive(value);
    }

    private void ShowName(bool value)
    {
        playerName.gameObject.SetActive(value);
    }

    void OnDestroy()
    {
        GameData.OnShowIconValueChanged -= ShowIcon;
        GameData.OnShowNameValueChanged -= ShowName;
        GameData.OnPlayerNameValueChanged -= SetPlayerName;
    }
}
