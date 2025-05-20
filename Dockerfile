# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY Emma.Api/Emma.Api.csproj Emma.Api/
COPY Emma.Core/Emma.Core.csproj Emma.Core/
RUN dotnet restore Emma.Api/Emma.Api.csproj

# Copy the rest of the source code
COPY Emma.Api/. Emma.Api/
COPY Emma.Core/. Emma.Core/

# Build the application
RUN dotnet publish Emma.Api/Emma.Api.csproj -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Emma.Api.dll"]
