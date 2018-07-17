using System.Collections.Generic;

namespace WDTA2.Models.BusinessModels
{
    public class Store
    {
        public int StoreID { get; set; }
        public string Name { get; set; }

        // StoreInventory属性持有多个entities，则需实现ICollection接口（1对多关系）
        public ICollection<StoreInventory> StoreInventory { get; } = new List<StoreInventory>();
    }
}
