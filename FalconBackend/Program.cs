using FalconBackend.Data;
using FalconBackend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register AppDbContext with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<FileStorageService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var basePath = configuration.GetValue<string>("FileStorage:BasePath");
    if (string.IsNullOrEmpty(basePath))
    {
        throw new InvalidOperationException("File storage base path is missing. Check appsettings.json.");
    }
    return new FileStorageService(basePath); // Pass only basePath
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
