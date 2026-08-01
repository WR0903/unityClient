using UnityEngine;

namespace GEngine
{
    class RoleStateJump : RoleState
    {
        public override RoleStateType GetState()
        {
            return RoleStateType.Jump;
        }

        public override RoleStateType Update()
        {
            return GetState();
        }

        public override void EnterState(RoleStateType lastStateType)
        {
            if (_parentObj.GetGameObject() == null)
                return;

            var animation = _parentObj.GetGameObject().GetComponent<Animation>();
            // 如果模型没有 jump 动画，Play 会无效果，不影响跳跃逻辑
            animation.Play("jump");
        }

        public override void LeaveState()
        {

        }
    }
}
