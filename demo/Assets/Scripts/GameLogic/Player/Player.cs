
using UnityEngine;
using UnityEngine.AI;

namespace GEngine
{
    class Player
    {
        protected ulong _sn;
        public ulong Sn => _sn;

        protected string _name;
        protected Proto.Gender _gender;

        protected GameObject _3dObj = null;

        public void SetGameObject(GameObject obj)
        {
            _3dObj = obj;
        }

        public GameObject GetGameObject()
        {
            return _3dObj;
        }

        public void Parse(Proto.Player proto)
        {
            _sn = proto.Sn;
            _name = proto.Name;
            _gender = proto.Base.Gender;
        }

        // 找到navmesh上最近的点
        public static Vector3 NavPosition(Vector3 srcPosition)
        {
            Vector3 dstPosition = srcPosition;
            int layer = 1 << NavMesh.GetAreaFromName("Walkable");
            if (NavMesh.SamplePosition(srcPosition, out var meshHit, 100, layer))
            {
                dstPosition = meshHit.position;
            }

            return dstPosition;
        }

        public void MoveTo(Vector3 hitPosition)
        {
            Vector3 destPosition = NavPosition(hitPosition);
            NavMeshPath navMeshPath = new NavMeshPath();

            var navMeshAgent = _3dObj.gameObject.GetComponent<NavMeshAgent>();
            navMeshAgent.CalculatePath(destPosition, navMeshPath);

            // 能移动到该点
            if (navMeshPath.status != NavMeshPathStatus.PathPartial)
            {
                // 不在客户端本地立即移动，只把目标路径发送给服务器，
                // 等服务器同步 S2CMove 回来后，由 MoveComponent 驱动角色移动
                Proto.Move proto = new Proto.Move();
                proto.PlayerSn = _sn;
                foreach (Vector3 one in navMeshPath.corners)
                {
                    proto.Position.Add(new Proto.Vector3() { X = one.x, Y = one.y, Z = one.z });
                }

                UnityEngine.Debug.Log($"move to. position:{destPosition} sn:{_sn}");
                NetworkMgr.GetInstance().SendPacket(Proto.MsgId.C2SMove, proto);
            }
        }
    }
}
