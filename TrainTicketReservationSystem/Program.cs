using Microsoft.EntityFrameworkCore;
using TrainTicketReservationSystem.BackgroundJobs.Reports;
using TrainTicketReservationSystem.Data;
using TrainTicketReservationSystem.Repositories.Bookings;
using TrainTicketReservationSystem.Repositories.Reports;
using TrainTicketReservationSystem.Repositories.SpecialRequests;
using TrainTicketReservationSystem.Services.Bookings;
using TrainTicketReservationSystem.Services.Journeys;
using TrainTicketReservationSystem.Services.Reports;
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
builder.Services.AddScoped<IBookingRepository, EfBookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddSingleton<ISpecialRequestRepository, XmlSpecialRequestRepository>();

builder.Services.AddHttpClient<IJourneyApiClient, JourneyApiClient>(
  client =>
  {
    client.BaseAddress = new Uri(builder.Configuration["Services:JourneyApi"]);
  });
builder.Services.AddScoped<ISpecialRequestService, SpecialRequestService>();

builder.Services.AddSingleton<IReportJobQueue, ReportJobQueue>();
builder.Services.AddScoped<IReportJobRepository, EfReportJobRepository>();
builder.Services.AddScoped<IWeeklyReportGenerator, WeeklyReportGenerator>();

builder.Services.AddHostedService<WeeklyReportWorker>();

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
