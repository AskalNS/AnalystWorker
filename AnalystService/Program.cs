using Analyst;
using Analyst.Utils;
using ClientService.EF;
using ClientService.Utils;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<AnalystService>();
builder.Services.AddScoped<EncryptionUtils>();
builder.Services.AddScoped<HalykService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var host = builder.Build();
host.Run();
