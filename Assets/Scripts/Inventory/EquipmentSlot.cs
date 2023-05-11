using Items;
using NaughtyAttributes;

namespace Inventory {
	public class EquipmentSlot: InventorySlot {
		public ItemType AcceptType = ItemType.Default;

		[ShowIf(nameof(AcceptType), ItemType.Armor)]
		public ArmorType ArmorType;

		public override void AddItem(ref ItemStack stack) {
			if (AcceptType != ItemType.Default &&
			    stack.Item.ItemType != AcceptType) {
				return;
			}
			if (stack.Item.ItemType == ItemType.Armor &&
			    ((ItemArmor) stack.Item).ArmorType != ArmorType) {
				return;
			}

			if (stack.Item.ItemType is ItemType.Weapon
			    && !HasItem) {
				InventoryController.Instance.player.ChangeEquippedWeapon((ItemWeapon) stack.Item);
			}

			base.AddItem(ref stack);
		}

		public override void RemoveItem() {
			InventoryController.Instance.player.ChangeEquippedWeapon(null);
			base.RemoveItem();
		}
	}
}