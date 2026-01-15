using AudioAssistant.Api.Data;
using AudioAssistant.Api.Middleware;
using AudioAssistant.Api.Services;
using AudioAssistant.Api.Services.Abstractions;
using AudioAssistant.Api.Services.Providers;
using AudioAssistant.Api.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
builder.Services.AddDbContext<AudioAssistantDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
var expirationMinutes = jwtSettings.GetValue<int>("ExpirationMinutes", 1440);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddSingleton(sp => 
    new JwtTokenGenerator(secretKey, issuer, audience, expirationMinutes));

var encryptionKey = builder.Configuration.GetSection("EncryptionSettings")["EncryptionKey"] 
    ?? throw new InvalidOperationException("EncryptionKey not configured");
builder.Services.AddSingleton(sp => new EncryptionService(encryptionKey));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<ITranscriptionService, TranscriptionService>();
builder.Services.AddScoped<IResponseService, ResponseService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<IExportService, ExportService>();

// Register HttpClient for transcription providers
builder.Services.AddHttpClient<GroqWhisperProvider>();
builder.Services.AddHttpClient<WhisperCppProvider>();
builder.Services.AddHttpClient<OpenAIWhisperProvider>();
builder.Services.AddHttpClient<ClaudeHaikuSTTProvider>();

// Register transcription providers
builder.Services.AddScoped<GroqWhisperProvider>();
builder.Services.AddScoped<WhisperCppProvider>();
builder.Services.AddScoped<OpenAIWhisperProvider>();
builder.Services.AddScoped<ClaudeHaikuSTTProvider>();

// Register HttpClient for AI response providers
builder.Services.AddHttpClient();

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>() 
    ?? new[] { "*" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        if (corsOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// Add controllers
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Audio Assistant API",
        Version = "v1",
        Description = "API for Audio Assistant application with AI-powered conversation support",
        Contact = new OpenApiContact
        {
            Name = "Audio Assistant Team",
            Email = "support@audioassistant.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply migrations and ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AudioAssistantDbContext>();
    try
    {
        dbContext.Database.Migrate();
        Log.Information("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while applying database migrations");
    }
}

// Configure middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();

// Enable Swagger in all environments (for development and testing)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audio Assistant API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowConfiguredOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

Log.Information("Audio Assistant API is starting up...");

app.Run();
