@echo off
echo Starting Frontend...
cd frontend\Frontend
call npm run dev
echo.
echo [Frontend stopped. Press any key to close...]
pause >nul
