namespace MilkApp.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Supabase JWT secret — found in Supabase > Project Settings > API > JWT Settings > JWT Secret
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}
