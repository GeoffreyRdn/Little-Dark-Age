using System;
using UnityEngine;

public enum ItemType {
	Default,
	Weapon,
	Armor,
	Consumable
}

namespace Items {
	[Serializable]
	public abstract class Item: ScriptableObject {
		public int BuyCost;
		public int SellCost => BuyCost / 2;

		public string Name;
		public int    Id;
		public string Description;

		public Sprite Icon;

		[HideInInspector] public ItemType ItemType = ItemType.Default;

		public abstract string GetStats();
	}
}