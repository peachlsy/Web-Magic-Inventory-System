using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WDTA2.Models.BusinessModels
{
    public class OrderItem
    {
        public int OrderItemID { set; get; }

        [ForeignKey("Order")]
        public int OrderID { set; get; }
        public Order Order { get; set; }

        [ForeignKey("Product")]
        public int ProductID { set; get; }
        public Product Product { get; set; }

        [ForeignKey("Store")]
        public int StoreID { get; set; }
        public Store Store { get; set; }

        public int Quantity { set; get; }

    }
}
