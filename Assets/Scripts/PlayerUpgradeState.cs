using UnityEngine;

/// <summary>
/// 대장장이 강화로 바뀌는 전투 수치(무기 데미지 보너스는 <see cref="PlayerMeleeCombat"/>에 반영, 방패는 가드 시 피해 비율).
/// </summary>
public class PlayerUpgradeState : MonoBehaviour
{
    [Header("방패 — 정면 가드 성공 시 받는 피해(원래 공격 피해의 비율)")]
    [SerializeField, Range(0.001f, 1f)] private float guardDamageTakenMultiplier = 0.1f;
    [SerializeField] private bool shieldUpgradeDone;

    [Header("무기")]
    [SerializeField, Min(0)] private int weaponUpgradeSteps;

    public float GuardDamageTakenMultiplier => guardDamageTakenMultiplier;
    public bool ShieldUpgradeDone => shieldUpgradeDone;
    public int WeaponUpgradeSteps => weaponUpgradeSteps;

    public static PlayerUpgradeState Resolve(Transform t) => TransformHierarchy.FindComponent<PlayerUpgradeState>(t);

    public int GetWeaponDamageAfterNextStep(PlayerMeleeCombat melee)
    {
        if (melee == null) return 0;
        return melee.CurrentAttackDamage + 10;
    }

    public bool TryApplyWeaponUpgrade(PlayerMeleeCombat melee)
    {
        if (melee == null) return false;
        melee.AddAttackDamage(10);
        weaponUpgradeSteps++;
        return true;
    }

    /// <summary>요구사항: 10% 받기 → 1% 받기(한 번 적용 후 더 이상 내려가지 않음).</summary>
    public bool TryApplyShieldUpgrade()
    {
        if (shieldUpgradeDone) return false;
        guardDamageTakenMultiplier = 0.01f;
        shieldUpgradeDone = true;
        return true;
    }

    public bool CanApplyShieldUpgrade() => !shieldUpgradeDone;
}
