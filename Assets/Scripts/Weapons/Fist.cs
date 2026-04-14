using UnityEngine;

public class Fist : Melee
{
    private enum PartType
    {
        LeftHand = 0, RightHand, LeftFoot, RightFoot, Max,
    }
    
    protected override void Reset()
    {
        base.Reset();

        type = WeaponType.Fist;
    }

    protected override void Awake()
    {
        base.Awake();

        for(int i = 0; i < (int)PartType.Max; i++)
        {
            Transform t = colliders[i].transform;
            
            t.DetachChildren();
            t.position = Vector3.zero;
            t.rotation = Quaternion.identity;

            Fist_Trigger trigger = t.GetComponent<Fist_Trigger>();
            trigger.OnTrigger += OnTriggerEnter;


            string partName = ((PartType)i).ToString();
            Transform parent = rootObject.transform.FindChildByName(partName);
            Debug.Assert(parent != null);

            t.SetParent(parent, false);
        }
    }

    public override void Begin_Collision(AnimationEvent e)
    {
        //base.Begin_Collision(e);

        colliders[e.intParameter].enabled = true;
    }
}