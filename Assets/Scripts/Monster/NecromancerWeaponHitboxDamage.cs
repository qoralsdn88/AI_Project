using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NecromancerWeaponHitboxDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damagePerTick = 1;
    [SerializeField, Min(0.05f)] private float tickIntervalSeconds = 1f;

    private readonly Dictionary<SimplePlayerHealth, float> _nextTickTimes = new Dictionary<SimplePlayerHealth, float>();
    private Collider _hitbox;
    private NecromancerBossController _owner;
    private CharacterController _playerCharacterController;
    private SimplePlayerHealth _playerHealth;

    public void Initialize(NecromancerBossController owner)
    {
        _owner = owner;
    }

    private void Awake()
    {
        _hitbox = GetComponent<Collider>();
        _hitbox.isTrigger = true;
        if (_owner == null) _owner = GetComponentInParent<NecromancerBossController>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void Update()
    {
        TryTickDamageByCharacterControllerFallback();
    }

    private void OnDisable()
    {
        _nextTickTimes.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTickDamage(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        TryTickDamage(other, false);
    }

    private void OnTriggerExit(Collider other)
    {
        SimplePlayerHealth hp = SimplePlayerHealth.Resolve(other != null ? other.transform : null);
        if (hp == null) return;
        _nextTickTimes.Remove(hp);
    }

    private void TryTickDamage(Collider other, bool forceTick)
    {
        if (_owner == null || !_owner.IsMeleeAttacking) return;
        if (other == null) return;

        SimplePlayerHealth hp = SimplePlayerHealth.Resolve(other.transform);
        if (hp == null || hp.IsDead) return;

        float now = Time.time;
        if (!forceTick && _nextTickTimes.TryGetValue(hp, out float nextTick) && now < nextTick)
        {
            return;
        }

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        hp.TakeDamage(damagePerTick, _owner.gameObject, hitPoint);
        _nextTickTimes[hp] = now + tickIntervalSeconds;
    }

    private void TryTickDamageByCharacterControllerFallback()
    {
        if (_owner == null || !_owner.IsMeleeAttacking) return;
        if (_hitbox == null || !_hitbox.enabled) return;

        if (_playerCharacterController == null || _playerHealth == null || _playerHealth.IsDead)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            _playerCharacterController = player.GetComponentInParent<CharacterController>();
            _playerHealth = SimplePlayerHealth.Resolve(player.transform);
        }

        if (_playerCharacterController == null || !_playerCharacterController.enabled) return;
        if (_playerHealth == null || _playerHealth.IsDead) return;
        if (!_hitbox.bounds.Intersects(_playerCharacterController.bounds)) return;

        float now = Time.time;
        if (_nextTickTimes.TryGetValue(_playerHealth, out float nextTick) && now < nextTick) return;

        Vector3 hitPoint = _playerCharacterController.bounds.ClosestPoint(_hitbox.bounds.center);
        _playerHealth.TakeDamage(damagePerTick, _owner.gameObject, hitPoint);
        _nextTickTimes[_playerHealth] = now + tickIntervalSeconds;
    }
}
