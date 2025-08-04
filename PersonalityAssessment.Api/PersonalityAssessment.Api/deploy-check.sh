#!/bin/bash

# Render Deployment Script for Personality Assessment System
# This script helps prepare and verify your deployment

echo "🚀 Personality Assessment System - Render Deployment Helper"
echo "=========================================================="

# Check if we're in the right directory
if [ ! -f "render.yaml" ] || [ ! -f "Dockerfile" ]; then
    echo "❌ Error: Missing deployment files. Please run this from the repository root."
    exit 1
fi

echo "✅ Deployment files found"

# Check .NET version
echo "🔍 Checking .NET version..."
dotnet --version

# Test build locally
echo "🔨 Testing local build..."
cd PersonalityAssessment.Api/PersonalityAssessment.Api
dotnet restore
if [ $? -eq 0 ]; then
    echo "✅ Package restore successful"
else
    echo "❌ Package restore failed"
    exit 1
fi

dotnet build -c Release
if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# Test publish
echo "📦 Testing publish..."
dotnet publish -c Release -o ../../../test-publish
if [ $? -eq 0 ]; then
    echo "✅ Publish successful"
    rm -rf ../../../test-publish
else
    echo "❌ Publish failed"
    exit 1
fi

cd ../..

echo ""
echo "🎉 Pre-deployment checks passed!"
echo ""
echo "Next steps:"
echo "1. Commit and push these changes to GitHub"
echo "2. Go to https://render.com and create a new Web Service"
echo "3. Connect your GitHub repository"
echo "4. Render will automatically detect the render.yaml configuration"
echo "5. Set up your environment variables:"
echo "   - ASPNETCORE_ENVIRONMENT=Production"
echo "   - ConnectionStrings__DefaultConnection=[Your Database Connection]"
echo "6. Deploy and monitor the logs"
echo ""
echo "📝 For detailed instructions, see RENDER-DEPLOYMENT-GUIDE.md"
