@echo off
REM Render Deployment Script for Personality Assessment System
REM This script helps prepare and verify your deployment

echo 🚀 Personality Assessment System - Render Deployment Helper
echo ==========================================================

REM Check if we're in the right directory
if not exist "render.yaml" (
    echo ❌ Error: Missing render.yaml file. Please run this from the repository root.
    exit /b 1
)

if not exist "Dockerfile" (
    echo ❌ Error: Missing Dockerfile. Please run this from the repository root.
    exit /b 1
)

echo ✅ Deployment files found

REM Check .NET version
echo 🔍 Checking .NET version...
dotnet --version

REM Test build locally
echo 🔨 Testing local build...
cd PersonalityAssessment.Api\PersonalityAssessment.Api

dotnet restore
if %errorlevel% neq 0 (
    echo ❌ Package restore failed
    exit /b 1
)
echo ✅ Package restore successful

dotnet build -c Release
if %errorlevel% neq 0 (
    echo ❌ Build failed
    exit /b 1
)
echo ✅ Build successful

REM Test publish
echo 📦 Testing publish...
dotnet publish -c Release -o ..\..\test-publish
if %errorlevel% neq 0 (
    echo ❌ Publish failed
    exit /b 1
)
echo ✅ Publish successful

REM Clean up test publish
rmdir /s /q ..\..\test-publish

cd ..\..

echo.
echo 🎉 Pre-deployment checks passed!
echo.
echo Next steps:
echo 1. Commit and push these changes to GitHub
echo 2. Go to https://render.com and create a new Web Service
echo 3. Connect your GitHub repository
echo 4. Render will automatically detect the render.yaml configuration
echo 5. Set up your environment variables:
echo    - ASPNETCORE_ENVIRONMENT=Production
echo    - ConnectionStrings__DefaultConnection=[Your Database Connection]
echo 6. Deploy and monitor the logs
echo.
echo 📝 For detailed instructions, see RENDER-DEPLOYMENT-GUIDE.md

pause
