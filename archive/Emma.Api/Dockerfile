# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install EF CLI tools globally for migration support
RUN dotnet tool install --global dotnet-ef
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy solution and project files
COPY ["Emma.sln", "./"]
COPY ["Emma.Api/Emma.Api.csproj", "Emma.Api/"]
COPY ["Emma.Core/Emma.Core.csproj", "Emma.Core/"]
COPY ["Emma.Data/Emma.Data.csproj", "Emma.Data/"]

# Restore dependencies (for all projects)
RUN dotnet restore "Emma.Api/Emma.Api.csproj"

# Copy all source code
COPY . .

# Build and publish both projects to the SAME /app/publish directory
WORKDIR /src/Emma.Api
RUN dotnet publish "Emma.Api.csproj" -c Release -o /app/publish

WORKDIR /src/Emma.Data
RUN dotnet publish "Emma.Data.csproj" -c Release -o /app/publish

# --- Migration stage (for running dotnet ef commands) ---
FROM build AS migration
WORKDIR /src
# Now you can run: dotnet ef database update --project Emma.Data --startup-project Emma.Api

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ["Emma.Api/appsettings.Docker.json", "appsettings.json"]
EXPOSE 80
ENTRYPOINT ["dotnet", "Emma.Api.dll"]
