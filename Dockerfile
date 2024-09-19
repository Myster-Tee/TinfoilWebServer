FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS sdk

FROM sdk AS build

WORKDIR /src
COPY . .
RUN dotnet restore TinfoilWebServer/TinfoilWebServer.csproj
WORKDIR /src/TinfoilWebServer
RUN dotnet build TinfoilWebServer.csproj -c Release -o /app

FROM build AS publish

RUN dotnet publish TinfoilWebServer.csproj -p ContainerUser=root -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
USER root
RUN mkdir -p /app/config
ENTRYPOINT ["dotnet", "TinfoilWebServer.dll", "-d", "/app/config"]