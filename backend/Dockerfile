FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製專案檔做 restore，原始碼變動時可沿用套件層快取
COPY global.json Directory.Build.props ./
COPY EndlessBattle.Domain/EndlessBattle.Domain.csproj EndlessBattle.Domain/
COPY EndlessBattle.Application/EndlessBattle.Application.csproj EndlessBattle.Application/
COPY EndlessBattle.Infrastructure/EndlessBattle.Infrastructure.csproj EndlessBattle.Infrastructure/
COPY EndlessBattle.Api/EndlessBattle.Api.csproj EndlessBattle.Api/
RUN dotnet restore EndlessBattle.Api/EndlessBattle.Api.csproj

COPY . .
RUN dotnet publish EndlessBattle.Api/EndlessBattle.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY content/ /app/content/
USER $APP_UID
ENTRYPOINT ["dotnet", "EndlessBattle.Api.dll"]
