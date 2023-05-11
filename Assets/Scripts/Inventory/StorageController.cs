using System.Collections.Generic;
using System.Linq;
using Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inventory {
	public abstract class StorageController: MonoBehaviour {
		public GameObject      ItemDescriptionGo;
		public TextMeshProUGUI ItemDescriptionText;

		public    GameObject      Inventory;
		protected List<ItemSlot>  slots = new();
		public    List<ItemStack> StartingItems;

		protected virtual void Start() {
			ItemDescriptionGo.SetActive(false);
			slots = Inventory.GetComponentsInChildren<ItemSlot>().ToList();
			foreach (ItemSlot itemSlot in slots) {
				itemSlot.StorageController = this;
			}

			for (int i = 0 ; i < StartingItems.Count && i < slots.Count ; i++) {
				slots[i].SetItem(StartingItems[i]);
			}
		}

		public void ResetStorage() {
			slots.ForEach(slot => slot.RemoveItem());
			slots.Clear();
		}

		public bool AddItem(ref ItemStack item) {
			for (int i = 0 ; i < slots.Count && item.Count > 0 ; i++) {
				slots[i].AddItem(ref item);
			}

			return item.Count == 0;
		}

		public void ShowItemDescription(ItemSlot slot) {
			if (!slot.HasItem) {
				return;
			}
			float verticalOffset = (float) 1.2 * slot.GetComponent<RectTransform>().rect.height;
			ItemDescriptionGo.transform.localPosition = slot.transform.localPosition +
			                                            new Vector3(0, verticalOffset, 0);
			// ItemDescriptionText.text = slot.Item!.Description;
			string description = slot.Item!.Name + "\n";
			description              += slot.Item.Description + "\n";
			description              += slot.Item.GetStats();
			ItemDescriptionText.text =  description;
			ItemDescriptionGo.SetActive(true);
		}

		public void ClearItemDescription() {
			ItemDescriptionGo.SetActive(false);
		}

		public void OpenOrCloseInventory() {
			Debug.Log("Opening inventory -> " + !gameObject.activeSelf);
			gameObject.SetActive(!gameObject.activeInHierarchy);
		}
	}
}