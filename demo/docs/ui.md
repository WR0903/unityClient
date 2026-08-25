# UI 模块文档

## 目录结构

```
Scripts/UI/
├── Base/                          # UI 基础框架
│   ├── UiBase.cs                  # UI 抽象基类
│   ├── UiFactory.cs               # UI 工厂（创建 & AB路径映射）
│   ├── UiSubComponent.cs          # UI 子组件基类
│   ├── UiType.cs                  # UI 类型枚举
│   └── UiUtil.cs                  # UI 工具类（Transform查找）
├── Load/                          # 加载界面
│   └── UiLoadBar.cs               # 加载进度条
├── Login/                         # 登录界面
│   └── UiLogin.cs                 # 登录流程（HTTP获取服务器 → 连接 → 验证账号）
├── Modal/                         # 模态弹窗
│   ├── ModalBox0.cs               # 纯提示弹窗（无按钮，事件触发/关闭）
│   └── ModalBox1.cs               # 确认弹窗（一个确认按钮）
├── Roles/                         # 角色相关界面
│   ├── UiRoles.cs                 # 角色路由（根据角色数量决定打开创建/选择）
│   ├── UiRoleBase.cs              # 角色界面基类（3D模型加载、相机渲染）
│   ├── UiRoleCreate.cs            # 角色创建界面
│   ├── UiRoleSelect.cs            # 角色选择界面（含进入游戏流程）
│   └── UiRoleSelectCharacter.cs   # 角色列表行子组件
├── UiComplexType.cs               # UI复合类型（UiType + Sn 唯一标识）
├── UiMgr.cs                       # UI 管理器（打开/关闭/生命周期/AB加载回调）
└── UiMgrUpdate.cs                 # UI 数据更新机制（数据驱动UI刷新）
```

---

## 核心架构

### 类继承关系

```
UiBase (抽象基类)
├── UiLoadBar           # 加载进度条
├── UiLogin             # 登录界面
├── UiRoles             # 角色路由（无GameObject）
├── UiRoleBase (抽象)   # 角色界面基类
│   ├── UiRoleCreate    # 角色创建
│   └── UiRoleSelect    # 角色选择
├── ModalBox0           # 纯提示弹窗
└── ModalBox1           # 确认弹窗

UiSubComponent (抽象基类)
└── UiRoleSelectCharacter  # 角色列表行
```

### 管理类

- **UiMgr** (SingletonBehaviour)：UI 管理器，负责 UI 的打开/关闭/生命周期管理
- **UiFactory** (SingletonObject)：UI 工厂，负责 UI 实例创建和 AB 路径映射

---

## UI 生命周期


### 状态机

```
None → Loading → LoadedGameObj → Ok
```

| 状态 | 说明 |
|------|------|
| `None` | 初始状态，调用 `OnAwake()`，判断是否需要加载资源 |
| `Loading` | 等待 AssetBundle 异步加载完成 |
| `LoadedGameObj` | GameObject 已就绪，等待 `IsLoaded()` 返回 true 后执行 `OnInit()` |
| `Ok` | 初始化完成，每帧执行 `OnUpdate()`，界面可见 |

### 生命周期方法

| 方法 | 调用时机 | 说明 |
|------|----------|------|
| `OnAwake()` | 状态从 None 转出时 | 用于发起资源加载请求 |
| `IsLoaded()` | LoadedGameObj 状态每帧检查 | 返回 true 表示所有前置资源就绪 |
| `OnInit()` | IsLoaded 首次返回 true 时 | 初始化 UI 控件引用、注册事件 |
| `OnUpdate()` | Ok 状态每帧调用 | 处理数据刷新逻辑 |
| `OnDestroy()` | UI 被关闭销毁时 | 清理事件注册、销毁对象 |

---

## 数据驱动机制

### 数据流

```
游戏逻辑层 → IToUi<T>.UpdataUiData() → UiMgr._updateDatas 字典
                                              ↓
具体UI.OnUpdate() → UiMgr.GetUpdateData<T>() → 版本号比对 → 执行刷新
```

### 关键类型

- **UiUpdateDataType**：数据类型枚举（当前仅 `AccountInfo`）
- **ToUiData**：数据基类，包含 `Version` 版本号字段
- **IToUi\<T\>**：数据推送抽象类，调用 `UpdataUiData()` 时自增版本号并存入 UiMgr

### 版本号比对

每个 UI 持有 `_lastVersion`，在 `OnUpdate()` 中与数据的 `Version` 比较，不同则执行刷新逻辑，避免每帧重复渲染。

---

## 各文件详细说明

### Base 层

| 文件 | 职责 |
|------|------|
| **UiBase.cs** | UI 抽象基类，定义状态机生命周期，提供 `Show/Hide/Destroy/CloseThisUi/GetUi<T>()` 等通用方法 |
| **UiFactory.cs** | 单例工厂，维护 `UiType → AssetBundle路径` 映射表，`Create()` 根据类型实例化具体 UI |
| **UiSubComponent.cs** | UI 子组件抽象基类，持有父 UI 引用和 GameObject，提供 `GetUi<T>()` 和 `Update(ToUiData)` |
| **UiType.cs** | UI 类型枚举：`Login=1, Roles=2, RoleCreate=3, RoleSelect=4, LoadingBar=100, ModalBox0/1/2` |
| **UiUtil.cs** | 静态工具类，按路径（支持 `/` 分隔）递归查找子 Transform |

### 管理层

| 文件 | 职责 |
|------|------|
| **UiMgr.cs** | UI 管理器主逻辑（partial class）：维护 UI 实例字典、AB 异步加载回调、Canvas 管理、全局事件处理、ModalBox 快捷方法 |
| **UiMgrUpdate.cs** | UI 管理器数据更新部分（partial class）：定义数据类型枚举、数据基类、数据推送抽象类、数据存取接口 |
| **UiComplexType.cs** | 复合键结构体 `(UiType, Sn)`，用于区分同类型不同实例的 UI |

### 业务界面层

| 文件 | 职责 |
|------|------|
| **UiLoadBar.cs** | 加载进度条，通过修改 `Image.rectTransform.sizeDelta` 实现填充效果 |
| **UiLogin.cs** | 登录界面完整流程：HTTP GET 获取服务器地址 → 连接 Login 服务器 → 发送账号+MD5密码 → 处理返回码 |
| **ModalBox0.cs** | 纯提示模态弹窗，显示标题+内容，无交互按钮，由外部事件关闭 |
| **ModalBox1.cs** | 确认模态弹窗，显示标题+内容+确认按钮，点击触发 `CloseAction` 回调 |
| **UiRoles.cs** | 角色路由界面（无 GameObject），监听 AccountInfo 数据：角色数为 0 打开创建界面，否则打开选择界面 |
| **UiRoleBase.cs** | 角色界面基类：异步加载男/女 3D 模型、创建独立 Camera + RenderTexture 用于角色预览 |
| **UiRoleCreate.cs** | 角色创建界面：性别 Toggle 切换模型、输入角色名、发送 `C2LCreatePlayer` 协议 |
| **UiRoleSelect.cs** | 角色选择界面：动态创建角色列表、Toggle 切换模型、选择角色进入游戏（Token 机制切换服务器） |
| **UiRoleSelectCharacter.cs** | 角色列表行子组件，显示角色名，暴露 `PlayerSn`、`Gender`、`IsOn()` |

---

## UiMgr 核心接口

```csharp
// 打开 UI
UiBase OpenUi(UiType uiType)
UiBase OpenUi(UiType uiType, ulong sn)

// 关闭 UI
void CloseUi(UiType uiType)
void CloseUi(UiType uiType, ulong sn)
void CloseAll()

// 模态弹窗快捷方法
ModalBox0 OpenModalBox0(string title, string tips)
void OpenModalBox1(string title, string tips, Action closeAction)
void CloseModalBox0()

// 数据更新
void UpdateUiData(UiUpdateDataType eType, ToUiData obj)
T GetUpdateData<T>(UiUpdateDataType eType)
```

---

## 关键设计特点

1. **状态机驱动**：每个 UI 通过 `UiState` 状态机管理生命周期，确保资源加载完成后才初始化和显示
2. **工厂模式**：`UiFactory` 统一管理 UI 类型到 AB 路径的映射和实例创建
3. **复合键标识**：`UiComplexType(UiType, Sn)` 支持同类型多实例（如多个玩家面板）
4. **数据驱动更新**：通过版本号比对实现按需刷新，避免每帧重复渲染
5. **partial class 分离**：`UiMgr` 拆分为主逻辑和数据更新两个文件，职责清晰
6. **子组件模式**：`UiSubComponent` 支持列表项等可复用的 UI 子元素
7. **异步资源加载**：所有 UI 的 Prefab 和模型通过 AssetBundle 异步加载，不阻塞主线程

---

## 登录流程

```
1. 打开 UiLogin
2. 用户输入账号密码，点击登录
3. HTTP GET → 获取 Login 服务器 IP/Port
4. TCP 连接 Login 服务器
5. 发送 C2LAccountCheck（账号 + MD5密码）
6. 收到 AccountCheckRs → 验证成功
7. 打开 UiRoles → 监听 AccountInfo 数据
8. 角色数为 0 → UiRoleCreate（创建角色）
   角色数 > 0 → UiRoleSelect（选择角色）
9. 选择角色 → 发送 C2LSelectPlayer
10. 收到 GameToken → 断开 Login → 连接 Game 服务器
11. 发送 C2GLoginByToken → 进入游戏
```