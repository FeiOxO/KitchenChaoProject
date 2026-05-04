using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GuestController : MonoBehaviour
{
    public enum GuestState
    {
        // 肚子饿
        Hungry,
        // 开心
        Happy,
        // 愤怒
        Anger,
        // 死亡
        Death,
        // 闲逛
        Idle,
        // 等待
        Wait,
    }
    [SerializeField] GuestState guestState = GuestState.Idle;
    [SerializeField] GuestAnimator guestAnimtor;
    [SerializeField] GuestStateUI guestStateUI;
    [SerializeField] KitchenObjectGridUI kitchenObjectGridUI;
    [SerializeField] RecipeSO targetRecipe = null;
    [SerializeField] private Transform target;
    [SerializeField] private Transform idlePosition;

    void Start()
    {
        guestAnimtor = GetComponent<GuestAnimator>();
    }

    void Update()
    {
        switch (guestState)
        {
            case GuestState.Idle:
                Idle();
                break;
            case GuestState.Anger:
                Anger();
                break;
            case GuestState.Happy:
                Happy();
                break;
            case GuestState.Hungry:
                Hungry();
                break;
            case GuestState.Death:
                Death();
                break;
            case GuestState.Wait:
                Wait();
                break;
            default:
                break;
        }
        guestAnimtor.AnimatorUpdate();
    }

    private void Idle()
    {
        guestAnimtor.IdleState(idlePosition.position);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.transform == target)
        {
            guestState = GuestState.Wait;
            return;
        }
    }

    private void Hungry()
    {
        guestAnimtor.HungryState(target.position);        
    }

    private void Wait()
    {
        guestAnimtor.WaitState();
    }

    private void Anger()
    {
        if (guestAnimtor.ResponseState())
        {
            guestState = GuestState.Idle;
            guestStateUI.Hide();
        }
    }

    private void Happy()
    {
        
        if (guestAnimtor.ResponseState())
        {
            guestState = GuestState.Idle;
            guestStateUI.Hide();
        }
    }

    private void Death()
    {
        
    }

    private void ShowKitchenObjectSO(RecipeSO recipe)
    {
        foreach(KitchenObjectSO item in recipe.kitchenObjectSOList)
        {
            kitchenObjectGridUI.ShowKitchenObjectUI(item);
        }
    }

    public void SetOrder(RecipeSO recipe,Transform targetPositon)
    {
        targetRecipe = recipe;
        target = targetPositon;
        ShowKitchenObjectSO(recipe);
        guestState = GuestState.Hungry;
    }

    public void DeliverRecipe(PlateKitchenObject _plateKitchenObject)
    {
        //TODO:根据Guest内部的菜单表格判断是否是正确的菜单 是：Happy 否：Anger
        if(OrderManager.IsCorrect(targetRecipe,_plateKitchenObject))
        {
            guestState = GuestState.Happy;
            guestStateUI.ShowHappy();
        }
        else
        {
            guestState = GuestState.Anger;
            guestStateUI.ShowAngrey();
        }
        kitchenObjectGridUI.HideKitchenObjectUI();
        targetRecipe = null;
    }

    public GuestState GetGuestState()
    {
        return guestState;
    }

    public void SetIdlePositon(Transform _idlePositon)
    {
        idlePosition = _idlePositon;
    }

}
