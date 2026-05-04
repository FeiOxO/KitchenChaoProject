using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter
{
    [SerializeField] private FryingRecipeListSO fryingRecipeList;
    [SerializeField] private FryingRecipeListSO burningRecipeList;
    [SerializeField] private StoveCounterVisual stoveCounterVisual;
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private AudioSource audioSource;
    public enum StoveState
    {
        Idle,
        Frying,
        Burning
    }

    private StoveState state = StoveState.Idle;
    private FryingRecipeSO fryingRecipe;
    private float fryingTimer = 0;
    void Start()
    {
        audioSource.volume = GameData.soundVolumeValue;
        GameData.OnSoundVolumeValueChanged += SetVolume;
    }

    private void SetVolume(float value)
    {
        audioSource.volume = value;
    }

    public override void Interact(PlayerController player)
    {
        // 手上有食材
        if (player.IsHaveKitchenObject())
        {// 柜台无食材 and 手上的食材要为可以煎烤的食物
            if (!IsHaveKitchenObject())
            {
                if (fryingRecipeList.TryGetFryingRecipe(
                    player.GetKitchenObject().GetKitchenObjectSO(), out FryingRecipeSO _fryingRecipe))
                {
                    TransferKitchenObject(player, this);
                    StartFrying(_fryingRecipe);
                }
                else if (burningRecipeList.TryGetFryingRecipe(
                    player.GetKitchenObject().GetKitchenObjectSO(), out FryingRecipeSO _burmingRecipe))
                {
                    TransferKitchenObject(player, this);
                    StartBurning(_burmingRecipe);
                }
                else
                {

                }
            }
        }
        else // 手上无食材
        {// 柜台有食材
            if (IsHaveKitchenObject())
            {
                TurnToIdle();
                fryingTimer = 0;
                TransferKitchenObject(this, player);
            }
        }
    }

    // public override void InteractOperate(PlayerController player)
    // {

    // }

    private void Update()
    {
        switch (state)
        {
            case StoveState.Idle:
                break;
            case StoveState.Frying:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer / fryingRecipe.fryingTime);
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestroykitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);

                    burningRecipeList.TryGetFryingRecipe(
                        GetKitchenObject().GetKitchenObjectSO(), out FryingRecipeSO newFryingRecipe);
                    StartBurning(newFryingRecipe);
                }
                break;
            case StoveState.Burning:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer / fryingRecipe.fryingTime);
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestroykitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);
                    TurnToIdle();
                }
                break;
            default:
                break;
        }
    }

    public void StartFrying(FryingRecipeSO _fryingRecipe)
    {
        fryingTimer = 0;
        fryingRecipe = _fryingRecipe;
        state = StoveState.Frying;
        stoveCounterVisual.ShowStoveEffect();
        audioSource.Play();
    }

    public void StartBurning(FryingRecipeSO _fryingRecipe)
    {
        if (_fryingRecipe == null)
        {
            fryingTimer = 0;
            TurnToIdle();
            Debug.LogWarning("无法获取Buring的食谱，无法进行BUring。");
            return;
        }
        stoveCounterVisual.ShowStoveEffect();
        fryingTimer = 0;
        fryingRecipe = _fryingRecipe;
        state = StoveState.Burning;
        audioSource.Play();
    }

    private void TurnToIdle()
    {
        state = StoveState.Idle;
        progressBarUI.Hide();
        stoveCounterVisual.HideStoveEffect();
        audioSource.Pause();
    }

    void OnDestroy()
    {
        GameData.OnSoundVolumeValueChanged -= SetVolume;
    }
}
