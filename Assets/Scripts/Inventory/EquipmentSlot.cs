using Items;
using NaughtyAttributes;

namespace Inventory {
	public class EquipmentSlot: InventorySlot {
		public ItemType AcceptType = ItemType.Default;

		[ShowIf(nameof(AcceptType), ItemType.Armor)]
		public ArmorType ArmorType;

		public override void Start() {
			base.Start();
		}


		public override void AddItem(ref ItemStack stack) {
			if (AcceptType != ItemType.Default &&
			    stack.Item.ItemType != AcceptType) {
				return;
			}
			if (stack.Item.ItemType == ItemType.Armor &&
			    ((ItemArmor) stack.Item).ArmorType != ArmorType) {
				return;
			}
			
			base.AddItem(ref stack);
		}
	}
}