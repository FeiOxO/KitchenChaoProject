using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class GuestAnimator : MonoBehaviour
{
    [SerializeField] private NavMeshAgent nav;
    [SerializeField] private Animator anim; 
    [SerializeField] private float moveSpeed = 1;
    [SerializeField] private float idelSpeed = 2;
    [SerializeField] private float hungrySpeed = 3;
    [SerializeField] private float turnSpeed = 0;
    private float timer = 0;
    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        anim.SetFloat("移动速度",moveSpeed);
        anim.SetFloat("转弯速度",turnSpeed);
        anim.SetFloat("玩家姿态",1);
    }
    public void AnimatorUpdate()
    {
        anim.SetFloat("移动速度",moveSpeed);
        nav.speed = moveSpeed;
    }

    public void IdleState(Vector3 targetPosition)
    {
        moveSpeed = idelSpeed;
        nav.destination = targetPosition;
    }

    public void HungryState(Vector3 target)
    {
        moveSpeed = hungrySpeed;
        nav.destination = target;
    }

    public void WaitState()
    {
        moveSpeed = 0;
    }

    public bool ResponseState()
    {
        moveSpeed = 0;
        if(timer > 3f)
        {
            timer = 0;
            return true;
        }

        timer += Time.deltaTime;
        return false;
        
    }
}
