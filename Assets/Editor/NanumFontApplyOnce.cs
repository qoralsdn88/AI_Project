using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 마커 파일이 있으면 다음 에디터 프레임에서 한 번만 한글 TMP 적용을 실행합니다.
/// </summary>
[InitializeOnLoad]
internal static class KoreanFontApplyOnce
{
    static readonly string[] MarkerPaths =
    {
        "Assets/Editor/ApplyKoreanUiFontNextLoad.txt",
        "Assets/Editor/ApplyNanumFontNextLoad.txt",
    };

    static KoreanFontApplyOnce()
    {
        if (!AnyMarkerExists()) return;
        EditorApplication.delayCall += RunOnce;
    }

    static bool AnyMarkerExists()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot)) return false;
        foreach (string rel in MarkerPaths)
        {
            string full = Path.Combine(projectRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) return true;
        }

        return false;
    }

    static void RunOnce()
    {
        if (!AnyMarkerExists()) return;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        foreach (string rel in MarkerPaths)
        {
            string full = Path.Combine(projectRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (File.Exists(full)) File.Delete(full);
                if (File.Exists(full + ".meta")) File.Delete(full + ".meta");
            }
            catch
            {
                /* ignore */
            }
        }

        AssetDatabase.Refresh();
        KoreanTmpFontSetup.ApplyKoreanFontQuiet();
    }
}
