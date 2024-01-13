# TinfoilWebServer

## Description

*Build your custom Nintendo Switch shop.*  

TinfoilWebServer is a simple and efficient cross platform web server application aiming at serving your personal Nintendo Switch packages (NSP, NSZ, XCI, etc.) to [Tinfoil](https://tinfoil.io/Download).

All served files must have "[titleid]" in the file name to be recognized by Tinfoil to show up in "New Games", "New DLC", and "New Updates".  
Official Tinfoil documentation [here](https://blawar.github.io/tinfoil/network/).

## Download

Releases page [here](https://github.com/Myster-Tee/TinfoilWebServer/releases).

** If you're downloading using macOS Safari, download your desired package by right-clicking on the link and selecting "Download Linked File", so Safari doesn't unzip the package automatically. Then you can unzip it afterwards using macOS's built-in Archive Utility.*

## Requirements

The requirements depend on the version you choose to download.

### Framework-Dependent version

This version is lightweight but you'll need to install the [ASP.NET Core Runtime 8.X.X](https://dotnet.microsoft.com/download/dotnet/8.0) before running the server.

### Framework-Independent version

No requirements but heavyweight.

## Running the server

1. Unzip the desired distribution to a location of your choice
1. Create a *TinfoilWebServer.config.json* according to your needs
1. Start the server according to the chosen distribution

By default, the *TinfoilWebServer.config.json* file will be searched in the program's current directory.  
A template of *TinfoilWebServer.config.json* can be downloaded in the assets of the releases page [here](https://github.com/Myster-Tee/TinfoilWebServer/releases).

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
-c, --config        Custom location of the configuration file.
-d, --currentDir    Change the current directory.
-s, --winService    Run the server as a Windows service.
--sha256            Compute SHA256 passwords interactively.
--help              Display this help screen.
--version           Display version information.
```

## Setting up Tinfoil on your Switch

1. Launch **Tinfoil**
1. Go to **File Browser**
1. Press **[-]** button to add a new server
1. Set **Protocol** to HTTP or HTTPS according to the server configuration
1. Set **Host** to any host pointing to your server (or the server IP address)  
   _The server IP address is logged at server startup._
1. If authentication is enabled, set **Username** and **Password** to one of the allowed users
1. Set **Title** to a name of your choice

***Download speed note**: having a download/installation speed of ~10MB from Tinfoil is normal.  
This low speed is only due to hardware limitation of Nintendo Switch, and is not at all related to some server limitation.*


## TinfoilWebServer.config.json format

```js
{
  "ServedDirectories": string[],        // ex: ["C:\\SomeDir\\WindowsDirWithPackages", "/dev/sda1/LinuxDirWithPackages", ".", "/Users/yourname/Documents/macOSDirWithPackages"] !!! Don't forget to escape backslashes with another one !!! No need to escape spaces
  "StripDirectoryNames": boolean,       // «true» to remove directories names in URLs of served files, «false» otherwise
  "ServeEmptyDirectories": boolean,     // «true» to serve empty directories, «false» otherwise (has no effect when "StripDirectoryNames" is «true»)
  "AllowedExt": string[],               // List of file extensions to serve (default is ["xci", "nsz", "nsp", "xcz", "zip"])
  "MessageOfTheDay": string,            // The welcome message displayed when Tinfoil successfully contacts the server
  "CustomIndexPath": string,            // The path to a custom JSON file to be merged with the served index
  "Cache": {
    "AutoDetectChanges": boolean,       // «true» to auto-refresh the list of served files when a file system change is detected in the served directories, «false» otherwise
    "PeriodicRefreshDelay": string      // Periodic delay for forcing the refresh of served files, format is «[d'.']hh':'mm':'ss['.'fffffff]», ex: "01:30:15" for 1h30m15s, set «null» to disable
  },
  "Authentication": {
    "Enabled": boolean,                 // «true» to enable authentication, «false» otherwise
    "WebBrowserAuthEnabled": boolean,   // «true» to enable the native Web Browser login prompt when not authenticated (has no effect when "Authentication.Enabled" is «false»)
    "PwdType": string,                  // Defines the format of user passwords. Can be either "Sha256" or "Plaintext". Default is "Sha256".
    "Users": [                          // List of allowed users (use a comma as separator for declaring multiple users)
      {
        "Name": string,                 // The user name
        "Pwd": string,                  // The password
        "MaxFingerprints": number,      // The maximum number of fingerprints allowed for this user (default is 1)
        "MessageOfTheDay": string,      // Custom message for the user
        "CustomIndexPath": string       // The path to a custom JSON file for this user to be merged with the served index
      }
    ]
  },
  "FingerprintsFilter": {
    "Enabled" : boolean,                // «true» to enable fingerprints validation filter, «false» otherwise (default is true)
    "FingerprintsFilePath": string      // The path to the file where to save allowed fingerprints
    "MaxFingerprints" : number          // The maximum number of global fingerprints allowed
  },
  "Blacklist": {
    "Enabled": boolean,                 // Enable or disable the IP blacklisting feature
    "FilePath": string,                 // The path of the file where to save blacklisted IPs
    "MaxConsecutiveFailedAuth": number, // The maximum number of consecutive unauthenticated requests to reach for blacklisting an IP
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
    },
    "Limits": {
      "MaxConcurrentConnections": number, // Sets the maximum number of open connection
    }
  },
  "Logging": {                          // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
    "LogLevel": {
      "Default": string                 // The global log level, can be one of "Trace", "Debug", "Information", "Warning", "Error", "Critical", or "None"
    },
    "Console": {                        // See https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-6.0 for more information
      "LogLevel": {
        "Default": string
      },
      "FormatterOptions": {
        "Format": string,               // The custom log format (see below for more information)
        "ExceptionFormat": string       // The custom exception format (see below for more information)
	  }	
    },
    "File": {                           // See https://github.com/nreco/logging#how-to-use for more information
      "Path": string,
      "Append": boolean,
      "MinLevel": string,
      "FileSizeLimitBytes": number,
      "MaxRollingFiles": number,
      "FormatterOptions": {
        "Format": string,               // The custom log format (see below for more information)
        "ExceptionFormat": string       // The custom exception format (see below for more information)
	  }	
    }
  }
}
```

When *"Kestrel"* configuration is omitted, server default listens to _http://localhost:5000/_ and _https://localhost:5001_.

### Custom Index

Specifying a custom index in the configuration file allows to set (or combine) any extra property described in [Tinfoil Custom Index documentation](https://blawar.github.io/tinfoil/custom_index/).  
For example, using the JSON below, it is possible to enrich the served files with custom files **out of served directories**.
```
{
  "files": ["https://some/other/url1", "https://some/other/url2"] // Will be combined with served files
}
```

### IP Blacklisting

File format is text, one line per blacklisted IP. Empty lines will be ignored. Use # to write comments.


### Fingerprints

A fingerprint consists in a unique Nintendo Switch device identifier.
Tinfoil emits a fingerprint only when requesting the index, but not when requesting files. Thus specifying fingerprints in configuration <u>will not prevent a user from downloading files</u>.

When fingerprints are both allowed globally and at user level, server will check for a valid fingerprint among allowed user fingerprints and globally allowed fingerprints.

### Custom log format

You can specify a custom log format using keywords between braces.  
Some keywords accept options in the format of *\{SomeKeyword:SomeOptions\}*.

Example:

```
"Format" : "> {Date:yyyy-MM-dd@HH:mm:ss}-{LogLevel:U}-{Category}: {Message}{NewLine}{Exception}"
```

#### Supported keywords for *Format* field:

- \{Date:\<Option\>\}: the log date, optional option: [a valid date format](https://learn.microsoft.com/fr-fr/dotnet/api/system.datetime.tostring?view=net-8.0#system-datetime-tostring(system-string))
- \{NewLine\}: appends a new line according to the current system (\r, \n or \r\n)
- \{Message\}: the log message
- \{Category\}: the log category (.NET Namespace)
- \{EventId\}: the log event ID
- \{LogLevel:\<Option\>\}: the log level. Optional options:
  - SU: short log level upper case
  - SL: short log level lower case
  - U: log level upper case
  - L: log level lower case
- \{Exception\}: the exception formatted according to the *ExceptionFormat* field

#### Supported keywords for *ExceptionFormat* field:

- \{Date:\<Option\>\}: same as *\{Date\}* keyword of *Format* field
- \{NewLine\}: same as *\{NewLine\}* keyword of *Format* field
- \{Message\}: the exception message
- \{Type\}: the exception type name
- \{StackTrace\}: the exception stack trace

### Running as a Windows service

Server can be run as a Windows service. To to this, service must first be registered using [sc.exe](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-config), PowerShell [New-Service](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/new-service) cmdlet or any other method.  
Don't forget to specify the `--winService` command line parameter when registering the service.

*Note: by default, Windows services are run in **%WinDir%\System32**, you can change the current directory using **--currentDir** command line parameter.*

```powershell
# PowerShell service registration example
New-Service TinfoilWebServer -BinaryPathName "<FullPathToExe> --winService --config ""<SomePathToConfigJson>"" --currentDir ""<SomePathToCurrentDir>"""
```

## Sharing game saves

TinfoilWebServer can serve game saves by allowing "zip" file extension (by default).  
To share your own saves, copy Tinfoil saves from `SDCard/switch/tinfoil/saves/**/*.zip` to any of the served directories, preserving original file name. 

## Security considerations and recommendations

If you plan to open your server to the Internet network (WAN) instead of local network (LAN) only, I would highly recommend you to:

1. Setup HTTPS only
1. Enable authentication to restrict access to some specific users
1. Serve directories containining only Nintendo Switch files (without personal data)
1. Restrict served file extensions to Nintendo Switch files
1. Setup _StripDirectoryNames_ setting to _true_ to hide your personal folder tree organization
1. Enable IP blacklistng feature
1. Enable fingerprints filter feature
1. Set *Authentication.PwdType* to "Sha256" to avoid storing plaintext user passwords in your config file


## Similar Projects
If you want to create your personal NSP Shop then check out these other similar projects:
- [a1ex4/Ownfoil](https://github.com/a1ex4/ownfoil)
- [eXhumer/pyTinGen](https://github.com/eXhumer/pyTinGen)
- [JackInTheShop/FT-SCEP](https://github.com/JackInTheShop/FT-SCEP)
- [gianemi2/tinson-node](https://github.com/gianemi2/tinson-node)
- [BigBrainAFK/tinfoil_gdrive_generator](https://github.com/BigBrainAFK/tinfoil_gdrive_generator)
- [ibnux/php-tinfoil-server](https://github.com/ibnux/php-tinfoil-server)
- [ramdock/nut-server](https://github.com/ramdock/nut-server)
- [DevYukine/rustfoil](https://github.com/DevYukine/rustfoil)
- [Orygin/gofoil](https://github.com/Orygin/gofoil)