using System;
using NaughtyAttributes;
using UnityEngine;

public enum ConsumableType {
	Default,
	Heal,
	SpeedBoost,
	Defense
}

namespace Items {
	[CreateAssetMenu(fileName = "NewConsumable", menuName = "Item/Consumable", order = 2)]
	public class ItemConsumable: Item {
		[Min(1)] public int            MaxStack;
		public          ConsumableType ConsumableType = ConsumableType.Default;

		[ShowIf(nameof(ConsumableType), ConsumableType.Heal)]
		public float HealthGain;

		[ShowIf(nameof(ConsumableType), ConsumableType.SpeedBoost)]
		public float SpeedGain;

		[ShowIf(nameof(ConsumableType), ConsumableType.SpeedBoost)]
		public float SpeedDuration;

		[ShowIf(nameof(ConsumableType), ConsumableType.Defense)]
		public float ArmorGain;

		[ShowIf(nameof(ConsumableType), ConsumableType.Defense)]
		public float ArmorDuration;

		public ItemConsumable() {
			this.ItemType = ItemType.Consumable;
		}

		public override string GetStats() {
			string stats;
			switch (ConsumableType) {
				case ConsumableType.Heal:
					stats = $"Heal Amount: {HealthGain} - ";
					break;
				case ConsumableType.SpeedBoost:
					stats =  $"Speed Amount: {SpeedGain} - ";
					stats += $"Duration: {SpeedDuration:.2f} - ";
					break;
				case ConsumableType.Defense:
					stats =  $"Armor Amount: {ArmorGain} - ";
					stats += $"Duration: {ArmorDuration:.2f} - ";
					break;
				case ConsumableType.Default:
				default:
					throw new ArgumentOutOfRangeException();
			}
			stats += $"Max Stack: {MaxStack}";
			return stats;
		}
	}
}