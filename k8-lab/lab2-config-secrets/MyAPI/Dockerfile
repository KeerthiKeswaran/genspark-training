# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY MyAPI.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# 2. Intermediate runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-env
WORKDIR /app
COPY --from=build /app .

# 3. Final metadata-clean stage
FROM scratch

# Copy everything from the intermediate stage to scratch (preserves files, removes base metadata/labels)
COPY --from=runtime-env / /

# Re-configure environment variables and runtime settings
WORKDIR /app
EXPOSE 8080
ENV PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    APP_UID=1654 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_VERSION=10.0.9 \
    ASPNET_VERSION=10.0.9

ENTRYPOINT ["dotnet", "MyAPI.dll"]
