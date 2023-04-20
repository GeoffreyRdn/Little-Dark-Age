using UnityEngine;

public enum ArmorType {
	Default,
	Helmet,
	Chestplate,
	Leggings,
	Boots
}

namespace Items {
	[CreateAssetMenu(fileName = "NewArmor", menuName = "Item/Armor", order = 0)]
	public class ItemArmor: Item {
		public float Health;
		public float Armor;

		public ArmorType ArmorType = ArmorType.Default;

		public ItemArmor() {
			this.ItemType = ItemType.Armor;
		}

		public override string GetStats() {
			string stats = $"Health: {Health} - ";
			stats += $"Armor: {Armor}";
			return stats;
		}
	}
}