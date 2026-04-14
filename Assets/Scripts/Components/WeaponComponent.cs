using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
	Unarmed = 0, Fist, Sword, Hammer, FireBall, Max,
}

public class WeaponComponent : MonoBehaviour
{
	[SerializeField]
	private GameObject[] originPrefabs;


	private Animator animator;
	private StateComponent state;


	private WeaponType type = WeaponType.Unarmed;

	public event Action<WeaponType, WeaponType> OnWeaponTyeChanged;

	public event Action OnEndEquip;
	public event Action<string> OnEndDoAction;


	public bool UnarmedMode { get => type == WeaponType.Unarmed; }
	public bool FistMode { get => type == WeaponType.Fist; }
	public bool SwordMode { get => type == WeaponType.Sword; }
	public bool HammerMode { get => type == WeaponType.Hammer; }
	public bool FireBallMode { get => type == WeaponType.FireBall; }

	
	private Dictionary<WeaponType, Weapon> weaponTable;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		state = GetComponent<StateComponent>();
	}

	private void Start()
	{
		weaponTable = new Dictionary<WeaponType, Weapon>();

		for (int i = 0; i < (int)WeaponType.Max; i++)
			weaponTable.Add((WeaponType)i, null);


		for (int i = 0; i < originPrefabs.Length; i++)
		{
			GameObject obj = Instantiate<GameObject>(originPrefabs[i], transform);
			Weapon weapon = obj.GetComponent<Weapon>();
			obj.name = weapon.Type.ToString();

			weaponTable[weapon.Type] = weapon;
		}
	}

	public void SetFistMode()
	{
		if (state.IdleMode == false)
			return;

		SetMode(WeaponType.Fist);
	}

	public void SetSwordMode()
	{
		if (state.IdleMode == false)
			return;

		SetMode(WeaponType.Sword);
	}

	public void SetHammerMode()
	{
		if (state.IdleMode == false)
			return;

		SetMode(WeaponType.Hammer);
	}

	public void SetFireBallMode()
	{
		if (state.IdleMode == false)
			return;

		SetMode(WeaponType.FireBall);
	}

	public void SetUnarmedMode()
	{
		if (state.IdleMode == false)
			return;

		
		animator.SetInteger("WeaponType", (int)WeaponType.Unarmed);

		if (weaponTable[type] != null)
			weaponTable[type].UnEquip();

		
		ChangeType(WeaponType.Unarmed);
	}

	private void SetMode(WeaponType type)
	{
		if (this.type == type)
		{
			SetUnarmedMode();

			return;
		}
		else if (UnarmedMode == false)
		{
			weaponTable[this.type].UnEquip();
		}

		if (weaponTable[type] == null)
		{
			SetUnarmedMode();

			return;
		}


		animator.SetBool("IsEquipping", true);
		animator.SetInteger("WeaponType", (int)type);

		weaponTable[type].Equip();


		ChangeType(type);
	}

	private void ChangeType(WeaponType type)
	{
		if (this.type == type)
			return;


		WeaponType prevType = this.type;
		this.type = type;

		OnWeaponTyeChanged?.Invoke(prevType, type);
	}

	public void Begin_Equip()
	{
		weaponTable[type].Begin_Equip();
	}

	public void End_Equip()
    {
		animator.SetBool("IsEquipping", false);

		weaponTable[type].End_Equip();
		OnEndEquip?.Invoke();
	}

	public void DoAction()
    {
		if (weaponTable[type] == null)
			return;


		animator.SetBool("IsAction", true);

		weaponTable[type].DoAction();
	}

	private void Begin_DoAction()
    {
		weaponTable[type].Begin_DoAction();
	}

	public void End_DoAction(string blendTreeName)
	{
		animator.SetBool("IsAction", false);

		weaponTable[type].End_DoAction();
		OnEndDoAction?.Invoke(blendTreeName);
	}

	private void Begin_Combo()
    {
		Melee melee = weaponTable[type] as Melee;

		melee?.Begin_Combo();
    }

	private void End_Combo()
	{
		Melee melee = weaponTable[type] as Melee;

		melee?.Begin_Combo();
	}

	private void Begin_Collision(AnimationEvent e)
	{
		Melee melee = weaponTable[type] as Melee;

		melee?.Begin_Collision(e);
	}

	private void End_Collision()
	{
		Melee melee = weaponTable[type] as Melee;

		melee?.End_Collision();
	}

	private void Play_DoAction_Particle()
    {
		weaponTable[type].Play_Particle();
    }
}