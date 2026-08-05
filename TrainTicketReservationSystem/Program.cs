using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Repositories.SpecialRequests;
using TrainTicketReservationSystem.Services.Journeys;
using TrainTicketReservationSystem.Services.SpecialRequests;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("BookingDatabase"),
  sqlOptions =>
  {
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null);

    sqlOptions.CommandTimeout(60);
  }));

builder.Services.AddHttpClient<IJourneyApiClient, JourneyApiClient>(
  client =>
  {
    client.BaseAddress = new Uri(builder.Configuration["Services:JourneyApi"]);
  });
builder.Services.AddSingleton<ISpecialRequestRepository, XmlSpecialRequestRepository>();
builder.Services.AddScoped<ISpecialRequestService, SpecialRequestService>();

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
