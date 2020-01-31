using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationSample.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public decimal TotalAmount { get; set; }

        public Customer Customer { get; set; }
    }
}
