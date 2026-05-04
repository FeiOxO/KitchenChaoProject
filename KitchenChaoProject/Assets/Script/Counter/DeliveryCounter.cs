using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryCounter : BaseCounter
{
    [SerializeField] private GuestController guest;
    public override void Interact(PlayerController player)
    {
        if (player.IsHaveKitchenObject() &&
            player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject _plateKitchenObject)
            && guest != null)
        {
            // TODO：判断是否为正确的菜
            OrderManager.Instance.DeliverRecipe(_plateKitchenObject);        
            guest.DeliverRecipe(_plateKitchenObject);
            guest = null;
            player.DestroykitchenObject();
        }
    }

    public void SetGuest(GuestController _guest)
    {
        guest = _guest;
    }

    public GuestController GetGuess()
    {
        return guest;
    }
}
