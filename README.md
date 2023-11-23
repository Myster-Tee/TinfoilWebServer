# TinfoilWebServer

## Description

Install your Nintendo Switch packages (NSP, NSZ, XCI, etc.) with [Tinfoil](https://tinfoil.io/Download) from your own server.

All served files must have "[titleid]" in the file name to be recognized by Tinfoil to show up in "New Games", "New DLC", and "New Updates". Official Tinfoil documentation [here](https://blawar.github.io/tinfoil/network/).

## Download

Releases page [here](https://github.com/Myster-Tee/TinfoilWebServer/releases).

If you're downloading using macOS Safari, download your desired package by right-clicking on the link and selecting "Download Linked File", so Safari doesn't unzip the package automatically. Then you can unzip it afterwards using macOS's built-in Archive Utility.

## Requirements

The requirements depend on the version you choose to download.

### Framework-Dependent version

This version is lightweight but you'll need to install the [ASP.NET Core Runtime 6.X.X](https://dotnet.microsoft.com/download/dotnet/6.0) before running the server.

### Framework-Independent version

No requirements but heavyweight.

## Running the server

1. Unzip the desired distribution to a location of your choice
1. Edit the configuration file _TinfoilWebServer.config.json_ according to your needs
1. Start the server according to the chosen distribution

### Windows

Run

```sh
TinfoilWebServer.exe
```

### Linux and macOS

Run

```sh
./TinfoilWebServer
```

### Portable version - Windows, Linux and macOS (Framework required)

Run

```sh
dotnet TinfoilWebServer.dll
```

#### Command line options

```txt
-c, --config    Custom location of the configuration file.
--help          Display this help screen.
--version       Display version information.
```

## Setting up Tinfoil on your Switch

1. Launch **Tinfoil**
1. Go to **File Browser**
1. Press **[-]** button to add a new server
1. Set **Protocol** to HTTP or HTTPS according to the server configuration
1. Set **Host** to any host pointing to your server (or the server IP address)
   _The server IP address is logged at server startup_
1. If authentication is enabled, set **Username** and **Password** to one of the allowed users
1. Set **Title** to a name of your choice

## TinfoilWebServer.config.json format

```js
{
  "ServedDirectories": string[],        // ex: ["C:\\SomeDir\\WindowsDirWithPackages", "/dev/sda1/LinuxDirWithPackages", ".", "/Users/yourname/Documents/macOSDirWithPackages"] !!! Don't forget to escape backslashes with another one !!! No need to escape spaces
  "StripDirectoryNames": boolean,       // «true» to remove directories names in URLs of served files, «false» otherwise
  "ServeEmptyDirectories": boolean,     // «true» to serve empty directories, «false» otherwise (has no effect when "StripDirectoryNames" is «true»)
  "AllowedExt": string[],               // List of file extensions to serve, ex: [ "nsp", "nsz", "xci" ]
  "MessageOfTheDay": string,            // The welcome message displayed when Tinfoil successfully contacts the server
  "ExtraRepositories": string[],        // A set of extra repositories sent to Tinfoil for scanning (see https://blawar.github.io/tinfoil/custom_index/ for more information)
  "CacheExpiration": {
    "Enable": boolean ,                 // «true» to enable cache expiration, «false» otherwise
    "ExpirationDelay": string,          // Index cache expiration time, format is «[d'.']hh':'mm':'ss['.'fffffff]», ex: "01:30:15" for 1h30m15s
  },
  "Authentication": {
    "Enabled": boolean,                 // «true» to enable authentication, «false» otherwise
    "WebBrowserAuthEnabled": boolean,   // «true» to enable the native Web Browser login prompt when not authenticated (has no effect when "Authentication.Enabled" is «false»)
    "Users": [                          // List of allowed users (use a comma as separator for declaring multiple users)
      {
        "Name": string,                 // The user name
        "Pwd": string,                  // The password
        "MessageOfTheDay": string       // Custom message for the user
      }
    ]
  },
  "Blacklist": {
    "Enabled": boolean,                 // Enable or disable the IP blacklisting feature
    "FilePath": string,                 // The path of the file where to save blacklisted IPs
    "MaxConsecutiveFailedAuth": number, // The number of consecutived unauthenticated requests to reach for blacklisting an IP
    "IsBehindProxy": boolean            // When set to true, incoming IP address will be taken fromFo "X-Forwarded-For" header otherwise it will be taken from TCP/IP protocol
  },
  "Kestrel": {                          // Web server configuration, see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-6.0 for more information
    "Endpoints": {
      "Http": {
        "Url": string                   // The HTTP host (or IP address) and port that the server should listen to (ex: "http://0.0.0.0:80", "http://*:80/", "http://somedomain.com")
      },
      "HttpsInlineCertAndKeyFile": {    // See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-6.0 for more examples and possibilities
        "Url": string,                  // The HTTPS host (or IP address) and port that the server should listen to (ex: "https://somedomain.com", "https://somedomain.com:8081")
        "Certificate": {
          "Path": string,               // The path to the certificate file (ex: "MyCertificate.cer")
          "KeyPath": string             // The path to the private key file (ex: "MyPrivateKey.key")
        }
      }
    }
  },
  "Logging": {                          // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
    "LogLevel": {
      "Default": string                 // The global log level, can be one of "Trace", "Debug", "Information", "Warning", "Error", "Critical", or "None"
    },
    "Console": {                        // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
      "LogLevel": {
        "Default": string
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

#### Default settings

- When _"Kestrel"_ configuration is omitted, server listens to _http://localhost:5000/_ and _https://localhost:5001_.
- When _"AllowedExt"_ is omitted, the following extensions _["xci", "nsz", "nsp"]_ are used.

## Security considerations and recommendations

If you plan to open your server to the Internet network (WAN) instead of local network (LAN) only, I would highly recommend you to:

1. Setup HTTPS only
1. Enable authentication to restrict access to some specific users
1. Serving directories with only Switch packages (without personal data)
1. Serve only Switch packages file extensions
1. Setup _StripDirectoryNames_ setting to _true_ to hide your personal folder tree organization
1. Enable IP blacklistng feature
