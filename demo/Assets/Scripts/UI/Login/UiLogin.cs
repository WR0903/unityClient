using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using LitJson;
using UnityEngine.UI;

namespace GEngine
{
    enum UiLoginState
    {
        None,
        WaitingForHttp,
        Connect,
        Normal,
    }

    struct HttpJson
    {
        public string ip;
        public int port;
        public int returncode;
    }

    class UiLogin : UiBase
    {
        private InputField _inputAccount;
        private InputField _inputPassword;
        private Button _btnLogin;

        private string _serverIp;
        private int _serverPort;

        private UiLoginState _loginState = UiLoginState.None;

        public UiLogin() : base(UiType.Login, 0) { }

        protected override void OnAwake()
        {
        }

        protected override bool IsLoaded()
        {
            return true;
        }

        protected override void OnInit()
        {
            _inputAccount = GetUi<InputField>(@"Input Field (Username)");
            _inputPassword = GetUi<InputField>(@"Input Field (Password)");

            _btnLogin = GetUi<Button>(@"Button (Login)");
            _btnLogin.onClick.AddListener(OnClickLogin);

            // 事件
            EventDispatcher eDispatcher = EventDispatcher.GetInstance();
            eDispatcher.RegisterEvent<AppType>(eEventType.Connected, EventNetworkConnected);
            eDispatcher.RegisterEvent<AppType>(eEventType.Disconnect, EventNetworkDisconnect);

            // 网络事件
            MessagePackDispatcher msgDispatcher = MessagePackDispatcher.GetInstance();
            msgDispatcher.RegisterFollowPacket((int)Proto.MsgId.C2LAccountCheckRs, MsgAccoutCheckRs);
        }

        protected override void OnDestroy()
        {
            // 事件
            EventDispatcher eDispatcher = EventDispatcher.GetInstance();
            eDispatcher.RemoveEvent<AppType>(eEventType.Connected, EventNetworkConnected);
            eDispatcher.RemoveEvent<AppType>(eEventType.Disconnect, EventNetworkDisconnect);

            // 网络事件
            MessagePackDispatcher msgDispatcher = MessagePackDispatcher.GetInstance();
            msgDispatcher.RemoveFollowPacket((int)Proto.MsgId.C2LAccountCheckRs, MsgAccoutCheckRs);
        }

        protected override void OnUpdate()
        {
            if (_loginState == UiLoginState.Connect)
            {
                NetworkMgr.GetInstance().Connect(_serverIp, _serverPort, AppType.Login);
                Hide();
                UiMgr.GetInstance().OpenModalBox0("网络连接", $"正在连接服务器...");
                _loginState = UiLoginState.Normal;
            }
        }

        private void OnClickLogin()
        {
            _loginState = UiLoginState.WaitingForHttp;
            CoroutineEngine.GetInstance().Execute(GetServer());
        }

        private IEnumerator GetServer()
        {
            var globalObj = Global.GetInstance();
            string url = $"http://{globalObj.GetServerIp()}:{globalObj.GetServerPort()}/login";
            
            // Unity 2022 默认禁止 HTTP 明文请求，使用 HttpWebRequest 绕过限制
            string resultText = null;
            string errorMsg = null;
            
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    var httpReq = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                    httpReq.Method = "GET";
                    httpReq.ContentType = "application/json;charset=utf-8";
                    httpReq.Timeout = 10000;
                    using (var response = (System.Net.HttpWebResponse)httpReq.GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8))
                    {
                        resultText = reader.ReadToEnd();
                    }
                }
                catch (System.Exception ex)
                {
                    errorMsg = ex.Message;
                }
            });
            thread.Start();
            
            while (thread.IsAlive)
                yield return null;
            
            if (errorMsg != null)
            {
                UnityEngine.Debug.Log(errorMsg);
                UiMgr.GetInstance().OpenModalBox1("登录消息", $"{errorMsg}", Show);
            }
            else
            {
                UnityEngine.Debug.Log(resultText);
                string result = resultText;
                var jsonData = JsonMapper.ToObject<HttpJson>(result);
                if (jsonData.returncode == 0)
                {
                    _serverIp = jsonData.ip;
                    _serverPort = jsonData.port;
                    _loginState = UiLoginState.Connect;
                }
                else
                {
                    UnityEngine.Debug.Log(errorMsg);
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"获取服务器失败：{jsonData.returncode}", Show);
                }
            }
        }

        public string Md5(byte[] data)
        {
            byte[] bin;
            using (MD5CryptoServiceProvider md5Crypto = new MD5CryptoServiceProvider())
            {
                bin = md5Crypto.ComputeHash(data);
            }
            return BitConverter.ToString(bin).Replace("-", "").ToLower();
        }

        private void EventNetworkConnected(AppType appType)
        {
            // 发送登录协议
            Proto.AccountCheck protoCheck = new Proto.AccountCheck
            {
                Account = _inputAccount.text.Trim(),
                Password = Md5(System.Text.Encoding.Default.GetBytes(_inputPassword.text.Trim()))
            };

            NetworkMgr.GetInstance().SendPacket(Proto.MsgId.C2LAccountCheck, protoCheck);

            Hide();
            UiMgr.GetInstance().OpenModalBox0("登录消息", "正在验证账号...");
        }

        private void EventNetworkDisconnect(AppType appType)
        {
            // 不相等的情况，有可能是linkGame之后，收到 login 的 disconnect，此种情况，不需要处理
            UiMgr.GetInstance().CloseModalBox0();
            UiMgr.GetInstance().OpenModalBox1("登录消息", $"网络断开，请重新登录...", Show);
        }

        private void MsgAccoutCheckRs(Google.Protobuf.IMessage msg)
        {
            Proto.AccountCheckRs protoRs = msg as Proto.AccountCheckRs;
            if (protoRs == null)
                return;

            Proto.AccountCheckReturnCode returnCode = protoRs.ReturnCode;

            UnityEngine.Debug.LogFormat("Recv account check rs. return code:{0}", returnCode);

            switch (returnCode)
            {
                case Proto.AccountCheckReturnCode.ArcOnline:
                    UiMgr.GetInstance().CloseModalBox0();
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"[No:{returnCode}]该账号在线...", Show);
                    break;
                case Proto.AccountCheckReturnCode.ArcLogging:
                    UiMgr.GetInstance().CloseModalBox0();
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"[No:{returnCode}]该账号正在登录...", Show);
                    break;
                case Proto.AccountCheckReturnCode.ArcNotFoundAccount:
                    UiMgr.GetInstance().CloseModalBox0();
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"[No:{returnCode}]没有找到账号...", Show);
                    break;
                case Proto.AccountCheckReturnCode.ArcPasswordWrong:
                    UiMgr.GetInstance().CloseModalBox0();
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"[No:{returnCode}]密码错误...", Show);
                    break;
                case Proto.AccountCheckReturnCode.ArcOk:
                    UiMgr.GetInstance().OpenModalBox0("登录消息", "登录成功，正在分配游戏服务器...");
                    break;
                default:
                    UiMgr.GetInstance().CloseModalBox0();
                    UiMgr.GetInstance().OpenModalBox1("登录消息", $"[No:{returnCode}]未知错误...", Show);
                    break;
            }
        }

    }
}
