namespace AppApi.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _csp;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _csp = configuration["Security:Csp"]
            ?? "default-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'; upgrade-insecure-requests";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Response.HasStarted)
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Content-Security-Policy"] = _csp;
        }

        await _next(context);
    }
}