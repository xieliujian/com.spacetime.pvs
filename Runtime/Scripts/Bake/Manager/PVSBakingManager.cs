using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// PVS 烘焙任务调度管理器，负责排队、启动和逐帧推进烘焙协程。
    /// </summary>
    public static class PVSBakingManager
    {
#pragma warning disable 0414
        /// <summary>当前是否正在执行烘焙任务。</summary>
        public static bool IsBaking => m_ActiveBake != null;

        /// <summary>活跃烘焙协程枚举器。</summary>
        static IEnumerator m_ActiveBake = null;

        /// <summary>正在烘焙的 Behaviour 实例。</summary>
        static PVSBakingBehaviour m_ActiveBakingBehaviour = null;

        /// <summary>等待烘焙的任务队列。</summary>
        static readonly Queue<BakeInformation> m_ScheduledBakes = new Queue<BakeInformation>();

        /// <summary>所有烘焙完成后触发的事件（含队列中的后续任务）。</summary>
        public static Action ComplateEvent;

        /// <summary>是否需要在下一帧执行收尾逻辑。</summary>
        public static bool bNeedNextFrameExe = false;
#pragma warning restore 0414

        // ──────────────────────────────────────────
        // 公共方法
        // ──────────────────────────────────────────

        /// <summary>
        /// 将一个烘焙任务加入队列，不立即执行；需调用 <see cref="BakeAllScheduled"/> 启动。
        /// </summary>
        public static void ScheduleBake(BakeInformation bakeInformation)
        {
            m_ScheduledBakes.Enqueue(bakeInformation);
        }

        /// <summary>
        /// 立即烘焙多个 Behaviour（清空已有队列，跳过重复 BakeData）。
        /// </summary>
        public static void BakeNow(PVSBakingBehaviour[] cullingBakingBehaviours, HashSet<Renderer> additionalOccluders = null)
        {
            m_ScheduledBakes.Clear();

            var bakeDatas = new HashSet<PVSBakeData>();

            foreach (PVSBakingBehaviour behaviour in cullingBakingBehaviours)
            {
                if (behaviour.BakeData == null)
                    continue;

                if (bakeDatas.Add(behaviour.BakeData))
                {
                    ScheduleBake(new BakeInformation()
                    {
                        BakingBehaviour = behaviour,
                        AdditionalOccluders = additionalOccluders,
                    });
                }
            }

            BakeAllScheduled();
        }

        /// <summary>
        /// 立即烘焙单个 Behaviour（清空已有队列）。
        /// </summary>
        public static void BakeNow(PVSBakingBehaviour bakingBehaviour, HashSet<Renderer> additionalOccluders = null)
        {
            m_ScheduledBakes.Clear();

            ScheduleBake(new BakeInformation()
            {
                BakingBehaviour = bakingBehaviour,
                AdditionalOccluders = additionalOccluders
            });

            BakeAllScheduled();
        }

        /// <summary>
        /// 启动所有已排队的烘焙任务（逐帧推进）。
        /// </summary>
        public static void BakeAllScheduled()
        {
#if UNITY_EDITOR
            if (m_ScheduledBakes.Count <= 0)
            {
                Logger.LogError("PVSBakingManager: 队列为空，没有可烘焙的任务。");
                return;
            }

            UnityEditor.EditorApplication.update += EditorUpdate;
            bNeedNextFrameExe = true;

            BakeInformation bakeInformation = m_ScheduledBakes.Dequeue();
            m_ActiveBakingBehaviour = bakeInformation.BakingBehaviour;
            m_ActiveBake = m_ActiveBakingBehaviour.PerformBakeAsync(false, true, bakeInformation.AdditionalOccluders);
#endif
        }

        /// <summary>
        /// 烘焙多场景合并临时场景，完成后恢复各原始场景。
        /// </summary>
        public static void BakeMultiScene(List<UnityEngine.SceneManagement.Scene> scenes)
        {
#if UNITY_EDITOR
            var tmpPaths = new List<string>();
            var actualScenes = new List<UnityEngine.SceneManagement.Scene>();

            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                    continue;

                tmpPaths.Add(scene.path);
                actualScenes.Add(scene);
            }

            var relevantScenes = actualScenes.ToArray();

            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveModifiedScenesIfUserWantsTo(relevantScenes))
                return;

            for (int i = 1; i < relevantScenes.Length; i++)
                UnityEditor.SceneManagement.EditorSceneManager.MergeScenes(relevantScenes[i], relevantScenes[0]);

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(relevantScenes[0], PVSConstants.MultiSceneTempPath);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PVSConstants.MultiSceneTempPath,
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            PVSBakingBehaviour[] bakingBehaviours = UnityEngine.Object.FindObjectsOfType<PVSBakingBehaviour>();
            var renderers = new HashSet<Renderer>();

            foreach (var b in bakingBehaviours)
            {
                foreach (var g in b.bakeGroups)
                {
                    foreach (var r in g.renderers)
                        renderers.Add(r);
                }
            }

            void OnMultiBakeFinished()
            {
                try
                {
                    foreach (string scenePath in tmpPaths)
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                            UnityEditor.SceneManagement.OpenSceneMode.Additive);

                    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene(), true);

                    UnityEditor.AssetDatabase.DeleteAsset(PVSConstants.MultiSceneTempPath);
                }
                finally
                {
                    PVSAPI.Bake.OnAllBakesFinished -= OnMultiBakeFinished;
                }
            }

            PVSAPI.Bake.OnAllBakesFinished += OnMultiBakeFinished;
            BakeNow(bakingBehaviours, renderers);
#endif
        }

        // ──────────────────────────────────────────
        // 私有方法
        // ──────────────────────────────────────────

        /// <summary>Editor Update 钩子，逐帧推进烘焙协程并处理完成/中止逻辑。</summary>
        static void EditorUpdate()
        {
#if UNITY_EDITOR
            bool needsSceneReload = true;

            if (m_ActiveBake != null)
            {
                if (m_ActiveBake.MoveNext())
                {
                    if (m_ActiveBake.Current is PVSBakeAbortedYieldInstruction)
                    {
                        Logger.LogDebug("PVSBakingManager: 烘焙已中止。");
                        m_ScheduledBakes.Clear();
                    }
                    else if (m_ActiveBake.Current is PVSBakeNotStartedYieldInstruction)
                    {
                        needsSceneReload = false;
                        Logger.LogDebug("PVSBakingManager: 烘焙未能启动，已中止所有任务。");
                        m_ScheduledBakes.Clear();
                    }
                    else
                    {
                        return;
                    }
                }

                if (m_ActiveBake is IDisposable disposable)
                    disposable.Dispose();

                PVSAPI.Bake.OnBakeFinished?.Invoke(m_ActiveBakingBehaviour);

                if (m_ScheduledBakes.Count > 0)
                {
                    BakeInformation bakeInformation = m_ScheduledBakes.Dequeue();
                    m_ActiveBakingBehaviour = bakeInformation.BakingBehaviour;
                    m_ActiveBake = m_ActiveBakingBehaviour.PerformBakeAsync(false, false, bakeInformation.AdditionalOccluders);
                    return;
                }

                m_ActiveBake = null;
                m_ActiveBakingBehaviour = null;
            }

            if (bNeedNextFrameExe)
            {
                if (needsSceneReload && PVSConstants.AllowSceneReload)
                {
                    string scenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                }

                PVSBridge.onPVSBakeFinish?.Invoke();
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                bNeedNextFrameExe = false;
            }
            else
            {
                UnityEditor.EditorApplication.update -= EditorUpdate;
                PVSAPI.Bake.OnAllBakesFinished?.Invoke();
            }
#endif
        }
    }
}
