using UnityEngine;

namespace Items {
	[CreateAssetMenu(fileName = "NewWeapon", menuName = "Item/Weapon", order = 1)]
	public class ItemWeapon: Item {
		public float Damage;
		public int   MaxAmmo;
		public float ReloadTime;
		public float FireRate;

		public ItemWeapon() {
			this.ItemType = ItemType.Weapon;
		}

		public override string GetStats() {
			string stats = $"Damage: {Damage} - ";
			stats += $"Max Ammo: {MaxAmmo} - ";
			stats += $"Fire Rate: {FireRate:.2f}/s - ";
			stats += $"Reload Time: {ReloadTime:.2f}s";
			return stats;
		}
	}
}