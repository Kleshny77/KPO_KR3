using Microsoft.EntityFrameworkCore;
using order_service.Data;
using order_service.Services;
using order_service.Hubs;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

builder.Services.AddControllers();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddScoped<OrderStatusNotifier>();
// builder.Services.AddHostedService<OutboxProcessor>();
// builder.Services.AddHostedService<OrderPaymentResultConsumer>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8081")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Host.ConfigureHostOptions(o => o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

var app = builder.Build();

Console.WriteLine("Before migration");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}
Console.WriteLine("After migration");

app.UseSwagger();
app.UseSwaggerUI();

app.UsePathBase("/orders");
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderStatusHub>("/order-status");
app.MapGet("/ping", () => "pong");

Console.WriteLine("App is starting...");
app.Run();
