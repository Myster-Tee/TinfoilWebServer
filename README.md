# TinfoilWebServer

## Description
Install your Nintendo Switch packages (NSP, NSZ, XCI, etc.) with [Tinfoil](https://tinfoil.io/Download) from your own server.

All served files must have "[titleid]" in the file name to be recognized by Tinfoil to show up in "New Games", "New DLC", and "New Updates".

## Download
Releases page [here](https://github.com/Myster-Tee/TinfoilWebServer/releases).

## Requirements

The requirements depend on the version you choose to download.

### Framework-Dependent version (recommended)
This version is lightweight but you'll need to install the [ASP.NET Core Runtime 6.X.X](https://dotnet.microsoft.com/download/dotnet/6.0) before running the server.

### Framework-Dependent version
No requirements but heavyweight.

## TinfoilWebServer.config.json format

```js
{
  "ServedDirectories" : string[],       // ex: ["C:\\SomeDir\\DirWithPackages", "D:\\AnotherDir", "."] !!! Don't forget to escape backslashes with another one !!!
  "StripDirectoryNames" : boolean,      // «true» to remove directories names in URLs of served files, «false» otherwise
  "ServeEmptyDirectories" : boolean,    // «true» to serve empty directories, «false» otherwise (has no effect when "StripDirectoryNames" is «true»)
  "AllowedExt" : string[],              // List of file extensions to serve, ex: [ "nsp", "nsz", "xci" ]
  "MessageOfTheDay" : string,           // The welcome message displayed when Tinfoil successfully contacts the server
  "ExtraRepositories" : string[],       // A set of extra repositories sent to Tinfoil for scanning (see https://blawar.github.io/tinfoil/custom_index/ for more information)
  "CacheExpiration" : {
    "Enable" : boolean ,                // «true» to enable cache expiration, «false» otherwise
    "ExpirationDelay" : string,         // Index cache expiration time, format is «[d'.']hh':'mm':'ss['.'fffffff]», ex: "01:30:15" for 1h30m15s
  },
  "Authentication" : {
    "Enabled" : boolean,                // «true» to enable authentication, «false» otherwise
    "WebBrowserAuthEnabled" : boolean,  // «true» to enable the native Web Browser login prompt when not authenticated (has no effect when "Authentication.Enabled" is «false»)
    "Users" : [                         // List of allowed users (use a comma as separator for declaring multiple users)
      {
        "Name" : string,                // The user name
        "Pwd" : string,                 // The password
        "MessageOfTheDay" : string      // Custom message for the user
      }
    ]
  },
  "Blacklist": {
    "Enabled": boolean,                 // Enable or disable the IP blacklisting feature
    "FilePath": string,                 // The path of the file where to save blacklisted IPs
    "MaxConsecutiveFailedAuth": number, // The number of consecutived unauthenticated requests to reach for blacklisting an IP
    "IsBehindProxy": boolean            // When set to true, incoming IP address will be taken from "X-Forwarded-For" header otherwise it will be taken from TCP/IP protocol
  },
  "Kestrel" : {                         // HTTP server configuration see «https://docs.microsoft.com/fr-fr/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-6.0#configureiconfiguration» for more information
    "Endpoints" : {
      "Http" : {
        "Url" : string                  // The IP addresses or host addresses with ports and protocols that the server should listen, ex: "http://0.0.0.0:80"
      }
    }
  },
  "Logging" : {                         // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
    "LogLevel" : {
      "Default" : string                // Can be one of "Trace", "Debug", "Information", "Warning", "Error", "Critical", or "None"
    }
    "Console": {                        // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
      "LogLevel": {
        "Default": "Information"
      }
    },
    "File": {                           // See https://github.com/nreco/logging#how-to-use for more information
      "Path": string,   
      "Append": boolean,
      "MinLevel": string,
      "FileSizeLimitBytes": number,
      "MaxRollingFiles": number
    }
  }
}
```

### Default settings
- When *"Kestrel"* configuration is omitted, server listens to *http://localhost:5000/* and *https://localhost:5001*.
- When *"ServedDirectories"* is omitted, program directory is used.
- When *"AllowedExt"* is omitted, the following extensions *["xci", "nsz", "nsp"]* are used.