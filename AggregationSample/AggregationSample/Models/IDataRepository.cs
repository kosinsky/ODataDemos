using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationSample.Models
{
    public interface IDataRepository
    {
        IEnumerable<Customer> GetCustomers();

        IEnumerable<Order> GetOrders();
    }
}
