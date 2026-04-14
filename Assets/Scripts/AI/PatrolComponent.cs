using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PatrolComponent : MonoBehaviour
{
	[SerializeField]
	private float radius = 10; //¼øÂû ¹Ý°æ

	[SerializeField]
	private float goalDelay = 2.0f; //µµ´Þ½Ã ´ë±â½Ã°£

	[SerializeField]
	private float goalDelayRandom = 0.5f; //goalDelay + (-·£´ý ~ +·£´ý)

	[SerializeField]
	private PatrolPoints patrolPoints;
	public bool HasPatrolPoints { get => patrolPoints != null; }


	private Vector3 initPosition;
	private Vector3 goalPosition;

	private NavMeshAgent navMeshAgent;
	private NavMeshPath navMeshPath;

    private void Awake()
    {
		navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
		initPosition = goalPosition = transform.position;
    }

	public void StartMove()
    {
		if (navMeshPath == null)
			navMeshPath = CreateNavMeshPath();

		navMeshAgent.SetPath(navMeshPath);
    }

	private NavMeshPath CreateNavMeshPath()
    {
		NavMeshPath path = null;

		if(HasPatrolPoints)
        {
			goalPosition = patrolPoints.GetMoveToPosition();

			
			path = new NavMeshPath();

			bool bCheck = navMeshAgent.CalculatePath(goalPosition, path);
			Debug.Assert(bCheck);

			patrolPoints.UpdateNextIndex();


			return path;
        }


		Vector3 prevGoalPosition = goalPosition;

		while(true)
        {
			while(true)
            {
				float x = Random.Range(-radius * 0.5f, +radius * 0.5f);
				float z = Random.Range(-radius * 0.5f, +radius * 0.5f);

				goalPosition = new Vector3(x, 0, z) + initPosition;

				if (Vector3.Distance(goalPosition, prevGoalPosition) > radius * 0.25f)
					break;
			}


			path = new NavMeshPath();

			if (navMeshAgent.CalculatePath(goalPosition, path))
				return path;
        }
    }


	private bool bArrived;

    private void Update()
    {
		if (navMeshPath == null)
			return;

		if (bArrived == true)
			return;


		float distance = Vector3.Distance(transform.position, goalPosition);
		
		if (distance >= navMeshAgent.stoppingDistance)
			return;


		bArrived = true;

		float waitTime = goalDelay + Random.Range(-goalDelayRandom, +goalDelayRandom);

		IEnumerator waitRoutine = WaitDelay(waitTime);
		StartCoroutine(waitRoutine);
    }

	private IEnumerator WaitDelay(float time)
    {
		yield return new WaitForSeconds(time);

		navMeshPath = CreateNavMeshPath();
		navMeshAgent.SetPath(navMeshPath);

		bArrived = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
		if (Application.isPlaying == false)
			return;


		Vector3 from = transform.position + new Vector3(0.0f, 0.1f, 0.0f);
		Vector3 to = goalPosition + new Vector3(0.0f, 0.1f, 0.0f);

		Gizmos.color = Color.red;
		Gizmos.DrawLine(from, to);

		Gizmos.color = Color.green;
		Gizmos.DrawSphere(goalPosition, 0.5f);

		Gizmos.color = Color.blue;
		Gizmos.DrawSphere(initPosition, 0.25f);
	}
#endif
}