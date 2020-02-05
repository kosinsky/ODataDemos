# Intro
OData v4.0 specification includes an aggregation extensions that allows not only extract data that your really need, but execute server-side aggregations.

Basic support for aggregation extension was added into ASP.NET OData v7.0 and extended with each new version.
This tutorial assumes that you already have the knowledge to build an ASP.NET Core Web Application service using ASP.NET Core OData NuGget package. If not, start by reading ASP.NET Core OData now Available and refer to the [sample project](https://github.com/kosinsky/ODataDemos/tree/master/AggregationSample) used in this article.
Let’s get started.


# Data Model
We aren't going to build project from scratch. Please, refer to [ASP.ET Core OData now Available](https://devblogs.microsoft.com/odta/asp-net-core-odata-now-available/) if you need sta.

As are data model we are going to ue following CLR classes:

```C#
// Entity types
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

public class Order
{
    public int Id { get; set; }
    public string Title { get; set; }
    public decimal TotalAmount { get; set; }
    public Customer Customer { get; set; }
}

// Complex types
public class Address
{
    public string Street { get; set }
    public string City { get; se }
    public ZipCode ZipCode { get; set }
}

public class BillAddress : Address
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class ZipCode
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
}
```
Also we are going to have two OData controllers: Customers and Orders

```C#
public class OrdersController : ODataController
{
    // Skipped

    [EnableQuery]
    public IEnumerable<Order> Get()
    {
        return _repository.GetOrders();
    }
}
public class CustomersController : ODataController
{
    // Skipped

    [EnableQuery]
    public IEnumerable<Customer> Get()
    {
        return _repository.GetOrders();
    }
}

```
that will allow to have two OData entity sets that we are going to query:
```http://localhost:5000/odata/Orders``` and ```http://localhost:5000/odata/Customer```

# $apply
$apply query option allows to specify a sequence of transformations to the entity set such as ```groupby```, ```filter```, ```aggregate``` etc.

## aggregate transformation
Let's start with simples one and get total number of orders

```OData
http://localhost:5000/odata/Orders?$apply=aggregate($count as OrderCount)
```
query will collapse response into single record and introduce new dynamic property ```OrderCount``` we will get following output:
```JSON
{
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(OrderCount)",
    "value": [
    {
        "@odata.id": null,
        "OrderCount": 3
    }
    ]
}
```

This query might look not very impressive, because you could get number of order without aggregation extensions by using ```http://localhost:5000/odata/Orders/$count```. In addition to $count we could use aggregation methods like ```sum```, ```max```, ```min```, ```countdistinct```, ```average``` and we could combine with aggregation into single query. For example, following query returns not only number or orders by total amount as well as average:

```OData
http://localhost:5000/odata/Orders?$apply=aggregate($count as OrderCount, TotalAmount with sum as TotalAmount, TotalAmount with average as AverageAmount)
```
We will get following output. We introduced 3 new properties with requested aggregations

```JSON
{
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(OrderCount,TotalAmount,AverageAmount)",
    "value": [
        {
            "@odata.id": null,
            "AverageAmount": 22,
            "TotalAmount": 66,
            "OrderCount": 3
        }
    ]
}
```

## groupby transformation
We could get more complex results if we start using groupby transformation with or without nested aggregate.

To get total orders by customer we could use

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))

```
and get following response:

```JSON
    {
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(Customer(Name),OrderCount,TotalAmount)",
    "value": [
        {
            "@odata.id": null,
            "TotalAmount": 21,
            "OrderCount": 1,
            "Customer": {
                "@odata.id": null,
                "Name": "Balmy"
            }
        },
        {
            "@odata.id": null,
            "TotalAmount": 45,
            "OrderCount": 2,
            "Customer": {
                "@odata.id": null,
                "Name": "Chilly"
            }
        }
    ]
}
```
Please, note that we are using ```Customer/Name``` to access properties from related entities in the same way as we are doing it in ```$filter``` and getting properties from Customer entity as nested JSON in the same way as we will get them while using ```$expand```

Trick: If we use groupby without aggregation we could get distinct customer names ```http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name))```

## filter transformation
We are calling groupby and aggregate transformations, however used only one per query. $apply allows to combine multiple transformations to get desired output.

We could adjust previous query by getting orders only from customers in particular city. To do that we first need to filter order using filter transformation ```filter(Customer/HomeAddress/City eq 'Redonse')```, followed by the same groupby expression as in previous query, ```/``` used as delimiter. Full query will look like:

```OData
http://localhost:5000/odata/Orders?$apply=filter(Customer/HomeAddress/City eq 'Redonse')/groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))
```

We will get following as output:

```JSON
    {
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(Customer(Name),OrderCount,TotalAmount)",
    "value": [
        {
            "@odata.id": null,
            "TotalAmount": 21,
            "OrderCount": 1,
            "Customer": {
                "@odata.id": null,
                "Name": "Balmy"
            }
        }
    ]
}
```
Transformation will be executed from left to right. In the query above ```filter(...)``` will be executed first and then ```groupby(...)``` will be executed on already filtered data. It means that we could do filtering of aggregated results. 

For example, if we are interested in finding customers that spent more that particular amount we could use ```groupby``` first and then ```filter``` results:

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))/filter(TotalAmount gt 23)   
```

```JSON
    {
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(Customer(Name),OrderCount,TotalAmount)",
    "value": [
        {
            "@odata.id": null,
            "TotalAmount": 45,
            "OrderCount": 2,
            "Customer": {
                "@odata.id": null,
                "Name": "Chilly"
            }
        }
    ]
}
```

It's important to always remember order of transformation or we could get unexpected results. If we try to aggregate first and then try to filter by customers' city:

```OData
    http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))/filter(Customer/HomeAddress/City eq 'Redonse')
```
we will get error **The query specified in the URI is not valid. $apply/groupby grouping expression 'City' must evaluate to a property access value.**.  It happens because after we applied groupby transformation we have access only to properties from groupby and aggregate.  

### $apply and other query options

# Query providers

# Summary