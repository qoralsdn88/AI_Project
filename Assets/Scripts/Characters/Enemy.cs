using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class Enemy 
    : Character
    , IDamagable
{
    [SerializeField]
    private Color damageColor = Color.red;

    [SerializeField]
    private float changeColorTime = 0.15f;

    private Color originColor;
    private Material skinMaterial;

    
    private AIController aiController;

    protected override void Awake()
    {
        base.Awake();

        aiController = GetComponent<AIController>();


        Transform surface = transform.FindChildByName("Alpha_Surface");
        skinMaterial = surface.GetComponent<SkinnedMeshRenderer>().material;
        originColor = skinMaterial.color;
    }

    public void OnDamage(GameObject attacker, Weapon causer, Vector3 hitPoint, DoActionData data)
    {
        healthPoint.Damage(data.Power);

        StartCoroutine(Change_Color(changeColorTime));
        MovableStopper.Instance.Start_Delay(data.StopFrame);

        if (healthPoint.Dead == false)
            transform.LookAt(attacker.transform, Vector3.up);

        if(data.HitParticle != null)
        {
            GameObject obj = Instantiate<GameObject>(data.HitParticle, transform, false);
            obj.transform.localPosition = hitPoint + data.HitParticlePositionOffset;
            obj.transform.localScale = data.HitParticleScaleOffset;
        }

        if(healthPoint.Dead == false)
        {
            aiController?.SetDamageMode();


            state.SetDamagedMode();

            animator.SetInteger("ImpactIndex", (int)data.HitImpactIndex);
            animator.SetTrigger("Impact");


            rigidbody.isKinematic = false;

            float launch = rigidbody.linearDamping * data.Distance * 10.0f;
            rigidbody.AddForce(-transform.forward * launch);

            StartCoroutine(Change_IsKinemetics(5));

            return;
        }

        
        state.SetDeadMode();

        Collider collider = GetComponent<Collider>();
        collider.enabled = false;

        animator.SetTrigger("Dead");

        Destroy(gameObject, 5);
    }

    private IEnumerator Change_Color(float time)
    {
        skinMaterial.color = damageColor;

        yield return new WaitForSeconds(time);

        skinMaterial.color = originColor;
    }

    private IEnumerator Change_IsKinemetics(int frame)
    {
        for (int i = 0; i < frame; i++)
            yield return new WaitForFixedUpdate();

        rigidbody.isKinematic = true;
    }

    protected override void End_Damaged()
    {
        base.End_Damaged();

        animator.SetInteger("ImpactIndex", 0);
        state.SetIdleMode();

        aiController?.SetWaitMode();
    }
}