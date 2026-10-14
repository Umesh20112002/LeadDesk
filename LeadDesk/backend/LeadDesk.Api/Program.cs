using System.Data;
using LeadDesk.Api.Data;
using LeadDesk.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/lead-desk-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Register the document referenced by Swagger UI: /swagger/v1/swagger.json.
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "LeadDesk API",
            Version = "v1"
        });
    });
    builder.Services.AddCors(options => options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:4200")
              .AllowAnyHeader().AllowAnyMethod()));
    builder.Services.AddScoped<IDbConnection>(_ => new SqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.AddScoped<ILeadRepository, LeadRepository>();
    builder.Services.AddScoped<ILeadService, LeadService>();

    var app = builder.Build();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseCors("AngularClient");
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseHttpsRedirection();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "LeadDesk API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
