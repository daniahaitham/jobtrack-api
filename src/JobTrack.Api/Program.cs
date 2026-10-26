using System.Text;
using System.Text.Json.Serialization;
using JobTrack.Api.Data;
using JobTrack.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---- Database -------------------------------------------------------------
// The connection string is read lazily so tests can swap in a different database file.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")));

// ---- JWT settings ---------------------------------------------------------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or too short (min 32 characters). " +
        "In development it comes from appsettings.Development.json; " +
        "in production set the environment variable Jwt__Key.");
}

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();

// ---- Authentication: check the token on every request ----------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names exactly as we wrote them ("uid", "email")
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// ---- Controllers + JSON ----------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // Show statuses as words ("Interview") in JSON instead of numbers.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ---- Swagger: an interactive page to try the API at /swagger ----------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobTrack API", Version = "v1" });

    // Adds the "Authorize" button so you can paste your token in Swagger.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token from /api/auth/login (no 'Bearer ' prefix needed)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});


// ---- CORS: allow the React app (running on port 5173) to call this API ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});
var app = builder.Build();

// Create the database file + tables on first run. (Proper migrations come in part 2.)
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

app.UseCors("Frontend");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // 1) who are you?
app.UseAuthorization();  // 2) are you allowed?

app.MapControllers();

app.Run();

// Lets the test project start the whole app in memory.
public partial class Program
{
}
