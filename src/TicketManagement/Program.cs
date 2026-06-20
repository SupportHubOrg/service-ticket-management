using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;

using SupportHub.TicketManagement.Infrastructure.Data;
using SupportHub.TicketManagement.Infrastructure.Messaging;
using SupportHub.TicketManagement.Infrastructure.Services;

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
using SupportHub.TicketManagement.Infrastructure.Middleware;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

//Add a Serilog logger with console output and configuration from appsettings.json
builder.Host.UseSerilog((ctx, config) =>
{
    config.ReadFrom.Configuration(ctx.Configuration)
          .Filter.ByExcluding("RequestPath like '%/health%'");
});

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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

//Fetch authentication settings from configuration
var authority = builder.Configuration["Authentication:Authority"]!;
var audience = builder.Configuration["Authentication:Audience"]!;

//Validate that required authentication settings are present
if (string.IsNullOrWhiteSpace(authority))
{
    throw new InvalidOperationException(
        "Authentication:Authority is not configured. " +
        "Please set it in appsettings.json (e.g., 'https://keycloak.example.com/realms/support-hub')");
}
if (string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException(
        "Authentication:Audience is not configured. " +
        "Please set it in appsettings.json (e.g., 'ticket-management-service')");
}
//Validate that Authority is a valid URL
if (!Uri.TryCreate(authority, UriKind.Absolute, out var authorityUri) || authorityUri.Scheme != "https" && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        $"Authentication:Authority must be a valid HTTPS URL. Received: '{authority}'. " +
        "HTTP is only allowed in Development environment.");
}

//Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //Keycloak uses asymmetric RSA keys published at the JWKS endpoint
        options.Authority = authority;
        options.Audience = audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "http://supporthub.local/realms/supporthub",
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        //Keycloak-specific configuration for development environments
        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }
    });

//Add authorization services to enable role-based or policy-based access control for API endpoints
builder.Services.AddAuthorization();

//Add the sla monitoring service
builder.Services.AddHostedService<SlaMonitoringBackgroundService>();

//Add exception handling middleware
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

//Configure the application to look for and honor proxy network signatures
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    
    //Clear restrictions for containerized architectures where upstream proxy IPs change dynamically
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

//Handle forwarded headers for reverse proxy scenarios
app.UseForwardedHeaders();

//Register exception handling middleware
app.UseExceptionHandler(_ => {});

//Add HTTP request logging middleware to log incoming requests and outgoing responses for better observability and debugging
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        //If an exception happens log them at Error level
        if (ex != null) return Serilog.Events.LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 500) return Serilog.Events.LogEventLevel.Error;

        //Mute periodic Kubernetes health probes by demoting them to verbose
        var path = httpContext.Request.Path.Value;
        if (path != null && path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return Serilog.Events.LogEventLevel.Verbose;
        }

        return Serilog.Events.LogEventLevel.Information;
    };
});

//Register swagger when in development environment for API documentation and testing
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Redirect HTTP requests to HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

//Apply the defined CORS policy to allow cross-origin requests from the frontend application and other clients
app.UseCors("DefaultCorsPolicy");

//Enable authentication and authorization middleware to protect API endpoints and ensure only authorized users can access them
app.UseAuthentication();
app.UseAuthorization();

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
        await dbContext.Database.EnsureCreatedAsync();
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
