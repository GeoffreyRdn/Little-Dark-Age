using System;
using System.Collections;
using Enemies;
using NaughtyAttributes;
using Photon.Pun;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Health {
	public class HealthController: MonoBehaviour {
		[BoxGroup("Health"), SerializeField] private float health         = 100f;
		[BoxGroup("Health"), SerializeField] private float maxHealth      = 100f;
		[BoxGroup("Health"), SerializeField] private float armor          = 0f;
		[BoxGroup("Health"), SerializeField] private float healMultiplier = 1f;
		
		[BoxGroup("UI"), SerializeField] private TextMeshProUGUI enemiesRemainingText;

		[SerializeField] [NaughtyAttributes.Tag] private string playerTag;

		public delegate void OnPlayerDeath(GameObject player);
		public delegate void OnEnemyDeath(int enemiesRemaining);
		public delegate void OnBossDeath();
		public delegate void OnDungeonComplete();
		

		public static OnPlayerDeath onPlayerDeath;
		public static OnEnemyDeath onEnemyDeath;
		public static OnBossDeath onBossDeath;
		public static OnDungeonComplete onDungeonComplete;

		public float Health         => health;
		public float MaxHealth      => maxHealth;
		public float Armor          => armor;
		public float HealMultiplier => healMultiplier;

		private PhotonView photonView;

		private void Start()
		{
			photonView = gameObject.GetComponent<PhotonView>();
		}

		public void AddArmor(float amount) {
			armor += amount;
		}

		private float ComputeMitigatedDamage(float amount) {
			return (100 / (armor + 100)) * amount;
		}

		public void Damage(float amount) {
			float mitigated = ComputeMitigatedDamage(amount);
			health -= mitigated;
			if (health <= 0) {
				Kill();
			}

			else
			{
				photonView.RPC(nameof(TransmitHealth), RpcTarget.All, health);
			}

			Debug.Log($"Health after damage: {health}");
		}

		public void Heal(float amount) {
			amount *= healMultiplier;
			health =  Mathf.Clamp(health + amount, 0, maxHealth);
			Debug.Log($"Health after heal: {health}");
		}

		public void ResetHealth()
			=> health = maxHealth;

		private void Kill() {

			if (gameObject.CompareTag(playerTag))
			{
				Debug.Log("KILLING PLAYER");
				onPlayerDeath?.Invoke(gameObject);
				photonView.RPC(nameof(KillPlayer), RpcTarget.All);
			}
			
			else
			{
				Debug.Log("CALLING KILL ENEMY");
				photonView.RPC(nameof(KillEnemy), RpcTarget.MasterClient);
			}
		}
		
		[PunRPC]
		private void KillPlayer()
			=> gameObject.GetComponent<PlayerController>().isDead = true;

		[PunRPC]
		private IEnumerator KillEnemy()
		{
			yield return new WaitForSeconds(.2f);

			Debug.Log("KILLING: " + gameObject.name);
				
			if (SceneManager.GetActiveScene().name == "Boss Room")
			{
				Debug.Log("Killing the boss");
				onBossDeath?.Invoke();
				yield break;
			}
			
			EnemyInstantiation.Enemies.Remove(gameObject);
			EnemyInstantiation.EnemiesRemaining--;
			PhotonNetwork.Destroy(gameObject);
			onEnemyDeath?.Invoke(EnemyInstantiation.EnemiesRemaining);

			Debug.Log("ENEMY KILLED , REMAINING : " + EnemyInstantiation.EnemiesRemaining);

			if (EnemyInstantiation.EnemiesRemaining == 0)
			{
				Debug.Log("DUNGEON COMPLETE !");
				onDungeonComplete?.Invoke();
			}
		}
		
		[PunRPC]
		private void TransmitHealth(float health)
		{
			this.health = health;
			gameObject.GetComponentInChildren<HealthBar>()?.UpdateHealthBar(health, maxHealth);
		}
	}
}