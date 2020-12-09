# TinfoilWebServer

## Description
Install your Nintendo Switch packages (NSP, NSZ, XCI, etc.) with [Tinfoil](https://tinfoil.io/Download) from your own server.

## Requirements
To run the server you'll need to install the [ASP.NET Core Runtime 5.X.X or more](https://dotnet.microsoft.com/download/dotnet/5.0).

## Config file

```
{
  "ServedDirectory": "C:\SomeDirContainingPackages",
  "AllowedExt": [ "nsp", "nsz", "xci" ],
  "Kestrel": {
    /*
     * Please refer to this link for more information about how to fill this section
     * https://docs.microsoft.com/fr-fr/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-5.0#configureiconfiguration
     */
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:80"
      }
    }
  }
}
```
