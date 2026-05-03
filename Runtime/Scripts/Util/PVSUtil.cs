using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = ST.Core.Logging.Logger;

namespace ST.PVS
{
    /// <summary>
    /// PVS 系统通用工具方法集合。
    /// </summary>
    public static class PVSUtil
    {
        // ──────────────────────────────────────────
        // 格式化
        // ──────────────────────────────────────────

        /// <summary>
        /// 将秒数格式化为 hh:mm:ss 字符串，用于显示剩余烘焙时间。
        /// </summary>
        /// <param name="seconds">秒数。</param>
        /// <returns>格式化后的时间字符串。</returns>
        public static string FormatSeconds(double seconds)
        {
            System.TimeSpan ts = System.TimeSpan.FromSeconds(seconds);
            return ts.ToString(@"hh\:mm\:ss");
        }

        /// <summary>
        /// 将整数格式化为带单位的简短字符串（如 1.2M、34.5k）。
        /// </summary>
        /// <param name="number">要格式化的整数。</param>
        /// <returns>简短数字字符串。</returns>
        public static string FormatNumber(int number)
        {
            if (number >= 100000000)
                return (number / 1000000f).ToString("0.#M");

            if (number >= 1000000)
                return (number / 1000000f).ToString("0.##M");

            if (number >= 100000)
                return (number / 1000f).ToString("0.#k");

            if (number >= 10000)
                return (number / 1000f).ToString("0.##k");

            return number.ToString("#,0");
        }

        /// <summary>
        /// 在给定体积尺寸范围内，找到最接近用户指定除数的合法除数（可整除 volumeSize）。
        /// </summary>
        public static float FindValidDivisorCloseToUserProvided(float userProvidedDivisor, float volumeSize)
        {
            float bestFit = 0;

            for (float i = 0; i < volumeSize; i += 1f / 4f)
            {
                if (volumeSize % i == 0)
                {
                    if ((bestFit <= 0) ||
                        Mathf.Abs(i - userProvidedDivisor) < Mathf.Abs(bestFit - userProvidedDivisor))
                    {
                        bestFit = i;
                    }
                }
            }

            if (bestFit <= 0)
                return volumeSize;

            return bestFit;
        }

        // ──────────────────────────────────────────
        // 材质 / 渲染器判断
        // ──────────────────────────────────────────

        /// <summary>透明材质检测使用的 Shader 关键字提示列表。</summary>
        static readonly string[] transparentShaderKeywordHints = new string[]
        {
            "_ALPHATEST_ON",
            "ALPHACLIPPING_ON"
        };

        /// <summary>
        /// 判断材质是否使用了风格化水面 Shader（Shader 名称含 "StylizedWater"）。
        /// </summary>
        /// <param name="material">要检查的材质。</param>
        /// <returns>是否为风格化水面材质。</returns>
        public static bool IsStylizedWater(Material material)
        {
            if (material == null)
                return false;

            var shader = material.shader;
            return shader != null && shader.name.Contains("StylizedWater");
        }

        /// <summary>
        /// 判断渲染器是否使用了风格化水面 Shader。
        /// </summary>
        /// <param name="renderer">要检查的渲染器。</param>
        /// <returns>是否为风格化水面渲染器。</returns>
        public static bool IsStylizedWater(Renderer renderer)
        {
            if (renderer == null)
                return false;

            return IsStylizedWater(renderer.sharedMaterial);
        }

        /// <summary>
        /// 判断材质是否为透明材质（综合关键字与 renderQueue 判断）。
        /// </summary>
        /// <param name="mat">要检查的材质，允许为 null（返回 false）。</param>
        /// <returns>是否为透明材质。</returns>
        public static bool IsMaterialTransparent(Material mat)
        {
#pragma warning disable 162
            if (mat == null)
                return false;

            if (!PVSSettings.Instance.renderTransparency)
                return false;

            string nameLower = mat.name.ToLower();

            if (nameLower.Contains("pc_trans"))
                return true;

            if (nameLower.Contains("pc_opaque"))
                return false;

            foreach (var keyword in transparentShaderKeywordHints)
            {
                if (mat.IsKeywordEnabled(keyword))
                    return true;
            }

            return mat.renderQueue >= 2450;
#pragma warning restore 162
        }

        // ──────────────────────────────────────────
        // 渲染器显隐
        // ──────────────────────────────────────────

        /// <summary>
        /// 根据全局配置的切换模式显示或隐藏渲染器。
        /// </summary>
        /// <param name="r">目标渲染器；为 null 时直接返回。</param>
        /// <param name="visible">true 为显示，false 为隐藏。</param>
        /// <param name="forceNullCheck">是否强制进行 null 检查（通常 false 即可）。</param>
        public static void ToggleRenderer(Renderer r, bool visible, bool forceNullCheck)
        {
            if (r == null)
                return;

#pragma warning disable 162
            switch (PVSConstants.ToggleRenderMode)
            {
                case PVSRenderToggleMode.ToggleRendererComponent:
                    r.enabled = visible;
                    break;

                case PVSRenderToggleMode.ToggleForceRenderingOff:
#if !UNITY_2019_1_OR_NEWER
                    r.enabled = visible;
#else
                    r.forceRenderingOff = !visible;
#endif
                    break;

                default:
                    throw new System.InvalidOperationException();
            }
#pragma warning restore 162
        }
    }
}
