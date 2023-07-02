<#
    PowerShell script for generating binaries assets

    - Publish command
        
        PowerShell ./Publish.ps1


    - To allow PowerShell script execution, run PowerShell as Administrator and run

        Set-ExecutionPolicy Unrestricted

    - To restore default policy, run the command

        Set-ExecutionPolicy RemoteSigned

    - Documentation

        dotnet publish     :    https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish
        runtime identifier :    https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
#>

${OutDirRoot}="Publish"

$TinfoilWebServerVersion= Select-Xml -Path "TinfoilWebServer/TinfoilWebServer.csproj" -XPath "//Project/PropertyGroup/Version" | Select-Object -ExpandProperty Node | Select-Object -ExpandProperty InnerText
Write-Host "Tinfoil version read: $TinfoilWebServerVersion"


if (Test-Path ${OutDirRoot}) {
     Write-Host "Deleting output folder ""${OutDirRoot}""."
    Remove-Item ${OutDirRoot} -Recurse
}

dotnet clean "TinfoilWebServer/TinfoilWebServer.csproj"

#Framework Dependent Portable
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Dependent-portable"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained false `
    -c Release `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "${PublishDir}.zip"
Remove-Item ${PublishDir} -Recurse

#=== Framework Dependent win-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Dependent-win-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained false `
    -c Release `
    -r win-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "$PublishDir.zip"
Remove-Item ${PublishDir} -Recurse

#Framework Independent win-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Independent-win-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained true `
    -c Release `
    -r win-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "${PublishDir}.zip"
Remove-Item ${PublishDir} -Recurse

#Framework Dependent linux-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Dependent-linux-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained false `
    -c Release `
    -r linux-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "$PublishDir.zip"
Remove-Item ${PublishDir} -Recurse

#Framework Independent linux-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Independent-linux-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained true `
    -c Release `
    -r linux-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "$PublishDir.zip"
Remove-Item ${PublishDir} -Recurse

#Framework Dependent osx-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Dependent-osx-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained false `
    -c Release `
    -r osx-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "${PublishDir}.zip"
Remove-Item ${PublishDir} -Recurse

#Framework Independent osx-x64
$PublishDir="${OutDirRoot}/TinfoilWebServer_v${TinfoilWebServerVersion}_Framework-Independent-osx-x64"

dotnet publish TinfoilWebServer/TinfoilWebServer.csproj `
    --self-contained true `
    -c Release `
    -r osx-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir

Compress-Archive -Path $PublishDir -DestinationPath "${PublishDir}.zip"
Remove-Item ${PublishDir} -Recurse