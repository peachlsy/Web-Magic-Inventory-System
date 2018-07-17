using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WDTA2.Models.BusinessModels
{
    public class StockReqViewModel
    {
        [Required]
        [Display(Name= "ID")]
        public int StockRequestID { get; set; }

        [Required]
        [Display(Name = "Store")]
        public string StoreName { get; set; }

        [Required]
        [Display(Name = "Product")]
        public string ProductName { get; set; }

        [Required]
        public int Quantity { get; set; }

    }
}
