#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// BlackSmith 프리팹(FBX 인스턴스)에 <see cref="BlackSmithInteractStation"/>과 트리거 콜라이더를
/// Unity가 인식하는 방식으로 한 번 저장합니다. (수동 YAML m_AddedComponents는 깨지기 쉬움)
/// </summary>
[InitializeOnLoad]
public static class BlackSmithPrefabEnsureComponents
{
    private const string PrefabPath = "Assets/Prefabs/Character/BlackSmith.prefab";

    static BlackSmithPrefabEnsureComponents()
    {
        EditorApplication.delayCall += TryEnsureOnce;
    }

    private static void TryEnsureOnce()
    {
        EditorApplication.delayCall -= TryEnsureOnce;
        if (PrefabAssetAlreadyHasStation())
        {
            return;
        }

        EnsureOnPrefabAsset();
    }

    private static bool PrefabAssetAlreadyHasStation()
    {
        GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        return assetRoot != null && assetRoot.GetComponent<BlackSmithInteractStation>() != null;
    }

    [MenuItem("Tools/BlackSmith/Ensure Prefab Components (Save)", priority = 10)]
    private static void MenuEnsure()
    {
        EnsureOnPrefabAsset();
    }

    /// <summary>CI / 배치: <c>Unity.exe -executeMethod BlackSmithPrefabEnsureComponents.ApplyFromCommandLine</c></summary>
    public static void ApplyFromCommandLine()
    {
        try
        {
            EnsureOnPrefabAsset();
        }
        finally
        {
            EditorApplication.Exit(0);
        }
    }

    private static void EnsureOnPrefabAsset()
    {
        if (!AssetDatabase.AssetPathExists(PrefabPath))
        {
            Debug.LogWarning($"[BlackSmithPrefabEnsureComponents] 경로 없음: {PrefabPath}");
            return;
        }

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (contents == null)
        {
            Debug.LogWarning($"[BlackSmithPrefabEnsureComponents] LoadPrefabContents 실패: {PrefabPath}");
            return;
        }

        try
        {
            bool changed = false;

            SphereCollider sphere = contents.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                sphere = contents.AddComponent<SphereCollider>();
                changed = true;
            }

            sphere.isTrigger = true;
            sphere.radius = 2.4f;
            sphere.center = new Vector3(0f, 1.1f, 0f);

            if (contents.GetComponent<BlackSmithInteractStation>() == null)
            {
                contents.AddComponent<BlackSmithInteractStation>();
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[BlackSmithPrefabEnsureComponents] 저장 완료: {PrefabPath}");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
#endif
