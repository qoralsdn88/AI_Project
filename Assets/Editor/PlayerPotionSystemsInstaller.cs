#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 플레이어 프리팹에 포션 사용 시스템 컴포넌트를 보장하고 Drinking 애니메이션 상태를 AnimatorController에 추가합니다.
/// </summary>
[InitializeOnLoad]
public static class PlayerPotionSystemsInstaller
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Character/Player.prefab";
    private const string DrinkFbxPath = "Assets/Art/Animations/Drinking.fbx";
    private const string DrinkTrigger = "DrinkPotion";
    private const string DrinkState = "DrinkPotionState";

    static PlayerPotionSystemsInstaller()
    {
        EditorApplication.delayCall += EnsureSetup;
    }

    private static void EnsureSetup()
    {
        GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (playerRoot == null) return;

        try
        {
            bool changed = false;

            PlayerPotionInventory inventory = playerRoot.GetComponent<PlayerPotionInventory>();
            if (inventory == null)
            {
                inventory = playerRoot.AddComponent<PlayerPotionInventory>();
                changed = true;
            }

            PlayerPotionUseController useController = playerRoot.GetComponent<PlayerPotionUseController>();
            if (useController == null)
            {
                useController = playerRoot.AddComponent<PlayerPotionUseController>();
                changed = true;
            }

            SerializedObject useSo = new SerializedObject(useController);
            useSo.FindProperty("inventory").objectReferenceValue = inventory;
            useSo.ApplyModifiedPropertiesWithoutUndo();

            Animator animator = playerRoot.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null && EnsureDrinkingState(controller)) changed = true;
            }

            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
                AssetDatabase.SaveAssets();
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerRoot);
        }
    }

    private static bool EnsureDrinkingState(AnimatorController controller)
    {
        bool changed = false;
        if (!HasTrigger(controller, DrinkTrigger))
        {
            controller.AddParameter(DrinkTrigger, AnimatorControllerParameterType.Trigger);
            changed = true;
        }

        AnimationClip clip = LoadDrinkClip();
        if (clip == null) return changed;

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState drinkState = FindState(sm, DrinkState);
        if (drinkState == null)
        {
            drinkState = sm.AddState(DrinkState, new Vector3(450f, 170f, 0f));
            changed = true;
        }

        if (drinkState.motion != clip)
        {
            drinkState.motion = clip;
            changed = true;
        }

        if (!HasAnyStateTransition(sm, drinkState, DrinkTrigger))
        {
            AnimatorStateTransition t = sm.AddAnyStateTransition(drinkState);
            t.hasExitTime = false;
            t.duration = 0.05f;
            t.AddCondition(AnimatorConditionMode.If, 0f, DrinkTrigger);
            changed = true;
        }

        AnimatorState defaultState = sm.defaultState;
        if (defaultState != null && !HasTransition(drinkState, defaultState))
        {
            AnimatorStateTransition back = drinkState.AddTransition(defaultState);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.08f;
            changed = true;
        }

        if (changed) EditorUtility.SetDirty(controller);
        return changed;
    }

    private static bool HasTrigger(AnimatorController controller, string name)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].type == AnimatorControllerParameterType.Trigger &&
                controller.parameters[i].name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string stateName)
    {
        for (int i = 0; i < sm.states.Length; i++)
        {
            if (sm.states[i].state != null && sm.states[i].state.name == stateName) return sm.states[i].state;
        }
        return null;
    }

    private static bool HasAnyStateTransition(AnimatorStateMachine sm, AnimatorState dst, string trigger)
    {
        for (int i = 0; i < sm.anyStateTransitions.Length; i++)
        {
            AnimatorStateTransition t = sm.anyStateTransitions[i];
            if (t.destinationState != dst) continue;
            for (int c = 0; c < t.conditions.Length; c++)
            {
                if (t.conditions[c].parameter == trigger) return true;
            }
        }

        return false;
    }

    private static bool HasTransition(AnimatorState from, AnimatorState to)
    {
        for (int i = 0; i < from.transitions.Length; i++)
        {
            if (from.transitions[i].destinationState == to) return true;
        }
        return false;
    }

    private static AnimationClip LoadDrinkClip()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(DrinkFbxPath);
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null) continue;
            if (clip.name.StartsWith("__preview__")) continue;
            return clip;
        }

        return AssetDatabase.LoadAssetAtPath<AnimationClip>(DrinkFbxPath);
    }
}
#endif
