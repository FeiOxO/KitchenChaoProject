using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounter : KitchenObjectHolder
{
    [SerializeField] private GameObject selectCounter;

    private BaseCounter spawnSourcePrefab;

    /// <summary>由 CounterManager 生成时绑定，用于缓存/重建布局。</summary>
    public void BindSpawnSourcePrefab(BaseCounter prefab)
    {
        spawnSourcePrefab = prefab;
    }

    public BaseCounter GetSpawnSourcePrefab()
    {
        return spawnSourcePrefab;
    }
    
    virtual public void Interact(PlayerController player)
    {
        Debug.LogWarning(this.gameObject + "没有重写Interact方法");
    }

    virtual public void InteractOperate(PlayerController player)
    {
        
    }

    public void SelectCounter()
    {
        selectCounter.SetActive(true);
    }

    public void CancelSelect()
    {
        selectCounter.SetActive(false);
    }
}
