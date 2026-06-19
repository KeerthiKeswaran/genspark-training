# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy solution and project files first (better caching)
COPY *.slnx .
COPY LibrarySystem.API/*.csproj LibrarySystem.API/
COPY LibrarySystem.Business/*.csproj LibrarySystem.Business/
COPY LibrarySystem.Contracts/*.csproj LibrarySystem.Contracts/
COPY LibrarySystem.Data/*.csproj LibrarySystem.Data/
COPY LibrarySystem.Models/*.csproj LibrarySystem.Models/
# Restore dependencies
RUN dotnet restore library-system-api.slnx

# Copy everything else
COPY . .

# Publish API project only
RUN dotnet publish LibrarySystem.API -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

USER root
RUN apt-get update && apt-get install -y iputils-ping && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "LibrarySystem.API.dll"]