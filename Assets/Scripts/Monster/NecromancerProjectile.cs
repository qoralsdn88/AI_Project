using UnityEngine;

/// <summary>
/// 네크로맨서 구체 투사체:
/// - 직선 이동
/// - 플레이어와 충돌 시 20 데미지(기본값)
/// - 수명 만료 시 자동 삭제
/// </summary>
public class NecromancerProjectile : MonoBehaviour
{
    private const int DeflectSampleRate = 44100;
    private const float DeflectDurationSeconds = 0.18f;
    private const float DeflectVolume = 0.45f;

    private static AudioClip _deflectClip;
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
            bool blocked = PlayerShieldBlock.TryBlockHit(playerHealth.transform, _owner);
            if (blocked)
            {
                PlayBlockDeflectFeedback();
                Destroy(gameObject);
                return;
            }

            playerHealth.TakeDamage(_damage, _owner, transform.position);
            Destroy(gameObject);
            return;
        }

        if (IsMonsterCollider(other.transform))
        {
            // 몬스터는 완전히 관통하고, 어떤 피해도 주지 않습니다.
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    private static bool IsMonsterCollider(Transform target)
    {
        if (target == null) return false;
        if (target.GetComponentInParent<SimpleMonsterHealth>() != null) return true;
        if (target.GetComponentInParent<MonsterDetectChaseSimple>() != null) return true;
        return false;
    }

    private void PlayBlockDeflectFeedback()
    {
        HitImpactVfx.PlayAt(transform.position, _owner);

        AudioClip clip = GetOrCreateDeflectClip();
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, DeflectVolume);
        }
    }

    private static AudioClip GetOrCreateDeflectClip()
    {
        if (_deflectClip != null) return _deflectClip;

        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(DeflectDurationSeconds * DeflectSampleRate));
        float[] samples = new float[sampleCount];
        float twoPi = Mathf.PI * 2f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)DeflectSampleRate;
            float envelope = Mathf.Exp(-18f * t);
            float toneA = Mathf.Sin(twoPi * 1300f * t);
            float toneB = Mathf.Sin(twoPi * 2100f * t) * 0.55f;
            float toneC = Mathf.Sin(twoPi * 3200f * t) * 0.3f;
            samples[i] = (toneA + toneB + toneC) * envelope * 0.6f;
        }

        _deflectClip = AudioClip.Create("ShieldDeflectSynth", sampleCount, 1, DeflectSampleRate, false);
        _deflectClip.SetData(samples, 0);
        return _deflectClip;
    }
}
