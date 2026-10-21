using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterEquipment
{
    public long CharacterId { get; set; }

    public string Slot { get; set; } = null!;

    public long InventoryId { get; set; }

    public int? SupplyQuantity { get; set; }

    public virtual Character Character { get; set; } = null!;

    public virtual CharacterInventory Inventory { get; set; } = null!;
}
