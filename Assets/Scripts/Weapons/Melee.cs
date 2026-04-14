using System.Collections.Generic;
using UnityEngine;

public class Melee : Weapon
{
	private bool bEnable;
	private bool bExist;
	private int index;

    protected Collider[] colliders;
    private List<GameObject> hittedList;

    protected override void Awake()
    {
        base.Awake();

        colliders = GetComponentsInChildren<Collider>();
        hittedList = new List<GameObject>();
    }

    protected override void Start()
    {
        base.Start();

        End_Collision();
    }

    public virtual void Begin_Collision(AnimationEvent e)
    {
        foreach (Collider collider in colliders)
            collider.enabled = true;
    }

    public virtual void End_Collision()
    {
        foreach (Collider collider in colliders)
            collider.enabled = false;

        hittedList.Clear();
    }

    public void Begin_Combo()
    {
		bEnable = true;
    }

	public void End_Combo()
	{
		bEnable = false;
	}

    public override void DoAction()
    {
        if(bEnable)
        {
            bEnable = false;
            bExist = true;

            return;
        }


        if (state.IdleMode == false)
            return;

        base.DoAction();
    }

    public override void Begin_DoAction()
    {
        base.Begin_DoAction();

        if (bExist == false)
            return;


        bExist = false;

        index++;
        animator.SetTrigger("NextCombo");

        
        if (doActionDatas[index].bCanMove)
        {
            Move();

            return;
        }

        CheckStop(index);
    }

    public override void End_DoAction()
    {
        base.End_DoAction();

        index = 0;
        bEnable = false;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == rootObject)
            return;


        if (hittedList.Contains(other.gameObject) == true)
            return;

        hittedList.Add(other.gameObject);


        IDamagable damage = other.gameObject.GetComponent<IDamagable>();

        if (damage == null)
            return;


        Vector3 hitPoint = Vector3.zero;

        Collider enabledCollider = null;
        foreach(Collider collider in colliders)
        {
            if (collider.enabled == true)
            {
                enabledCollider = collider;

                break;
            }
        }
        
        
        hitPoint = enabledCollider.ClosestPoint(other.transform.position);
        hitPoint = other.transform.InverseTransformPoint(hitPoint);
        

        //GameObject temp = Instantiate<GameObject>(GameObject.CreatePrimitive(PrimitiveType.Sphere), other.transform, false);
        //temp.transform.localPosition = hitPoint;
        //temp.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        //print(temp.transform.localPosition);
        //print(temp.transform.position);

        //print(other.gameObject.name);

        damage.OnDamage(rootObject, this, hitPoint, doActionDatas[index]);
    }
}