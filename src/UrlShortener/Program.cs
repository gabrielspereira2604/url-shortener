using Microsoft.EntityFrameworkCore;
using UrlShortener.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();
