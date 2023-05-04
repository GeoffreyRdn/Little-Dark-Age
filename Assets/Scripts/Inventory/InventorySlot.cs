using Items;
using UnityEngine;

namespace Inventory {
	public class InventorySlot: ItemSlot {

		public override void OnLeftClick() {
			if (HasItem && !InventoryController.IsHoldingItem) {
				InventoryController.SetHeldItem(new ItemStack() {Item = Item, Count = Count});
				RemoveItem();
			}
			else if (InventoryController.IsHoldingItem) {
				AddItem(ref InventoryController.HeldItem);
				if (InventoryController.HeldItem.Count == 0) {
					InventoryController.UnsetHeldItem();
				}
			}
		}

		public override void OnRightClick() {
			if (!HasItem || Item!.ItemType != ItemType.Consumable) {
				return;
			}

			ItemConsumable consumable = (ItemConsumable) Item;
			// TODO: Add reference to player to apply effects of the consumed item
			Debug.Log($"Consuming {consumable.Name} {consumable.ConsumableType}");
			Count--;
			CountText.text = Count.ToString();
			if (Count == 0) {
				RemoveItem();
			}
		}
	}
}