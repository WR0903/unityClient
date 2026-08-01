
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GEngine
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NavMeshAgent))]
    class MoveComponent : MonoBehaviour
    {
        public List<Proto.Vector3> CornerPoints = new List<Proto.Vector3>();

        private Vector3 _nextPosition = Vector3.zero;
        private bool _isMoving = false;

        // 跳跃相关
        private bool _isJumping = false;
        private float _jumpForce = 6f;
        private float _jumpStartTime = 0f;
        private RoleAppear _role;
        public RoleAppear Role => _role;
        public void AttachRole(RoleAppear role)
        {
            _role = role;
        }

        void Awake()
        {
            var rigidbody = GetComponent<Rigidbody>();

            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;

            Vector3 extraGravityForce = Physics.gravity;
            rigidbody.AddForce(extraGravityForce);

            var navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.speed = 2f;
            navMeshAgent.acceleration = 360;
            navMeshAgent.angularSpeed = 360;
            navMeshAgent.stoppingDistance = 0.1f;

            CoroutineEngine.GetInstance().Execute(TimerChange());
        }

        private IEnumerator TimerChange()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.0f);
                GameLogger.GetInstance().Debug($"player position:{gameObject.transform.position}");
            }
        }

        void Update()
        {
            var navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            var rigidbody = GetComponent<Rigidbody>();

            // 空格键跳跃（只对主玩家响应输入）
            if (_role != null && _role.Sn == GameMain.GetInstance().MainPlayer?.Sn)
            {
                // if (Input.GetKeyDown(KeyCode.Space) && !_isJumping)
                // {
                //     _isJumping = true;
                //     _jumpStartTime = Time.time;
                //     // 跳跃时禁用 NavMeshAgent，让 Rigidbody 物理生效
                //     navMeshAgent.enabled = false;
                //     rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
                //     _role?.ChangeState(RoleStateType.Jump);
                // }
            }

            // 跳跃中检测落地
            if (_isJumping)
            {
                // 起跳后至少等 0.2 秒再检测落地，避免起跳瞬间误判
                if (Time.time - _jumpStartTime > 0.2f)
                {
                    // 检测是否回到地面（垂直速度接近0）
                    if (Mathf.Abs(rigidbody.velocity.y) < 0.1f)
                    {
                        _isJumping = false;
                        // 落地后重新启用 NavMeshAgent
                        navMeshAgent.enabled = true;
                        // 落地后根据是否在移动切换状态
                        if (_isMoving)
                            _role?.ChangeState(RoleStateType.Move);
                        else
                            _role?.ChangeState(RoleStateType.Stand);
                    }
                }
            }

            // 消费 CornerPoints：服务器同步过来的路径点，驱动 NavMeshAgent 移动到最终目的地
            if (CornerPoints.Count > 0 && !_isJumping)
            {
                var lastPoint = CornerPoints[CornerPoints.Count - 1];
                var targetPosition = new Vector3(lastPoint.X, lastPoint.Y, lastPoint.Z);
                navMeshAgent.SetDestination(targetPosition);
                CornerPoints.Clear();

                // 标记正在移动，切换到移动状态
                _isMoving = true;
                _role?.ChangeState(RoleStateType.Move);
            }

            // 正在移动中，检查是否到达目的地（跳跃中跳过，NavMeshAgent被禁用）
            if (_isMoving && !_isJumping)
            {
                // 路径还在计算中或正在移动，不切回站立
                if (navMeshAgent.pathPending)
                    return;

                if (navMeshAgent.hasPath)
                {
                    // 朝向下一个路径点
                    Vector3 comparePos;
                    if (navMeshAgent.path.corners.Length > 2)
                    {
                        comparePos = navMeshAgent.path.corners[1];
                    }
                    else
                    {
                        comparePos = navMeshAgent.destination;
                    }

                    if (_nextPosition != comparePos)
                    {
                        _nextPosition = comparePos;
                        gameObject.transform.LookAt(comparePos);
                    }

                    // 检查是否到达目的地（剩余距离小于停止距离）
                    if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                    {
                        _isMoving = false;
                        _role?.ChangeState(RoleStateType.Stand);
                    }
                }
            }
        }
    }
}
