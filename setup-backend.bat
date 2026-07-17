@echo off
echo ============================================
echo   Backend Setup
echo ============================================
echo.

dotnet restore backend\AppApi\AppApi.csproj
if %errorlevel% neq 0 (
    echo [ERROR] dotnet restore failed.
    pause
    exit /b 1
)

dotnet build backend\AppApi\AppApi.csproj
if %errorlevel% neq 0 (
    echo [ERROR] dotnet build failed.
    pause
    exit /b 1
)

echo Installing EF Core tools...
dotnet tool install --global dotnet-ef 2>nul
dotnet tool update --global dotnet-ef 2>nul

echo.
echo Setting up dev secrets (User Secrets)...
dotnet user-secrets --project backend\AppApi set "JwtSettings:Secret" "DevOnly_SuperSecretKey_DoNotUseInProd_32chars!" >nul 2>&1
dotnet user-secrets --project backend\AppApi set "AdminSettings:Password" "Admin@12345" >nul 2>&1
echo   JWT secret + admin password configured for dev.

echo.
echo Backend ready!
echo   Admin login: admin / Admin@12345
pause
