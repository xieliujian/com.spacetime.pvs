using System;
using System.Collections.Generic;
using UnityEngine;

namespace ST.PVS
{
    /// <summary>
    /// PVS 系统对外公开 API，所有外部调用均通过此类完成。
    /// </summary>
    public static class PVSAPI
    {
        /// <summary>
        /// PVS 烘焙相关 API。
        /// </summary>
        public static class Bake
        {
#if UNITY_EDITOR
            /// <summary>单个 Behaviour 烘焙完成时触发的事件。</summary>
            public static Action<PVSBakingBehaviour> OnBakeFinished;

            /// <summary>所有排队烘焙任务全部完成时触发的事件。</summary>
            public static Action OnAllBakesFinished;

            /// <summary>查找场景中所有指定类型的 PVSBakingBehaviour。</summary>
            public static T[] FindAllBakingBehaviours<T>() where T : PVSBakingBehaviour
                => UnityEngine.Object.FindObjectsOfType<T>();

            /// <summary>查找场景中所有 PVSBakingBehaviour。</summary>
            public static PVSBakingBehaviour[] FindAllBakingBehaviours()
                => FindAllBakingBehaviours<PVSBakingBehaviour>();

            /// <summary>
            /// 将烘焙任务加入队列（不立即执行），需调用 <see cref="BakeAllScheduled"/> 启动。
            /// </summary>
            public static void ScheduleBake(BakeInformation bakeInformation)
                => PVSBakingManager.ScheduleBake(bakeInformation);

            /// <summary>立即烘焙多个 Behaviour。</summary>
            public static void BakeNow(PVSBakingBehaviour[] cullingBakingBehaviours, HashSet<Renderer> additionalOccluders = null)
                => PVSBakingManager.BakeNow(cullingBakingBehaviours, additionalOccluders);

            /// <summary>立即烘焙单个 Behaviour。</summary>
            public static void BakeNow(PVSBakingBehaviour bakingBehaviour, HashSet<Renderer> additionalOccluders = null)
                => PVSBakingManager.BakeNow(bakingBehaviour, additionalOccluders);

            /// <summary>启动所有已排队的烘焙任务。</summary>
            public static void BakeAllScheduled()
                => PVSBakingManager.BakeAllScheduled();

            /// <summary>烘焙多场景合并临时场景（完成后恢复各原始场景）。</summary>
            public static void BakeMultiScene(List<UnityEngine.SceneManagement.Scene> scenes)
                => PVSBakingManager.BakeMultiScene(scenes);
#endif
        }
    }
}
