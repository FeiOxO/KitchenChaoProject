using Unity.VisualScripting;
using UnityEngine;

public static class GameData
{
    public static event System.Action<float> OnVolumeValueChanged;
    public static event System.Action<float> OnBackGroundVolumeValueChanged;
    public static event System.Action<float> OnSoundVolumeValueChanged;
    public static event System.Action<string> OnPlayerNameValueChanged;
    public static event System.Action<bool> OnShowNameValueChanged;
    public static event System.Action<bool> OnShowIconValueChanged;
    private static string PlayerName = "FEIOXO";
    public static string playerName{
        get => PlayerName;
        set
        {
            if(PlayerName != value.Trim())
            {
                PlayerName = value.Trim();
                OnPlayerNameValueChanged?.Invoke(playerName);
            }
        }
    }
    
    private static float VolumeValue = 1;
    public static float volumeValue
    {
        get => VolumeValue;
        set
        {
            if(VolumeValue != value)
            {
                VolumeValue = Mathf.Clamp01(value);
                OnVolumeValueChanged?.Invoke(volumeValue);
                OnBackGroundVolumeValueChanged?.Invoke(backGroundVolumeValue);
                OnSoundVolumeValueChanged?.Invoke(soundVolumeValue);
            }
        }
    }
    private static float BackGroundVolumeValue = 1;
    public static float backGroundVolumeValue
    {
        get => BackGroundVolumeValue * volumeValue;
        set
        {
            if(BackGroundVolumeValue != value)
            {
                BackGroundVolumeValue = Mathf.Clamp01(value);
                OnBackGroundVolumeValueChanged?.Invoke(backGroundVolumeValue);
            }
        }
    }
    private static float SoundVolumeValue = 1;
    public static float soundVolumeValue{
        get => SoundVolumeValue * volumeValue;
        set
        {
            if(SoundVolumeValue != value)
            {
                SoundVolumeValue = Mathf.Clamp01(value);
                OnSoundVolumeValueChanged?.Invoke(soundVolumeValue);
            }
        }
    }

    private static bool ShowName = true;
    public static bool showName
    {
        get=>ShowName;
        set
        {
            if(ShowName != value)
            {
                ShowName = value;
                Debug.Log("Name_____Manbo");
                OnShowNameValueChanged?.Invoke(showName);
            }
        }
    }
    private static bool ShowIcon = true;
    public static bool showIcon
    {
        get=>ShowIcon;
        set
        {
            if(ShowIcon != value)
            {
                ShowIcon = value;
                Debug.Log("Icon_____Manbo");
                OnShowIconValueChanged?.Invoke(showIcon);
            }
        }
    }
}
