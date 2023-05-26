using System;
using TinfoilWebServer.Services.VirtualFS;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class VirtualFileSystemProvider : IVirtualFileSystemProvider
{
    private readonly IVirtualFileSystemBuilder _virtualFileSystemBuilder;
    private readonly IAppSettings _appSettings;
    private VirtualFileSystemRoot? _root;

    public VirtualFileSystemProvider(IVirtualFileSystemBuilder virtualFileSystemBuilder, IAppSettings appSettings)
    {
        _virtualFileSystemBuilder = virtualFileSystemBuilder ?? throw new ArgumentNullException(nameof(virtualFileSystemBuilder));
        _appSettings = appSettings;
    }

    public void Initialize()
    {
        _root = _virtualFileSystemBuilder.BuildHierarchical(_appSettings.ServedDirectories);
    }

    public VirtualFileSystemRoot Root
    {
        get
        {
            if (_root == null)
                throw new InvalidOperationException($"{nameof(Initialize)} method should be invoked first!");
            return _root;
        }
    }
}