using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotesAPI.Data;
using NotesAPI.Development;
using NotesAPI.Endpoints;
using NotesAPI.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

// --------------------------------------------------
// Database
// --------------------------------------------------

builder.Services.AddDbContext<NotesDbContext>(options =>
    options.UseSqlite("Data Source=NotesAPI.db"));


// --------------------------------------------------
// Routing
// --------------------------------------------------

// Required by Swagger for regex route constraints
builder.Services.Configure<RouteOptions>(options =>
{
    options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
});


// --------------------------------------------------
// JSON
// --------------------------------------------------

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        AppJsonSerializerContext.Default);
});


// --------------------------------------------------
// Services
// --------------------------------------------------

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<JwtService>();


// --------------------------------------------------
// Authentication
// --------------------------------------------------

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin", "MainAdmin");
    });

    options.AddPolicy("MainAdminOnly", policy =>
    {
        policy.RequireRole("MainAdmin");
    });
});


// --------------------------------------------------
// Swagger
// --------------------------------------------------

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.ParameterLocation.Header,
            Description =
                "Enter your JWT token."
        });

    options.AddSecurityRequirement(document =>
        new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [
                new Microsoft.OpenApi.OpenApiSecuritySchemeReference(
                    "Bearer",
                    document)
            ] = []
        });
});


// --------------------------------------------------
// Application
// --------------------------------------------------

var app = builder.Build();


// --------------------------------------------------
// Development Test Users
// --------------------------------------------------

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<NotesDbContext>();

    var passwordService = scope.ServiceProvider
        .GetRequiredService<PasswordService>();

    // Apply pending migrations before creating development test data.
    await db.Database.MigrateAsync();

    await TestUsers.SeedAsync(
        db,
        passwordService);
}


// --------------------------------------------------
// Middleware
// --------------------------------------------------

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();


// --------------------------------------------------
// Endpoints
// --------------------------------------------------

app.MapNotesEndpoints();
app.MapCategoriesEndpoints();
app.MapAuthEndpoints();
app.MapAdminEndpoints();


// --------------------------------------------------
// Run
// --------------------------------------------------

app.Run();


// --------------------------------------------------
// JSON source generation
// --------------------------------------------------

public record Todo(
    int Id,
    string? Title,
    DateOnly? DueBy = null,
    bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}