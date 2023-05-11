using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Health;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Enemies
{
    public class Boss : MonoBehaviour
    {
        #region Variables

        #region Editor
        
        [Tag] [SerializeField] private string playerTag;

        
        [BoxGroup("FX")] [SerializeField] private ParticleSystem sparkle;
        [BoxGroup("FX")] [SerializeField] private ParticleSystem stageChangeFX;
        
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
        private bool isRunning;
        private bool isChangingStage;

        
        
        
        #endregion

        #region Animations

        private static readonly int IdleAnimation = Animator.StringToHash("idle");
        private static readonly int RunAnimation = Animator.StringToHash("run");
        private static readonly int AttackAnimation = Animator.StringToHash("attack");
        private static readonly int HitAnimation = Animator.StringToHash("hit");
        private static readonly int StageAnimation = Animator.StringToHash("stage change");
        private static readonly int ComboAttackAnimation = Animator.StringToHash("melee_combo");
        private static readonly int TurningAttackAnimation = Animator.StringToHash("melee_360");
        private float cameraShakeMagnitude = 0.2f;
        private float cameraShakeDuration = 0.1f;
        private float cameraShakeInterval = 0.1f;
        private float nextCameraShakeTime;
        
        private float lockedUntil;
        private int currentState;
        private float initialHealth;
        private float health;
        private HealthController healthController;
        private bool stage1Enabled = false;
        private bool stage2Enabled = false;
        
        private Animator animator;

        #endregion

        private NavMeshAgent agent;
        private GameObject[] players;
        
        private Vector3 destination;
        private bool hasDestination;
        
        #endregion

        #region Start - Update

        private void Start()
        {
            healthController = GetComponent<HealthController>();
            Debug.Log("health = " + healthController.Health);
            initialHealth = healthController.MaxHealth;
            
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            players = GameObject.FindGameObjectsWithTag(playerTag);
        }
        
        private void Update()
        {
            health = healthController.Health;
            
            var state = GetState();
            if (state != AttackAnimation && state != TurningAttackAnimation && state != ComboAttackAnimation)
            {
                UpdateTarget();

                if (target != null)
                {
                    if (InAttackRange()) Attack();
                    else Chase();
                }
            }
            
            if (state == currentState) return;

            animator.CrossFade(state, 0, 0);
            currentState = state;
            
            //Sword FX
            var emissionModule = sparkle.emission;
            var rateOverTime = emissionModule.rateOverTime;
            if (isAttacking)
            {
                rateOverTime.constant = 50;
                emissionModule.rateOverTime = rateOverTime;
            }
            else
            {
                rateOverTime.constant = 1.5f;
                emissionModule.rateOverTime = rateOverTime;
            }

            if (isRunning)
            {
                if (Time.time >= nextCameraShakeTime)
                {
                    nextCameraShakeTime = Time.time + cameraShakeInterval;
                }
            }

            if (isHit)
            {
                health -= 25;
            }

            if (!stage1Enabled)
            {
                //stage 1
                if (health <= initialHealth * 0.50f)
                {

                    chaseSpeed *= 1.25f;
                    Debug.Log("changing stage");
                    StageChange1();
                    stage1Enabled = true;
                }
            }

            //stage 2
            if (!stage2Enabled)
            {
                //stage 1
                if (health <= initialHealth * 0.50f)
                {
                    chaseSpeed *= 1.25f;
                    Debug.Log("changing stage 2222222");
                    StageChange1();
                    stage2Enabled = true;
                }
            }
            
            if (health <= 0)
            {
                //DEATH
            }
        }

        #endregion

        #region StageChange

        private void StageChange1()
        {
            StageChangeAnimation();
            Debug.Log("StageChange1");
        }


        private void StageChangeAnimation()
        {
            Vector3 agentPosition = transform.position;
            Vector3 targetPosition = target.position;
            
            agent.SetDestination(agentPosition);
            transform.LookAt(new Vector3(targetPosition.x, agentPosition.y, targetPosition.z));
            isChangingStage = true;
        }

        #endregion
        
        #region Methods Anim
        
        private int GetState()
        {
            
            if (currentState == AttackAnimation) isAttacking = false;
            if (currentState == ComboAttackAnimation) isAttacking = false;
            if (currentState == TurningAttackAnimation) isAttacking = false;
            if (currentState == StageAnimation) isChangingStage= false;
            if (currentState == HitAnimation) isHit = false;
            if (currentState == RunAnimation) isRunning = true;

            if (Time.time < lockedUntil) return currentState;

            if (isAttacking)
            {
                //different attack types
                int randomAttack = Random.Range(1, 4);
                switch (randomAttack)
                {
                    case 1:
                        return LockState(AttackAnimation, 1.5f);
                    case 2:
                        return LockState(TurningAttackAnimation, 2.2f);
                    case 3:
                        return LockState(ComboAttackAnimation, 4f);
                }
            }

            if (isChangingStage)
            {
                Debug.Log("stage animation launched");
                isChangingStage = false;
                return LockState(StageAnimation, 2.8f);
            }
            if (isHit) return LockState(HitAnimation, .7f);
            
            return agent.velocity.magnitude < .15f 
                ? IdleAnimation
                : RunAnimation;

            int LockState(int s, float t)
            {
                lockedUntil = Time.time + t;
                return s;
            }
        }
        
        

        private void ShakeCamera()
        {
            
            // Get all the cameras in the scene
            GameObject[] cameras = GameObject.FindGameObjectsWithTag("virtualCamera");

            // Shake each camera individually
            foreach (GameObject camera in cameras)
            {
                CinemachineImpulseSource impulse = camera.GetComponent<CinemachineImpulseSource>();
                
                StartCoroutine(ShakeCameraCoroutine(impulse));
            }
        }

        private IEnumerator ShakeCameraCoroutine(CinemachineImpulseSource impulse)
        {
                yield return null;
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
        

        private void Chase()
        {
            agent.speed = chaseSpeed;
            Vector3 enemyPosition = target.position;
            Vector3 targetPosition = new Vector3(enemyPosition.x, transform.position.y, enemyPosition.z);
            agent.SetDestination(targetPosition);
        }

        private void Attack()
        {
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
