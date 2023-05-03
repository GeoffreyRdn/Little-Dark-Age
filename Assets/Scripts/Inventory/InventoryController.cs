using Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Inventory {
	public class InventoryController: MonoBehaviour {
		public static InventoryController Instance;

		public static  ItemStack  HeldItem;
		public static  bool       IsHoldingItem = false;
		private static GameObject itemGo;

		public GameObject      ItemDescriptionGo;
		public TextMeshProUGUI ItemDescriptionText;

		public EquipmentSlot HelmetSlot;
		public EquipmentSlot ChestplateSlot;
		public EquipmentSlot LeggingsSlot;
		public EquipmentSlot BootsSlot;
		public EquipmentSlot WeaponSlot;
		public EquipmentSlot ConsumableSlot;

		private void Start() {
			Instance = this;
			ItemDescriptionGo.SetActive(false);
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

		public void ShowItemDescription(InventorySlot slot) {
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

		private void Update() {
			if (!IsHoldingItem) {
				return;
			}

			// itemGo.transform.localPosition = Input.mousePosition - transform.localPosition;
			itemGo.transform.position = Mouse.current.position.ReadValue();
		}

		public void OpenOrCloseInventory()
		{
			Debug.Log("Opening inventory -> " + !gameObject.activeInHierarchy);
			gameObject.SetActive(!gameObject.activeInHierarchy);
		}
	}
}