using UnityEngine;

/// <summary>
/// 네크로맨서 구체 투사체:
/// - 직선 이동
/// - 플레이어와 충돌 시 20 데미지(기본값)
/// - 수명 만료 시 자동 삭제
/// </summary>
public class NecromancerProjectile : MonoBehaviour
{
    private GameObject _owner;
    private Vector3 _direction = Vector3.forward;
    private float _speed = 10f;
    private int _damage = 20;
    private float _lifeTime = 5f;
    private float _spawnTime;

    public void Initialize(GameObject owner, Vector3 direction, float speed, int damage, float lifeTime)
    {
        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        _speed = Mathf.Max(0.1f, speed);
        _damage = Mathf.Max(1, damage);
        _lifeTime = Mathf.Max(0.2f, lifeTime);
        _spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
        if (Time.time - _spawnTime >= _lifeTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (_owner != null && (other.gameObject == _owner || other.transform.IsChildOf(_owner.transform))) return;

        SimplePlayerHealth playerHealth = SimplePlayerHealth.Resolve(other.transform);
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(_damage, _owner, transform.position);
            Destroy(gameObject);
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(_damage, _owner, transform.position);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
