using System.Reflection;
using System.Text.Json.Serialization;
using EngiFlow.Api.ExceptionHandling;
using EngiFlow.Application;
using EngiFlow.Application.Abstractions.Tenancy;
using EngiFlow.Domain.Ecos;
using EngiFlow.Infrastructure;
using EngiFlow.Infrastructure.Tenancy;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<EcoPriority>(allowIntegerValues: false));
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<EcoStatus>(allowIntegerValues: false));
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<EcoEventType>(allowIntegerValues: false));
    });
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EngiFlow API",
        Version = "v1",
        Description = "REST API for managing tenant-scoped Engineering Change Orders."
    });

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");
var currentCompanyId = StaticTenantProvider.FromConfigurationValue(
    builder.Configuration["EngiFlow:Tenancy:CurrentCompanyId"]);
var currentUserId = StaticTenantProvider.UserIdFromConfigurationValue(
    builder.Configuration["EngiFlow:Tenancy:CurrentUserId"]);

builder.Services.AddScoped<ITenantProvider>(_ => new StaticTenantProvider(currentCompanyId, currentUserId));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EngiFlow API v1");
        options.DocumentTitle = "EngiFlow API";
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
