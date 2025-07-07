# Используем официальный образ SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем все файлы и восстанавливаем зависимости
COPY . ./
RUN dotnet restore

# Сборка проекта
RUN dotnet publish -c Release -o out

# Рантайм-образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Указываем команду запуска
ENTRYPOINT ["dotnet", "AnalystService.dll"]
