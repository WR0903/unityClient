
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

        private RoleAppear _role;
        public RoleAppear Role => _role;
        public void AttachRole(RoleAppear role)
        {
            _role = role;
        }

        void Awake()
        {
            var rigidbody = GetComponent<Rigidbody>();
            //_collider = GetComponent<CapsuleCollider>( );

            rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            //_collider.center = new Vector3( 0, 1, 0 );
            //_collider.radius = 1;

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

            // 消费 CornerPoints：服务器同步过来的路径点，驱动 NavMeshAgent 移动到最终目的地
            if (CornerPoints.Count > 0)
            {
                var lastPoint = CornerPoints[CornerPoints.Count - 1];
                var targetPosition = new Vector3(lastPoint.X, lastPoint.Y, lastPoint.Z);
                navMeshAgent.SetDestination(targetPosition);
                CornerPoints.Clear();

                // 标记正在移动，切换到移动状态
                _isMoving = true;
                _role?.ChangeState(RoleStateType.Move);
            }

            // 正在移动中，检查是否到达目的地
            if (_isMoving)
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
