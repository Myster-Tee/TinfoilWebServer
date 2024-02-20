FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src
COPY . .
RUN dotnet restore "TinfoilWebServer/TinfoilWebServer.csproj"
WORKDIR "/src/TinfoilWebServer"
RUN dotnet build "TinfoilWebServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TinfoilWebServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN chown app:app /app
USER app
RUN mkdir -p /app/config
ENTRYPOINT ["dotnet", "TinfoilWebServer.dll", "-d", "/app/config"]
