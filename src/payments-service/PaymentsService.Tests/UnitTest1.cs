namespace PaymentsService.Tests;
using Xunit;
using payments_service.Controllers;
using payments_service.Data;
using payments_service.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {

    }

    [Fact]
    public async Task CreateAccount_ReturnsAccount()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        using var db = new PaymentsDbContext(options);
        var controller = new AccountsController(db);
        var result = await controller.CreateAccount("user1");
        var okResult = Assert.IsType<OkObjectResult>(result);
        var acc = Assert.IsType<Account>(okResult.Value);
        Assert.Equal("user1", acc.UserId);
        Assert.Equal(0, acc.Balance);
    }
}