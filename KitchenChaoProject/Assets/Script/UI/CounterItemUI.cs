using UnityEngine;
using UnityEngine.EventSystems;

public class CounterItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("点击该 UI 时要生成并拖拽的柜台预制体")]
    [SerializeField] private BaseCounter counterPrefab;

    /// <summary>
    /// 运行时也可以动态设置（例如由列表数据绑定）。
    /// </summary>
    public void SetCounterPrefab(BaseCounter prefab)
    {
        counterPrefab = prefab;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            return;

        TrySpawnCounterGhost();
    }

    private void TrySpawnCounterGhost()
    {
        if (counterPrefab == null)
        {
            Debug.LogWarning($"{nameof(CounterItemUI)}: 未绑定 counterPrefab，无法生成柜台。");
            return;
        }

        if (CounterManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(CounterItemUI)}: 未找到 CounterManager.Instance。");
            return;
        }

        BaseCounter ghost = CounterManager.Instance.BeginPlaceCounterFromExternal(counterPrefab);
        if (ghost == null)
        {
            Debug.LogWarning($"{nameof(CounterItemUI)}: 当前可能不在创建模式，未开始拖拽放置。");
        }
    }
}
