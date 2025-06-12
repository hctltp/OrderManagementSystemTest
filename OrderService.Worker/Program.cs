using Microsoft.EntityFrameworkCore;
using OrderService.Worker;
using OrderService.Worker.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<DLQWorker>();



var host = builder.Build();
host.Run();
