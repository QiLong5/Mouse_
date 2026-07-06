# AppLovin 打点接入

本目录三件套：`LunaManager.cs / MyTextListener.js / VibrationBridge.js`

## 用法

### 1. 场景挂 LunaManager

创建空 GameObject 挂 `LunaManager` 组件，Inspector 设置 `jumpType`：
- `Default` — 仅展示结算页
- `GuideJump` — 启动引导手指
- `EndForceJump` — 结算 2 秒后强制跳商店
- `Progress75Jump` — 进度 ≥75% 自动跳商店

### 2. 在游戏代码中调用

```csharp
// 进度节点（按游戏流程顺序递增调用，自动去重）
LunaManager.instance.GameUpdated(25);
LunaManager.instance.GameUpdated(50);
LunaManager.instance.GameUpdated(75);
LunaManager.instance.GameUpdated(100);

// 胜利界面弹出时
LunaManager.instance.GameOver();

// CTA 按钮 onClick（立即下载/了解更多/Logo）
btn.onClick.AddListener(() => LunaManager.instance.GotoStore());

// 失败重试
LunaManager.instance.ReLoad();

// 震动
LunaManager.instance.Vibrate(200);
//外部引用
MyTextListener和VibrationBridge要在playwork打包添加外部引用
```
