using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanelUI : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider backGroundVolumeSlider;
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Toggle showIcon;
    [SerializeField] private Toggle showName;
    void Awake()
    {
        volumeSlider.value = GameData.volumeValue;
        backGroundVolumeSlider.value = GameData.backGroundVolumeValue;
        soundEffectsSlider.value = GameData.soundVolumeValue;
        playerNameInputField.text= GameData.playerName;
        showIcon.isOn = GameData.showIcon;
        showName.isOn = GameData.showName;

        volumeSlider.onValueChanged.AddListener((v) =>
        {
            GameData.volumeValue = v;
        });
        backGroundVolumeSlider.onValueChanged.AddListener((v) =>
        {
            GameData.backGroundVolumeValue = v;
        });
        soundEffectsSlider.onValueChanged.AddListener((v) =>
        {
            GameData.soundVolumeValue = v;
        });
        playerNameInputField.onValueChanged.AddListener((name) =>
        {
            GameData.playerName = name;
        });
        showIcon.onValueChanged.AddListener((value) =>
        {
            GameData.showIcon = value;
        });
        showName.onValueChanged.AddListener((value) =>
        {
            GameData.showName = value;
        });
    }

}
