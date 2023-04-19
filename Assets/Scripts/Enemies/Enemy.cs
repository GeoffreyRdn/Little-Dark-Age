using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies
{
    public class Enemy : MonoBehaviour
    {
        [Tag] [SerializeField] private string playerTag;

        [BoxGroup("Mask")] [SerializeField] private LayerMask playerMask;
        [BoxGroup("Mask")] [SerializeField] private LayerMask obstacleMask;
        [BoxGroup("Mask")] [SerializeField] private LayerMask wallMask;
        


        [BoxGroup("Settings")] [SerializeField] private float attackRange;
        [BoxGroup("Settings")] [SerializeField] private float hearRange;
        [BoxGroup("Settings")] [SerializeField] private float sightRange;
        [BoxGroup("Settings")] [SerializeField] private float destinationRange;
        [BoxGroup("Settings")] [SerializeField] private float abandonRange;
        [BoxGroup("Settings")] [SerializeField] private float sightAngle;
        
        [BoxGroup("Debug")] [SerializeField] Transform target;

        private NavMeshAgent agent;
        private GameObject[] players;
        
        private Vector3 destination;
        private bool hasDestination;
        
        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            players = GameObject.FindGameObjectsWithTag(playerTag);
        }
        
        private void Update()
        {
            UpdateTarget();

            if (target != null) Chase();
            else Patrol();
        }

        private bool InDetectionRange(Transform player)
        {
            if (player == null) return false;
            
            Vector3 position = transform.position;
            
            // in hear range, the AI notice the player
            if (Physics.CheckSphere(position, hearRange, playerMask))
            {
                Debug.Log("IN HEAR RANGE");
                return true;
            }

            Vector3 playerDirection = (player.position - position).normalized;
            
            // check if the player is in sightRange + in FOV + no obstacle between player and AI
            bool inRange = Physics.CheckSphere(position, sightRange, playerMask)
                           && Mathf.Abs(Vector3.Angle(transform.forward, playerDirection)) < sightAngle / 2
                           && !Physics.Raycast(position, playerDirection, sightRange, obstacleMask);

            Debug.Log(inRange ? "IN SIGHT RANGE RANGE" : "NOT IN RANGE");

            return inRange;
        }
        
        private bool InFollowRange() 
            => Physics.CheckSphere(transform.position, abandonRange, playerMask);
        
        private void UpdateTarget()
        {
            // the AI is already following a player
            if (target != null && InFollowRange()) return;

            (GameObject playerInRange, float distance) = (null, 0);
            
            foreach (GameObject player in players)
            {
                if (InDetectionRange(player.transform))
                {
                    var currentDistance = Vector3.Distance(player.transform.position, transform.position);
                    
                    // the AI will follow the closest player in detection range
                    if (playerInRange == null || currentDistance < distance)
                    {
                        playerInRange = player;
                        distance = currentDistance;
                    } 
                }
            }

            target = playerInRange != null ? playerInRange.transform : null;
        }


        private void SearchForDestination()
        {
            Vector3 position = transform.position;
            Vector3 route;
            
            do
            {
                float rndX = Random.Range(-destinationRange, destinationRange);
                float rndZ = Random.Range(-destinationRange, destinationRange);
                destination = new Vector3(position.x + rndX, position.y, position.z + rndZ);
                route = destination - position;
            } // ensure it does not cross any wall (to avoid pointing outside) and not pointing to an obstacle
            while (Physics.Raycast(position, route.normalized, route.magnitude, wallMask) ||
                   Physics.CheckSphere(destination, 0.5f, obstacleMask));

            hasDestination = true;
        }

        private void Patrol()
        {
            Debug.Log("PATROL");
            if (!hasDestination) SearchForDestination();
            if (hasDestination) agent.SetDestination(destination);

            // AI has reached the destination
            if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance == 0) hasDestination = false;
        }

        private void Chase()
        {
            Debug.Log("CHASING");
            Vector3 enemyPosition = target.position;
            Vector3 targetPosition = new Vector3(enemyPosition.x, transform.position.y, enemyPosition.z);
            agent.SetDestination(targetPosition);
        }

        #region DEBUG

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            float yAngle = transform.eulerAngles.y;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position, destinationRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(position, sightRange);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(position, hearRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(position, attackRange);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(position, abandonRange);

            Gizmos.color = Color.blue;
            float angleInDegL = yAngle + (sightAngle / 2);
            float angleInDegR = yAngle - (sightAngle / 2);

            Vector3 leftSide = new Vector3(Mathf.Sin(angleInDegL * Mathf.Deg2Rad), 0,
                Mathf.Cos(angleInDegL * Mathf.Deg2Rad));
            Vector3 rightSide = new Vector3(Mathf.Sin(angleInDegR * Mathf.Deg2Rad), 0,
                Mathf.Cos(angleInDegR * Mathf.Deg2Rad));

            Gizmos.DrawLine(position, position + leftSide * sightRange);
            Gizmos.DrawLine(position, position + rightSide * sightRange);
        }

        #endregion

    }
}
