using System.Collections.Generic;
using Items;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inventory {
	public class InventoryController: StorageController {
		public static InventoryController Instance;

		public static  ItemStack  HeldItem;
		public static  bool       IsHoldingItem = false;
		private static GameObject itemGo;

		public EquipmentSlot HelmetSlot;
		public EquipmentSlot ChestplateSlot;
		public EquipmentSlot LeggingsSlot;
		public EquipmentSlot BootsSlot;
		public EquipmentSlot WeaponSlot;
		public EquipmentSlot ConsumableSlot;

		protected override void Start() {
			base.Start();
			Instance = this;
		}

		[CanBeNull]
		public Item GetEquippedWeapon() {
			return WeaponSlot.HasItem ? WeaponSlot.Item : null;
		}

		[CanBeNull]
		public Item GetEquippedConsumable() {
			return ConsumableSlot.HasItem ? ConsumableSlot.Item : null;
		}
		
		public float GetTotalBonusArmor() {
			float total = 0;
			total += HelmetSlot.HasItem ? (HelmetSlot.Item as ItemArmor).Armor : 0;
			total += ChestplateSlot.HasItem ? (ChestplateSlot.Item as ItemArmor).Armor : 0;
			total += LeggingsSlot.HasItem ? (LeggingsSlot.Item as ItemArmor).Armor : 0;
			total += BootsSlot.HasItem ? (BootsSlot.Item as ItemArmor).Armor : 0;
			return total;
		}

		public float GetTotalBonusHealth() {
			float total = 0;
			total += HelmetSlot.HasItem ? (HelmetSlot.Item as ItemArmor).Health : 0;
			total += ChestplateSlot.HasItem ? (ChestplateSlot.Item as ItemArmor).Health : 0;
			total += LeggingsSlot.HasItem ? (LeggingsSlot.Item as ItemArmor).Health : 0;
			total += BootsSlot.HasItem ? (BootsSlot.Item as ItemArmor).Health : 0;
			return total;
		}

		public static void SetHeldItem(ItemStack stack) {
			HeldItem                                   = stack;
			IsHoldingItem                              = true;
			itemGo                                     = new GameObject();
			itemGo.AddComponent<Image>().sprite        = HeldItem.Item.Icon;
			itemGo.GetComponent<Image>().raycastTarget = false;
			itemGo.transform.SetParent(Instance.transform);
		}

		public static void UnsetHeldItem() {
			HeldItem      = default;
			IsHoldingItem = false;
			Destroy(itemGo);
			itemGo = null;
		}

		private void Update() {
			if (!IsHoldingItem) {
				return;
			}

			// itemGo.transform.localPosition = Input.mousePosition - transform.localPosition;
			itemGo.transform.position = Mouse.current.position.ReadValue();
		}
	}
}