using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// =============================================================================
// BaseCounterControl — 大致逻辑说明
// =============================================================================
// 本脚本只在「CounterManager 为创建模式」时工作，负责把柜台放在水平面上跟随鼠标，并在松手时做判定。
//
// 【两种拖拽来源】（用 DragKind 区分）
// 1) ExternalNew（情况1）
//    - 入口：CounterManager.BeginPlaceCounterFromExternal → Instantiate 临时体 → BeginExternalDrag
//    - 行为：临时柜台跟鼠标；松手后若仍在 CounterManager 编辑区内 → AddCounterAtPose 正式生成并销毁临时体；否则销毁临时体。
// 2) InZoneExisting（情况2）
//    - 入口：CounterManager.Update 里射线点到柜台且柜台在创建区内 → BeginInZoneDrag
//    - 行为：登记 selectedCounterForEdit；松手后若仍在编辑区内 → UpdateSelectedCounterPose；否则 RemoveAndDestroyCounter。
//
// 【每帧 Update】若正在拖拽：把 transform 投到 placementPlaneY 水平面跟鼠标；左键松开则 EndDrag。
//
// 【EndDrag 顺序】先结束拖拽标记 → 记录对齐前坐标 → XZ 网格对齐 → 用对齐前/后两点做「是否仍在编辑区」检测
// （避免对齐把物体推出触发器后误判）。检测通过与否见 CounterManager.TryGetCounterManagerNearPoint。
//
// 【与 CounterManager 的配合】IsAnyDragging 防止管理器在拖拽中再次射线开新拖拽；NotifyExternalPlaceDragStarted
// 避免外部生成同一帧立刻被射线当成「区内拾取」。
// =============================================================================

/// <summary>
/// 创建模式下柜台拖拽：水平面跟随鼠标、松手网格对齐、区内/区外落地逻辑。
/// </summary>
[RequireComponent(typeof(BaseCounter))]
public class BaseCounterControl : MonoBehaviour
{
    /// <summary>任意柜台是否正在拖拽，供 CounterManager 避免重复拾取。</summary>
    public static bool IsAnyDragging { get; private set; }

    /// <summary>当前拖拽属于「外部临时体」还是「区内已有柜台」。</summary>
    private enum DragKind
    {
        None,
        ExternalNew,
        InZoneExisting
    }

    [SerializeField] private float placementPlaneY;
    [Tooltip("松开左键后，将 X、Z 对齐到该步长的整数倍（默认 1.5）。")]
    [SerializeField] private float positionSnapGrid = 1.5f;
    [SerializeField] private float releaseOverlapRadius = 1f;
    [SerializeField] private LayerMask releaseCheckLayers = ~0;

    [Header("两个事件（对应两种拖拽流程）")]
    [Tooltip("情况1：由外部触发的「范围外生成并拖拽」开始时调用。")]
    [SerializeField] private UnityEvent onCaseExternalDragStarted;
    [Tooltip("情况2：在创建触发器范围内拾取已有柜台并开始拖拽时调用。")]
    [SerializeField] private UnityEvent onCaseInZoneDragStarted;

    private BaseCounter _baseCounter;
    private DragKind _dragKind;
    private BaseCounter _sourcePrefabForSpawn;
    private bool _isDragging;

    /// <summary>BeginPlaceCounterFromExternal 时用来把新生成物体摆在同一高度平面。</summary>
    public float PlacementPlaneY => placementPlaneY;

    private void Awake()
    {
        _baseCounter = GetComponent<BaseCounter>();
    }

    /// <summary>拖拽中：每帧跟鼠标；松开左键当帧调用 EndDrag 做落地与列表/销毁处理。</summary>
    private void Update()
    {
        if (CounterManager.Instance == null || !CounterManager.Instance.IsCreateMode())
            return;

        if (!_isDragging)
            return;

        if (Mouse.current == null)
            return;

        FollowMouseOnPlane();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            EndDrag();
    }

    /// <summary>
    /// 情况1：外部生成后调用，柜台跟随鼠标，放下时决定 Add 或销毁。
    /// </summary>
    public void BeginExternalDrag(BaseCounter sourcePrefab)
    {
        if (CounterManager.Instance == null || !CounterManager.Instance.IsCreateMode())
            return;

        CounterManager.Instance.NotifyExternalPlaceDragStarted();
        _sourcePrefabForSpawn = sourcePrefab;
        _dragKind = DragKind.ExternalNew;
        _isDragging = true;
        IsAnyDragging = true;
        onCaseExternalDragStarted?.Invoke();
    }

    /// <summary>
    /// 情况2：在创建区内拾取已有柜台后由 CounterManager 调用。
    /// </summary>
    public void BeginInZoneDrag()
    {
        if (CounterManager.Instance == null || !CounterManager.Instance.IsCreateMode())
            return;

        CounterManager.Instance.SetSelectedCounterForEdit(_baseCounter);
        _dragKind = DragKind.InZoneExisting;
        _sourcePrefabForSpawn = null;
        _isDragging = true;
        IsAnyDragging = true;
        onCaseInZoneDragStarted?.Invoke();
    }

    /// <summary>从屏幕射线与水平面求交，更新柜台世界坐标（Y 由 placementPlaneY 决定）。</summary>
    private void FollowMouseOnPlane()
    {
        if (Camera.main == null || Mouse.current == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane plane = new Plane(Vector3.up, new Vector3(0f, placementPlaneY, 0f));
        if (!plane.Raycast(ray, out float enter))
            return;

        Vector3 p = ray.GetPoint(enter);
        transform.position = p;
    }

    /// <summary>松手后将 X、Z 吸附到网格步长，Y 不变。</summary>
    private void SnapTransformXZToGrid()
    {
        float step = positionSnapGrid;
        if (step <= 0f)
            return;

        Vector3 p = transform.position;
        p.x = Mathf.Round(p.x / step) * step;
        p.z = Mathf.Round(p.z / step) * step;
        transform.position = p;
    }

    /// <summary>
    /// 松手收尾：网格对齐 → 判断是否在 CounterManager 编辑区内 → 按 DragKind 分支（if）执行生成/改位姿/销毁。
    /// </summary>
    private void EndDrag()
    {
        _isDragging = false;
        IsAnyDragging = false;

        // 对齐网格可能把坐标推出创建区，放下检测同时参考对齐前、后两点。
        Vector3 positionBeforeSnap = transform.position;
        SnapTransformXZToGrid();
        Vector3 checkPoint = transform.position;

        CounterManager mgr = null;
        bool hasManager = false;
        if (CounterManager.Instance != null)
        {
            // 必须排除本柜台：否则 OverlapSphere 会扫到自己身上的 Collider，父级又是 CounterManager，会误判永远在区内。
            if (CounterManager.Instance.TryGetCounterManagerNearPoint(checkPoint, out mgr, _baseCounter))
                hasManager = true;
            else if (CounterManager.Instance.TryGetCounterManagerNearPoint(positionBeforeSnap, out mgr, _baseCounter))
                hasManager = true;
        }

        if (_dragKind == DragKind.ExternalNew)
        {
            if (hasManager && _sourcePrefabForSpawn != null)
                mgr.AddCounterAtPose(_sourcePrefabForSpawn, transform.position, transform.rotation);

            Destroy(gameObject);
        }
        else if (_dragKind == DragKind.InZoneExisting)
        {
            // 下面不是「循环」，而是松手后的三种互斥分支（只会走其中一个）：
            // 1) hasManager：仍在编辑区内 → 把当前位姿写回选中的柜台。
            // 2) 不在区内但 CounterManager 单例还在 → 从列表移除并销毁该柜台（管理器逻辑）。
            // 3) 单例都不存在（极端）→ 本地直接 Destroy，避免空引用。
            if (hasManager)
                mgr.UpdateSelectedCounterPose(transform.position, transform.rotation);
            else if (CounterManager.Instance != null)
                CounterManager.Instance.RemoveAndDestroyCounter(_baseCounter);
            else
                Destroy(gameObject);
        }

        _dragKind = DragKind.None;
        _sourcePrefabForSpawn = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
        Gizmos.DrawSphere(transform.position, releaseOverlapRadius);
    }
}
