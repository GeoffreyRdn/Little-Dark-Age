using System;
using System.Collections.Generic;
using System.Linq;
using Health;
using NaughtyAttributes;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemies
{
    public class Enemy : MonoBehaviour
    {
        #region Variables

        #region Editor
        
        [Tag] [SerializeField] private string playerTag;

        [BoxGroup("Mask")] [SerializeField] private LayerMask playerMask;
        [BoxGroup("Mask")] [SerializeField] private LayerMask obstacleMask;
        [BoxGroup("Mask")] [SerializeField] private LayerMask wallMask;
        
        [BoxGroup("Settings")] [SerializeField] private float attackRange;
        [BoxGroup("Settings")] [SerializeField] private float hearRange;
        [BoxGroup("Settings")] [SerializeField] private float sightRange;
        [BoxGroup("Settings")] [SerializeField] private float destinationRange;
        [BoxGroup("Settings")] [SerializeField] private float abandonRange;
        [BoxGroup("Settings")] [SerializeField] private float stopRange;
        [BoxGroup("Settings")] [SerializeField] private float sightAngle;
        
        [BoxGroup("Settings")] [SerializeField] private float patrolSpeed;
        [BoxGroup("Settings")] [SerializeField] private float chaseSpeed;
        
        [BoxGroup("Debug")] [SerializeField] Transform target;
        
        #endregion

        #region boolean

        private bool isAttacking;
        private bool isHit;

        #endregion

        #region Animations

        private static readonly int IdleAnimation = Animator.StringToHash("idle");
        private static readonly int WalkAnimation = Animator.StringToHash("walk");
        private static readonly int RunAnimation = Animator.StringToHash("run");
        private static readonly int AttackAnimation = Animator.StringToHash("attack");
        private static readonly int HitAnimation = Animator.StringToHash("hit");
        
        private float lockedUntil;
        private int currentState;

        private Animator animator;

        #endregion

        #region Sounds


        [SerializeField, BoxGroup("Audio")] private AudioClip walkSound;
        [SerializeField, BoxGroup("Audio")] private AudioClip runSound;
        [SerializeField, BoxGroup("Audio")] private AudioClip attackSound;
        [SerializeField, BoxGroup("Audio")] private AudioClip deathSound;
        [SerializeField, BoxGroup("Audio")] private AudioClip hurtSound;
        
        private AudioSource audioSource;

        #endregion
        
        private NavMeshAgent agent;
        private List<GameObject> players;
        private PhotonView photonView;

        private Vector3 destination;
        private bool hasDestination;
        
        #endregion

        #region Start - Update

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            players = GameObject.FindGameObjectsWithTag(playerTag).ToList();
            photonView = GetComponent<PhotonView>();
            audioSource = GetComponent<AudioSource>();
        }
        
        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            
            var state = GetState();
            if (state != AttackAnimation)
            {
                UpdateTarget();

                if (target != null)
                {
                    if (InAttackRange()) Attack();
                    else Chase();
                }

                else Patrol();
            }
            
            if (state == currentState) return;

            animator.CrossFade(state, 0, 0);
            currentState = state;
            
            AudioClip sound = walkSound;
            bool repeat = true;

            if (state == AttackAnimation) (sound, repeat) = (attackSound, false);
            else if (state == HitAnimation) (sound, repeat) = (hurtSound, false);
            else if (state == RunAnimation) sound = runSound;

            audioSource.clip = sound;
            
            if (repeat) audioSource.Play();
            else audioSource.PlayOneShot(sound);
            
            photonView.RPC(nameof(SendAnimations), RpcTarget.Others, currentState);
        }

        #endregion

        #region Events

        private void OnEnable()
        {
            HealthController.onPlayerDeath += HandlePlayerDeath;
            DamageBehavior.onEnemyDamaged += HandleEnemyDamaged;
        }

        private void OnDisable()
        {
            HealthController.onPlayerDeath -= HandlePlayerDeath;
            DamageBehavior.onEnemyDamaged -= HandleEnemyDamaged;
        }

        #endregion

        #region Methods Animations
        
        [PunRPC]
        private void SendAnimations(int state)
        {
            if (gameObject == null) return;
            
            animator.CrossFade(state, 0, 0);
            currentState = state;
            
            AudioClip sound = walkSound;
            bool repeat = true;

            if (state == AttackAnimation) (sound, repeat) = (attackSound, false);
            else if (state == HitAnimation) (sound, repeat) = (hurtSound, false);
            else if (state == RunAnimation) sound = runSound;

            var audioS = GetComponent<AudioSource>();
            audioS.clip = sound;
            
            if (repeat) audioS.Play();
            else audioS.PlayOneShot(sound);
        }

        private int GetState()
        {
            if (currentState == AttackAnimation) isAttacking = false;
            if (currentState == HitAnimation) isHit = false;

            if (Time.time < lockedUntil) return currentState;
            
            if (isAttacking) return LockState(AttackAnimation, 1.5f);
            if (isHit) return LockState(HitAnimation, .7f);
            
            return agent.velocity.magnitude < .15f 
                ? IdleAnimation 
                : Math.Abs(agent.speed - patrolSpeed) < .1f 
                    ? WalkAnimation 
                    : RunAnimation;

            int LockState(int s, float t)
            {
                lockedUntil = Time.time + t;
                return s;
            }
        }
        
        #endregion

        #region Check Range

        private bool InDetectionRange(Transform player)
        {
            if (player == null) return false;
            
            Vector3 position = transform.position;
            
            // in hear range, the AI notice the player
            if (Physics.CheckSphere(position, hearRange, playerMask)) return true;
            
            Vector3 playerDirection = (player.position - position).normalized;
            
            // check if the player is in sightRange + in FOV + no obstacle between player and AI
            bool inRange = Physics.CheckSphere(position, sightRange, playerMask)
                           && Mathf.Abs(Vector3.Angle(transform.forward, playerDirection)) < sightAngle / 2
                           && !Physics.Raycast(position, playerDirection, sightRange, obstacleMask);
            
            return inRange;
        }
        
        private bool InFollowRange() 
            => Physics.CheckSphere(transform.position, abandonRange, playerMask);
        
        private bool InAttackRange()
        {
            Vector3 position = transform.position;
            return Physics.CheckSphere(position, attackRange, playerMask) &&
                   (Physics.CheckSphere(position, stopRange, playerMask) ||
                    !Physics.Raycast(position, (target.position - position).normalized, sightRange, obstacleMask));
        }
        
        #endregion
        
        #region AI

        private void UpdateTarget()
        {
            if (target != null)
            {
                Player player = PhotonNetwork.PlayerList.FirstOrDefault(
                    x => ((GameObject) x.TagObject).GetComponent<PhotonView>().ViewID ==
                         target.GetComponent<PhotonView>().ViewID);

                if (player != null)
                {
                    GameObject playerGO = (GameObject) player.TagObject;
                    if (playerGO.GetComponent<HealthController>().Health <= 0 ||
                        playerGO.GetComponent<PlayerController>().isDead)
                    {
                        target = null;
                    }
                }
                
                if (target != null && InFollowRange()) return;
            }


            (GameObject playerInRange, float distance) = (null, 0);
            
            foreach (Player playerPhoton in PhotonNetwork.PlayerList)
            {
                GameObject player = playerPhoton.TagObject as GameObject;
                
                if (player == null || player.GetComponent<PlayerController>().isDead || player.GetComponent<HealthController>().Health <= 0) 
                    continue;
                
                var pc = player.GetComponent<PlayerController>();
                var hc = player.GetComponent<HealthController>();
                if (player == null || pc.isDead || hc.Health <= 0) continue;

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
            agent.speed = patrolSpeed;
            
            if (!hasDestination) SearchForDestination();
            if (hasDestination) agent.SetDestination(destination);

            // AI has reached the destination
            if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance == 0) hasDestination = false;
        }

        private void Chase()
        {
            agent.speed = chaseSpeed;
            Vector3 enemyPosition = target.position;
            Vector3 targetPosition = new Vector3(enemyPosition.x, transform.position.y, enemyPosition.z);
            agent.SetDestination(targetPosition);
        }

        private void Attack()
        {
            if (target.GetComponent<PlayerController>().isDead)
            {
                target = null;
                return;
            }
            
            Vector3 agentPosition = transform.position;
            Vector3 targetPosition = target.position;
            
            agent.SetDestination(agentPosition);
            transform.LookAt(new Vector3(targetPosition.x, agentPosition.y, targetPosition.z));
            isAttacking = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.CompareTag("Player")) isHit = true;
        }
        
        #endregion

        #region Methods

        private void HandlePlayerDeath(GameObject playerDead)
        {
            int viewID = playerDead.GetComponent<PhotonView>().ViewID;
            photonView.RPC(nameof(TransmitPlayerDeath), RpcTarget.All, viewID);
        }

        [PunRPC]
        private void TransmitPlayerDeath(int viewID)
        {
            if (target != null && target.gameObject.GetComponent<PhotonView>().ViewID == viewID)
            {
                target = null;
                List<GameObject> remainingPlayers = players
                    .Where(player => player != null && 
                                     !player.GetComponent<PlayerController>().isDead && 
                                     player.GetComponent<PhotonView>().ViewID != viewID)
                    .ToList();
                players = remainingPlayers;
            }
        }

        private void HandleEnemyDamaged(GameObject enemyDamaged)
        {
            if (gameObject != enemyDamaged) return;

            isHit = true;
        }

        #endregion

        #region DEBUG

        private void OnDrawGizmos()
        {
            Vector3 position = transform.position;
            float yAngle = transform.eulerAngles.y;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(position, destinationRange);
            
            Gizmos.color = Color.grey;
            Gizmos.DrawWireSphere(position, stopRange);

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
