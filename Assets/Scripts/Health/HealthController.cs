using System;
using NaughtyAttributes;
using Photon.Pun;
using UnityEngine;

namespace Health {
	public class HealthController: MonoBehaviour {
		[BoxGroup("Health"), SerializeField] private float health         = 100f;
		[BoxGroup("Health"), SerializeField] private float maxHealth      = 100f;
		[BoxGroup("Health"), SerializeField] private float armor          = 0f;
		[BoxGroup("Health"), SerializeField] private float healMultiplier = 1f;

		public float Health         => health;
		public float MaxHealth      => maxHealth;
		public float Armor          => armor;
		public float HealMultiplier => healMultiplier;

		private void Update() {
			if (Input.GetKeyDown(KeyCode.S)) {
				Damage(1);
			}

			if (Input.GetKeyDown(KeyCode.W)) {
				Heal(1);
			}

			if (Input.GetKeyDown(KeyCode.D)) {
				AddArmor(1);
				Debug.Log($"Armor: {armor}");
			}

			if (Input.GetKeyDown(KeyCode.A)) {
				AddArmor(-1);
				Debug.Log($"Armor: {armor}");
			}
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

		public void Kill() {
			Debug.Log("Kill player");
			Destroy(gameObject);
		}
	}
}