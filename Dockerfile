FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PersonalityAssessment.Api/PersonalityAssessment.Api/PersonalityAssessment.Api.csproj", "PersonalityAssessment.Api/PersonalityAssessment.Api/"]
RUN dotnet restore "PersonalityAssessment.Api/PersonalityAssessment.Api/PersonalityAssessment.Api.csproj"
COPY . .
WORKDIR "/src/PersonalityAssessment.Api/PersonalityAssessment.Api"
RUN dotnet build "PersonalityAssessment.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PersonalityAssessment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:10000
ENTRYPOINT ["dotnet", "PersonalityAssessment.Api.dll"]
