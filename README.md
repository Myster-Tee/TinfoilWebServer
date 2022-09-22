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
  "IndexType": "<enum>",                        // The type of index file returned to Tinfoil, can be either "Flatten" or "Hierarchical".
  "CacheExpiration": "<duration>",              // Index cache expiration time, format is «[d'.']hh':'mm':'ss['.'fffffff]».
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

- ***Note 1:** If "ServedDirectories" is omitted, current directory will be used.*
- ***Note 2:** If "AllowedExt" is omitted, the following extensions ["xci", "nsz", "nsp"] will be used.*
- ***Note 3:** If "IndexType" is omitted, "Flatten" type will be used.  
When "Flatten" index is used, all files are returned at once, including files from subdirectories.  
When "Hierachical" index is used, only files and folders contained in the corresponding requested directory will be returned.*

