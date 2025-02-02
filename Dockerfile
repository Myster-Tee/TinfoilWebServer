# Take official .NET 8 SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-img
WORKDIR /build-dir

# Copy all files to the build-img image
COPY . ./

# Restore Nugets and publish the project
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Take official .NET 8 Runtime target image and copy the published files from the build image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime-img
WORKDIR /app
COPY --from=build-img /build-dir/out .

# The TinfoilWebServer working directory to map
VOLUME /working_dir

# Setup the startup command
WORKDIR /working_dir
ENTRYPOINT ["dotnet", "/app/TinfoilWebServer.dll" ]
