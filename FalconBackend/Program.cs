using FalconBackend.Data;
using FalconBackend.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Add this line to use Serilog

// Add services to the container.

// Register AppDbContext with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<MailService>();

// Register FileStorageService with a configurable base path
builder.Services.AddScoped<FileStorageService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<FileStorageService>>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var basePath = configuration.GetValue<string>("FileStorage:BasePath");
    if (string.IsNullOrEmpty(basePath))
    {
        throw new InvalidOperationException("File storage base path is missing. Check appsettings.json.");
    }
    return new FileStorageService(basePath, logger);
});

// Add Controllers
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
