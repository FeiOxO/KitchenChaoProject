using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// =============================================================================
// CounterManager — 大致逻辑说明
// =============================================================================
// 【单例】场景中通常只有一个；Instance 供 BaseCounterControl、GameManager 等访问。
//
// 【两种模式】CounterManagerMode —— 运行时由 GameManager 决定，勿在其它脚本随意 SetMode（除非你有意覆盖关卡流程）。
// - GameManager：SceneManager.sceneLoaded → ApplyCounterManagerModeForScene（默认按场景名 99-CreateScene=Create / 否则=Game；
//   可在 GameManager 勾选「使用自定义 CounterManager 模式」改为始终用 Inspector 里指定的 Create 或 Game），再 RefreshCountersForCurrentMode。
//
// - Create（创建）：可射线拾取区内柜台拖拽（Update）；可外部 BeginPlaceCounterFromExternal 拖临时体落地；
//   松左键后 LateUpdate 会清空 selectedCounterForEdit（在 EndDrag 之后执行，不挡位姿写入）。
// - Game（游戏）：不走上述创建拖拽逻辑；柜台来自 CounterJson 持久化（见私有 LoadCountersFromPersistentJsonForCurrentScene）。
//   gameplaySpawnCache 与 CacheCurrentCountersToGameplayCache / LoadCountersFromGameplayCache 为内存快照重生（当前未接线，可按需在类内调用）。
//
// 【数据】
// - counters：当前登记在管理器下的柜台实例列表；内部 SpawnCounter 会 Instantiate、BindSpawnSourcePrefab、加入列表。
// - spawnLayout：关卡布局表；spawnOnStart 时 SpawnFromLayout 读取它生成实例。编辑柜台后由 RebuildSpawnLayoutFromCounters
//   根据当前 counters 回写（与场景内实例一致）；批量 SpawnFromLayout 时用 _suppressSpawnLayoutSync 避免 foreach 中改表。
// - gameplaySpawnCache：游戏用布局快照（内存）；持久化布局见 CounterJson（编辑器：Assets/CounterLayouts；正式包：persistentDataPath/CounterLayouts）+ SceneNN.json。
// - selectedCounterForEdit：区内拖拽时「当前编辑目标」，供 UpdateSelectedCounterPose 使用。
//
// 【创建区与放下检测】
// - createModeZoneTrigger：用于 IsWorldPositionInCreateZone / TryGetCounterManagerNearPoint 的 bounds.Contains，
//   与「射线能否开始区内拖拽」一致；务必在 Inspector 赋值。
// - TryGetCounterManagerNearPoint：先 Contains 创建区，再 OverlapSphere 兜底，避免仅靠物理层扫不到 Trigger。
//
// 【与 BaseCounterControl 的时序】
// - Update：左键按下 + 非拖拽中 → 射线命中带 BaseCounterControl 的 BaseCounter 且在区内 → BeginInZoneDrag。
// - NotifyExternalPlaceDragStarted：外部生成当帧屏蔽一次射线拾取，避免误开第二次拖拽。
// =============================================================================

/// <summary>一条「预制体 + 世界坐标位姿」的生成/缓存记录。</summary>
[System.Serializable]
public class CounterSpawnEntry
{
    public BaseCounter prefab;
    public Vector3 position;
    public Vector3 eulerAngles;
}

/// <summary>创建 = 关卡编辑式摆柜台；游戏 = 运行时用缓存布局生成。</summary>
public enum CounterManagerMode
{
    Create,
    Game
}

/// <summary>
/// 柜台单例管理：列表与生成、创建区检测、与 BaseCounterControl 协同的射线拾取与选中状态。
/// Create / Game 模式由 <see cref="GameManager"/> 在场景加载时写入（默认按场景名；可选 Inspector 自定义覆盖），本类只负责响应 <see cref="SetMode"/> 与 <see cref="RefreshCountersForCurrentMode"/>。
/// </summary>
public class CounterManager : MonoBehaviour
{
    public static CounterManager Instance { get; private set; }

    [Tooltip("运行时由 GameManager 按场景名设置（99-CreateScene=Create，其它=Game）；Inspector 默认值仅作编辑参考。")]
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
    [Tooltip("关卡布局：预制体 + 位姿。SpawnFromLayout 会读取；生成/移动/删除柜台后也会按当前 counters 自动回写。")]
    [SerializeField] private List<CounterSpawnEntry> spawnLayout = new List<CounterSpawnEntry>();
    [SerializeField] private bool spawnOnStart = true;

    [Header("游戏模式缓存（内存）")]
    [Tooltip("进入游戏模式时用于生成柜台的缓存数据（可由 CacheCurrentCountersToGameplayCache 填充）。")]
    [SerializeField] private List<CounterSpawnEntry> gameplaySpawnCache = new List<CounterSpawnEntry>();


    [Header("加载数据文件栏")]
    [Tooltip("若赋值：开始与保存后会刷新列表；选项文案为 SceneNN（无 .json），加载时自动补全扩展名。")]
    [SerializeField] private TMP_Dropdown dropdown;

    [Header("JSON 持久化：counterId 与预制体（1=Clear … 6=Trash，顺序固定）")]
    [Tooltip("下标 0 → counterId 1 ClearCounter；…；下标 5 → counterId 6 TrashCounter。保存/加载 JSON 依赖此映射。")]
    [SerializeField] private BaseCounter[] counterPrefabsByCounterId = new BaseCounter[6];

    [Header("创建模式：当前选中的柜台")]
    [SerializeField] private BaseCounter selectedCounterForEdit;

    private int _blockInZonePickUntilFrame = -1;

    /// <summary>为 true 时不在 SpawnCounter 等路径里回写 spawnLayout，避免 SpawnFromLayout 的 foreach 与改表冲突。</summary>
    private bool _suppressSpawnLayoutSync;

    public CounterManagerMode CurrentMode => currentMode;
    public IReadOnlyList<BaseCounter> Counters => counters;
    public BaseCounter SelectedCounterForEdit => selectedCounterForEdit;

    // --- 生命周期与单例 ----------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        RefreshCounterLayoutDropdownIfAssigned();
    }

    /// <summary>当已绑定 <c>dropdown</c> 时刷新 CounterLayouts 文件名列表。</summary>
    private void RefreshCounterLayoutDropdownIfAssigned()
    {
        if (dropdown != null)
            PopulateCounterLayoutDropdownOptions();
    }

    // --- 创建模式：左键按下射线 → 区内柜台开始拖拽（交给 BaseCounterControl） ----------------

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
        if (!Physics.Raycast(ray, out RaycastHit hit, counterPickRayDistance, counterPickLayers, QueryTriggerInteraction.Ignore))
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

    // --- 创建模式：左键松开后先提交选中柜台（挂点 + 列表），再清空 selected（在 EndDrag 的 LateUpdate 之后） ---

    private void LateUpdate()
    {
        if (currentMode != CounterManagerMode.Create)
            return;

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            CommitSelectedCounterFromEdit();
            ClearSelectedCounterForEdit();
        }
    }

    /// <summary>
    /// 左键松开、清空选中前：用当前 selectedCounterForEdit 做一次「归属与列表」提交——
    /// 重新挂到本管理器节点下（与 SpawnCounter 一致），若尚未在 counters 中则 RegisterCounter 添加；
    /// 最后按 counters 回写 spawnLayout，使 Inspector 中的布局数据与场景一致。
    /// </summary>
    private void CommitSelectedCounterFromEdit()
    {
        if (selectedCounterForEdit == null)
            return;

        BaseCounter c = selectedCounterForEdit;
        if (c == null)
            return;

        if (c.transform.parent != transform)
            c.transform.SetParent(transform, true);

        if (!counters.Contains(c))
            RegisterCounter(c);
        else
            TryRebuildSpawnLayoutFromCounters();
    }

    /// <summary>用当前 counters 覆盖 spawnLayout（需柜台已 BindSpawnSourcePrefab）。</summary>
    private void RebuildSpawnLayoutFromCountersCore()
    {
        spawnLayout.Clear();
        foreach (BaseCounter c in counters)
        {
            if (c == null)
                continue;

            BaseCounter prefab = c.GetSpawnSourcePrefab();
            if (prefab == null)
            {
                Debug.LogWarning($"CounterManager: 柜台 {c.name} 无源预制体，已跳过写入 spawnLayout。");
                continue;
            }

            spawnLayout.Add(new CounterSpawnEntry
            {
                prefab = prefab,
                position = c.transform.position,
                eulerAngles = c.transform.eulerAngles
            });
        }
    }

    private void TryRebuildSpawnLayoutFromCounters()
    {
        if (_suppressSpawnLayoutSync)
            return;

        RebuildSpawnLayoutFromCountersCore();
    }

    // --- 模式 ----------------------------------------------------------------

    /// <summary>
    /// 设置 Create / Game。正常流程下仅由 <see cref="GameManager"/> 在 <c>SceneManager.sceneLoaded</c> 中调用；改模式后通常应再调 <see cref="RefreshCountersForCurrentMode"/>。
    /// </summary>
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

    // --- 持久化 JSON（CounterJson）与场景柜台初始化 --------------------------------

    /// <summary>
    /// 由 <see cref="GameManager"/> 在场景加载流程中于 <see cref="CounterManager.SetMode"/> 之后调用：按当前模式清空并重生柜台。
    /// 游戏模式从 CounterLayouts 读取编号最大的 SceneNN.json；创建模式且 <c>spawnOnStart</c> 时从 <c>spawnLayout</c> 生成。
    /// </summary>
    public void RefreshCountersForCurrentMode()
    {
        Debug.Log("RefreshCountersForCurrentMode");
        if (IsGameMode()){
            ClearSpawnedCounters();
            LoadCountersFromPersistentJsonForCurrentScene(clearExistingFirst: false);
        }
        else if (spawnOnStart && IsCreateMode())
            SpawnFromLayout();
    }

    /// <summary>
    /// 供 UI Button 绑定：将当前 <c>spawnLayout</c>（会先按 counters 重建）写入 SceneNN.json。
    /// </summary>
    public void UIButtonSaveCurrentCounterLayoutToJson()
    {
        if (!IsCreateMode())
            Debug.LogWarning("CounterManager: 当前非创建模式，仍会将当前布局写入 JSON。");

        RebuildSpawnLayoutFromCountersCore();
        CounterLayoutJsonRoot root = BuildJsonRootFromSpawnLayout();
        string fileName = GetCurrentSceneLayoutFileName();
        if (CounterJson.SaveLayout(fileName, root))
        {
            Debug.Log($"柜台布局已保存: {Path.Combine(CounterJson.GetLayoutDirectoryPath(false), fileName)}");
            RefreshCounterLayoutDropdownIfAssigned();
        }
    }

    /// <summary>
    /// 下一次保存应使用的 JSON 文件名：<c>Scene(当前 CounterLayouts 下符合 SceneNN.json 规则的文件数量 + 1).json</c>。
    /// 例如目录为空 → Scene01；已有 Scene01.json → Scene02。
    /// </summary>
    private string GetCurrentSceneLayoutFileName()
    {
        return CounterJson.GetNextSceneLayoutFileName();
    }

    /// <summary>
    /// 扫描 CounterLayouts 中 SceneNN.json，按编号升序填入 <c>dropdown</c>；选项文案为 <c>SceneNN</c>（不含 <c>.json</c>），读盘时由 <see cref="CounterJson.ToSceneLayoutFileNameWithJsonExtension"/> 补扩展名。
    /// 由 <see cref="Start"/> 与保存布局成功后通过 <see cref="RefreshCounterLayoutDropdownIfAssigned"/> 调用。
    /// </summary>
    private void PopulateCounterLayoutDropdownOptions()
    {
        if (dropdown == null)
        {
            Debug.LogWarning("CounterManager.PopulateCounterLayoutDropdownOptions: TMP_Dropdown 为空。");
            return;
        }

        List<string> names = CounterJson.GetSceneLayoutFileNamesSorted();
        dropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>(names.Count);
        foreach (string fileName in names)
            options.Add(new TMP_Dropdown.OptionData(Path.GetFileNameWithoutExtension(fileName)));

        dropdown.AddOptions(options);
        if (names.Count > 0)
        {
            dropdown.value = 0;
            dropdown.RefreshShownValue();
        }
    }

    /// <summary>
    /// 读取指定 <paramref name="dropdown"/> 选中项对应的布局 JSON 数据（不生成柜台）。
    /// </summary>
    private bool TryReadSelectedCounterLayoutFromDropdown(TMP_Dropdown dropdown, out CounterLayoutJsonRoot root)
    {
        root = null;

        if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
            return false;

        int index = dropdown.value;
        if (index < 0 || index >= dropdown.options.Count)
            return false;

        string optionText = dropdown.options[index].text;
        if (string.IsNullOrWhiteSpace(optionText))
            return false;

        string fileName = CounterJson.ToSceneLayoutFileNameWithJsonExtension(optionText);
        return CounterJson.TryLoadLayout(fileName, out root);
    }

    /// <summary>使用已序列化的 <c>dropdown</c> 读取当前选中项的布局 JSON（不生成柜台）。</summary>
    private bool TryReadSelectedCounterLayoutFromDropdown(out CounterLayoutJsonRoot root)
    {
        return TryReadSelectedCounterLayoutFromDropdown(dropdown, out root);
    }

    /// <summary>
    /// 供 UI Button：先清空已登记柜台，再按 Inspector 绑定的 <c>dropdown</c> 当前选中文件名加载 JSON 并生成柜台。
    /// </summary>
    /// <returns>是否成功（dropdown 已赋值且文件可读）。</returns>
    public void UIButtonLoadCountersFromSelectedDropdown()
    {
        if (dropdown == null)
        {
            Debug.LogWarning("CounterManager.UIButtonLoadCountersFromSelectedDropdown: dropdown 未赋值。");
            return;
        }

        if (!TryReadSelectedCounterLayoutFromDropdown(dropdown, out CounterLayoutJsonRoot root))
        {
            Debug.LogWarning("CounterManager: 无法读取下拉框当前选中项对应的布局文件。");
            return;
        }

        ClearSpawnedCounters();
        ApplyCounterLayoutJsonRoot(root);
    }

    /// <summary>
    /// 游戏模式：从 CounterJson 布局目录读取编号最大的 SceneNN.json 并生成柜台。
    /// </summary>
    /// <param name="clearExistingFirst">为 true 时先清空列表并销毁已有柜台（一般已由 RefreshCountersForCurrentMode 处理）。</param>
    private void LoadCountersFromPersistentJsonForCurrentScene(bool clearExistingFirst = true)
    {
        if (clearExistingFirst)
            ClearSpawnedCounters();

        if (!CounterJson.TryGetLatestSceneLayoutFileName(out string fileName) ||
            !CounterJson.TryLoadLayout(fileName, out CounterLayoutJsonRoot root))
        {
            Debug.LogError(
                $"CounterManager 游戏模式：CounterLayouts 下没有可加载的 SceneNN.json。请先在建关场景保存布局（Button 调用 UIButtonSaveCurrentCounterLayoutToJson）。目录：{CounterJson.GetLayoutDirectoryPath(false)}");
            return;
        }

        ApplyCounterLayoutJsonRoot(root);
    }

    private void ApplyCounterLayoutJsonRoot(CounterLayoutJsonRoot root)
    {
        if (root == null || root.entries == null)
            return;

        _suppressSpawnLayoutSync = true;
        try
        {
            foreach (CounterLayoutJsonEntry e in root.entries)
            {
                BaseCounter prefab = GetPrefabByCounterId(e.counterId);
                if (prefab == null)
                {
                    Debug.LogWarning($"CounterManager: 未知的 counterId={e.counterId}，已跳过。");
                    continue;
                }

                SpawnCounter(prefab, e.position, e.rotation);
            }
        }
        finally
        {
            _suppressSpawnLayoutSync = false;
        }

        RebuildSpawnLayoutFromCountersCore();
    }

    private CounterLayoutJsonRoot BuildJsonRootFromSpawnLayout()
    {
        var list = new List<CounterLayoutJsonEntry>();
        foreach (CounterSpawnEntry entry in spawnLayout)
        {
            if (entry == null || entry.prefab == null)
                continue;

            int id = GetCounterIdForPrefab(entry.prefab);
            if (id < 0)
            {
                Debug.LogWarning($"CounterManager: 无法为预制体 {entry.prefab.name} 映射 counterId（1–6），已跳过写入 JSON。");
                continue;
            }

            list.Add(new CounterLayoutJsonEntry
            {
                counterId = id,
                position = entry.position,
                rotation = entry.eulerAngles
            });
        }

        return new CounterLayoutJsonRoot { entries = list.ToArray() };
    }

    private BaseCounter GetPrefabByCounterId(int counterId)
    {
        if (counterId < 1 || counterId > 6)
            return null;

        int i = counterId - 1;
        if (counterPrefabsByCounterId == null || i >= counterPrefabsByCounterId.Length)
            return null;

        return counterPrefabsByCounterId[i];
    }

    private int GetCounterIdForPrefab(BaseCounter prefabTemplate)
    {
        if (prefabTemplate == null || counterPrefabsByCounterId == null)
            return -1;

        for (int i = 0; i < counterPrefabsByCounterId.Length && i < 6; i++)
        {
            BaseCounter p = counterPrefabsByCounterId[i];
            if (p != null && p == prefabTemplate)
                return i + 1;
        }

        string stripped = prefabTemplate.name.Replace("(Clone)", string.Empty).Trim();
        for (int i = 0; i < counterPrefabsByCounterId.Length && i < 6; i++)
        {
            BaseCounter p = counterPrefabsByCounterId[i];
            if (p == null)
                continue;

            if (p.name == stripped || stripped.StartsWith(p.name, System.StringComparison.Ordinal))
                return i + 1;
        }

        return -1;
    }

    // --- 游戏模式：缓存当前布局 → 按缓存重生 ----------------------------------------

    /// <summary>
    /// 将当前 counters 中的柜台快照写入 gameplaySpawnCache（需柜台由生成流程绑定源预制体）。
    /// </summary>
    private void CacheCurrentCountersToGameplayCache()
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
    private void LoadCountersFromGameplayCache()
    {
        _suppressSpawnLayoutSync = true;
        try
        {
            ClearSpawnedCounters();
            foreach (CounterSpawnEntry entry in gameplaySpawnCache)
            {
                if (entry == null || entry.prefab == null)
                    continue;

                SpawnCounter(entry.prefab, entry.position, entry.eulerAngles);
            }
        }
        finally
        {
            _suppressSpawnLayoutSync = false;
        }

        RebuildSpawnLayoutFromCountersCore();
    }

    // --- 创建模式：当前编辑目标与位姿写入 ----------------------------------------------

    public void SetSelectedCounterForEdit(BaseCounter counter)
    {
        selectedCounterForEdit = counter;
    }

    private void ClearSelectedCounterForEdit()
    {
        selectedCounterForEdit = null;
    }

    /// <summary>
    /// 创建模式情况1：在有效区域放下时，用预制体在指定位置生成并登记（用于从临时拖拽物落地）。
    /// </summary>
    public BaseCounter AddCounterAtPose(BaseCounter prefab, Vector3 worldPosition, Quaternion worldRotation)
    {
        return SpawnCounter(prefab, worldPosition, worldRotation);
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

    // --- 创建区 / 放下点是否在「本管理器」编辑范围内 --------------------------------------

    private bool IsWorldPositionInCreateZone(Vector3 worldPosition)
    {
        if (createModeZoneTrigger == null)
            return false;

        return createModeZoneTrigger.bounds.Contains(worldPosition);
    }

    /// <summary>
    /// 检测放下点是否仍属于本 CounterManager 的编辑范围：
    /// 优先使用与拾取相同的「创建区域」Trigger 的 bounds（避免仅依赖 OverlapSphere 时层矩阵/触发器未被扫到）；
    /// 再退回 OverlapSphere 查找带 CounterManager 的碰撞体。
    /// </summary>
    /// <param name="excludeOverlapFromCounter">
    /// 拖拽中的柜台实例。OverlapSphere 会扫到其自身碰撞体；若柜台挂在 CounterManager 下，
    /// 若不忽略则会误把「自己身上的 Collider」当成编辑区命中，导致 hasManager 恒为 true。传 null 则不忽略。
    /// </param>
    public bool TryGetCounterManagerNearPoint(Vector3 worldPosition, out CounterManager manager,
        BaseCounter excludeOverlapFromCounter = null)
    {
        manager = null;

        if (createModeZoneTrigger != null && createModeZoneTrigger.bounds.Contains(worldPosition))
        {
            manager = this;
            return true;
        }

        Collider[] cols = Physics.OverlapSphere(worldPosition, releaseOverlapRadius, releaseCheckLayers,
            QueryTriggerInteraction.Collide);
        foreach (Collider col in cols)
        {
            if (excludeOverlapFromCounter != null &&
                col.GetComponentInParent<BaseCounter>() == excludeOverlapFromCounter)
                continue;

            CounterManager m = col.GetComponentInParent<CounterManager>();
            if (m != null && m == this)
            {
                manager = m;
                return true;
            }
        }

        return false;
    }

    // --- 与 BaseCounterControl 的防冲突 ------------------------------------------------

    internal void NotifyExternalPlaceDragStarted()
    {
        _blockInZonePickUntilFrame = Time.frameCount + 1;
    }

    // --- 生成、注册与列表维护 --------------------------------------------------------

    /// <returns>生成出的实例；prefab 为空时返回 null。</returns>
    private BaseCounter SpawnCounter(BaseCounter prefab, Vector3 worldPosition, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("CounterManager: prefab 为空，无法生成。");
            return null;
        }

        // 实例挂到管理器下，便于统一销毁与层级管理。
        BaseCounter instance = Instantiate(prefab, worldPosition, rotation, transform);
        instance.BindSpawnSourcePrefab(prefab);
        counters.Add(instance);
        TryRebuildSpawnLayoutFromCounters();
        return instance;
    }

    /// <summary>
    /// <see cref="SpawnCounter(BaseCounter, Vector3, Quaternion)"/> 的重载：用欧拉角表示世界旋转。
    /// </summary>
    private BaseCounter SpawnCounter(BaseCounter prefab, Vector3 worldPosition, Vector3 eulerAngles)
    {
        return SpawnCounter(prefab, worldPosition, Quaternion.Euler(eulerAngles));
    }

    private void SpawnFromLayout()
    {
        _suppressSpawnLayoutSync = true;
        try
        {
            foreach (CounterSpawnEntry entry in spawnLayout)
            {
                if (entry == null || entry.prefab == null)
                    continue;

                SpawnCounter(entry.prefab, entry.position, entry.eulerAngles);
            }
        }
        finally
        {
            _suppressSpawnLayoutSync = false;
        }

        RebuildSpawnLayoutFromCountersCore();
    }

    private void RegisterCounter(BaseCounter counter)
    {
        if (counter == null || counters.Contains(counter))
            return;

        counters.Add(counter);
        TryRebuildSpawnLayoutFromCounters();
    }

    private bool UnregisterCounter(BaseCounter counter)
    {
        if (counter == null)
            return false;

        bool removed = counters.Remove(counter);
        if (removed)
            TryRebuildSpawnLayoutFromCounters();

        return removed;
    }

    private void ClearSpawnedCounters()
    {
        for (int i = counters.Count - 1; i >= 0; i--)
        {
            if (counters[i] != null)
                Destroy(counters[i].gameObject);
        }

        counters.Clear();
        selectedCounterForEdit = null;
        TryRebuildSpawnLayoutFromCounters();
    }

    // --- 外部入口：从 UI 等发起「临时体 → 拖入编辑区落地」----------------------------------

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

    /// <summary>屏幕射线与 Y=planeY 水平面相交，用于把新生成物体摆在鼠标下。</summary>
    private static Vector3 GetMouseOnPlaneWorldPosition(float planeY)
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
}
