using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 由 <see cref="GameManager"/> 持有：<see cref="GameMode.Create"/> = 建关编辑柜台；<see cref="GameMode.Game"/> = 游玩（读布局 JSON + 运行 GameManager 的倒计时状态机）。
/// </summary>
public enum GameMode
{
    Create,
    Game
}

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

    [Header("GameMode")]
    [Tooltip("关闭：按场景名默认规则（99-CreateScene=Create，否则=Game）。开启：始终使用下方「自定义 GameMode」，忽略场景名。")]
    [SerializeField] private bool useCustomGameMode;
    [Tooltip("仅在「使用自定义 GameMode」开启时生效。")]
    [SerializeField] private GameMode customGameMode = GameMode.Game;

    /// <summary>当前 Create / Game，由场景加载逻辑写入；<see cref="CounterManager"/> 等据此行为分支。</summary>
    public GameMode CurrentGameMode { get; private set; } = GameMode.Game;

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
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCounterManagerModeForScene(scene);
    }

    /// <summary>
    /// 场景加载后写入 <see cref="CurrentGameMode"/>，再刷新 <see cref="CounterManager"/> 柜台。
    /// <list type="bullet">
    /// <item><description><c>useCustomGameMode == false</c>（默认）：当前场景名为 <c>99-CreateScene</c> → <see cref="GameMode.Create"/>，否则 → <see cref="GameMode.Game"/>。</description></item>
    /// <item><description><c>useCustomGameMode == true</c>：使用 Inspector 中的 <c>customGameMode</c>。</description></item>
    /// </list>
    /// </summary>
    private void ApplyCounterManagerModeForScene(Scene _)
    {
        GameMode mode;
        if (useCustomGameMode)
            mode = customGameMode;
        else
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            mode = currentSceneName == CreateSceneName ? GameMode.Create : GameMode.Game;
        }

        CurrentGameMode = mode;

        CounterManager manager = FindObjectOfType<CounterManager>();
        if (manager == null)
            return;

        manager.RefreshCountersForCurrentMode();
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        ToggleGame();
    }

    private void Update()
    {
        // 仅「游玩」GameMode 下推进等待/倒计时/局内计时；建关 Create 不跑此状态机。
        if (CurrentGameMode != GameMode.Game)
            return;

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
