using UnityEngine;

public class Hammer : Melee
{
    [SerializeField]
    private string handName = "Hand_Hammer";

    [SerializeField]
    private GameObject particlePrefab;

    [SerializeField]
    private string particleTransformName = "warhammer_head_low";

    private Transform handTransform;
    private Transform particleTransform;
    private GameObject trailParticle;

    protected override void Reset()
    {
        base.Reset();

        type = WeaponType.Hammer;
    }

    protected override void Awake()
    {
        base.Awake();

        handTransform = rootObject.transform.FindChildByName(handName);
        Debug.Assert(handTransform != null);

        transform.SetParent(handTransform, false);
        gameObject.SetActive(false);


        particleTransform = transform.FindChildByName(particleTransformName);
    }

    public override void Begin_Equip()
    {
        base.Begin_Equip();

        gameObject.SetActive(true);
    }

    public override void UnEquip()
    {
        base.UnEquip();

        gameObject.SetActive(false);
    }

    public override void Begin_Collision(AnimationEvent e)
    {
        base.Begin_Collision(e);

        if (particleTransform == null)
            return;

        if (particlePrefab == null)
            return;

        trailParticle = Instantiate<GameObject>(particlePrefab, particleTransform);
    }

    public override void End_Collision()
    {
        base.End_Collision();

        Destroy(trailParticle);
    }
}