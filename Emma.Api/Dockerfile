# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Emma.sln", "./"]
COPY ["Emma.Api/Emma.Api.csproj", "Emma.Api/"]
COPY ["Emma.Core/Emma.Core.csproj", "Emma.Core/"]
COPY ["Emma.Data/Emma.Data.csproj", "Emma.Data/"]

# Restore dependencies
RUN dotnet restore "Emma.Api/Emma.Api.csproj"

# Copy all source code
COPY . .

# Build and publish the application
WORKDIR "/src/Emma.Api"
RUN dotnet build "Emma.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Emma.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Copy the Docker-specific appsettings file
COPY ["Emma.Api/appsettings.Docker.json", "appsettings.json"]

ENTRYPOINT ["dotnet", "Emma.Api.dll"]
