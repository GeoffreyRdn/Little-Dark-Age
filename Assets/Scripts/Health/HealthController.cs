using System;
using Enemies;
using NaughtyAttributes;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

namespace Health {
	public class HealthController: MonoBehaviour {
		[BoxGroup("Health"), SerializeField] private float health         = 100f;
		[BoxGroup("Health"), SerializeField] private float maxHealth      = 100f;
		[BoxGroup("Health"), SerializeField] private float armor          = 0f;
		[BoxGroup("Health"), SerializeField] private float healMultiplier = 1f;

		[SerializeField] [Tag] private string playerTag;

		public delegate void OnPlayerDeath(GameObject player);
		public delegate void OnDungeonComplete();
		

		public static OnPlayerDeath onPlayerDeath;
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

			Debug.Log($"Health after damage: {health}");
		}

		public void Heal(float amount) {
			amount *= healMultiplier;
			health =  Mathf.Clamp(health + amount, 0, maxHealth);
			Debug.Log($"Health after heal: {health}");
		}

		private void Kill() {
			Debug.Log("Killing target");

			if (gameObject.CompareTag(playerTag))
			{
				onPlayerDeath?.Invoke(gameObject);
			}
			
			else
			{
				photonView.RPC(nameof(KillEnemy), RpcTarget.MasterClient);
			}
		}

		[PunRPC]
		private void KillEnemy()
		{
			PhotonNetwork.Destroy(gameObject);
			EnemyInstantiation.EnemiesRemaining--;
			Debug.Log("ENEMY KILLED , REMAINING : " + EnemyInstantiation.EnemiesRemaining);

			if (EnemyInstantiation.EnemiesRemaining == 0)
			{
				Debug.Log("DUNGEON COMPLETE !");
				onDungeonComplete?.Invoke();
			}
		}
	}
}