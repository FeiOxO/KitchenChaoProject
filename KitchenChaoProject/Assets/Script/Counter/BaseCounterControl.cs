using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 仅在 CounterManager 创建模式下生效：处理柜台拖拽放置（范围外新生成 / 范围内移动已有柜台）。
/// </summary>
[RequireComponent(typeof(BaseCounter))]
public class BaseCounterControl : MonoBehaviour
{
    public static bool IsAnyDragging { get; private set; }

    private enum DragKind
    {
        None,
        ExternalNew,
        InZoneExisting
    }

    [SerializeField] private float placementPlaneY;
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

    public float PlacementPlaneY => placementPlaneY;

    private void Awake()
    {
        _baseCounter = GetComponent<BaseCounter>();
    }

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

    private void EndDrag()
    {
        _isDragging = false;
        IsAnyDragging = false;

        Vector3 checkPoint = transform.position;
        CounterManager mgr = null;
        bool hasManager = CounterManager.Instance != null &&
                          CounterManager.Instance.TryGetCounterManagerNearPoint(checkPoint, out mgr);

        switch (_dragKind)
        {
            case DragKind.ExternalNew:
                if (hasManager && _sourcePrefabForSpawn != null)
                {
                    mgr.AddCounterAtPose(_sourcePrefabForSpawn, transform.position, transform.rotation);
                    Destroy(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }

                break;

            case DragKind.InZoneExisting:
                if (hasManager)
                {
                    mgr.UpdateSelectedCounterPose(transform.position, transform.rotation);
                }
                else if (CounterManager.Instance != null)
                {
                    CounterManager.Instance.RemoveAndDestroyCounter(_baseCounter);
                }
                else
                {
                    Destroy(gameObject);
                }

                break;
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
