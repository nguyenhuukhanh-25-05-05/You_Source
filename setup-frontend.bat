@echo off
echo ============================================
echo   Frontend Setup
echo ============================================
echo.

cd frontend\Frontend
call npm install
if %errorlevel% neq 0 (
    echo [ERROR] npm install failed.
    cd ..\..
    pause
    exit /b 1
)

call npx vite build
cd ..\..

echo.
echo Frontend ready!
pause
