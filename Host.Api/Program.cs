using Microsoft.EntityFrameworkCore;
using Modules.Identity;
using Modules.Orders;
using Modules.Tables;
using Modules.Users;
using Modules.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//modülleri ekledim
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddOrderModule(builder.Configuration);
builder.Services.AddTableModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
