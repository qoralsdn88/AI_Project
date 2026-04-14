#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// TreasureChest 프리팹에 상호작용용 컴포넌트를 자동으로 보장합니다.
/// </summary>
[InitializeOnLoad]
public static class TreasureChestPrefabEnsureComponents
{
    private const string PrefabPath = "Assets/Prefabs/MapObjects/TreasureChest.prefab";

    static TreasureChestPrefabEnsureComponents()
    {
        EditorApplication.delayCall += TryEnsureOnce;
    }

    private static void TryEnsureOnce()
    {
        EditorApplication.delayCall -= TryEnsureOnce;
        EnsureOnPrefabAsset();
    }

    private static void EnsureOnPrefabAsset()
    {
        if (!AssetDatabase.AssetPathExists(PrefabPath)) return;

        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (contents == null) return;

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
            sphere.radius = 2.2f;
            sphere.center = new Vector3(0f, 0.8f, 0f);

            if (contents.GetComponent<TreasureChestInteractStation>() == null)
            {
                contents.AddComponent<TreasureChestInteractStation>();
                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                AssetDatabase.SaveAssets();
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }
}
#endif
