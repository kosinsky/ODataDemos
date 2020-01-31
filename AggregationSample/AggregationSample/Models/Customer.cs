using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationSample.Models
{
    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public IList<string> Emails { get; set; }

        public Address HomeAddress { get; set; }

        public IList<Address> FavoriteAddresses { get; set; }

        public Order PersonOrder { get; set; }

        public IList<Order> Orders { get; set; }
    }
}
