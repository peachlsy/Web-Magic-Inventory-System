using System.ComponentModel.DataAnnotations;

namespace WDTA2.Models.BusinessModels
{
    public class StoreInventory
    {
        public int StoreID { get; set; }
        public Store Store { get; set; }

        public int ProductID { get; set; }
        public Product Product { get; set; }


        public int StockLevel { get; set; }
    }
}
