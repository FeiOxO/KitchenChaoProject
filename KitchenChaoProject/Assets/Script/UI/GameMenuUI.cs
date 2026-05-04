using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour
{
    [SerializeField] private Button startBtn;
    [SerializeField] private Button quitBtn;
    [SerializeField] private Button settingBtn;
    [SerializeField] private GameObject settingPanel;
    void Start()
    {
        startBtn.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameScence);
        });
        quitBtn.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        settingBtn.onClick.AddListener(() =>
        {
            settingPanel.SetActive(!settingPanel.activeInHierarchy);
        });
    }
}
