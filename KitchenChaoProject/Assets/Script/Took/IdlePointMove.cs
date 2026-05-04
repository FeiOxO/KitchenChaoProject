using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdlePointMove : MonoBehaviour
{
    [Header("椭圆参数")]
    [SerializeField] private float semiMajorAxis = 5f;    // 长半轴
    [SerializeField] private float semiMinorAxis = 3f;    // 短半轴
    
    [Header("运动控制")]
    [SerializeField] private float speed = 1f;           // 运动速度
    [SerializeField] private bool clockwise = true;      // 顺时针运动
    
    [Header("中心点")]
    [SerializeField] private Vector3 centerPoint = Vector3.zero; // 椭圆中心
    
    [Header("起始位置")]
    [SerializeField] [Range(0f, 360f)] private float startAngle = 0f; // 起始角度(度)
    
    private float currentAngle;    // 当前角度(弧度)
    private float direction;       // 运动方向

    void Start()
    {
        // 初始化角度和方向
        currentAngle = startAngle * Mathf.Deg2Rad;
        direction = clockwise ? -1f : 1f;
    }

    void Update()
    {
        // 更新角度
        currentAngle += direction * speed * Time.deltaTime;
        
        // 确保角度在0-2π范围内
        if (currentAngle > 2 * Mathf.PI) currentAngle -= 2 * Mathf.PI;
        if (currentAngle < 0) currentAngle += 2 * Mathf.PI;
        
        // 计算椭圆上的位置
        Vector3 newPosition = CalculateEllipsePosition(currentAngle);
        
        // 更新物体位置
        transform.position = newPosition;
    }

    /// <summary>
    /// 根据角度计算椭圆上的位置
    /// </summary>
    private Vector3 CalculateEllipsePosition(float angle)
    {
        float x = centerPoint.x + semiMajorAxis * Mathf.Cos(angle);
        float z = centerPoint.z + semiMinorAxis * Mathf.Sin(angle);
        
        return new Vector3(x, centerPoint.y, z);
    }

    /// <summary>
    /// 在Scene视图中绘制椭圆轨迹（仅在编辑模式下可见）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        Gizmos.color = Color.green;
        
        // 绘制椭圆轨迹
        int segments = 50;
        Vector3 prevPoint = CalculateEllipsePosition(0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2 * Mathf.PI;
            Vector3 nextPoint = CalculateEllipsePosition(angle);
            
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        
        // 绘制中心点
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centerPoint, 0.2f);
        #endif
    }

    /// <summary>
    /// 设置新的椭圆参数
    /// </summary>
    public void SetEllipseParameters(float newSemiMajor, float newSemiMinor, Vector3 newCenter)
    {
        semiMajorAxis = newSemiMajor;
        semiMinorAxis = newSemiMinor;
        centerPoint = newCenter;
    }

    /// <summary>
    /// 设置运动速度
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    /// <summary>
    /// 反转运动方向
    /// </summary>
    public void ReverseDirection()
    {
        direction *= -1f;
    }

    /// <summary>
    /// 获取当前在椭圆上的角度（弧度）
    /// </summary>
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    /// <summary>
    /// 跳转到指定角度位置
    /// </summary>
    public void JumpToAngle(float targetAngleDegrees)
    {
        currentAngle = targetAngleDegrees * Mathf.Deg2Rad;
    }
}
