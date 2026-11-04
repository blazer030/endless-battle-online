
namespace EndlessBattle.Api.Endpoints.Admin;

// admin API 一律掛在 /admin 群組下；只在 Admin:Enabled 為 true 時由 Program 註冊
public static class AdminEndpoints
{
    public const string EnabledSetting = "Admin:Enabled";

    public const string OpenApiDocument = "admin";

    // ApiDescription.RelativePath 不含開頭的斜線，例如 admin/content/{kind}
    public static bool IsAdminPath(string? relativePath) =>
        relativePath is not null && (relativePath == "admin" || relativePath.StartsWith("admin/", StringComparison.Ordinal));

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin");
        admin.MapAdminContentEndpoints();

        return app;
    }
}
