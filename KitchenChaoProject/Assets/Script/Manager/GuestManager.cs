using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class GuestManager : MonoBehaviour
{
    public static GuestManager Instance;
    [SerializeField] private int guestCount = 6;
    [SerializeField] private Transform generatePositon;
    [SerializeField] private Transform idlePositon;
    [SerializeField] private GameObject guestPrefab;
    [SerializeField] private int minGenertaDis = -10;
    [SerializeField] private int maxGenertaDis = 10;
    [SerializeField] private List<Transform> targetList;
    [SerializeField] private List<GuestController> guestList = new List<GuestController>();

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {
        foreach(var item in FindObjectsByType<DeliveryCounter>(FindObjectsSortMode.None))
        {
            targetList.Add(item.transform.GetChild(2).transform);
        }
        
        for(int i = 0; i < guestCount; i++)
        {
            Vector3 tempPosition = generatePositon.position + new Vector3(UnityEngine.Random.Range(minGenertaDis,maxGenertaDis),0,UnityEngine.Random.Range(minGenertaDis,maxGenertaDis));
            GuestController temp = Instantiate(guestPrefab,tempPosition,generatePositon.rotation).GetComponent<GuestController>();
            temp.SetIdlePositon(idlePositon);
            guestList.Add(temp);
        }
    }

    public void GuestGetOrder()
    {
        foreach(GuestController guest in guestList)
        {
            if(guest.GetGuestState() == GuestController.GuestState.Idle)
            {
                RecipeSO recipe = OrderManager.Instance.GetOrderRecipeSOList()[OrderManager.Instance.GetOrderRecipeSOList().Count-1];
                Transform target = null;
                foreach(Transform _target in targetList)
                {
                    if (_target.parent.GetComponent<DeliveryCounter>().GetGuess() == null)
                    {
                        _target.parent.GetComponent<DeliveryCounter>().SetGuest(guest);
                        target = _target;
                        guest.SetOrder(recipe,target);
                        return;
                    }
                }
                Debug.Log("柜台中都有用户");
                return;
            }
        }
    }

    
    public bool GetEmptyDeliveryCounter()
    {
        foreach(Transform _target in targetList)
        {
            if (_target.parent.GetComponent<DeliveryCounter>().GetGuess() == null)
            {
                return true;
            }
        }
        return false;
    }
}
