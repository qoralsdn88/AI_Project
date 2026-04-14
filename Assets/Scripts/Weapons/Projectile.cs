using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	[SerializeField]
	private float force = 1000.0f;

	private new Rigidbody rigidbody;
    private new Collider collider;

    public event Action<Collider, Collider, Vector3> OnProjectileHit;

    /// <summary>
    /// 플레이어 무기 외(예: 네크로맨서 보스)에서 동일 프리팹을 쓸 때,
    /// 기본 가속/수명/충돌 파괴를 끄고 다른 스크립트가 움직임·피해를 맡습니다.
    /// </summary>
    private bool _suppressDefaultBehavior;

    public void SuppressDefaultProjectileBehavior()
    {
        _suppressDefaultBehavior = true;
        if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (_suppressDefaultBehavior) return;

        Destroy(gameObject, 10.0f);

        rigidbody.AddForce(transform.forward * force);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_suppressDefaultBehavior) return;

        OnProjectileHit?.Invoke(collider, other, transform.position);

        Destroy(gameObject);
    }
}