# 多人在线游戏 Unity 客户端

## 开发环境

| 项 | 版本 |
|---|---|
| Unity | 2022.3 LTS |
| .NET | .NET Standard 2.1 |
| C# | 9.0 |
| 构建 | IL2CPP / Mono |
| 版本控制 | Git |

## 工程结构

```
unityClient/
├── art/        美术资源工程（模型/贴图/场景/AssetBundle 打包）
├── demo/       主游戏工程（C# 逻辑 + 运行时）
└── editor/     配套 WPF 桌面工具（编辑 CSV 配置表，基于 .NET Framework 4.6.1）
```

### art/ — 美术资源工程

- 只放美术资源、场景、Prefab
- 用于构建 AssetBundle（`.win` / `.android`）
- 构建产物输出到 `art/Assets/StreamingAssets/assetbundle.*/`

### demo/ — 主游戏工程

所有游戏逻辑都在这里。核心代码目录：

```
demo/Assets/Scripts/
├── ReadyToLoad.cs        游戏入口，所有系统的启动器
├── Global.cs             全局配置（服务器IP/资源路径）
│
├── Common/               基础工具（单例/协程/日志/状态机）
├── EventDispatcher/      事件系统 + 网络消息分发
├── Network/              网络层（Socket + Protobuf）
├── Loader/               资源加载（AssetBundle + 异步加载器）
├── Resource/             配置表（CSV 解析 + 数据管理）
├── UI/                   UI 框架（UIBase/UIFactory + 各界面）
├── GameLogic/            游戏逻辑（Player/World/Camera/GameMain）
└── System/               组件系统（移动/更新）
```

### editor/ — 配置表编辑器

- 独立的 WPF 桌面应用程序（非 Unity 工程）
- 基于 .NET Framework 4.6.1 + MahApps.Metro
- 用于在不打开 Unity 的情况下编辑 CSV 配置表
- 通过相对路径链接 `demo/Assets/Scripts/` 下的共享脚本

## 代码架构

### 启动流程

```
ReadyToLoad.Awake()        游戏入口，依次创建各系统
  ├── EventSystem          UI 点击事件
  ├── EventDispatcher      事件广播
  ├── AssetBundleMgr       资源加载
  ├── CoroutineEngine      协程调度
  ├── NetworkMgr           网络连接
  ├── GameMain             游戏主循环
  └── UiMgr                UI 管理

ReadyToLoad.Update()       状态机驱动初始化流程
  Init1 (AssetBundle) → Init2 (UI) → LoadScene → Ok
```

### 游戏运行流程

```
玩家启动
  → 加载 AssetBundle 资源
  → 显示登录界面 UiLogin
  → 玩家输入账号密码
  → HTTP 请求获取游戏服务器 IP
  → Socket 连接游戏服务器
  → 发送 AccountCheck 协议
  → 服务器返回 EnterWorld
  → 加载地图场景
  → 创建 World 对象

玩家移动
  → 鼠标点击地面
  → Gesturel 检测点击
  → Player.MoveTo() NavMesh 寻路
  → 发送 C2SMove 给服务器
  → 服务器广播 S2CMove
  → World.SyncMove() 填充 CornerPoints
  → MoveComponent 驱动 NavMeshAgent 移动
```

### 模块说明

| 模块 | 目录 | 核心文件 | 职责 |
|---|---|---|---|
| 基础工具 | `Common/` | `SingletonBehaviour.cs`、`StateTemplate.cs` | 单例基类、协程引擎、日志、状态机 |
| 事件系统 | `EventDispatcher/` | `EventDispatcher.cs`、`MessagePackDispatcher.cs` | 游戏内事件广播、网络消息分发 |
| 网络层 | `Network/` | `NetworkMgr.cs`、`Packet.cs` | Socket 收发、Protobuf 序列化 |
| 资源加载 | `Loader/` | `AssetBundleMgr.cs`、`AsyncLoader.cs` | AssetBundle 加载/缓存/依赖管理 |
| 配置表 | `Resource/` | `CvsAnalysis.cs`、`ResourceAll.cs` | CSV 解析、数据管理 |
| UI 框架 | `UI/` | `UiBase.cs`、`UiFactory.cs`、`UiMgr.cs` | 界面创建/显示/隐藏、登录界面 |
| 游戏逻辑 | `GameLogic/` | `GameMain.cs`、`World.cs`、`Player.cs` | 游戏主循环、世界管理、玩家角色 |
| 组件系统 | `System/` | `MoveComponent.cs`、`RoleUpdateComponent.cs` | 角色移动、状态更新 |

## 快速开始

### 1. 克隆仓库

```bash
git clone https://github.com/WR0903/unityClient.git
```

### 2. 用 Unity 2022.3 LTS 打开工程

1. 安装 [Unity Hub](https://unity.com/download)
2. 通过 Unity Hub 安装 **Unity 2022.3 LTS**
3. 用 Unity Hub 添加并打开 `demo/` 工程

### 3. 配置服务器

在 `demo/` 工程的 `ReadyToLoad` 组件（挂在场景 GameObject 上）的 Inspector 中配置：
- `ResPath`：AssetBundle 资源路径
- `ReferencePath`：CSV 配置表路径
- `ServerIp`：登录服务器 IP
- `ServerPort`：登录服务器端口

### 4. 运行

点击 Unity 的 Play 按钮即可启动游戏。

## 开发规范

- 所有游戏代码在 `GEngine` 命名空间下
- 单例类继承 `SingletonBehaviour<T>`（MonoBehaviour）或 `SingletonObject<T>`（纯 C#）
- UI 界面继承 `UiBase`，通过 `UiFactory` 创建
- 网络消息使用 Protobuf，消息 ID 定义在 `ProtoId.cs`
- 资源通过 `AssetBundleMgr.AsyncLoad()` 异步加载

## 技术栈

- **Unity 2022.3 LTS** 游戏引擎
- **C# 9.0** 编程语言
- **Protobuf** 网络协议序列化
- **LitJson** JSON 解析
- **NavMesh** 寻路
- **AssetBundle** 资源管理
- **WPF + MahApps.Metro** 配置表编辑器
