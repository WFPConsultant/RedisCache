# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy and restore the project file first (for better caching)
COPY RedisCache.csproj ./
RUN dotnet restore

# Copy the entire project and publish it
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# ✅ Expose only HTTP port
EXPOSE 80

# Copy the build output from the build stage
COPY --from=build-env /app/publish .

# Run the application
ENTRYPOINT ["dotnet", "RedisCache.dll"]





#docker build -t RedisCache-app .
#docker run -d \
  #-p 8080:8080 \
  #--name RedisCache-container \
  #-e ASPNETCORE_URLS=http://+:8080 \
  #RedisCache-app
