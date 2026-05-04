using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounter : KitchenObjectHolder
{
    [SerializeField] private GameObject selectCounter;

    /// <summary>生成时绑定的「预制体根」；未绑定时 <see cref="GetSpawnSourceGameObject"/> 退回 <see cref="GameObject"/> 自身。</summary>
    private GameObject spawnSourcePrefabRoot;

    /// <summary>由 CounterManager 在 Instantiate 后调用，保存预制体根物体（与传入的 <paramref name="prefab"/> 为同一套资源）。</summary>
    public void BindSpawnSourcePrefab(BaseCounter prefab)
    {
        spawnSourcePrefabRoot = prefab != null ? prefab.gameObject : null;
    }

    /// <summary>用于布局/缓存：有绑定则返回预制体根 <see cref="GameObject"/>，否则返回本实例的 <c>gameObject</c>。</summary>
    public GameObject GetSpawnSourceGameObject()
    {
        return spawnSourcePrefabRoot != null ? spawnSourcePrefabRoot : gameObject;
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
