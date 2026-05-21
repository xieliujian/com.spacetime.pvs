using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// PVSBakingBehaviour 的烘焙执行分部类，包含异步烘焙协程和相关辅助方法。
    /// </summary>
    public partial class PVSBakingBehaviour
    {
        /// <summary>
        /// 异步执行烘焙流程（仅 Editor 下有效）。
        /// </summary>
        /// <param name="sceneReload">烘焙结束后是否重新加载场景。</param>
        /// <param name="saveScene">烘焙前是否保存场景。</param>
        /// <param name="additionalOccludersHashset">额外遮挡物渲染器集合。</param>
        public IEnumerator PerformBakeAsync(bool sceneReload, bool saveScene, HashSet<Renderer> additionalOccludersHashset)
        {
#if !UNITY_EDITOR
            yield break;
#else
            bool needsSceneReload = false;

            try
            {
                if (bakeGroups.Length <= 0)
                {
                    Debug.LogError("PVS: 没有烘焙分组，烘焙中止。");
                    yield break;
                }

                UnityEditor.EditorUtility.DisplayProgressBar("初始化", "正在初始化...", 0);

                InitializeAllSamplingProviders();

                var copyAdditionalOccluders = CalcAdditionalOccluders(additionalOccludersHashset);

                if (!CheckBakeGroupsMeshStats())
                    yield break;

                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(BakeData);

                CheckSaveScene(saveScene);

                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("PreBakeProcessScene", "PreBakeProcessScene", 1))
                {
                    yield return new PVSBakeAbortedYieldInstruction();
                }

                PreBakeProcessScene();

                var bakeSettings = CreateBakeSettings(copyAdditionalOccluders);
                using (PVSBaker baker = PVSBakerFactory.CreateBaker(bakeSettings))
                {
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("PreBake", "PreBake", 1))
                    {
                        yield return new PVSBakeAbortedYieldInstruction();
                    }

                    if (!PreBake())
                        yield break;

                    BakeData.bakeCompleted = false;
                    needsSceneReload = true;

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("CalcSamplingLocations", "CalcSamplingLocations", 1))
                    {
                        yield return new PVSBakeAbortedYieldInstruction();
                    }

                    var samplingLocations = CalcSamplingLocations(out int activeSamplingPositionsCount);
                    SetSaveBigVisIndex(activeSamplingPositionsCount);

                    Logger.Log($"PVS: activeSamplingPositionsCount = {activeSamplingPositionsCount}");

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    int totalBatchCounts = activeSamplingPositionsCount / baker.BatchCount;
                    int currentBatchCount = 0;

                    var pending = new List<PVSBakeHandle>(samplingLocations.Count);

                    const float SMOOTHING_FACTOR = 0.005f;
                    float lastTime = Time.realtimeSinceStartup;
                    int lastElement = 0;
                    float lastSpeed = -1f;
                    float averageSpeed = PVSSettings.Instance.bakeAverageSamplingSpeedMs / 1000f;
                    int bakedCellCount = 0;

                    CalcSamplePosOffsetMask(samplingLocations);

                    for (int i = 0; i < samplingLocations.Count; ++i)
                    {
                        string strTitle = $"[ETA {PVSUtil.FormatSeconds((activeSamplingPositionsCount - bakedCellCount) * averageSpeed)}], Avg: {System.Math.Round(averageSpeed * 1000f, 2)} ms | ";

                        if (!samplingLocations[i].Active)
                        {
                            BakeData.SetRawData(i, System.Array.Empty<ushort>(), false);
                            continue;
                        }

                        ++bakedCellCount;

                        Vector3 vSamplePos = samplingLocations[i].Position;
                        PVSBakerHandle handle = baker.SamplePosition(vSamplePos);

                        pending.Add(new PVSBakeHandle()
                        {
                            Index = i,
                            Handle = handle
                        });

                        if (pending.Count >= baker.BatchCount)
                        {
                            System.GC.Collect();

                            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(strTitle + "GPU Readback",
                                strTitle + "GPU Readback",
                                currentBatchCount / (float)totalBatchCounts))
                            {
                                yield return new PVSBakeAbortedYieldInstruction();
                            }

                            CompletePending(pending);
                            ++currentBatchCount;

                            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                                strTitle + $"Batch: {currentBatchCount}/{totalBatchCounts}",
                                "正在采样批次...",
                                currentBatchCount / (float)totalBatchCounts))
                            {
                                yield return new PVSBakeAbortedYieldInstruction();
                            }

#if UNITY_EDITOR
                            UnityEditor.SceneView.RepaintAll();
#endif
                            yield return null;

                            lastSpeed = (Time.realtimeSinceStartup - lastTime)
                                        / (currentBatchCount - lastElement) / (float)baker.BatchCount;
                            averageSpeed = SMOOTHING_FACTOR * lastSpeed + (1 - SMOOTHING_FACTOR) * averageSpeed;
                            lastTime = Time.realtimeSinceStartup;
                            lastElement = currentBatchCount;
                        }
                    }

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("收尾批次", "正在完成剩余批次...",
                        currentBatchCount / (float)totalBatchCounts))
                    {
                        yield return new PVSBakeAbortedYieldInstruction();
                    }

                    CompletePending(pending);

                    sw.Stop();
                    Logger.LogDebugF("PVS 烘焙耗时: {0} | 每采样点: {1:F2} ms",
                        PVSUtil.FormatSeconds(sw.ElapsedMilliseconds * 0.001f),
                        sw.ElapsedMilliseconds / (float)samplingLocations.Count);

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("PostBake", "PostBake",
                        currentBatchCount / (float)totalBatchCounts))
                    {
                        yield return new PVSBakeAbortedYieldInstruction();
                    }

                    PostBake();

                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("压缩数据", "正在压缩并完成烘焙...",
                        currentBatchCount / (float)totalBatchCounts))
                    {
                        yield return new PVSBakeAbortedYieldInstruction();
                    }

                    BakeData.CompleteBake();
                    BakeData.bakeCompleted = true;

                    UnityEditor.EditorUtility.SetDirty(BakeData);
                    UnityEditor.AssetDatabase.SaveAssetIfDirty(BakeData);
                }
            }
            finally
            {
                PerformBakeAsync_Error(needsSceneReload, sceneReload);
            }
#endif
        }

        /// <summary>烘焙结束（含异常路径）的收尾处理：清理进度条、重载场景、触发完成事件。</summary>
        void PerformBakeAsync_Error(bool needsSceneReload, bool sceneReload)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.ClearProgressBar();

            if (needsSceneReload && sceneReload && PVSConstants.AllowSceneReload)
            {
                string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;

                if (!string.IsNullOrEmpty(scenePath))
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                else
                    UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                        UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
            }

            if (PVSBakingManager.ComplateEvent != null)
                PVSBakingManager.ComplateEvent();
#endif
        }

        /// <summary>在满足条件时保存当前场景。</summary>
        void CheckSaveScene(bool saveScene)
        {
#if UNITY_EDITOR
            if (saveScene && UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path !=
                PVSConstants.MultiSceneTempPath)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            }
#endif
        }

        /// <summary>构建烘焙配置对象。</summary>
        PVSBakeSettings CreateBakeSettings(HashSet<Renderer> copyAdditionalOccluders)
        {
            return new PVSBakeSettings()
            {
                Groups = bakeGroups,
                AdditionalOccluders = copyAdditionalOccluders,
                Width = PVSSettings.Instance.bakeCameraResolutionWidth,
                Height = PVSSettings.Instance.bakeCameraResolutionHeight,
                isCheckSamplePosOffsetMask = m_CheckSamplePosOffsetMask,
                isBakerUseFov90 = IsBakerFov90(),
            };
        }

        /// <summary>根据 FindWorldPosInfoList 结果计算有效采样点列表及数量。</summary>
        List<PVSBakeSettings.SamplingLocation> CalcSamplingLocations(out int activeSamplingPositionsCount)
        {
            var worldInfoList = FindWorldPosInfoList();
            bool isMaxDensityAreaExist = IsMaxDensityAreaExist(false);

            Logger.Log($"[PVS][CalcSamplingLocations] 总采样点数: {worldInfoList.Count}, isSinglePointBakeMode: {isSinglePointBakeMode}, isMaxDensityAreaExist: {isMaxDensityAreaExist}, SamplingProviders数量: {SamplingProviders.Count}");

            var samplingLocations = new List<PVSBakeSettings.SamplingLocation>(worldInfoList.Count);
            int count = 0;
            int nullCount = 0;
            int forceCount = 0;
            int providerRejectedCount = 0;
            int singlePointInvalidCount = 0;

            for (int i = 0; i < worldInfoList.Count; ++i)
            {
                var info = worldInfoList[i];
                if (info == null)
                {
                    nullCount++;
                    continue;
                }

                var pos = info.pos;
                bool isForceSamplePos = info.isForceSamplePos;

                bool active = false;
                if (isSinglePointBakeMode)
                {
                    if (IsSamplePosValid(pos, isMaxDensityAreaExist))
                    {
                        if (isForceSamplePos)
                        {
                            active = true;
                            forceCount++;
                        }
                        else if (SamplingProvidersIsPositionActive(pos))
                        {
                            active = true;
                        }
                        else
                        {
                            providerRejectedCount++;
                        }
                    }
                    else
                    {
                        singlePointInvalidCount++;
                    }
                }
                else
                {
                    if (isForceSamplePos)
                    {
                        active = true;
                        forceCount++;
                    }
                    else if (SamplingProvidersIsPositionActive(pos))
                    {
                        active = true;
                    }
                    else
                    {
                        providerRejectedCount++;
                    }
                }

                samplingLocations.Add(new PVSBakeSettings.SamplingLocation(pos, active));
                count += active ? 1 : 0;
            }

            Logger.Log($"[PVS][CalcSamplingLocations] 有效点: {count}, 强制点: {forceCount}, null点: {nullCount}, Provider拒绝: {providerRejectedCount}, SinglePoint无效: {singlePointInvalidCount}");

            activeSamplingPositionsCount = count;
            return samplingLocations;
        }

        /// <summary>校验所有分组的 Mesh 统计信息（Play 模式下 Static Batching 后无效）。</summary>
        bool CheckBakeGroupsMeshStats()
        {
#if UNITY_EDITOR
            int nIdx = 0;
            foreach (PVSBakeGroup group in bakeGroups)
            {
                if (!group.CollectMeshStats())
                {
                    UnityEditor.EditorUtility.DisplayDialog("错误：检测到无效的渲染器",
                        $"分组 {nIdx} 包含无效的渲染器引用（Renderer/MeshFilter/Mesh 为空）。", "确定");
                    return false;
                }
                nIdx++;
            }
            return true;
#else
            return true;
#endif
        }

        /// <summary>合并外部额外遮挡物与序列化 additionalOccluders，并执行剔除过滤。</summary>
        HashSet<Renderer> CalcAdditionalOccluders(HashSet<Renderer> additionalOccludersHashset)
        {
            var result = new HashSet<Renderer>();

            if (additionalOccludersHashset != null)
            {
                foreach (Renderer r in additionalOccludersHashset)
                    result.Add(r);
            }

            foreach (Renderer r in additionalOccluders)
                result.Add(r);

            CullAdditionalOccluders(ref result);
            return result;
        }
    }
}
