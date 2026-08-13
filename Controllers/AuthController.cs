using MilkApp.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MilkApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SupabaseOptions _supabaseOptions;
    private readonly HttpClient _httpClient;

    public AuthController(IOptions<SupabaseOptions> supabaseOptions, IHttpClientFactory httpClientFactory)
    {
        _supabaseOptions = supabaseOptions.Value;
        _httpClient = httpClientFactory.CreateClient();
    }

    /// <summary>
    /// Login with mobile number and password.
    /// Mobile number is used as the username; internally stored as {mobile}@milkflow.app
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Mobile number and password are required." });

        var email = ToEmail(request.MobileNumber);

        var payload = JsonSerializer.Serialize(new { email, password = request.Password });
        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_supabaseOptions.Url}/auth/v1/token?grant_type=password");
        httpRequest.Headers.Add("apikey", _supabaseOptions.Key);
        httpRequest.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var err = TryGetErrorMessage(body);
            return Unauthorized(new { message = err ?? "Invalid mobile number or password." });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return Ok(new
        {
            accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null,
            refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            expiresIn = root.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600,
            userId = root.TryGetProperty("user", out var u) && u.TryGetProperty("id", out var uid) ? uid.GetString() : null,
        });
    }

    /// <summary>
    /// Register a new user with mobile number and password.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MobileNumber) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Mobile number and password are required." });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var email = ToEmail(request.MobileNumber);

        var payload = JsonSerializer.Serialize(new { email, password = request.Password });
        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_supabaseOptions.Url}/auth/v1/signup");
        httpRequest.Headers.Add("apikey", _supabaseOptions.Key);
        httpRequest.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var err = TryGetErrorMessage(body);
            return BadRequest(new { message = err ?? "Registration failed. This mobile number may already be registered." });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // If Supabase email confirmation is disabled, we get tokens immediately
        if (root.TryGetProperty("access_token", out var at))
        {
            return Ok(new
            {
                accessToken = at.GetString(),
                refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                expiresIn = root.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600,
                userId = root.TryGetProperty("user", out var u) && u.TryGetProperty("id", out var uid) ? uid.GetString() : null,
            });
        }

        // If email confirmation is enabled, just return success without token
        return Ok(new { message = "Account created successfully. You can now log in." });
    }

    /// <summary>
    /// Refresh an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { message = "Refresh token is required." });

        var payload = JsonSerializer.Serialize(new { refresh_token = request.RefreshToken });
        var httpRequest = new HttpRequestMessage(HttpMethod.Post,
            $"{_supabaseOptions.Url}/auth/v1/token?grant_type=refresh_token");
        httpRequest.Headers.Add("apikey", _supabaseOptions.Key);
        httpRequest.Content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Unauthorized(new { message = "Session expired. Please log in again." });

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return Ok(new
        {
            accessToken = root.TryGetProperty("access_token", out var at) ? at.GetString() : null,
            refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            expiresIn = root.TryGetProperty("expires_in", out var ex) ? ex.GetInt32() : 3600,
        });
    }

    // ── Helpers ────────────────────────────────────────────────────────
    private static string ToEmail(string mobile) =>
        $"{mobile.Trim().Replace(" ", "")}@milkflow.app";

    private static string? TryGetErrorMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("msg", out var msg)) return msg.GetString();
            if (doc.RootElement.TryGetProperty("message", out var message)) return message.GetString();
            if (doc.RootElement.TryGetProperty("error_description", out var ed)) return ed.GetString();
        }
        catch { }
        return null;
    }
}

public record AuthRequest(string MobileNumber, string Password);
public record RefreshRequest(string RefreshToken);
