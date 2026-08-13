using MilkApp.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services
    .AddOptions<SupabaseOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Url) && !string.IsNullOrWhiteSpace(o.Key),
        "Supabase:Url and Supabase:Key must be configured (see appsettings or user-secrets).");

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Secret),
        "Jwt:Secret must be configured. Find it in Supabase > Project Settings > API > JWT Secret.");

// Configure JWT Bearer authentication using Supabase JWT secret
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSecret = builder.Configuration[$"{JwtOptions.SectionName}:Secret"] ?? string.Empty;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SupabaseOptions>>().Value;
    var client = new Supabase.Client(options.Url, options.Key, new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false
    });
    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Milk Flow API v1");
});

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", message = "Milk Flow API is online", timestamp = DateTime.UtcNow }));

app.MapControllers();

app.Run();
