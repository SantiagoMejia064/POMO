using System;
using System.Collections.Generic;

namespace Pomo.Data.DTOs
{
    [Serializable]
    public class StoreInventoryDTO
    {
        public List<string> ownedItemIds = new List<string>();
    }
}
