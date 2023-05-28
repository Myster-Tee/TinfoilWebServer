using Microsoft.AspNetCore.Http;
using System.Diagnostics.Contracts;
using TinfoilWebServer.Services.VirtualFS;

namespace TinfoilWebServer.Services;

public interface IVirtualItemFinder
{
    [Pure]
    VirtualItem? Find(PathString pathString);
}