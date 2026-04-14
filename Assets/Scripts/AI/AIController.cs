using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PerceptionComponent))]
[RequireComponent(typeof(NavMeshAgent))]
public class AIController : MonoBehaviour
{
    [SerializeField]
    private float attackRange = 1.5f;

    [SerializeField]
    private float attackDelay = 2.0f;

    [SerializeField]
    private float attackDelayRandom = 0.5f;


    public enum Type
    {
        Wait = 0, Patrol, Approach, Equip, Action, Damaged,
    }
    private Type type = Type.Wait;

    public event Action<Type, Type> OnAIStateTypeChanged;

    public bool WaitMode { get => type == Type.Wait; }
    public bool PatrolMode { get => type == Type.Patrol; }
    public bool ApproachMode { get => type == Type.Approach; }
    public bool EquipMode { get => type == Type.Equip; }
    public bool ActionMode { get => type == Type.Action; }
    public bool DamagedMode { get => type == Type.Damaged; }


    private PerceptionComponent perception;
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    private PatrolComponent patrol;
    private WeaponComponent weapon;

    private void Awake()
    {
        perception = GetComponent<PerceptionComponent>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        patrol = GetComponent<PatrolComponent>();
        
        weapon = GetComponent<WeaponComponent>();
        weapon.OnEndEquip += OnEndEquip;
        weapon.OnEndDoAction += OnEndDoAction;
    }

    private void FixedUpdate()
    {
        bool bCheck = false;
        bCheck |= (EquipMode == true);
        bCheck |= (ActionMode == true);
        bCheck |= (DamagedMode == true);

        if (bCheck) return;


        GameObject player = perception.GetPercievedPlayer();

        if (player == null)
        {
            if (weapon.UnarmedMode == false)
                weapon.SetUnarmedMode();


            if(patrol == null)
            {
                SetWaitMode();

                return;
            }

            SetPatrolMode();

            return;
        }

        if(weapon.UnarmedMode == true)
        {
            SetEquipMode(WeaponType.Sword);

            return;
        }


        float temp = Vector3.Distance(transform.position, player.transform.position);
        if(temp < attackRange)
        {
            if (weapon.SwordMode)
                SetActionMode();

            return;
        }


        SetApproachMode();
    }

    private void LateUpdate()
    {
        LateUpdate_SetSpeed();
        LateUpdate_Approach();
    }

    private void LateUpdate_SetSpeed()
    {
        switch(type)
        {
            case Type.Wait:
            case Type.Action:
            case Type.Damaged:
            {
                animator.SetFloat("SpeedY", 0.0f);
            }
            break;

            case Type.Patrol:
            case Type.Approach:
            {
                animator.SetFloat("SpeedY", navMeshAgent.velocity.magnitude);
            }
            break;
        }
    }

    private void LateUpdate_Approach()
    {
        if (ApproachMode == false)
            return;


        GameObject player = perception.GetPercievedPlayer();

        if (player == null)
            return;


        navMeshAgent.SetDestination(player.transform.position);
    }

    public void SetWaitMode()
    {
        if (WaitMode == true)
            return;


        navMeshAgent.isStopped = true;
        ChangeType(Type.Wait);
    }

    private void SetApproachMode()
    {
        if (ApproachMode == true)
            return;

        navMeshAgent.isStopped = false;
        ChangeType(Type.Approach);
    }

    private void SetPatrolMode()
    {
        if (PatrolMode == true)
            return;

        ChangeType(Type.Patrol);

        navMeshAgent.isStopped = false;
        patrol.StartMove();
    }

    private void SetEquipMode(WeaponType type)
    {
        if (EquipMode == true)
            return;

        ChangeType(Type.Equip);
        //navMeshAgent.isStopped = true;


        switch(type)
        {
            case WeaponType.Sword: weapon.SetSwordMode(); break;

            default: Debug.Assert(false); break;
        }
    }

    private void SetActionMode()
    {
        if (ActionMode == true)
            return;

        
        navMeshAgent.isStopped = true;
        ChangeType(Type.Action);

        
        GameObject player = perception.GetPercievedPlayer();
        
        if (player != null)
            transform.LookAt(player.transform);

        
        weapon.DoAction();
    }

    public void SetDamageMode()
    {
        if (DamagedMode == true)
            return;

        StopAllCoroutines();

        if (ActionMode)
            weapon.End_DoAction("Blend Tree");


        navMeshAgent.isStopped = true;
        ChangeType(Type.Damaged);
    }

    private void ChangeType(Type type)
    {
        Type prevType = this.type;
        this.type = type;

        OnAIStateTypeChanged?.Invoke(prevType, type);
    }

    private void OnEndEquip()
    {
        if(DamagedMode == false)
            SetWaitMode();
    }

    private void OnEndDoAction(string blendTreeName)
    {
        if(blendTreeName.Length > 0)
        {
            if(blendTreeName.Equals("Blend Tree"))
            {
                animator.Play(blendTreeName);
                SetWaitMode();

                return;
            }
        }

        StartCoroutine(Wait_EndDoAction_Random());
    }

    private IEnumerator Wait_EndDoAction_Random()
    {
        float time = 0.0f;
        time += attackDelay;
        time += UnityEngine.Random.Range(-attackDelayRandom, +attackDelayRandom);

        yield return new WaitForSeconds(time);


        SetWaitMode();
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (Selection.activeGameObject != gameObject)
            return;

        GUILayout.Label($"{gameObject.name} / {type}");
    }
#endif
}