using TMPro;
using UnityEngine;

namespace Inventory {
	public class ShopSlot: ItemSlot {
		public int             Price;
		public TextMeshProUGUI PriceText;

		public override void Start() {
			base.Start();
			if (HasItem) {
				SetPrice(Item!.BuyCost * Count);
			}
		}

		public void SetPrice(int price) {
			Price          = price;
			PriceText.text = $"${Price}";
		}

		public override void RemoveItem() {
			base.RemoveItem();
			PriceText.gameObject.SetActive(false);
		}

		public override void SetItem(ItemStack stack) {
			base.SetItem(stack);
			SetPrice(Item!.BuyCost * Count);
		}

		public override void OnRightClick() { }

		public override void OnLeftClick() {
			if (!HasItem) {
				return;
			}

			int       due   = Count * Item!.BuyCost;
			ItemStack stack = new() {Item = Item, Count = Count};
			InventoryController.Instance.AddItem(ref stack);
			due -= stack.Count * Item!.BuyCost;
			RemoveItem();
			// TODO: add reference to player to subtract golds
		}
	}
}