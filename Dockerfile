# Đã xác nhận máy bạn dùng .NET 10 (dotnet --version = 10.0.302)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/MommyAndSunny.API/MommyAndSunny.API.csproj
RUN dotnet publish src/MommyAndSunny.API/MommyAndSunny.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MommyAndSunny.API.dll"]