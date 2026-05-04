using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class CounterSpawnEntry
{
    public BaseCounter prefab;
    public Vector3 position;
    public Vector3 eulerAngles;
}

public enum CounterManagerMode
{
    Create,
    Game
}

public class CounterManager : MonoBehaviour
{
    public static CounterManager Instance { get; private set; }

    [SerializeField] private CounterManagerMode currentMode = CounterManagerMode.Create;
    [Tooltip("用于判断「在创建触发器范围内」：检测点是否落在此 Collider 的 bounds 内（建议用 BoxCollider / MeshCollider 且 Is Trigger）。")]
    [SerializeField] private Collider createModeZoneTrigger;
    [Tooltip("鼠标从屏幕射线检测柜台的可拾取距离。")]
    [SerializeField] private float counterPickRayDistance = 80f;
    [SerializeField] private LayerMask counterPickLayers = ~0;
    [Tooltip("放下时检测是否与带 CounterManager 的碰撞体重叠的半径。")]
    [SerializeField] private float releaseOverlapRadius = 1f;
    [SerializeField] private LayerMask releaseCheckLayers = ~0;

    [SerializeField] private List<BaseCounter> counters = new List<BaseCounter>();
    [Tooltip("在 Inspector 中配置每个柜台的预制体与世界坐标，可调用 SpawnFromLayout 或勾选 Spawn On Start 批量生成。")]
    [SerializeField] private List<CounterSpawnEntry> spawnLayout = new List<CounterSpawnEntry>();
    [SerializeField] private bool spawnOnStart;

    [Header("游戏模式缓存")]
    [Tooltip("进入游戏模式时用于生成柜台的缓存数据（可由 CacheCurrentCountersToGameplayCache 填充）。")]
    [SerializeField] private List<CounterSpawnEntry> gameplaySpawnCache = new List<CounterSpawnEntry>();

    [Header("创建模式：当前选中的柜台")]
    [SerializeField] private BaseCounter selectedCounterForEdit;

    private int _blockInZonePickUntilFrame = -1;

    public CounterManagerMode CurrentMode => currentMode;
    public IReadOnlyList<BaseCounter> Counters => counters;
    public BaseCounter SelectedCounterForEdit => selectedCounterForEdit;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (spawnOnStart)
            SpawnFromLayout();
    }

    private void Update()
    {
        if (currentMode != CounterManagerMode.Create)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (Time.frameCount <= _blockInZonePickUntilFrame)
            return;

        if (BaseCounterControl.IsAnyDragging)
            return;

        if (Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, counterPickRayDistance, counterPickLayers,
                QueryTriggerInteraction.Ignore))
            return;

        BaseCounter bc = hit.collider.GetComponentInParent<BaseCounter>();
        if (bc == null)
            return;

        BaseCounterControl control = bc.GetComponent<BaseCounterControl>();
        if (control == null)
            return;

        if (!IsWorldPositionInCreateZone(bc.transform.position))
            return;

        control.BeginInZoneDrag();
    }

    private void LateUpdate()
    {
        if (currentMode != CounterManagerMode.Create)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            ClearSelectedCounterForEdit();
    }

    public void SetMode(CounterManagerMode mode)
    {
        currentMode = mode;
    }

    public bool IsCreateMode()
    {
        return currentMode == CounterManagerMode.Create;
    }

    public bool IsGameMode()
    {
        return currentMode == CounterManagerMode.Game;
    }

    /// <summary>
    /// 将当前 counters 中的柜台快照写入 gameplaySpawnCache（需柜台由 SpawnCounter 生成并绑定了源预制体）。
    /// </summary>
    public void CacheCurrentCountersToGameplayCache()
    {
        gameplaySpawnCache.Clear();
        foreach (BaseCounter c in counters)
        {
            if (c == null)
                continue;

            BaseCounter template = c.GetSpawnSourcePrefab();
            if (template == null)
            {
                Debug.LogWarning($"CounterManager: 柜台 {c.name} 未绑定源预制体，已跳过缓存。");
                continue;
            }

            gameplaySpawnCache.Add(new CounterSpawnEntry
            {
                prefab = template,
                position = c.transform.position,
                eulerAngles = c.transform.eulerAngles
            });
        }
    }

    /// <summary>
    /// 游戏模式：清空当前柜台列表并依据 gameplaySpawnCache 重新生成。
    /// </summary>
    public void LoadCountersFromGameplayCache()
    {
        ClearSpawnedCounters();
        foreach (CounterSpawnEntry entry in gameplaySpawnCache)
        {
            if (entry == null || entry.prefab == null)
                continue;

            SpawnCounter(entry.prefab, entry.position, entry.eulerAngles);
        }
    }

    public void SetSelectedCounterForEdit(BaseCounter counter)
    {
        selectedCounterForEdit = counter;
    }

    public void ClearSelectedCounterForEdit()
    {
        selectedCounterForEdit = null;
    }

    /// <summary>
    /// 创建模式情况2：放下时更新当前选中柜台的位置与旋转。
    /// </summary>
    public void UpdateSelectedCounterPose(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (selectedCounterForEdit == null)
            return;

        selectedCounterForEdit.transform.SetPositionAndRotation(worldPosition, worldRotation);
    }

    /// <summary>
    /// 创建模式情况1：在有效区域放下时，用预制体在指定位置生成并登记（用于从临时拖拽物落地）。
    /// </summary>
    public BaseCounter AddCounterAtPose(BaseCounter prefab, Vector3 worldPosition, Quaternion worldRotation)
    {
        return SpawnCounter(prefab, worldPosition, worldRotation);
    }

    /// <summary>
    /// 从列表移除并销毁该柜台（创建模式情况2：未放到有效区域时）。
    /// </summary>
    public void RemoveAndDestroyCounter(BaseCounter counter)
    {
        if (counter == null)
            return;

        UnregisterCounter(counter);
        if (selectedCounterForEdit == counter)
            selectedCounterForEdit = null;

        Destroy(counter.gameObject);
    }

    public bool IsWorldPositionInCreateZone(Vector3 worldPosition)
    {
        if (createModeZoneTrigger == null)
            return false;

        return createModeZoneTrigger.bounds.Contains(worldPosition);
    }

    /// <summary>
    /// 检测某世界坐标附近是否存在带 CounterManager 的碰撞体（可在子物体上挂 Collider）。
    /// </summary>
    public bool TryGetCounterManagerNearPoint(Vector3 worldPosition, out CounterManager manager)
    {
        manager = null;
        Collider[] cols = Physics.OverlapSphere(worldPosition, releaseOverlapRadius, releaseCheckLayers,
            QueryTriggerInteraction.Collide);
        foreach (Collider col in cols)
        {
            CounterManager m = col.GetComponentInParent<CounterManager>();
            if (m != null)
            {
                manager = m;
                return true;
            }
        }

        return false;
    }

    internal void NotifyExternalPlaceDragStarted()
    {
        _blockInZonePickUntilFrame = Time.frameCount + 1;
    }

    public BaseCounter SpawnCounter(BaseCounter prefab, Vector3 worldPosition, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("CounterManager: prefab 为空，无法生成。");
            return null;
        }

        BaseCounter instance = Instantiate(prefab, worldPosition, rotation, transform);
        instance.BindSpawnSourcePrefab(prefab);
        counters.Add(instance);
        return instance;
    }

    public BaseCounter SpawnCounter(BaseCounter prefab, Vector3 worldPosition, Vector3 eulerAngles)
    {
        return SpawnCounter(prefab, worldPosition, Quaternion.Euler(eulerAngles));
    }

    public BaseCounter SpawnCounterAtTransform(BaseCounter prefab, Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning("CounterManager: target Transform 为空，无法生成。");
            return null;
        }

        return SpawnCounter(prefab, target.position, target.rotation);
    }

    public void SpawnFromLayout()
    {
        foreach (CounterSpawnEntry entry in spawnLayout)
        {
            if (entry == null || entry.prefab == null)
                continue;

            SpawnCounter(entry.prefab, entry.position, entry.eulerAngles);
        }
    }

    public void RegisterCounter(BaseCounter counter)
    {
        if (counter == null || counters.Contains(counter))
            return;

        counters.Add(counter);
    }

    public bool UnregisterCounter(BaseCounter counter)
    {
        return counter != null && counters.Remove(counter);
    }

    public void ClearSpawnedCounters()
    {
        for (int i = counters.Count - 1; i >= 0; i--)
        {
            if (counters[i] != null)
                Destroy(counters[i].gameObject);
        }

        counters.Clear();
        selectedCounterForEdit = null;
    }

    /// <summary>
    /// 由外部事件调用：在创建模式下生成一个临时柜台并进入「范围外放置」拖拽流程（情况1）。
    /// </summary>
    public BaseCounter BeginPlaceCounterFromExternal(BaseCounter prefab)
    {
        if (!IsCreateMode())
        {
            Debug.LogWarning("CounterManager: 当前非创建模式，已忽略 BeginPlaceCounterFromExternal。");
            return null;
        }

        if (prefab == null)
            return null;

        if (Camera.main == null)
            return null;

        BaseCounter ghost = Instantiate(prefab);
        BaseCounterControl control = ghost.GetComponent<BaseCounterControl>();
        if (control == null)
            control = ghost.gameObject.AddComponent<BaseCounterControl>();

        Vector3 pos = GetMouseOnPlaneWorldPosition(control.PlacementPlaneY);
        ghost.transform.SetPositionAndRotation(pos, ghost.transform.rotation);
        control.BeginExternalDrag(prefab);
        return ghost;
    }

    private static Vector3 GetMouseOnPlaneWorldPosition(float planeY)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
}
