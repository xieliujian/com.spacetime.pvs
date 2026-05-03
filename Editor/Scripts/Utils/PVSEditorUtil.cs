using System.IO;
using UnityEditor;

namespace ST.PVS
{
    /// <summary>
    /// PVS Editor 通用工具方法集合。
    /// </summary>
    public static class PVSEditorUtil
    {
        /// <summary>
        /// 尝试获取指定烘焙数据资产的文件大小（格式化字符串）。
        /// </summary>
        /// <param name="bakeData">目标烘焙数据资产。</param>
        /// <param name="strBakeSize">输出的格式化文件大小字符串（失败时为空字符串）。</param>
        /// <returns>是否成功获取文件大小。</returns>
        public static bool TryGetAssetBakeSize(PVSBakeData bakeData, out string strBakeSize)
        {
            strBakeSize = "";

            if (bakeData == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(bakeData);

            if (string.IsNullOrEmpty(assetPath))
                return false;

            try
            {
                FileInfo fi = new FileInfo(assetPath);

                if (!fi.Exists)
                    return false;

                strBakeSize = EditorUtility.FormatBytes(fi.Length);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
