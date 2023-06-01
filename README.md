# TinfoilWebServer

## Description
Install your Nintendo Switch packages (NSP, NSZ, XCI, etc.) with [Tinfoil](https://tinfoil.io/Download) from your own server.

All served files must have "[titleid]" in the file name to be recognized by Tinfoil to show up in "New Games", "New DLC", and "New Updates".

## Download
Releases page [here](https://github.com/Myster-Tee/TinfoilWebServer/releases/tag).

## Requirements

The requirements depend on the version you choose to download.

### Framework-Dependent version (recommended)
This version is lightweight but you'll need to install the [ASP.NET Core Runtime 6.X.X or more](https://dotnet.microsoft.com/download/dotnet/6.0) before running the server.

### Framework-Dependent version
No requirements but heavyweight.

## TinfoilWebServer.config.json format

```jsonc
{
  "ServedDirectories": ["dir1", "dir2", ...],   // ex: ["C:\\SomeDir\\DirWithPackages", "D:\\AnotherDir", "."] !!! Don't forget to escape backslashes with another one !!!
  "AllowedExt": ["ext1", "ext2", ...],          // List of file extensions to serve, ex: [ "nsp", "nsz", "xci" ].
  "MessageOfTheDay": "SomeText",                // The welcome message displayed when Tinfoil requests the root index.
  "ExtraRepositories": ["SomeRepo1", "...],     // A set of extra repositories sent to Tinfoil for scanning (see https://blawar.github.io/tinfoil/custom_index/ for more information)
  "CacheExpiration": {
    "Enable":                                   // «true» to enable cache expiration, «false» otherwise
    "ExpirationDelay" : "<duration>",           // Index cache expiration time, format is «[d'.']hh':'mm':'ss['.'fffffff]», ex: "01:30:15" for 1h30m15s.
  },
  "Kestrel": {                                  // HTTP server configuration see «https://docs.microsoft.com/fr-fr/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-5.0#configureiconfiguration» for more information.
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"                  // See «https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-5.0» for more information.
    }
  }
  "Authentication": {
    "Enabled": true,                            // «true» to enable authentication, «false» otherwise
    "Users": [                                  // List of allowed users
      {
        "Name": "SomeUserName",
        "Pwd": "SomePassword"
      },
      ...
    ]
  }
}
```

### Default settings
- When *"Kestrel"* configuration is omitted, server listens to *http://localhost:5000/*.
- When *"ServedDirectories"* is omitted, current directory is used.
- When *"AllowedExt"* is omitted, the following extensions *["xci", "nsz", "nsp"]* are used.