using MilkApp.Api.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "MilkApp API",
        Version = "v1",
        Description = "API for monitoring milk deposits, backed by Supabase."
    });
});

builder.Services
    .AddOptions<SupabaseOptions>()
    .Bind(builder.Configuration.GetSection(SupabaseOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Url) && !string.IsNullOrWhiteSpace(o.Key),
        "Supabase:Url and Supabase:Key must be configured (see appsettings or user-secrets).");

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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MilkApp API v1");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
