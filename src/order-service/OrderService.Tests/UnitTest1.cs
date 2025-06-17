namespace OrderService.Tests;
using Xunit;
using order_service.Controllers;
using order_service.Data;
using order_service.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }

    [Fact]
    public async Task CreateOrder_ReturnsOrder()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        using var db = new OrdersDbContext(options);
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var controller = new OrdersController(db, publishEndpoint.Object);
        var request = new OrdersController.CreateOrderRequest
        {
            CustomerId = "user1",
            Amount = 100,
            Description = "test"
        };
        var result = await controller.CreateOrder(request);
        var okResult = Assert.IsType<OkObjectResult>(result);
        var order = Assert.IsType<Order>(okResult.Value);
        Assert.Equal("user1", order.UserId);
        Assert.Equal(100, order.Amount);
        Assert.Equal("test", order.Description);
    }
}