using Microsoft.EntityFrameworkCore;
using Orion.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configure Entity Framework Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("OrionDb");
builder.Services.AddDbContext<OrionDbContext>(options =>
    options.UseNpgsql(connectionString));


var app = builder.Build();

// 3. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();