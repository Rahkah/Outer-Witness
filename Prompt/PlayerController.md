```md id="player-gravity-camera-spec-v1"
# 🧍 玩家 + 🌍 重力 + 🎥 摄像机系统规范（MVP）

> 适用于：Unity 迷你星系项目（Outer Wilds 风格）  
> 目标：实现**球面行走 + 动态重力 + 稳定摄像机系统**  
> 面向对象：AI Agent（必须严格执行）

---

# 🧠 1. 总体设计目标

系统必须支持：

- 玩家在**球形星球表面行走**
- 重力方向**始终指向星球中心**
- 摄像机跟随玩家并保持**稳定视角**
- 系统可扩展（多星球 / 太空飞行）

---

# ⚠️ 2. 核心约束（必须遵守）

## 2.1 禁止事项

- ❌ 禁止使用 Unity 默认重力（Physics.gravity）
- ❌ 禁止使用 Rigidbody.AddForce 作为核心移动
- ❌ 禁止依赖 Vector3.up 作为世界上方向
- ❌ 禁止使用传统 FPS 控制器

---

## 2.2 必须满足

- ✅ 所有“上方向”必须动态计算
- ✅ 重力方向 = 指向星球中心
- ✅ 玩家旋转必须对齐重力法线
- ✅ 摄像机必须解耦（不能写死逻辑）

---

# 🏗️ 3. 系统结构

```

Player
├── PlayerController
├── MovementController
├── GravityController
└── CameraTarget

Camera
└── CinemachineVirtualCamera

```

---

# 🧩 4. 模块职责

## 4.1 PlayerController（输入层）

职责：
- 读取输入（移动 / 视角）
- 不直接操作 Transform
- 将输入传递给 MovementController

输入包括：
- Move (Vector2)
- Look (Vector2)
- Jump（预留）

---

## 4.2 GravityController（核心模块）

职责：
- 计算当前重力方向
- 提供 up 向量
- 提供重力加速度

---

## 4.3 MovementController（核心模块）

职责：
- 根据输入移动玩家
- 应用重力
- 控制旋转（贴合星球）

---

## 4.4 CameraTarget

职责：
- 提供摄像机跟随点
- 通常为玩家头部位置

---

# 🌍 5. 重力系统规范

## 5.1 当前版本（MVP）

仅支持：

```

单星球重力（最近天体）

```

---

## 5.2 重力方向计算

```

gravityDir = (planet.position - player.position).normalized

```

---

## 5.3 Up 向量定义

```

up = -gravityDir

```

---

## 5.4 重力强度

推荐：

```

gravityStrength = 20 ~ 40

```

---

# 🔄 6. 玩家旋转规范

## 6.1 对齐地面

玩家必须始终：

```

player.up == up

```

---

## 6.2 旋转方式

使用：

```

Quaternion.FromToRotation(currentUp, targetUp)

```

---

## 6.3 约束

- 必须平滑插值（避免抖动）
- 不允许瞬间跳变

---

# 🚶 7. 移动系统规范

## 7.1 移动方向

输入方向必须投影到切平面：

```

moveDir = ProjectOnPlane(inputDir, up)

```

---

## 7.2 地面移动速度

推荐：

```

5 ~ 10 m/s

```

---

## 7.3 移动应用方式

允许：

- CharacterController.Move（推荐）
- 或 Transform 位移（简化版）

---

## 7.4 重力应用

每帧：

```

velocity += gravityDir * gravityStrength * deltaTime

```

---

## 7.5 贴地机制（关键）

必须保证：

- 玩家不会“漂浮”
- 玩家不会“抖动”

可选方式：

- Raycast 检测地面
- 或持续向地面施加力

---

# 🎥 8. 摄像机系统规范

## 8.1 技术选型

必须使用：

- Cinemachine Virtual Camera

---

## 8.2 跟随目标

```

Follow = CameraTarget
LookAt = CameraTarget

```

---

## 8.3 相机行为

- 平滑跟随
- 不抖动
- 不锁死方向

---

## 8.4 视角控制

由 PlayerController 控制：

- 水平旋转（绕 up）
- 垂直旋转（限制角度）

---

# 🧭 9. 坐标系统规范

## 9.1 禁止使用

```

Vector3.up ❌

```

---

## 9.2 必须使用

```

动态 up（来自 GravityController） ✅

```

---

## 9.3 本地坐标

移动必须基于：

- 玩家 forward
- 玩家 right
- 当前 up

---

# 🔁 10. 每帧执行流程

## 执行顺序：

```

1. PlayerController 读取输入
2. GravityController 计算 gravityDir 和 up
3. MovementController：

   * 更新旋转（对齐 up）
   * 计算移动方向
   * 应用移动
   * 应用重力
4. Camera 跟随更新

```

---

# 🧪 11. 验收标准（必须满足）

## 11.1 基础功能

- 玩家可以在球面自由移动
- 不会滑动或漂浮
- 始终贴地

---

## 11.2 重力正确

- 玩家始终“站在地面”
- 上方向始终正确

---

## 11.3 摄像机稳定

- 不抖动
- 跟随自然
- 无奇怪旋转

---

## 11.4 边界情况

- 玩家走到星球另一侧仍然正常
- 不会翻转或锁死

---

# ⚠️ 12. 常见错误（必须避免）

## ❌ 错误 1

使用：

```

Vector3.up

```

---

## ❌ 错误 2

直接修改：

```

transform.position += ...

```

不考虑重力方向

---

## ❌ 错误 3

摄像机绑定世界坐标而非玩家

---

## ❌ 错误 4

使用 Rigidbody 完全控制角色

---

# 🚀 13. 扩展方向（后续）

## 13.1 多星球引力
- 选择最近天体

---

## 13.2 太空模式
- 无地面约束
- 惯性系统

---

## 13.3 飞船控制
- 独立控制器

---

## 13.4 时间系统联动
- 时间暂停 / 回溯

---

# 🎯 14. Definition of Done

当满足以下条件时，本系统完成：

- 玩家可在球面稳定行走
- 重力方向动态变化
- 摄像机稳定跟随
- 无抖动 / 无穿模 / 无翻转

---

# 📌 总结

本系统本质是：

> **一个“动态参考系下的角色控制系统”**

核心难点在于：

- 重力方向变化
- 局部坐标系转换
- 摄像机稳定性

必须保证：

- 解耦
- 数学驱动
- 可扩展
```

---
