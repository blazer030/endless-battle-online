namespace EndlessBattle.Api.Endpoints.Admin;

// admin API 一律掛在 /admin 群組下；只在 Admin:Enabled 為 true 時由 Program 註冊
public static class AdminEndpoints
{
    public const string EnabledSetting = "Admin:Enabled";

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/admin");
        admin.MapGet("/content/{kind}", (string kind) => TypedResults.Ok(Array.Empty<object>()))
            .WithTags("Admin");

        return app;
    }
}
