[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# 模块说明

## 目录

1. [Bake 模块](#bake-模块)
2. [Baker 模块](#baker-模块)
3. [Bridge 模块](#bridge-模块)
4. [API 入口](#api-入口)
5. [Settings 配置](#settings-配置)
6. [工具类](#工具类)
7. [编辑器扩展](#编辑器扩展)

---

## Bake 模块

### PVSBakingBehaviour（抽象基类）

所有可烘焙对象的基类，提供：

- `bakeGroups`：渲染器分组数组
- `BakeData`：对应的烘焙数据资产
- `PreBake()` / `PostBake()` / `CompleteBake()`：烘焙生命周期钩子
- `GetIndicesForWorldPos()`：运行时采样入口
- `SamplingProviders`：可插拔的采样激活提供者列表

### PVSBakeData（ScriptableObject）

烘焙结果的抽象基类，保存：

- `bakeDataVersion`：数据版本号（Ver3 ~ Ver7）
- `bakeCompleted`：是否烘焙完成
- `SetRawData()` / `SampleAtIndex()`：写入与读取可见索引

### PVSBakeGroup

单个渲染器组，包含：

- `renderers`：`Renderer[]` 数组
- `groupType`：`Other` / `Foliage`
- `HasRenderer()` / `RefreshTerrain()`

### PVSBakingManager

烘焙调度器（Editor Only），负责：

- `BakeNow(behaviour)`：立即烘焙单个 Behaviour
- `ScheduleBake(info)`：将任务加入队列
- `BakeAllScheduled()`：批量执行队列中的烘焙任务
- `BakeMultiScene(scenes)`：多场景合并烘焙

### IActiveSamplingProvider

采样点激活状态的可插拔接口：

```csharp
public interface IActiveSamplingProvider
{
    string Name { get; }
    void InitializeSamplingProvider(...);
    bool IsSamplingPositionActive(Vector3 pos);
}
```

内置实现：`DefaultActiveSamplingProvider`（根据 `PVSExcludeVolume` 剔除采样点）

### PVSBakeSettings

烘焙参数配置（挂载在 Behaviour 上）：

- `bakeCellSize`：格子尺寸
- `openCamMaxDisOffset`：是否启用摄像机最大偏移距离
- `bakerFov90`：是否使用 90° FOV 烘焙器

### PVSExcludeVolume

放置在场景中用于屏蔽采样点的体积组件，实现 `CustomHandle.IResizableByHandle` 可在 Scene 视图拖拽调整大小。

### PVSWorldPosInfo

采样点的世界坐标信息，包含：

- `pos`：世界坐标
- `bounds`：格子包围盒
- `leafNodeIdx`：八叉树叶节点索引
- `samplePosOffsetMask`：摄像机偏移方向掩码
- `isForceSamplePos`：是否强制采样

---

## Baker 模块

### PVSBaker（抽象）

烘焙器基类，定义 `BakeAtPosition(pos, bakeSettings)` 接口。

### PVSBakerUnity

基于 Unity Camera 的烘焙器实现：

1. 在指定位置放置临时摄像机
2. 使用 `PVSColorTable` 为每个渲染器分配唯一颜色
3. 渲染场景，读取像素颜色
4. 通过 `PVSSceneColor` 将颜色映射回渲染器索引

### 摄像机类型

| 类 | FOV | 说明 |
|----|-----|------|
| `PVSBakerCam_HorizFov45` | 水平 45° | 精度高，速度慢 |
| `PVSBakerCam_HorizFov90` | 水平 90° | 速度快，适合大场景 |

### PVSColorTable

维护「渲染器 → 唯一颜色」映射表，每个渲染器分配一个 16 位索引对应的颜色。

---

## Bridge 模块

`PVSBridge` 提供与外部系统解耦的回调钩子：

```csharp
// 注册外部 LOD 控制回调
PVSBridge.onPVSProcDistanceLOD = (enable) => { /* ... */ };
```

| 委托 | 触发时机 |
|------|---------|
| `onPVSProcDistanceLOD` | 烘焙前后切换距离 LOD |
| `onPVSSamplePosSetCallback` | 采样点更新时 |

---

## API 入口

`PVSAPI` 是所有外部调用的统一入口（静态类，Editor Only 的方法均在 `#if UNITY_EDITOR` 块内）：

```csharp
// 立即烘焙
PVSAPI.Bake.BakeNow(behaviour);

// 队列烘焙
PVSAPI.Bake.ScheduleBake(bakeInfo);
PVSAPI.Bake.BakeAllScheduled();

// 事件
PVSAPI.Bake.OnBakeFinished      += OnSingleBakeDone;
PVSAPI.Bake.OnAllBakesFinished  += OnAllDone;

// 查找场景中的烘焙行为
var volumes = PVSAPI.Bake.FindAllBakingBehaviours<PVSBakingBehaviour>();
```

---

## Settings 配置

`PVSSettings`（ScriptableObject）存放全局参数，通过 `Resources.Load` 或 Asset Database 加载。

---

## 工具类

| 类 | 说明 |
|----|------|
| `PVSConstants` | 常量定义（路径、Layer、Tag 等） |
| `PVSUtil` | 通用工具方法 |
| `PVSTemp` | 临时缓存，减少 GC |

---

## 编辑器扩展

| 类 | 目标组件 | 说明 |
|----|---------|------|
| `PVSBakeDataEditor` | `PVSBakeData` | 显示烘焙数据摘要 |
| `PVSColorTableEditor` | `PVSColorTable` | 颜色表预览 |
| `PVSExcludeVolumeEditor` | `PVSExcludeVolume` | 排除体积可视化 |
| `PVSSettingsEditor` | `PVSSettings` | 全局配置面板 |
| `PVSEditorUtil` | — | 编辑器通用工具 |

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
