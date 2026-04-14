using UnityEngine;

/// <summary>
/// 씬에 배치된 이름이 <c>BlackSmith</c> 인 오브젝트에 상호작용 스테이션이 없으면 붙입니다.
/// (프리팹에 이미 <see cref="BlackSmithInteractStation"/>이 있으면 아무 것도 하지 않습니다.)
/// </summary>
public static class BlackSmithSceneAutoWire
{
    private const string TargetName = "BlackSmith";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void WireBlacksmithsInLoadedScene()
    {
        WireScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private static void WireScene(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Walk(roots[i].transform);
        }
    }

    private static void Walk(Transform t)
    {
        if (t == null) return;
        if (t.name == TargetName && t.GetComponent<BlackSmithInteractStation>() == null)
        {
            if (t.GetComponent<SphereCollider>() == null)
            {
                SphereCollider sc = t.gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 2.4f;
                sc.center = new Vector3(0f, 1.1f, 0f);
            }

            t.gameObject.AddComponent<BlackSmithInteractStation>();
        }

        for (int c = 0; c < t.childCount; c++)
        {
            Walk(t.GetChild(c));
        }
    }
}
