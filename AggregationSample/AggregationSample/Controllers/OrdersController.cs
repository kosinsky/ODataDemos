using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AggregationSample.Models;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AggregationSample.Controllers
{
    public class OrdersController : ODataController
    {

        private readonly ILogger<OrdersController> _logger;
        private IDataRepository _repository;

        public OrdersController(IDataRepository repository, ILogger<OrdersController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        public IEnumerable<Order> Get()
        {
            return _repository.GetOrders();
        }

    }
}