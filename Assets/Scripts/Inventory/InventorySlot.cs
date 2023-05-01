using Items;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory {
	public class InventorySlot: MonoBehaviour, IPointerClickHandler {
		[CanBeNull] public Item Item  = null;
		public             int  Count = 0;

		public TextMeshProUGUI CountText;

		protected Image  image;
		public    Sprite DefaultSprite;

		public bool HasItem = false;

		public virtual void Start() {
			image = GetComponent<Image>();

			if (Item != null) {
				ItemStack stack = new() {Item = Item, Count = Count};
				HasItem        = true;
				Item           = stack.Item;
				image.sprite   = stack.Item.Icon;
				CountText.text = Count.ToString();
				CountText.gameObject.SetActive(true);
			}
			else {
				image.sprite = DefaultSprite;

				Color color = image.color;
				color.a     = .5f;
				image.color = color;
			}
		}

		public void OnHover() {
			if (!HasItem) {
				return;
			}
			InventoryController.Instance.ShowItemDescription(this);
		}

		public void OnStopHover() {
			if (!HasItem) {
				return;
			}
			InventoryController.Instance.ClearItemDescription();
		}

		public void OnLeftClick() {
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

		public void OnRightClick() {
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

		public void RemoveItem() {
			HasItem      = false;
			Count        = 0;
			Item         = null;
			image.sprite = DefaultSprite;
			CountText.gameObject.SetActive(false);

			Color color = image.color;
			color.a     = .5f;
			image.color = color;
		}

		public virtual void AddItem(ref ItemStack stack) {
			if (HasItem && stack.Item.Id != Item.Id) {
				return;
			}

			int addCount = stack.Count;
			if (stack.Item.ItemType == ItemType.Consumable) {
				ItemConsumable cons = (ItemConsumable) stack.Item;

				if (Count + stack.Count > cons.MaxStack) {
					addCount -= Count + stack.Count - cons.MaxStack;
				}
			}
			stack.Count -= addCount;
			Count       += addCount;

			HasItem        = true;
			Item           = stack.Item;
			image.sprite   = stack.Item.Icon;
			CountText.text = Count.ToString();
			CountText.gameObject.SetActive(true);

			Color color = image.color;
			color.a     = 1f;
			image.color = color;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Debug.Log("ON POINTER CLICKED");
			if (eventData.button == PointerEventData.InputButton.Left) {
				OnLeftClick();
			}
			if (eventData.button == PointerEventData.InputButton.Right) {
				OnRightClick();
			}
		}
	}
}