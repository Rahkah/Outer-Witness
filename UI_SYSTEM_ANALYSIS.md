# UI 系统深度分析

## 一、UI 系统总览

游戏 UI 由 6 个独立模块组成，分布在两个命名空间下：

```
UI 系统
├── UI.UiActions          交互提示（准星 + 操作文字）
├── UI.SpaceSuitStatus    太空服状态条（生命/氧气/燃料）
├── UI.DeathScreen        死亡黑屏淡入
├── UI.YouAreTakingDamageText  受伤提示文字
├── UI.Debug.CornerDebug  左下角调试信息
└── PlayerTools.SpaceNavigator  太空导航仪（独立 HUD）
    + PlayerTools.SpaceShipParts.SpaceShipAccelerationShowcase（飞船加速度指示，3D 材质 UI）
```

所有 UI 均通过 C# 事件订阅驱动，不在 Update 中轮询状态，而是由游戏逻辑主动推送。

---

## 二、各模块详细分析

### 1. UiActions — 交互提示系统

**文件**：`Assets/Scripts/UI/UiActions.cs`  
**数据类**：`Assets/Scripts/UI/UiActionParts/UiAction.cs`

**职责**：在玩家视野中心显示当前可用的交互操作提示，并监听按键执行回调。

**Unity 组件引用**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `player` | Player | 玩家引用 |
| `centerActionsTextMesh` | TextMeshProUGUI | 屏幕中央操作提示文字 |
| `topRightActionsTextMesh` | TextMeshProUGUI | 右上角操作提示文字（驾驶/烹饪状态） |
| `crosshair` | GameObject | 准星，无交互时显示 |

**工作流程**：

```
每帧 Update()
  ├── 清空 availableActions 列表
  ├── AddRaycastActions()
  │     └── 从玩家相机向前发射射线（距离 2 单位）
  │           └── 命中对象 → 匹配类型 → 添加对应 UiAction
  │               ├── SpaceShipHatch   → [E] open the hatch
  │               ├── SpaceShipSeat    → [E] buckle up
  │               ├── SpaceShipHealthAndFuelStation → [E] Use Medkit / Refuel Jetpack
  │               └── Campfire         → [E] Roast Marshmallow
  ├── AddStateActions()
  │     ├── IsBuckledUp()  → [Q] Unbuckle / [F] Toggle flashlight（右上角）
  │     └── IsCooking()    → [F] Extend / [Q] Put away / [E] Eat|Extinguish|Replace / [R] Toss（右上角）
  ├── 分类渲染：center / topRight
  ├── 准星：centerActions.Count == 0 时显示
  └── FireNeededActions() → 检测 GetKeyDown → 执行 Callback
```

**UiAction 数据结构**：

```csharp
readonly struct UiAction {
    KeyCode KeyCode;           // 触发按键
    string Description;        // 显示文字
    Action Callback;           // 执行回调（可为 null）
    bool TopRightInsteadOfCenter; // 显示位置
}
```

**文字格式**：`[KeyCode] - Description\n`（多条叠加）

---

### 2. SpaceSuitStatus — 太空服状态 HUD

**文件**：`Assets/Scripts/UI/SpaceSuitStatus.cs`

**职责**：显示玩家生命值、氧气、燃料、超级燃料的实时状态。

**Unity 组件引用**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `healthIndicatorImage` | Image | 生命值指示图（颜色变化） |
| `oxygenBarFillerImage` | Image | 氧气条填充图 |
| `fuelBarFillerImage` | Image | 燃料条填充图 |
| `superFuelBarFillerImage` | Image | 超级燃料条填充图 |
| `player` | Player | 玩家引用 |

**事件订阅**（Start 订阅，OnDestroy 取消）：

```
player.Damageable.OnHealthPointsChange       → UpdateHealthIndicator(float %)
player.spaceSuit.OnOxygenTankFillPercentageChanged  → UpdateOxygenBar(float %)
player.spaceSuit.OnFuelTankFillPercentageChanged    → UpdateFuelBar(float %)
player.spaceSuit.OnSuperFuelTankFillPercentageChanged → UpdateSuperFuelBar(float %)
```

**渲染逻辑**：

- 生命值：修改 `Image.color`，红色分量固定为 1，绿蓝分量 = `0.01 * percentage`（血量越低越红）
- 燃料条：修改 `Image.fillAmount = percentage / 100 * 0.25f`（最大填充 25%，对应圆弧形状）

注意：`fillAmount` 乘以 0.25 说明 `CurvedBarCircle.png` 是一个完整圆，UI 只使用其 1/4 弧段。

---

### 3. DeathScreen — 死亡黑屏

**文件**：`Assets/Scripts/UI/DeathScreen.cs`

**职责**：玩家死亡时，全屏 Image 逐帧增加 alpha，实现黑屏淡入效果。

**Unity 组件引用**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `deathBlackFadeImage` | Image（自身） | 全屏黑色遮罩 |
| `player` | Player | 玩家引用 |

**关键参数**：`FadingSpeed = 0.005f`（每帧 alpha +0.005，约 200 帧淡入完成）

**触发**：`player.Dieable.OnDeath` 事件 → `enabled = true` → Update 开始淡入

---

### 4. YouAreTakingDamageText — 受伤提示

**文件**：`Assets/Scripts/UI/YouAreTakingDamageText.cs`

**职责**：受伤时显示一段文字，持续 `timeToShow` 秒后消失。

**Unity 组件引用**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `textMeshProUGUI` | TextMeshProUGUI（自身） | 提示文字 |
| `text` | string（Inspector） | 显示内容（可配置） |
| `timeToShow` | float（Inspector） | 显示持续时间 |
| `player` | Player | 玩家引用 |

**触发**：`player.Damageable.OnTakingDamage` → 设置文字 + 重置计时器 → FixedUpdate 倒计时清空

---

### 5. CornerDebug — 调试信息

**文件**：`Assets/Scripts/UI/Debug/CornerDebug.cs`

**职责**：左下角显示调试信息（游戏时间、重力、地面检测等），F1 切换显示。

**Unity 组件引用**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `textMeshPro` | TextMeshProUGUI | 调试文字 |
| `playerControllable` | PlayerControllable | 监听 F1 切换事件 |

**静态接口**（供其他系统调用）：

```csharp
CornerDebug.AddDebug(string)                          // 追加一行调试文字
CornerDebug.AddGravityDebug(string name, string text) // 去重追加重力调试
```

每帧 FixedUpdate 渲染后重置，默认显示游戏时间 `MM:SS`。

---

### 6. SpaceNavigator — 太空导航仪 HUD

**文件**：`Assets/Scripts/PlayerTools/SpaceNavigator.cs`

**职责**：在屏幕上追踪天体位置，显示距离/速度，并用速度箭头指示相对运动方向。

**Unity 组件引用（全部为 GameObject/TextMeshProUGUI）**：

| 字段 | 说明 |
|------|------|
| `suggestCursor` | 建议光标根节点 |
| `lockCursor` | 锁定光标根节点 |
| `suggesterCursorTopRightArc` / `TopLeftArc` / `BottomRightArc` / `BottomLeftArc` | 建议光标四角弧片 |
| `lockCursorTopRightArc` / `TopLeftArc` / `BottomRightArc` / `BottomLeftArc` | 锁定光标四角弧片 |
| `topVelocityArrowMask` / `rightVelocityArrowMask` / `bottomVelocityArrowMask` / `leftVelocityArrowMask` | 速度箭头遮罩（控制箭头可见范围） |
| `topVelocityArrow` / `rightVelocityArrow` / `bottomVelocityArrow` / `leftVelocityArrow` | 速度方向箭头 |
| `lockInformation` | TextMeshProUGUI，显示天体名/距离/速度 |
| `player` | Player |

**核心逻辑**：

```
FixedUpdate
  ├── 未锁定 → TryToSuggestCelestialBody()
  │     └── 射线检测 → 找到 CelestialBody → 更新 suggestCursor 位置和大小
  └── 已锁定 → UpdateLock()
        ├── 检查天体是否在相机后方（隐藏 lockCursor）
        ├── UpdateCursorCoordinates() → camera.WorldToScreenPoint() → transform.position
        ├── UpdateCursorSize()        → 根据距离和半径计算弧片偏移量
        ├── UpdateLockText()          → "天体名\n距离\n速度"
        └── UpdateVelocityArrows()    → 速度差 → 箭头 localPosition（遮罩裁剪实现渐显）

Update → 鼠标左键点击 → 锁定/解锁
```

**速度箭头实现原理**：箭头默认被遮罩完全遮住（localPosition = -100），速度越大箭头越从遮罩中露出。

**距离显示**：> 5000 单位显示 km，否则显示 m。

---

### 7. SpaceShipAccelerationShowcase — 飞船加速度指示（3D UI）

**文件**：`Assets/Scripts/PlayerTools/SpaceShipParts/SpaceShipAccelerationShowcase.cs`

**职责**：飞船内部的物理指示灯，根据推进器方向点亮对应方向的 1~5 格指示灯。

**材质引用**：

| 字段 | 说明 |
|------|------|
| `defaultMaterial` | 未激活状态材质 |
| `glowMaterial` | 激活发光材质 |

**层级结构**（Inspector 中）：

```
SpaceShipAccelerationShowcase (此脚本)
├── Front/  (子对象，名称对应 Directions 常量)
│   ├── Step1 (Renderer)
│   ├── Step2
│   ├── Step3
│   ├── Step4
│   └── Step5
├── Back/
├── Left/
├── Right/
├── Top/
└── Bottom/
```

**点亮规则**：
- 单轴移动：点亮 5 格（FullPower）
- 双轴同时移动：点亮 3 格（PartPower）
- 垂直轴（Y）：始终 5 格

---

## 三、UI 系统依赖关系

```
UiActions
  ├── Player (PlayerLogic)
  ├── SpaceShipHatch, SpaceShipSeat, SpaceShipHealthAndFuelStation (PlayerTools.SpaceShipParts)
  ├── Campfire (StaticObjects)
  ├── TextMeshProUGUI (TMPro)
  └── UiAction (UI.UiActionParts)

SpaceSuitStatus
  ├── Player → Damageable.OnHealthPointsChange
  ├── Player → SpaceSuit.OnOxygenTankFillPercentageChanged
  ├── Player → SpaceSuit.OnFuelTankFillPercentageChanged
  └── Player → SpaceSuit.OnSuperFuelTankFillPercentageChanged

DeathScreen
  └── Player → Dieable.OnDeath

YouAreTakingDamageText
  └── Player → Damageable.OnTakingDamage

CornerDebug
  └── PlayerControllable.OnCornerDebugToggle

SpaceNavigator
  ├── Player (rigidbody, camera, transform)
  └── CelestialBody (rigidbody, name, radius)

SpaceShipAccelerationShowcase
  └── SpaceShipThrusters.UpdateInput() → 推送 acceleration 向量
```

---

## 四、迁移清单

如需在新项目中完整复刻该 UI 系统，需迁移以下文件：

### 脚本文件（必须全部迁移）

```
Assets/Scripts/UI/
├── UiActions.cs
├── SpaceSuitStatus.cs
├── DeathScreen.cs
├── YouAreTakingDamageText.cs
├── UiActionParts/
│   └── UiAction.cs
└── Debug/
    └── CornerDebug.cs

Assets/Scripts/PlayerTools/
├── SpaceNavigator.cs
└── SpaceShipParts/
    └── SpaceShipAccelerationShowcase.cs
```

依赖的非 UI 脚本（需同步迁移或重新实现接口）：

```
Assets/Scripts/PlayerLogic/
├── Player.cs
├── Damageable.cs        (OnHealthPointsChange, OnTakingDamage 事件)
├── Dieable.cs           (OnDeath 事件)
└── PlayerControllable.cs (OnCornerDebugToggle 事件)

Assets/Scripts/PlayerTools/
└── SpaceSuit.cs         (OnOxygenTankFillPercentageChanged 等事件)

Assets/Scripts/PlayerTools/SpaceShipParts/
├── SpaceShipHatch.cs
├── SpaceShipSeat.cs
└── SpaceShipHealthAndFuelStation.cs

Assets/Scripts/StaticObjects/
└── Campfire.cs

Assets/Scripts/Celestial/
└── CelestialBody.cs     (SpaceNavigator 需要 name, radius, rigidbody)
```

### 图片素材（Sprite，必须迁移）

| 文件路径 | 用途 | 类型 |
|----------|------|------|
| `Assets/Art/PlayerTools/SpaceSuit/SuitStatus/cosmonaut.png` | 太空服状态 HUD 宇航员图标 | Sprite |
| `Assets/Art/PlayerTools/SpaceSuit/SuitStatus/CurvedBarCircle.png` | 状态条圆弧底图（fillAmount 裁剪） | Sprite |
| `Assets/Art/PlayerTools/SpaceNavigator/outer_wilds_clone_cursor.png` | 导航仪自定义光标图 | Sprite |
| `Assets/Art/PlayerTools/SpaceNavigator/space_navigator_arrow.png` | 速度方向箭头图 | Sprite |
| `Assets/Art/PlayerTools/SpaceNavigator/space_navigator_lock_tick.png` | 锁定标记图 | Sprite |

### 材质文件（飞船加速度指示灯，3D UI）

| 文件路径 | 用途 |
|----------|------|
| `Assets/Art/PlayerTools/SpaceShip/AccelerationShowcase/AccelerationTick.mat` | 指示灯默认（未激活）材质 |
| `Assets/Art/PlayerTools/SpaceShip/AccelerationShowcase/AccelerationTickGlowing.mat` | 指示灯发光（激活）材质 |

### 字体依赖（TextMesh Pro）

所有文字 UI 均使用 TextMesh Pro，需迁移或在新项目中安装 TMP：

```
Assets/Art/Vendor/TextMesh Pro/
├── Fonts/LiberationSans.ttf          主字体文件
├── Resources/Fonts & Materials/      TMP 字体 Atlas 和材质
├── Resources/TMP Settings.asset      TMP 全局设置
└── Shaders/                          TMP 着色器（通常随 TMP 包自带）
```

> 如果新项目已通过 Package Manager 安装了 TextMesh Pro，Shaders 和 Resources 无需手动迁移，只需迁移 `LiberationSans.ttf` 及对应的 Font Asset。

### 迁移优先级汇总

| 优先级 | 内容 |
|--------|------|
| 必须 | 所有 `Scripts/UI/` 脚本 + `SpaceNavigator.cs` + `SpaceShipAccelerationShowcase.cs` |
| 必须 | `SpaceSuit/SuitStatus/` 下 2 张 PNG |
| 必须 | `SpaceNavigator/` 下 3 张 PNG |
| 必须 | `AccelerationShowcase/` 下 2 个材质 |
| 必须 | TextMesh Pro 字体资源 |
| 按需 | 依赖的游戏逻辑脚本（Damageable、Dieable、SpaceSuit 等事件接口） |
