using UnityEngine;

public class HealthPointComponent : MonoBehaviour
{
	[SerializeField]
	private float maxHealthPoint = 100.0f;
	private float currHealthPoint;

	public bool Dead { get => currHealthPoint <= 0.0f; }

    private void Start()
    {
        currHealthPoint = maxHealthPoint;
    }

    public void Damage(float amount)
    {
        if (amount < 1.0f)
            return;

        currHealthPoint += (amount * -1.0f);
        currHealthPoint = Mathf.Clamp(currHealthPoint, 0.0f, maxHealthPoint);
    }
}