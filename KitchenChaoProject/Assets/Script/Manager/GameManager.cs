using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region 单例模式
    public static GameManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        // DontDestroyOnLoad(gameObject);
    }
    #endregion

    private const string CreateSceneName = "99-CreateScene";
    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    [SerializeField] private PlayerController player;

    public enum State
    {
        WaitingToStart,
        CountDownToStart,
        GamePlaying,
        GameOver
    }
    public State state = State.WaitingToStart;
    private float waitingToStartTimer = 1;
    private float countDownToStartTimer = 3;
    [SerializeField]private float gamePlayingTimer = 300;
    private bool isGamePause = false;

    private void Start()
    {
        TurnToWaitingToStart();
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        ApplyCounterManagerModeForScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCounterManagerModeForScene(scene);
    }

    private static void ApplyCounterManagerModeForScene(Scene _)
    {
        CounterManager manager = FindObjectOfType<CounterManager>();
        if (manager == null)
            return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == CreateSceneName)
            manager.SetMode(CounterManagerMode.Create);
        else
            manager.SetMode(CounterManagerMode.Game);
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        ToggleGame();
    }

    private void Update()
    {
        switch (state)
        {
            case State.WaitingToStart:
                waitingToStartTimer -= Time.deltaTime;
                if (waitingToStartTimer <= 0)
                {
                    TurnToCountDownToStart();
                }
                break;
            case State.CountDownToStart:
                countDownToStartTimer -= Time.deltaTime;
                if (countDownToStartTimer <= 0)
                {
                    TurnToGamePlaying();
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer -= Time.deltaTime;
                if (gamePlayingTimer <= 0)
                {
                    TurnToGameOver();
                }
                break;
            case State.GameOver:
                break;
            default:
                break;
        }
    }

    private void TurnToWaitingToStart()
    {
        state = State.WaitingToStart;
        DisablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TurnToGameOver()
    {
        state = State.GameOver;
        DisablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TurnToGamePlaying()
    {
        state = State.GamePlaying;
        EnablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void TurnToCountDownToStart()
    {
        state = State.CountDownToStart;
        DisablePlayer();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DisablePlayer()
    {
        player.enabled = false;
    }

    private void EnablePlayer()
    {
        player.enabled = true;
    }

    public bool IsCountDownState()
    {
        return state == State.CountDownToStart;
    }

    public bool IsGamePlayingState()
    {
        return state == State.GamePlaying;
    }

    public bool IsGameOverState()
    {
        return state == State.GameOver;
    }

    public float GetCountDownTImer()
    {
        return countDownToStartTimer;
    }

    public float GetGamePlayingTimer()
    {
        return gamePlayingTimer;
    }

    public void ToggleGame()
    {
        isGamePause = !isGamePause;
        if (isGamePause)
        {
            Time.timeScale = 0;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1;
            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }


}
