using Microsoft.EntityFrameworkCore;
using RentalPS_BE.Repositories;
using RentalPSMiniProject.Data;
using RentalPS_BE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connStr = builder.Configuration.GetConnectionString("RentalPS");
builder.Services.AddDbContext<RentalPSDBContext>(opt => opt.UseNpgsql(connStr));
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IConsoleTypeService, ConsoleTypeService>();
builder.Services.AddScoped<IStickTypeService, StickTypeService>();
builder.Services.AddScoped<IStickService, StickService>();



builder.Services.AddScoped<IRentalRoomService, RentalRoomService>();
builder.Services.AddScoped<IRentalStickService, RentalStickService>();
builder.Services.AddScoped<IRentalService, RentalService>();








var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
