using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObjectGridUI : MonoBehaviour
{
    [SerializeField] private KitchenObjectIcanUI iconTemplateUI;

    private void Start()
    {
        iconTemplateUI.Hide();
    }

    public void ShowKitchenObjectUI(KitchenObjectSO kitchenObjectSO)
    {
        KitchenObjectIcanUI _kitchenObjectIcanUI = Instantiate(iconTemplateUI, transform);
        _kitchenObjectIcanUI.Show(kitchenObjectSO.sprite);
    }

    public void HideKitchenObjectUI()
    {
        var items = GetComponentsInChildren<KitchenObjectIcanUI>();
        foreach(var item in items)
        {
            Debug.Log(item);
            Destroy(item.gameObject);
        }
    }    
}
