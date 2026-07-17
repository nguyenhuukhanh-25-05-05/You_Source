using AppApi.Models;
using AppApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace AppApi.Tests;

public class TokenServiceTests
{
    private static ITokenService BuildService() => new TokenService(
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "Test_Key_At_Least_32_Characters_Long!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpirationInMinutes"] = "60"
            })
            .Build());

    private static ApplicationUser BuildUser() => new()
    {
        Id = "user-1",
        UserName = "alice",
        Email = "alice@test.com",
        FullName = "Alice"
    };

    [Fact]
    public void GenerateJwtToken_Contains_Expected_Claims()
    {
        var svc = BuildService();
        var token = svc.GenerateJwtToken(BuildUser(), new List<string> { "User" });
        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(3, token.Split('.').Length);
    }

    [Fact]
    public void GenerateRefreshToken_Returns_Distinct_Base64_Strings()
    {
        var svc = BuildService();
        var a = svc.GenerateRefreshToken();
        var b = svc.GenerateRefreshToken();
        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 64);
    }

    [Fact]
    public void HashToken_Is_Stable_And_Different_From_Input()
    {
        var svc = BuildService();
        var token = "some-refresh-token";
        var h1 = svc.HashToken(token);
        var h2 = svc.HashToken(token);
        Assert.Equal(h1, h2);
        Assert.NotEqual(token, h1);
    }
}
