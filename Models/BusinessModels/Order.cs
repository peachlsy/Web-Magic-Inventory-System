using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace WDTA2.Models.BusinessModels
{
    public class Order
    {
        public int OrderID { set; get; }


        public string OustomerName { set; get; }

        public DateTime CreaTime { set; get; }

    }
}
