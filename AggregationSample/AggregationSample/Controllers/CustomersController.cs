using AggregationSample.Models;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace AggregationSample.Controllers
{
    public class CustomersController : ODataController
    {
        private readonly ILogger<CustomersController> _logger;
        private IDataRepository _repository;

        public CustomersController(IDataRepository repository, ILogger<CustomersController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [EnableQuery]
        public IEnumerable<Customer> Get()
        {
            return _repository.GetCustomers();
        }

        [EnableQuery]
        public IActionResult Get(int key)
        {
            return Ok(_repository.GetCustomers().FirstOrDefault(c => c.Id == key));
        }
    }
}