using System;
using Items;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory {
	[RequireComponent(typeof(Image))]
	public abstract class ItemSlot: MonoBehaviour, IPointerClickHandler {
		public StorageController StorageController = null;

		[CanBeNull] public Item Item  = null;
		public             int  Count = 0;

		public TextMeshProUGUI CountText;

		protected Image  image;
		public    Sprite DefaultSprite;

		public bool HasItem = false;

		private void Awake() {
			image = GetComponent<Image>();
		}

		public virtual void Start() {
			// image = GetComponent<Image>();

			if (Item != null) {
				ItemStack stack = new() {Item = Item, Count = Count};
				HasItem      = true;
				Item         = stack.Item;
				image.sprite = stack.Item.Icon;
				CountText.gameObject.SetActive(false);
				if (Count > 1) {
					CountText.text = Count.ToString();
					CountText.gameObject.SetActive(true);
				}
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
			StorageController.ShowItemDescription(this);
		}

		public void OnStopHover() {
			if (!HasItem) {
				return;
			}
			StorageController.ClearItemDescription();
		}

		public virtual void RemoveItem() {
			HasItem      = false;
			Count        = 0;
			Item         = null;
			image.sprite = DefaultSprite;
			CountText.gameObject.SetActive(false);

			Color color = image.color;
			color.a     = .5f;
			image.color = color;

			StorageController.ClearItemDescription();
		}

		public virtual void SetItem(ItemStack stack) {
			Item    = stack.Item;
			Count   = stack.Count;
			HasItem = true;
			// image          = GetComponent<Image>();
			image.sprite = stack.Item.Icon;
			if (Count > 1) {
				CountText.text = Count.ToString();
				CountText.gameObject.SetActive(true);
			}

			Color color = image.color;
			color.a     = 1f;
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

			SetItem(new ItemStack() {Item = stack.Item, Count = Count});
		}

		public void OnPointerClick(PointerEventData eventData) {
			if (eventData.button == PointerEventData.InputButton.Left) {
				OnLeftClick();
			}
			if (eventData.button == PointerEventData.InputButton.Right) {
				OnRightClick();
			}
		}

		public abstract void OnRightClick();

		public abstract void OnLeftClick();
	}
}