# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/MommyAndSunny.API/MommyAndSunny.API.csproj
RUN dotnet publish src/MommyAndSunny.API/MommyAndSunny.API.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads /app/keys && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MommyAndSunny.API.dll"]
