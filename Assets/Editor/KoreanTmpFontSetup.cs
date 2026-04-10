using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

/// <summary>
/// 한글 지원 폰트(Noto Sans KR 등)로 TMP 동적 폰트(KoreanUi SDF)를 만들고,
/// LiberationSans Fallback + 프로젝트 기본 TMP 폰트를 이 에셋으로 설정합니다.
/// </summary>
public static class KoreanTmpFontSetup
{
    /// <summary>위에서부터 첫 번째로 존재·임포트된 파일을 사용합니다.</summary>
    static readonly string[] SourceFontCandidates =
    {
        "Assets/Fonts/NotoSansKR-Regular.ttf",
        "Assets/Fonts/NotoSansKR-VariableFont_wght.ttf",
        "Assets/Fonts/NanumSquareR.ttf",
    };

    const string KoreanFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/KoreanUi SDF.asset";
    const string LiberationSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Dungeon Knight/한글 폰트 적용 (TMP — Korean UI)")]
    public static void ApplyKoreanFont() => ApplyInternal(false, false, false);

    [MenuItem("Dungeon Knight/한글 폰트 강제 재생성 (TMP)")]
    public static void ApplyKoreanFontForce() => ApplyInternal(true, false, false);

    public static void ApplyKoreanFontBatch() => ApplyInternal(false, true, true);

    public static void ApplyKoreanFontBatchForce() => ApplyInternal(true, true, true);

    public static void ApplyKoreanFontQuiet() => ApplyInternal(false, false, true);

    static void ApplyInternal(bool forceRebuild, bool batchMode, bool quiet)
    {
        string resolvedPath;
        Font source = TryLoadSourceFont(out resolvedPath);
        if (source == null)
        {
            string msg = "한글용 폰트를 찾을 수 없습니다. 아래 중 하나를 Assets/Fonts/ 에 두세요.\n"
                + string.Join("\n", SourceFontCandidates);
            if (batchMode || quiet) Debug.LogError("[KoreanTmpFontSetup] " + msg);
            if (!batchMode && !quiet) EditorUtility.DisplayDialog("한글 폰트", msg, "확인");
            if (batchMode) EditorApplication.Exit(1);
            return;
        }

        EnsureFontIncludesData(source, resolvedPath);

        if (forceRebuild && AssetDatabase.LoadAssetAtPath<Object>(KoreanFontAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(KoreanFontAssetPath);
            AssetDatabase.Refresh();
        }

        TMP_FontAsset korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontAssetPath);
        if (korean == null)
        {
            korean = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (korean == null)
            {
                string err = "TMP 폰트 에셋 생성 실패. 폰트 임포트에서 Include Font Data를 켜 주세요. 파일: " + resolvedPath;
                if (batchMode || quiet) Debug.LogError("[KoreanTmpFontSetup] " + err);
                if (!batchMode && !quiet) EditorUtility.DisplayDialog("한글 폰트", err, "확인");
                if (batchMode) EditorApplication.Exit(1);
                return;
            }

            korean.name = "KoreanUi SDF";
            AssetDatabase.CreateAsset(korean, KoreanFontAssetPath);

            if (korean.atlasTextures != null)
            {
                for (int i = 0; i < korean.atlasTextures.Length; i++)
                {
                    Texture2D tex = korean.atlasTextures[i];
                    if (tex == null) continue;
                    tex.hideFlags = HideFlags.None;
                    AssetDatabase.AddObjectToAsset(tex, korean);
                }
            }

            if (korean.material != null)
            {
                korean.material.hideFlags = HideFlags.None;
                AssetDatabase.AddObjectToAsset(korean.material, korean);
            }

            if (!batchMode) Undo.RegisterCreatedObjectUndo(korean, "Create KoreanUi TMP Font");
            EditorUtility.SetDirty(korean);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        WireFallbackChain(korean);
        SetTmpDefaults(korean);
        HangulLineBreaking();

        AssetDatabase.SaveAssets();
        const string ok =
            "적용 완료: TMP 기본 폰트가 KoreanUi SDF(한글 지원)로 설정되었습니다. 네모가 남으면 강제 재생성을 실행하세요.";
        if (batchMode)
        {
            Debug.Log("[KoreanTmpFontSetup] " + ok);
            EditorApplication.Exit(0);
        }
        else if (quiet) Debug.Log("[KoreanTmpFontSetup] " + ok);
        else EditorUtility.DisplayDialog("한글 폰트", ok, "확인");
    }

    static Font TryLoadSourceFont(out string usedPath)
    {
        usedPath = null;
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot)) return null;

        foreach (string candidate in SourceFontCandidates)
        {
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, candidate.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(fullPath)) continue;
            Font f = AssetDatabase.LoadAssetAtPath<Font>(candidate);
            if (f != null)
            {
                usedPath = candidate;
                return f;
            }
        }

        return null;
    }

    static void EnsureFontIncludesData(Font font, string assetPath)
    {
        if (font == null) return;
        AssetImporter importer = AssetImporter.GetAtPath(assetPath);
        if (importer == null) return;

        SerializedObject so = new SerializedObject(importer);
        SerializedProperty inc = so.FindProperty("m_IncludeFontData") ?? so.FindProperty("includeFontData");
        if (inc == null || inc.boolValue) return;

        inc.boolValue = true;
        so.ApplyModifiedProperties();
        importer.SaveAndReimport();
        AssetDatabase.Refresh();
    }

    static void WireFallbackChain(TMP_FontAsset korean)
    {
        var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationSansPath);
        if (liberation == null)
            return;

        if (liberation.fallbackFontAssetTable == null)
            liberation.fallbackFontAssetTable = new List<TMP_FontAsset>();

        liberation.fallbackFontAssetTable.RemoveAll(
            f =>
                f != null
                && (f.name == "KoreanUi SDF"
                    || f.name == "NanumSquare SDF"
                    || f.name == "NotoSansKR SDF"));

        if (!liberation.fallbackFontAssetTable.Contains(korean))
        {
            if (!Application.isBatchMode) Undo.RecordObject(liberation, "Add KoreanUi TMP Fallback");
            liberation.fallbackFontAssetTable.Add(korean);
            EditorUtility.SetDirty(liberation);
        }
    }

    static void SetTmpDefaults(TMP_FontAsset korean)
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            return;

        if (!Application.isBatchMode) Undo.RecordObject(settings, "Set TMP Default Font KoreanUi");
        SerializedObject so = new SerializedObject(settings);
        SerializedProperty def = so.FindProperty("m_defaultFontAsset");
        if (def != null)
        {
            def.objectReferenceValue = korean;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
    }

    static void HangulLineBreaking()
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            return;

        if (!Application.isBatchMode) Undo.RecordObject(settings, "Enable Hangul Line Breaking");
        SerializedObject so = new SerializedObject(settings);
        SerializedProperty hangul = so.FindProperty("m_UseModernHangulLineBreakingRules");
        if (hangul != null && !hangul.boolValue)
        {
            hangul.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }
    }
}
