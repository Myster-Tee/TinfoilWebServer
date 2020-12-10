# TinfoilWebServer

## Description
Install your Nintendo Switch packages (NSP, NSZ, XCI, etc.) with [Tinfoil](https://tinfoil.io/Download) from your own server.

All served files must have "[titleid]" in the file name to be recognized by Tinfoil to show up in "New Games", "New DLC", and "New Updates".

## Requirements
To run the server you'll need to install the [ASP.NET Core Runtime 5.X.X or more](https://dotnet.microsoft.com/download/dotnet/5.0).

## Config file

```jsonc 
{
  ServedDirectory: "DirectoryPath",       // ex: "C:\SomeDirContainingPackages"
  "AllowedExt": ["ext1", "ext2", "ext3"], // List of file extensions to serve, ex: [ "nsp", "nsz", "xci" ]
  "MessageOfTheDay": "SomeText",          // The welcome message displayed when Tinfoil starts scanning files
  "IndexType": "enum",                    // The type of index file returned to Tinfoil, can be either "Flatten" or "Hierarchical".
  "Kestrel": {                            // HTTP server configuration see https://docs.microsoft.com/fr-fr/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-5.0#configureiconfiguration for more information
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      }
    }
  }
}
```
