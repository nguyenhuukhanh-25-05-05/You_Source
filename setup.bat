@echo off
echo ============================================
echo   Starter Kit - Full Setup
echo ============================================
echo.

call setup-backend.bat
call setup-frontend.bat

echo.
echo ============================================
echo   All Done!
echo ============================================
echo.
echo Run:  run-backend.bat   (API on :5000)
echo Run:  run-frontend.bat  (UI  on :5173)
echo.
echo Admin: admin / Admin@12345
echo.
pause
