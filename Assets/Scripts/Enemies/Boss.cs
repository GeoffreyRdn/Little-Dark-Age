using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Health;
using NaughtyAttributes;
using Photon.Pun;
using Unity.VisualScripting;
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
        
        [BoxGroup("SoundFX")] [SerializeField] private AudioClip attack1;
        [BoxGroup("SoundFX")] [SerializeField] private AudioClip attack2;
        [BoxGroup("SoundFX")] [SerializeField] private AudioClip takeoff;
        [BoxGroup("SoundFX")] [SerializeField] private AudioClip deathSound;
        [BoxGroup("SoundFX")] [SerializeField] private AudioClip swordSwoosh;
        
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
        private float lastLifeLevel;

        #endregion

        #region Animations

        private static readonly int IdleAnimation = Animator.StringToHash("idle");
        private static readonly int RunAnimation = Animator.StringToHash("run");
        private static readonly int AttackAnimation = Animator.StringToHash("attack");
        private static readonly int HitAnimation = Animator.StringToHash("hit");
        private static readonly int StageAnimation = Animator.StringToHash("stage change");
        private static readonly int ComboAttackAnimation = Animator.StringToHash("melee_combo");
        private static readonly int TurningAttackAnimation = Animator.StringToHash("melee_360");
        private static readonly int DeathAnimation = Animator.StringToHash("death");
        private float cameraShakeMagnitude = 0.2f;
        private float cameraShakeDuration = 0.1f;
        private float cameraShakeInterval = 0.1f;
        private float nextCameraShakeTime;
        
        private float lockedUntil;
        private int currentState;
        private float initialHealth;
        // private float health;
        private HealthController healthController;
        private bool stage1Enabled = false;
        private bool stage2Enabled = false;
        private bool isDead;
        
        private AudioSource audioSource;
        private Animator animator;

        #endregion

        private NavMeshAgent agent;
        private List<GameObject> players;
        
        private Vector3 destination;
        private bool hasDestination;
        private PhotonView photonView;
        
        #endregion

        #region Events
        
        private void OnEnable()
        {
            HealthController.onPlayerDeath += HandlePlayerDeath;
            HealthController.onBossDeath += HandleBossDeath;
            DamageBehavior.onEnemyDamaged += HandleEnemyDamaged;
        }

        private void OnDisable()
        {
            HealthController.onPlayerDeath -= HandlePlayerDeath;
            HealthController.onBossDeath -= HandleBossDeath;
            DamageBehavior.onEnemyDamaged -= HandleEnemyDamaged;
        }
        
        private void HandleEnemyDamaged(GameObject enemyDamaged)
        {
            if (gameObject == enemyDamaged) isHit = true;
            StartCoroutine(HandleBossChangeStage());
        }

        private IEnumerator HandleBossChangeStage()
        {
            // TIME TO UPDATE HEALTH
            yield return new WaitForSeconds(.2f);

            var health = healthController.Health;
            
            //stage 1
            if (!stage1Enabled && health <= initialHealth * 0.50f)
            {

                chaseSpeed *= 1.25f;
                Debug.Log("changing stage");
                StageChange1();
                stage1Enabled = true;
            }
            
            //stage 2
            if (!stage2Enabled && health <= initialHealth * 0.25f)
            {
                chaseSpeed *= 1.25f;
                Debug.Log("changing stage 2");
                StageChange1();
                stage2Enabled = true;
            }

            if (health <= lastLifeLevel)
            {
                Debug.Log("PLAYING ATTACK2 SOUND");
                audioSource.PlayOneShot(attack2);
                photonView.RPC(nameof(SendAttack2Audio), RpcTarget.Others);
                lastLifeLevel = health;
            }
        }

        private void HandleBossDeath()
        {
            isDead = true;
            audioSource.PlayOneShot(deathSound);
            photonView.RPC(nameof(SendDeathAudio), RpcTarget.Others);
            StartCoroutine(DestroyBoss());
        }

        private IEnumerator DestroyBoss()
        {
            yield return new WaitForSeconds(1);
            PhotonNetwork.Destroy(gameObject);
        }
        
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
            }
            
            List<GameObject> remainingPlayers = players
                .Where(player => player != null && 
                                 !player.GetComponent<PlayerController>().isDead && 
                                 player.GetComponent<PhotonView>().ViewID != viewID)
                .ToList();
            players = remainingPlayers;
        }
        
        #endregion
        
        #region Start - Update

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            healthController = GetComponent<HealthController>();
            initialHealth = healthController.MaxHealth;
            Debug.Log("BOSS HEALTH " + healthController.Health);

            photonView = GetComponent<PhotonView>();
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            players = GameObject.FindGameObjectsWithTag(playerTag).ToList();
        }
        
        private void Update()
        {
            if (isDead)
            {
                if (currentState != DeathAnimation)
                {
                    currentState = DeathAnimation;
                    animator.CrossFade(DeathAnimation, 0, 0);
                }
                return;
            }
            
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
            photonView.RPC(nameof(SendAnimations), RpcTarget.Others, state);
            
            //Sword FX
            /*var emissionModule = sparkle.emission;
            var rateOverTime = emissionModule.rateOverTime;

            bool attacking = currentState == AttackAnimation || currentState == ComboAttackAnimation ||
                             currentState == TurningAttackAnimation;
            
            rateOverTime.constant = attacking ? 50 : 1.5f;
            emissionModule.rateOverTime = rateOverTime;*/

            if (currentState == RunAnimation && Time.time >= nextCameraShakeTime)
            {
                nextCameraShakeTime = Time.time + cameraShakeInterval;
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
            
            Debug.Log("PLAYING TAKEOFF SOUND");
            audioSource.PlayOneShot(takeoff);
            photonView.RPC(nameof(SendTakeOffAudio), RpcTarget.Others);

            agent.SetDestination(agentPosition);
            transform.LookAt(new Vector3(targetPosition.x, agentPosition.y, targetPosition.z));
            isChangingStage = true;
        }

        #endregion
        
        #region Methods Anim
        
        [PunRPC]
        private void SendAnimations(int state)
        {
            if (gameObject == null) return;
            
            animator.CrossFade(state, 0, 0);
            currentState = state;
        }

        [PunRPC]
        private void SendAttack1Audio()
            => audioSource.PlayOneShot(attack1);

        
        [PunRPC]
        private void SendAttack2Audio()
            => audioSource.PlayOneShot(attack2);

        
        [PunRPC]
        private void SendAttackSwooshAudio()
            => audioSource.PlayOneShot(swordSwoosh);

        
        [PunRPC]
        private void SendDeathAudio()
            => audioSource.PlayOneShot(deathSound);

        
        [PunRPC]
        private void SendTakeOffAudio()
            => audioSource.PlayOneShot(takeoff);
        

        private int GetState()
        {
            if (currentState == AttackAnimation || currentState == ComboAttackAnimation || currentState == TurningAttackAnimation)
                isAttacking = false;

            if (currentState == StageAnimation) isChangingStage= false;
            if (currentState == HitAnimation) isHit = false;
            if (currentState == RunAnimation) isRunning = true;

            if (Time.time < lockedUntil) return currentState;

            if (isAttacking)
            {
                                
                Debug.Log("PLAYING SWOOSH");
                audioSource.PlayOneShot(swordSwoosh);
                photonView.RPC(nameof(SendAttackSwooshAudio), RpcTarget.Others);
                int randomSound = Random.Range(1, 3);
                
                switch (randomSound)
                {
                    case 1:
                        Debug.Log("PLAYING ATTACK1");
                        audioSource.PlayOneShot(attack1, 100);
                        photonView.RPC(nameof(SendAttack1Audio), RpcTarget.Others);

                        break;
                    case 2:
                        Debug.Log("PLAYING ATTACK2");
                        audioSource.PlayOneShot(attack2);
                        photonView.RPC(nameof(SendAttack2Audio), RpcTarget.Others);

                        break;
                }
                
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
