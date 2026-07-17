namespace AppApi.Helpers;

public static class AuthCookieHelper
{
    public const string AccessTokenCookie = "access_token";
    public const string RefreshTokenCookie = "refresh_token";

    public static void SetAuthCookies(
        HttpResponse response,
        string accessToken,
        string refreshToken,
        DateTime accessExpires,
        DateTime refreshExpires,
        bool isProduction)
    {
        var sameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax;

        response.Cookies.Append(AccessTokenCookie, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = sameSite,
            Expires = accessExpires,
            Path = "/"
        });

        response.Cookies.Append(RefreshTokenCookie, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = sameSite,
            Expires = refreshExpires,
            Path = "/"
        });
    }

    public static void ClearAuthCookies(HttpResponse response, bool isProduction)
    {
        var sameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax;
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = sameSite,
            Path = "/"
        };
        response.Cookies.Delete(AccessTokenCookie, options);
        response.Cookies.Delete(RefreshTokenCookie, options);
    }
}
