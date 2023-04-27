using System;
using Items;

namespace Inventory {
	[Serializable]
	public struct ItemStack {
		public Item Item;
		public int  Count;
	}
}