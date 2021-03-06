```$select```  and ```$filter```, as well as other OData query options, are a good way to receive only data that you need. However, they might be not the best option for reporting and analytical applications. If you want to get total sales to the particular customer and using only ```$select``` and ```$filter```, you end up selecting all orders for that customer and doing aggregation client-side. This approach means sending a lot of data over the network. If you need to show sales by region, product category, you have to send almost all the data.

Fortunately, OData v4.0 specification includes an [aggregation extensions](http://docs.oasis-open.org/odata/odata-data-aggregation-ext/v4.0/odata-data-aggregation-ext-v4.0.html) which allows to perform aggregations server-side and respond to a client with just a few numbers. It introduces new query option ```$apply```. We going to show how to use it.

Basic support for aggregation extensions was added into ASP.NET OData v7.0 and improved with each new version.

This tutorial assumes that you already know how to build an ASP.NET Core Web Application service using ASP.NET Core OData NuGget package. If not, start by reading [ASP.NET Core OData now Available](https://devblogs.microsoft.com/odata/asp-net-core-odata-now-available/) and add Data Model and controllers as described below. 

You could use [sample project](https://github.com/kosinsky/ODataDemos/tree/master/AggregationSample) to try all queries from this article.

Let’s get started.


# Data Model
As mentioned, we aren't going to build project from scratch. Please, refer to [ASP.NET Core OData now Available](https://devblogs.microsoft.com/odta/asp-net-core-odata-now-available/), if you need detailed steps on how to create OData application.

As are data model we are going to use following CLR classes:

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
Also, we are going to have two OData controllers: Customers and Orders

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
that will allow having two OData entity sets that we are going to query:
```http://localhost:5000/odata/Orders``` and ```http://localhost:5000/odata/Customers```

Now we ready to try a few aggregation queries. You don't need to do anything specific to enable ```$apply``` query option.

# $apply
$apply query option allows to specify a sequence of transformations to the entity set such as ```groupby```, ```filter```, ```aggregate``` etc. We will explaine and demonstrate each later.

## aggregate transformation
Let's start with simples one and get the total number of orders

```OData
http://localhost:5000/odata/Orders?$apply=aggregate($count as OrderCount)
```
The query will collapse response into a single record and introduce new dynamic property ```OrderCount``` we will get the following output:
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

This query might look not very impressive, you could get the number of orders without aggregation extensions by using ```http://localhost:5000/odata/Orders/$count```. In addition to $count we could use aggregation methods like ```sum```, ```max```, ```min```, ```countdistinct```, ```average``` and we could combine with aggregation into single query. For example, the following query returns not only the number of orders, but the total amount as well as average:

```OData
http://localhost:5000/odata/Orders?$apply=aggregate($count as OrderCount, TotalAmount with sum as TotalAmount, TotalAmount with average as AverageAmount)
```
We will get the following output. We introduced 3 new properties with requested aggregations

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
We could get more complex results if we start using ```groupby`` transformation with or without nested aggregate.

To get total orders by a customer we could use

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))

```
and get the following response:

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

> **Trick:** If we use ```groupby``` without aggregation we could get distinct customer names ```http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name))``` or ```http://localhost:5000/odata/Customers?$apply=groupby((Name))```. Please, note that syntac uses double parentnesses.

## filter transformation
We are calling ```groupby``` and ```aggregate``` transformations, however, we used only one transformation per query. $apply allows combining multiple transformations to get the desired output.

We could adjust the previous query by getting orders only from customers in a particular city. To do that we first need to filter order using ```filter``` transformation ```filter(Customer/HomeAddress/City eq 'Redonse')```, followed by the same ```groupby``` expression as in previous query, ```/``` used as the delimiter. The query will look like:

```OData
http://localhost:5000/odata/Orders?$apply=filter(Customer/HomeAddress/City eq 'Redonse')/groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))
```

We will get the following as output:

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
Transformations will be executed from left to right. In the query above ```filter(...)``` will be executed first and then ```groupby(...)``` will be executed on already filtered data. Transformations could be combined in any order. It means that we could do filtering of aggregated results. 

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

It's important to always remember the order of transformation or we could get unexpected results. If we try to aggregate first and then try to filter by customers' city:

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))/filter(Customer/HomeAddress/City eq 'Redonse')
```
we will get error **The query specified in the URI is not valid. $apply/groupby grouping expression 'City' must evaluate to a property access value.**.  It happens because after we applied ```groupby``` transformation we have access only to properties from ```groupby``` and ```aggregate```.  

### $apply and other query options

```$apply``` is yet another query option and can be combined with others. It's important to remember that $apply evaluated [first](http://docs.oasis-open.org/odata/odata-data-aggregation-ext/v4.0/cs02/odata-data-aggregation-ext-v4.0-cs02.html#_Toc435016590). It means that all dynamic properties introduced in the ```$apply``` will be available for later query options, however, properties that aren't part of ```groupby``` or ```aggregate``` will be gone.

To get customers ordered by the total amount you could use the following query:
```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as Total)&$orderby=Total desc
```
and get TOP N customers:
```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as Total)&$orderby=Total desc&$top=1
```

The result will look like
```JSON
{
    "@odata.context": "http://localhost:5000/odata/$metadata#Orders(Customer(Name),OrderCount,Total)",
    "value": [
        {
            "@odata.id": null,
            "Total": 45,
            "OrderCount": 2,
            "Customer": {
                "@odata.id": null,
                "Name": "Chilly"
            }
        }
    ]
}
```
> **Trick:** If you just looking for the buggest total amount, you could use additional ```aggregate``` after ```groupby```:
> ```OData
> http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate(TotalAmount with sum as Total))/aggregate(Total with max as MaxTotal)
>```
> Output will look like:
> ```JSON
>{
>   "@odata.context": "http://localhost:5000/odata/$metadata#Orders(MaxTotal)",
>   "value": [
>       {
>           "@odata.id": null,
>           "MaxTotal": 45
>       }
>   ]
>}
> ```

Using ```$filter``` after ```$apply``` is the same as final ```filter()```transformation. Following 3 queries are equal:

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))/filter(TotalAmount gt 23)   
```

```OData
http://localhost:5000/odata/Orders?$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))&$filter=TotalAmount gt 23
```

```OData
http://localhost:5000/odata/Orders?$filter=TotalAmount gt 23&$apply=groupby((Customer/Name), aggregate($count as OrderCount, TotalAmount with sum as TotalAmount))
```

*```$apply``` evaluated first no matter in which order it was specified in the query options*

# Query providers
In this tutorial, we are used Linq to objects, where all transformations will happen in memory. In real applications, you will use a more advanced query provider (concrete implementation of IQueryable) that will talk to some database and storage. Capabilities and performance of queries could be affected by the chosen query provider:

| Query provider | .NET Core or Classic | Notes |
| ---------------| -------------------| ----|
| EF6            | .NET Classic, .NET Core 3.0+ | Aggregation will be translated to SQL and executed as single SQL query |
| EF Core 1.0    | .NET Classic, .NET Core | Aggregations not supported |
| EF Core 2.1    | .NET Classic, .NET Core | Aggregation will be executed client side in memory |
| EF Core 3.0/3.1    | .NET Core 3.0+, .NET Classic (for EF Core 3.1) | Aggregations will be translated to SQL and executed as single SQL query. However, not all expressions are supported |

Stay tuned for next blog post about using Entity Framework with ```$apply```.

# Summary
```$apply``` is very powerful way to extend OData endpoint and minimize the amount of data that transferred between a service and a client for reporting and analytical scenarios.