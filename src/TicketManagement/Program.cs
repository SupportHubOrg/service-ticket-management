using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;

using SupportHub.TicketManagement.Features.Tickets.GetTickets;
using SupportHub.TicketManagement.Features.Tickets.CreateTicket;
using SupportHub.TicketManagement.Features.Tickets.GetTicket;
using SupportHub.TicketManagement.Features.Tickets.DeleteTicket;
using SupportHub.TicketManagement.Features.Tickets.UpdateTicket;
using SupportHub.TicketManagement.Features.TicketTags.CreateTicketTag;
using SupportHub.TicketManagement.Features.TicketTags.DeleteTicketTag;
using SupportHub.TicketManagement.Features.TicketTags.GetTicketTags;
using SupportHub.TicketManagement.Features.Tickets.EvaluateAiResponse;
using SupportHub.TicketManagement.Features.Tickets.ProposeArticle;
using SupportHub.TicketManagement.Features.Tags.CreateTag;
using SupportHub.TicketManagement.Features.Tags.GetTags;
using SupportHub.TicketManagement.Features.Tags.GetTag;
using SupportHub.TicketManagement.Features.Tags.DeleteTag;
using SupportHub.TicketManagement.Features.OperatorTags.GetOperatorTags;
using SupportHub.TicketManagement.Features.OperatorTags.CreateOperatorTag;
using SupportHub.TicketManagement.Features.OperatorTags.DeleteOperatorTag;
using SupportHub.TicketManagement.Features.Tags.UpdateTag;
using SupportHub.TicketManagement.Features.TicketAuditLogs.DeleteTicketAuditLog;
using SupportHub.TicketManagement.Features.TicketAuditLogs.GetTicketAuditLogs;
using SupportHub.TicketManagement.Features.TicketMessages.CreateTicketMessage;
using SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessages;
using SupportHub.TicketManagement.Features.TicketMessages.GetTicketMessage;
using SupportHub.TicketManagement.Features.TicketMessages.DeleteTicketMessage;
using SupportHub.TicketManagement.Features.TicketMessages.UpdateTicketMessage;

var builder = WebApplication.CreateBuilder(args);

//Add a Serilog logger with console output and configuration from appsettings.json
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

//Allow enums to be serialized as strings in JSON responses for better readability in API responses and documentation
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
});

//Add support for discovery and documenting API endpoints in the application
builder.Services.AddEndpointsApiExplorer();

//Add Swagger/OpenAPI support for API documentation and testing
builder.Services.AddSwaggerGen();

//Enable health monitoring of the application
builder.Services.AddHealthChecks();

//Add CORS policy
//Change AllowAnyOrigin to get the specific origins in production for better security, TDOO
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

//Add MediatR for in-process messaging and CQRS pattern implementation
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

//Add database context
builder.AddSqlServerDbContext<TicketDbContext>("TicketDb");

//Bind KafkaSettings from appsettings.json and override with Aspire-injected connection string if available
builder.Services.Configure<KafkaSettings>(options =>
{
    //Bind the standard settings from appsettings.json
    builder.Configuration.GetSection("Kafka").Bind(options);

    //Fetch the Aspire-injected connection string for Kafka
    var aspireKafkaConnectionString = builder.Configuration.GetConnectionString("kafka");

    //If Aspire provided a connection string, use it instead of localhost
    if (!string.IsNullOrEmpty(aspireKafkaConnectionString))
    {
        options.BootstrapServers = aspireKafkaConnectionString;
    }
});

//Register the Publisher for feature handlers to inject
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

//Register the Background Kafka Consumer
builder.Services.AddHostedService<TicketEventConsumerService>();

//Get jwt information from saved settings
var jwt = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

//Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //Configure JWT token validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

//Add authorization services to enable role-based or policy-based access control for API endpoints
builder.Services.AddAuthorization();

var app = builder.Build();

//Register swagger when in development environment for API documentation and testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();
//Handle forwarded headers for reverse proxy scenarios
app.UseForwardedHeaders();
//Apply the defined CORS policy to allow cross-origin requests from the frontend application and other clients
app.UseCors("DefaultCorsPolicy");

//Register health check endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.MapMetrics();

//Map Ticket API endpoints
app.MapUpdateTicketEndpoint();
app.MapCreateTicketEndpoint();
app.MapDeleteTicketEndpoint();
app.MapGetTicketEndpoint();
app.MapGetTicketsEndpoint();
app.MapEvaluateAiResponseEndpoint();
app.MapProposeArticleEndpoint();

//Map the Ticket Tag API endpoints
app.MapCreateTicketTagEndpoint();
app.MapDeleteTicketTagEndpoint();
app.MapGetTicketTagsEndpoint();

//Map the Tag API endpoints
app.MapCreateTagEndpoint();
app.MapGetTagsEndpoint();
app.MapGetTagEndpoint();
app.MapDeleteTagEndpoint();
app.MapUpdateTagEndpoint();

//Map the Operator Tag API endpoints
app.MapCreateOperatorTagEndpoint();
app.MapDeleteOperatorTagEndpoint();
app.MapGetOperatorTagsEndpoint();

//Map the Ticket Audit Log API endpoints
app.MapGetTicketAuditLogsEndpoint();
app.MapDeleteTicketAuditLogEndpoint();

//Map the Ticket Message API endpoints
app.MapCreateTicketMessageEndpoint();
app.MapGetTicketMessageEndpoint();
app.MapGetTicketMessagesEndpoint();
app.MapDeleteTicketMessageEndpoint();
app.MapUpdateTicketMessageEndpoint();

//Apply any pending database migrations on application startup to ensure the database schema is up to date with the application's data model
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "An error occurred while migrating the database.");
        throw;
    }
}

//Start the application
try
{
    Log.Information("Starting Service Ticket Management API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start correctly!");
}
finally
{
    Log.CloseAndFlush();
}
