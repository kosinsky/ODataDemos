using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AggregationSample.Models
{
    public class DefaultDataRespository : IDataRepository
    {
        private IList<Customer> _customers;

        private IList<Order> _orders;

        private void EnsureData()
        {
            if (_customers == null)
            {
                Address[] addresses = new Address[]
                {
                    new Address
                    {
                        Street = "145TH AVE",
                        City = "Redonse",
                        ZipCode = new ZipCode { Id = 71, DisplayName = "aebc" }
                    },
                    new BillAddress // Bill
                    {
                        FirstName = "Peter",
                        LastName = "Jok",
                        Street = "Main ST",
                        City = "Issaue",
                        ZipCode = new ZipCode { Id = 61, DisplayName = "yxbc" }
                    },
                    new Address
                    {
                        Street = "32ST NE",
                        City = "Bellewe",
                        ZipCode = new ZipCode { Id = 81, DisplayName = "bexc" }
                    }
                };

                _orders = new List<Order>
                {
                    new Order
                    {
                        Id = 11,
                        Title = "Redonse",
                        TotalAmount = 21,
                    },
                    new Order
                    {
                        Id = 12,
                        Title = "Bellewe",
                        TotalAmount = 22,
                    },
                    new Order
                    {
                        Id = 13,
                        Title = "Newcastle",
                        TotalAmount = 23
                    }
                };

                _customers = new List<Customer>
                {
                    new Customer
                    {
                        Id = 1,
                        Name = "Balmy",
                        Emails = new List<string> { "E1", "E3", "E2" },
                        HomeAddress = addresses[0],
                        FavoriteAddresses = addresses,
                        PersonOrder = _orders[0],
                        Orders = _orders.Take(2).ToList()
                    },
                    new Customer
                    {
                        Id = 2,
                        Name = "Chilly",
                        Emails = new List<string> { "E8", "E7", "E9" },
                        HomeAddress = addresses[1],
                        FavoriteAddresses = addresses,
                        PersonOrder = _orders[2],
                        Orders = _orders.Skip(1).ToList()
                    },
                };

                foreach (var customer in _customers)
                {
                    foreach(var order in customer.Orders)
                    {
                        order.Customer = customer;
                    }
                }
            }
        }

        public IEnumerable<Customer> GetCustomers()
        {
            EnsureData();

            return _customers;
        }

        public IEnumerable<Order> GetOrders()
        {
            EnsureData();

            return _orders;
        }
    }
}
