FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SportsManagementMVC.csproj", "./"]
RUN dotnet restore "SportsManagementMVC.csproj"

COPY . .
RUN dotnet publish "SportsManagementMVC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet SportsManagementMVC.dll --urls http://0.0.0.0:${PORT:-8080}"]