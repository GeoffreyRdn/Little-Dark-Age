using System;
using UnityEngine;

namespace Inventory {
	public class ShopController: StorageController {
		public static ShopController Instance;
		public        GameObject     SlotPrefab;

		protected override void Start() {
			AddSlots(StartingItems.Count);
			base.Start();
			Instance = this;
		}

		public void AddSlots(int n) {
			for (int i = 0 ; i < n ; i++) {
				GameObject slot   = Instantiate(SlotPrefab, Inventory.transform);
				ShopSlot   script = slot.GetComponent<ShopSlot>();
				script.StorageController = this;
				slots.Add(script);
			}
		}

		public void AddSlot(ItemStack item) {
			GameObject slot   = Instantiate(SlotPrefab, Inventory.transform);
			ShopSlot   script = slot.GetComponent<ShopSlot>();
			script.StorageController = this;
			// script.SetItem(item);
		}
		
		public void OpenOrCloseShopMenu()
		{
			Debug.Log("Opening shop -> " + !gameObject.activeInHierarchy);
			gameObject.SetActive(!gameObject.activeInHierarchy);
		}
	}
}