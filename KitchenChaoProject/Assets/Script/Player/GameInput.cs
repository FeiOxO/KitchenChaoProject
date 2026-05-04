using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    #region 单例模式
    public static GameInput Instance;
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
        // DontDestroyOnLoad(gameObject);
        Init();
    }
    #endregion
    public event EventHandler OnInteractAction;
    public event EventHandler OnOperateAction;
    public event EventHandler OnPauseAction;
    private GameController gameController;

    void Init()
    {
        gameController = new GameController();
        gameController.Player.Enable();

        gameController.Player.Interact.performed += Interact_Performed;
        gameController.Player.Operate.performed += Operate_Performed;
        gameController.Player.Pause.performed += Pause_Performed;
    }

    private void Pause_Performed(InputAction.CallbackContext context)
    {
        // print("asd");
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void Operate_Performed(InputAction.CallbackContext ctX)
    {
        OnOperateAction?.Invoke(this, EventArgs.Empty);
    }

    private void Interact_Performed(InputAction.CallbackContext ctx)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    public Vector3 GetMovementDirectionNormalized()
    {
        Vector2 inputVector2 = gameController.Player.Move.ReadValue<Vector2>();

        return new Vector3(inputVector2.x, 0, inputVector2.y).normalized;
    }

    private void OnDisable()
    {
        gameController.Player.Interact.performed -= Interact_Performed;
        gameController.Player.Operate.performed -= Operate_Performed;
        gameController.Player.Pause.performed -= Pause_Performed;

        gameController.Dispose();
    }

}
