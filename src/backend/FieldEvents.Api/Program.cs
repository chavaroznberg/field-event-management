using FieldEvents.Api.Hubs;
using FieldEvents.Api.Notifications;
using FieldEvents.Application.Interfaces;
using FieldEvents.Infrastructure;
using FieldEvents.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. " +
        "Set it via 'dotnet user-secrets set ConnectionStrings:DefaultConnection \"<value>\"' " +
        "or the CONNECTIONSTRINGS__DEFAULTCONNECTION environment variable.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<IEventNotificationService, SignalREventNotificationService>();

var app = builder.Build();

// SQLite local-dev: create schema from EF Core model (no migration needed).
// SQL Server production: schema managed by dotnet ef database update.
if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FieldEventsDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseCors("AngularDev");
app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<DispatcherHub>("/hubs/dispatcher");

app.Run();

public partial class Program { }
