[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)

# 流程与扩展

## 目录

1. [烘焙流程](#烘焙流程)
2. [运行时剔除流程](#运行时剔除流程)
3. [扩展采样提供者](#扩展采样提供者)
4. [注意事项](#注意事项)

---

## 烘焙流程

```
1. 调用 PVSAPI.Bake.BakeNow(volume)
2. PVSBakingManager.PreBake()
   ├── 收集场景渲染器 → PVSBakeGroup[]
   └── 计算采样点列表（通过 IActiveSamplingProvider 过滤）
3. 对每个采样点：
   ├── PVSBakerUnity 渲染场景
   ├── 读取颜色 → 解析为可见渲染器索引集合
   └── PVSBakeData.SetRawData(index, indices)
4. PVSBakingBehaviour.CompleteBake()
   └── 序列化为二进制流（按版本：Ver3 ~ Ver7）
5. 触发 PVSAPI.Bake.OnAllBakesFinished
```

---

## 运行时剔除流程

```
1. 摄像机每帧获取世界坐标
2. PVSBakingBehaviour.GetIndicesForWorldPos(worldPos)
   └── 定位到对应的格子索引
3. PVSBakeData.SampleAtIndex(index)
   └── 从二进制流中反序列化可见渲染器索引列表
4. 根据索引列表切换渲染器显隐（ToggleAllRenderers）
```

---

## 扩展采样提供者

实现 `IActiveSamplingProvider` 并注册到 `PVSBakingBehaviour.SamplingProviders`：

```csharp
public class MyCustomProvider : IActiveSamplingProvider
{
    public string Name => "MyCustomProvider";

    public void InitializeSamplingProvider(PVSSamplingPointMode mode,
        bool forceSample, Vector3 cellSize, float camMaxDis) { }

    public bool IsSamplingPositionActive(Vector3 pos)
    {
        // 自定义逻辑，返回 false 表示跳过此采样点
        return true;
    }
}

// 注册
volume.AddSamplingProvider(new MyCustomProvider());
```

---

## 注意事项

- 烘焙相关 API 仅在 Editor 模式下可用，运行时只保留剔除采样接口。
- `PVSBakeData` 数据版本从 Ver3 升至 Ver7，低版本数据仍可读取（向下兼容）。
- 场景中同时存在多个 `PVSBakingBehaviour` 时，各自维护独立的 `PVSBakeData` 资产，互不影响。
- 使用 `PVSExcludeVolume` 可标记不需要采样的区域，减少烘焙时间。

---

[← 返回索引](DETAILS.md) | [← 返回 README](../README.md)
