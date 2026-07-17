using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AppApi.Data;
using AppApi.Models;
using AppApi.Services;

namespace AppApi.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 4;
            options.User.RequireUniqueEmail = true;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                configuration.GetValue("Identity:Lockout:DefaultLockoutTimeSpanMinutes", 15));
            options.Lockout.MaxFailedAccessAttempts = configuration.GetValue("Identity:Lockout:MaxFailedAccessAttempts", 5);
            options.Lockout.AllowedForNewUsers = configuration.GetValue("Identity:Lockout:AllowedForNewUsers", true);

            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secret = jwtSettings["Secret"];

        if (string.IsNullOrWhiteSpace(secret) || secret.StartsWith("__"))
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret is not configured. Set it via User Secrets (`dotnet user-secrets set JwtSettings:Secret <key>`), " +
                "environment variable `JwtSettings__Secret`, or appsettings.Production.json. Minimum 32 characters.");
        }

        if (secret.Length < 32)
            throw new InvalidOperationException("JwtSettings:Secret must be at least 32 characters long.");

        var key = Encoding.UTF8.GetBytes(secret);
        var isProduction = environment.IsProduction();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = isProduction;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (string.IsNullOrEmpty(authHeader)
                        || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var cookieToken = context.Request.Cookies[Helpers.AuthCookieHelper.AccessTokenCookie];
                        if (!string.IsNullOrEmpty(cookieToken))
                            context.Token = cookieToken;
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userManager = context.HttpContext.RequestServices
                        .GetRequiredService<UserManager<ApplicationUser>>();
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        context.Fail("Invalid token");
                        return;
                    }
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null || !user.IsActive)
                    {
                        context.Fail("Invalid token");
                        return;
                    }
                    var stampClaim = context.Principal?.FindFirst("SecurityStamp")?.Value;
                    if (string.IsNullOrEmpty(stampClaim) || stampClaim != user.SecurityStamp)
                    {
                        context.Fail("Invalid token");
                        return;
                    }
                }
            };
        });

        return services;
    }

    public static IServiceCollection AddFluentValidationServices(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<Program>();
        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Starter API",
                Version = "v1",
                Description = "ASP.NET Core Starter API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT token"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}