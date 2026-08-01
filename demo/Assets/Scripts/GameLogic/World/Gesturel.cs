using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GEngine;
using UnityEngine;

namespace GEngine {

    public enum eGestureState {
        None,
        Down,
        Up,
        HoldDown, // 持续按下
        SwipeStart, // 滑动开始
        SwipeEnd, // 滑动结束
    }

    class Gesture : MonoBehaviour {

        public GameMain GameMain;
        Vector3 m_downPos;
        Vector3 m_upPos;

        float m_fUpTime;
        float m_fDownTime;

        eGestureState m_mouseState;

        // WASD 移动相关
        private float _moveSendInterval = 0.1f; // 发送移动协议的最小间隔（秒）
        private float _lastMoveSendTime = 0f;
        private float _moveStepDistance = 1.5f; // 每次发送的目标点距离当前位置的步长

        public void Update( ) {

            // WASD 实时移动
            UpdateWASDMove( );

            if( Input.GetKeyDown( KeyCode.Mouse0 ) ) {
                if( !UiMgr.GetInstance( ).MouseInGui( ) ) {
                    m_fDownTime = Time.time;
                    m_downPos = Input.mousePosition;
                    m_mouseState = eGestureState.Down;
                } else {
                    //GameLogger.GetInstance().Trace( string.Format( "{0} MouseInGUI", Time.realtimeSinceStartup ) );
                }
            }

            if( Input.GetKeyUp( KeyCode.Mouse0 ) ) {

                // GameLogger.GetInstance().Trace( string.Format( "{0} m_mouseState:{1}", Time.realtimeSinceStartup, m_mouseState ) );

                m_upPos = Input.mousePosition;

                float distance = Vector3.Distance( m_upPos, m_downPos );
                switch( m_mouseState ) {
                    case eGestureState.Down: {
                        if( distance > 50f ) {
                            UpdateSwipeEnd( ); // 滑动判断
                        } else {
                            float intervalTime = 0.2f;
                            if( Time.time - m_fUpTime < intervalTime && distance < 10f ) {
                                UpdateDoubleClick( );
                            } else {
                                UpdateClick( );
                            }
                        }
                    }
                    break;
                }

                m_fUpTime = Time.time;
                m_mouseState = eGestureState.Up;
            }
        }
        private void UpdateWASDMove( ) {
            var mainPlayer = GameMain.MainPlayer;
            if( mainPlayer == null || mainPlayer.GetGameObject( ) == null )
                return;

            // 读取输入
            float h = 0f; // A=-1, D=1
            float v = 0f; // S=-1, W=1
            if( Input.GetKey( KeyCode.W ) ) v += 1f;
            if( Input.GetKey( KeyCode.S ) ) v -= 1f;
            if( Input.GetKey( KeyCode.A ) ) h -= 1f;
            if( Input.GetKey( KeyCode.D ) ) h += 1f;

            // 没有按键，不处理
            if( Mathf.Approximately( h, 0f ) && Mathf.Approximately( v, 0f ) )
                return;

            // 限制发送频率
            if( Time.time - _lastMoveSendTime < _moveSendInterval )
                return;

            _lastMoveSendTime = Time.time;

            // 基于相机水平朝向计算移动方向
            var cameraFollow = Camera.main?.gameObject.GetComponent<CameraFollowBehaviour>( );
            float yaw = cameraFollow != null ? cameraFollow.GetCameraYaw( ) : 0f;

            // 相机朝前方向（水平面上）
            Vector3 forward = new Vector3( Mathf.Sin( yaw * Mathf.Deg2Rad ), 0f, Mathf.Cos( yaw * Mathf.Deg2Rad ) );
            Vector3 right = new Vector3( forward.z, 0f, -forward.x ); // 右 = forward 绕Y轴顺时针90度

            // 合成移动方向并归一化
            Vector3 dir = ( forward * v + right * h ).normalized;

            // 当前位置 + 方向 * 步长 = 目标位置
            Vector3 currentPos = mainPlayer.GetGameObject( ).transform.position;
            Vector3 targetPos = currentPos + dir * _moveStepDistance;

            mainPlayer.MoveTo( targetPos );
        }

        private void UpdateDoubleClick( ) {
            //GameLogger.GetInstance().Trace( string.Format( "#### PlayerGestureControl. UpdateDoubleClick. {0}", Time.realtimeSinceStartup ) );
        }

        private void UpdateClick( ) {

            //GameLogger.GetInstance().Trace( string.Format( "#### PlayerGestureControl. UpdateClick. {0}", Time.realtimeSinceStartup ) );
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
            if( Physics.Raycast( ray, out hit ) ) {
                if( hit.collider is CharacterController ) {
                    GameMain.CurrentWorld.SetSelectObj( hit.collider.gameObject );
                }
            }
        }

        private void UpdateSwipeEnd( ) {
            //GameLogger.GetInstance()
            //    .Trace( string.Format( "#### PlayerGestureControl. UpdateSwipeEnd. {0} m_upPos:{1} m_downPos:{2}",
            //        Time.realtimeSinceStartup, m_upPos, m_downPos ) );

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay( m_downPos );
            if( !Physics.Raycast( ray, out hit ) )
                return;

            if( !( hit.collider is CharacterController ) )
                return;

            ray = Camera.main.ScreenPointToRay( m_upPos );
            if( !Physics.Raycast( ray, out hit ) )
                return;

            GameMain.CurrentWorld.CancelSelectObj( );
        }
    }
}