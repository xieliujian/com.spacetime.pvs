[← 返回 README](../README.md)

# com.spacetime.pvs — 详细文档索引

## 架构说明

```
外部调用
  └── PVSAPI
        └── PVSBakingManager          ← 烘焙调度中心
              ├── PVSBakingBehaviour  ← 烘焙行为基类（Volume 等继承此类）
              │     ├── PVSBakeGroup[]       ← 渲染器分组
              │     └── PVSBakeData          ← 烘焙结果数据
              └── PVSBakerFactory
                    └── PVSBakerUnity        ← 基于 Unity Camera 的烘焙器
                          ├── PVSBakerBaseCam
                          ├── PVSColorTable
                          └── PVSSceneColor
```

## 子文档

| 文档 | 内容 |
|------|------|
| [modules.md](modules.md) | Bake / Baker / Bridge / API / Settings / 工具类 / 编辑器扩展 |
| [workflows.md](workflows.md) | 烘焙流程 / 运行时剔除流程 / 扩展采样提供者 / 注意事项 |

---

[← 返回 README](../README.md)
