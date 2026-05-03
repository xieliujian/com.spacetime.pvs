# com.spacetime.pvs — SpaceTime PVS

PVS（Potentially Visible Set）烘焙与运行时可见性剔除系统核心包。  
提供渲染器分组、采样烘焙、二进制流式加载及运行时剔除等基础能力。

## 目录

- [功能概述](#功能概述)
- [依赖](#依赖)
- [命名空间](#命名空间)
- [目录结构](#目录结构)
- [快速开始](#快速开始)
- [详细文档](#详细文档)

## 功能概述

| 模块 | 说明 |
|------|------|
| **Bake** | 烘焙管线：行为基类、数据模型、采样提供者、烘焙管理器 |
| **Baker** | 烘焙器实现：Unity Camera 渲染、颜色表、FOV 摄像机 |
| **Bridge** | 外部回调桥接接口 |
| **Settings** | 全局 PVS 配置（`PVSSettings`） |
| **API** | 对外公开的静态入口（`PVSAPI`） |

## 依赖

- `com.spacetime.core` ≥ 1.0.0
- Unity 2022.3+

## 命名空间

```csharp
using ST.PVS;
```

## 目录结构

```
com.spacetime.pvs/
├── Runtime/
│   └── Scripts/
│       ├── API/          # 对外接口
│       ├── Bake/         # 烘焙管线
│       ├── Baker/        # 烘焙器实现
│       ├── Bridge/       # 桥接回调
│       ├── Common/       # 公共组件
│       ├── Settings/     # 全局配置
│       └── Util/         # 常量与工具
└── Editor/
    └── Scripts/
        ├── Inspector/    # 自定义 Inspector
        └── Utils/        # 编辑器工具
```

## 快速开始

```csharp
// 触发烘焙
PVSAPI.Bake.BakeNow(bakingBehaviour);

// 监听烘焙完成
PVSAPI.Bake.OnAllBakesFinished += OnBakeDone;
```

## 详细文档

见 [DETAILS.md](readme/DETAILS.md)
