using System;
using UnityEngine;

[Serializable]
public class DoActionData
{
    public bool bCanMove;

    public float Power;
    public float Distance;
    public int StopFrame;

    public GameObject Particle;


    public int HitImpactIndex;

    public GameObject HitParticle;
    public Vector3 HitParticlePositionOffset;
    public Vector3 HitParticleScaleOffset = Vector3.one;
}

public abstract class Weapon : MonoBehaviour
{
	[SerializeField]
	protected WeaponType type;

    [SerializeField]
    protected DoActionData[] doActionDatas;


    public WeaponType Type { get => type; }


    protected GameObject rootObject;

    protected StateComponent state;
    protected Animator animator;

    protected virtual void Reset()
    {
        
    }

	protected virtual void Awake()
    {
        rootObject = transform.root.gameObject;
        Debug.Assert(rootObject != null);

        state = rootObject.GetComponent<StateComponent>();
        animator = rootObject.GetComponent<Animator>();
    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    public void Equip()
    {
        state.SetEquipMode();
    }

    public virtual void Begin_Equip()
    {

    }

    public virtual void End_Equip()
    {
        state.SetIdleMode();
    }

    public virtual void UnEquip()
    {

    }

    public virtual void DoAction()
    {
        state.SetActionMode();

        CheckStop(0);
    }

    public virtual void Begin_DoAction()
    {
        
    }

    public virtual void End_DoAction()
    {
        state.SetIdleMode();

        Move();
    }

    public virtual void Play_Particle()
    {

    }

    protected void Move()
    {
        PlayerMovingComponent moving = rootObject.GetComponent<PlayerMovingComponent>();

        if (moving != null)
            moving.Move();
    }

    protected void CheckStop(int index)
    {
        if (doActionDatas[index].bCanMove == false)
        {
            PlayerMovingComponent moving = rootObject.GetComponent<PlayerMovingComponent>();

            if (moving != null)
                moving.Stop();
        }
    }
}