# -----------------------------------------------------------------
# 1. RUNTIME STAGE
# -----------------------------------------------------------------
# recipe-notification-service har ingen HTTP-endepunkter, men Serilog.AspNetCore-pakken
# (brukt i SerilogExtensions.cs) krever likevel Microsoft.AspNetCore.App-rammeverket ved
# kjøretid - dotnet/runtime alene er derfor ikke nok (verifisert: appen nekter å starte).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# -----------------------------------------------------------------
# 2. BUILD STAGE
# -----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopier csproj-filer og gjenopprett avhengigheter (for optimal Docker-layer caching)
COPY ["Service/Service.csproj", "Service/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Persistence/Persistence.csproj", "Persistence/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "Service/Service.csproj"

# Kopier resten av koden og bygg
COPY . .
WORKDIR "/src/Service"
RUN dotnet build "Service.csproj" -c Release -o /app/build

# -----------------------------------------------------------------
# 3. PUBLISH STAGE
# -----------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------
# 4. FINAL RUNTIME STAGE
# -----------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Service.dll"]
