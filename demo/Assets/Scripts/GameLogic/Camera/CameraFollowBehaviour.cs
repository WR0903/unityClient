
using UnityEngine;

namespace GEngine {
    class CameraFollowBehaviour : MonoBehaviour {

        //相机相对于玩家的位置
        public Vector3 Offset = new Vector3( 0, 4, 8 );

        // player transform
        public Transform Target = null;

        // 相机移动时的旋转速度
        public float Speed = 2;

        // 远近平滑移动值
        public float Smooth = 50f;

        public float sensitivetyZ = 2f;

        // 鼠标控制相机旋转的灵敏度
        public float MouseRotateSpeed = 5f;
        // 相机绕玩家的当前水平角度（度）
        private float _yaw = 0f;
        // 相机俯仰角度（度）
        private float _pitch = 35f;
        // 相机距离玩家的距离
        public float CameraDistance = 8f;

        void Update( ) {
            if( Target == null )
                return;

            // 鼠标按住右键拖拽控制相机绕角色水平旋转（俯仰角固定，不可调）
            if( Input.GetMouseButton( 1 ) ) {
                _yaw += Input.GetAxis( "Mouse X" ) * MouseRotateSpeed;
            }

            // 根据角度计算相机位置和朝向（球坐标）
            // 位置和朝向都直接赋值，不做平滑插值，避免移动/转向时相机晃动
            Quaternion rotation = Quaternion.Euler( _pitch, _yaw, 0 );
            Vector3 desiredPos = Target.position - ( rotation * Vector3.forward * CameraDistance );
            this.transform.position = desiredPos;
            this.transform.rotation = rotation;

            // 鼠标滚轮控制相机远近
            if (((Input.mouseScrollDelta.y < 0 && Camera.main.fieldOfView >= 3)) || (Input.mouseScrollDelta.y > 0 && Camera.main.fieldOfView <= 80))
            {
                Camera.main.fieldOfView += Input.mouseScrollDelta.y * Smooth * Time.deltaTime;
            }
        }

        // 获取相机水平朝向（用于WASD移动方向计算），只取Y轴旋转
        public float GetCameraYaw( ) {
            return _yaw;
        }
    }
}
