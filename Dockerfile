# Kiểm tra đúng version .NET bạn đang dùng bằng lệnh `dotnet --version` trước khi build,
# đổi số "9.0" bên dưới nếu máy bạn đang dùng version khác (VD: 8.0, 10.0)

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/MommyAndSunny.API/MommyAndSunny.API.csproj
RUN dotnet publish src/MommyAndSunny.API/MommyAndSunny.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "MommyAndSunny.API.dll"]