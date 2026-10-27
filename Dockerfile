# Stage 1: build the app with the .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/JobTrack.Api/JobTrack.Api.csproj src/JobTrack.Api/
RUN dotnet restore src/JobTrack.Api/JobTrack.Api.csproj
COPY src/JobTrack.Api/ src/JobTrack.Api/
RUN dotnet publish src/JobTrack.Api/JobTrack.Api.csproj -c Release -o /app

# Stage 2: a small image that only runs the finished app
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Render tells us which port to use in the PORT variable (8080 if it is not set).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet JobTrack.Api.dll"]
