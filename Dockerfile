# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for better layer caching
COPY ["Drive.Api/Drive.Api.csproj", "Drive.Api/"]
COPY ["Drive.Application/Drive.Application.csproj", "Drive.Application/"]
COPY ["Drive.Core/Drive.Core.csproj", "Drive.Core/"]
COPY ["Drive.Infrastructure/Drive.Infrastructure.csproj", "Drive.Infrastructure/"]
RUN dotnet restore "Drive.Api/Drive.Api.csproj"

# Copy the rest of the source
COPY . .
WORKDIR /src/Drive.Api
RUN dotnet publish "Drive.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run as non-root (image already ships with $APP_UID user)
USER $APP_UID

COPY --from=build /app/publish .

# .NET 8+ default HTTP port inside containers
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Drive.Api.dll"]
