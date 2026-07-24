// SPDX-License-Identifier: MIT

namespace NosGM.Web;

public static class WebSecurityPolicy
{
    public const string ContentSecurityPolicy =
        "default-src 'none'; " +
        "base-uri 'self'; " +
        "connect-src 'self'; " +
        "font-src 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "img-src 'self' data:; " +
        "object-src 'none'; " +
        "script-src 'self'; " +
        "style-src 'self'";

    public static void Apply(HttpResponse response)
    {
        response.Headers["Content-Security-Policy"] = ContentSecurityPolicy;
        response.Headers["X-Content-Type-Options"] = "nosniff";
        response.Headers["X-Frame-Options"] = "DENY";
        response.Headers["Referrer-Policy"] = "no-referrer";
        response.Headers.Append("Permissions-Policy", "camera=(), geolocation=(), microphone=(), payment=(), usb=()");
        response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        response.Headers.Append("Cross-Origin-Resource-Policy", "same-origin");
        response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    }
}

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        WebSecurityPolicy.Apply(context.Response);
        await next(context);
    }
}
