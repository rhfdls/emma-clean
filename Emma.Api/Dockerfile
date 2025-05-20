# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Emma.Api/Emma.Api.csproj ./
COPY Emma.Core/Emma.Core.csproj ./Emma.Core/Emma.Core.csproj
COPY Emma.Data/Emma.Data.csproj ./Emma.Data/Emma.Data.csproj
COPY Emma.Core/ ./Emma.Core/
COPY Emma.Data/ ./Emma.Data/


# Restore dependencies
RUN ls -l
RUN ls -l Emma.Core
RUN ls -l Emma.Data

# Copy solution and project files
COPY ../Emma.sln ./
COPY Emma.Api/Emma.Api.csproj ./Emma.Api/
COPY Emma.Core/Emma.Core.csproj ./Emma.Core/
COPY Emma.Data/Emma.Data.csproj ./Emma.Data/

# Restore as distinct layers
RUN dotnet restore Emma.Api/Emma.Api.csproj

# Copy everything else
COPY Emma.Api/. ./Emma.Api/
COPY Emma.Core/. ./Emma.Core/
COPY Emma.Data/. ./Emma.Data/

# Build the application
RUN dotnet publish Emma.Api/Emma.Api.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Expose port 80
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "Emma.Api.dll"]

