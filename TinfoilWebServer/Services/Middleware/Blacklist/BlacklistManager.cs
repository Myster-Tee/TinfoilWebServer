using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.BlackList;

public class BlacklistManager : IBlacklistManager, IDisposable
{
    private readonly IBlacklistSettings _blacklistSettings;
    private readonly IBlacklistSerializer _blacklistSerializer;
    private readonly IFileChangeHelper _fileChangeHelper;
    private readonly ILogger<BlacklistManager> _logger;
    private readonly IBootInfo _bootInfo;

    private readonly object _lock = new();
    private readonly HashSet<IPAddress> _blacklistedIps = new();

    private readonly Dictionary<IPAddress, int> _consecutiveUnauthorizedPerIp = new();
    private string? _blacklistFullFilePath;


    public BlacklistManager(IBlacklistSettings blacklistSettings, IBlacklistSerializer blacklistSerializer, IFileChangeHelper fileChangeHelper, ILogger<BlacklistManager> logger, IBootInfo bootInfo)
    {
        _blacklistSettings = blacklistSettings ?? throw new ArgumentNullException(nameof(blacklistSettings));
        _blacklistSerializer = blacklistSerializer ?? throw new ArgumentNullException(nameof(blacklistSerializer));
        _fileChangeHelper = fileChangeHelper ?? throw new ArgumentNullException(nameof(fileChangeHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));

        _blacklistSettings.PropertyChanged += OnBlacklistSettingsChanged;
        _fileChangeHelper.FileChanged += OnBlacklistFileChanged;
    }

    private void OnBlacklistSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IBlacklistSettings.Enabled))
        {
            if (_blacklistSettings.Enabled)
                _logger.LogInformation("Blacklist feature enabled.");
            else
                _logger.LogWarning("Blacklist feature disabled.");

            SafeInitializeInternal(true);
        }
        else if (e.PropertyName == nameof(IBlacklistSettings.FilePath))
        {
            _logger.LogInformation($"Blacklist file path changed to \"{_blacklistSettings.FilePath}\".");

            SafeInitializeInternal(false);
        }

    }

    private void OnBlacklistFileChanged(object sender, FileChangedEventHandlerArgs args)
    {
        SafeLoadBlacklistedIps();
    }

    public bool IsIpBlacklisted(IPAddress ipAddress)
    {
        if (!_blacklistSettings.Enabled)
            return false;

        lock (_lock)
        {
            return _blacklistedIps.Contains(ipAddress);
        }
    }


    public void ReportIpUnauthorized(IPAddress ipAddress)
    {
        if (!_blacklistSettings.Enabled)
            return;

        lock (_lock)
        {
            if (_consecutiveUnauthorizedPerIp.TryGetValue(ipAddress, out var nbTries))
            {
                nbTries++;
                _consecutiveUnauthorizedPerIp[ipAddress] = nbTries;
            }
            else
            {
                nbTries = 1;
                _consecutiveUnauthorizedPerIp.Add(ipAddress, nbTries);
            }

            if (nbTries >= _blacklistSettings.MaxConsecutiveFailedAuth)
            {
                _logger.LogWarning($"IP \"{ipAddress}\" has been blacklisted after {nbTries} attempts.");
                _blacklistedIps.Add(ipAddress);
                _consecutiveUnauthorizedPerIp.Remove(ipAddress);
                SafeSaveBlacklistedIps();
            }
        }
    }

    private void SafeSaveBlacklistedIps()
    {
        _fileChangeHelper.EnableFileChangedEvent = false;

        try
        {
            HashSet<IPAddress> blacklistedIpsCopy;
            lock (_lock)
            {
                blacklistedIpsCopy = _blacklistedIps.ToHashSet();
            }

            if (_blacklistFullFilePath != null)
                _blacklistSerializer.Serialize(_blacklistFullFilePath, blacklistedIpsCopy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to save blacklisted IPs to \"{_blacklistFullFilePath}\": {ex.Message}");
        }

        _fileChangeHelper.EnableFileChangedEvent = true;
    }

    public void ReportIpAuthorized(IPAddress ipAddress)
    {
        if (!_blacklistSettings.Enabled)
            return;

        lock (_lock)
        {
            _consecutiveUnauthorizedPerIp.Remove(ipAddress);
        }
    }

    public void Initialize()
    {
        SafeInitializeInternal(true);
    }

    private void SafeInitializeInternal(bool clearConsecutiveUnauthorizedPerIp)
    {
        if (clearConsecutiveUnauthorizedPerIp)
            _consecutiveUnauthorizedPerIp.Clear();
        SafeInitializeBlacklistFullFilePath();
        SafeInitializeBlacklistFileChangeDetection();
        SafeLoadBlacklistedIps();
    }


    private void SafeInitializeBlacklistFullFilePath()
    {
        try
        {
            _blacklistFullFilePath = null;
            if (!_blacklistSettings.Enabled)
                return;

            var blacklistFilePath = (_blacklistSettings.FilePath ?? "").Trim();
            if (blacklistFilePath.Length == 0)
            {
                if (_blacklistSettings.Enabled)
                    _logger.LogWarning($"IP blacklisting is enabled but blacklist file path is empty in configuration file \"{_bootInfo.ConfigFileFullPath}\", blacklisted IPs won't be saved.");
            }
            else
            {
                _blacklistFullFilePath = Path.GetFullPath(blacklistFilePath);
                var blacklistFileDir = Path.GetDirectoryName(_blacklistFullFilePath);
                if (blacklistFileDir != null && !Directory.Exists(blacklistFileDir))
                {
                    Directory.CreateDirectory(blacklistFileDir);
                    _logger.LogInformation($"Directory \"{blacklistFileDir}\" created.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize blacklist full file path: {ex.Message}");
            _blacklistFullFilePath = null;
        }
    }

    private void SafeInitializeBlacklistFileChangeDetection()
    {
        _fileChangeHelper.StopWatching();
        try
        {
            if (_blacklistFullFilePath != null)
                _fileChangeHelper.WatchFile(_blacklistFullFilePath, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to watch changes of file \"{_blacklistFullFilePath}\": {ex.Message}");
        }
    }

    private void SafeLoadBlacklistedIps()
    {
        lock (_lock)
        {
            try
            {
                _blacklistedIps.Clear();

                if (_blacklistFullFilePath == null)
                    return;

                if (File.Exists(_blacklistFullFilePath))
                {
                    _blacklistSerializer.Deserialize(_blacklistFullFilePath, _blacklistedIps);

                    _logger.LogInformation($"{_blacklistedIps.Count} blacklisted IPs successfully loaded.");
                }
                else
                {
                    _logger.LogInformation($"Blacklist file \"{_blacklistFullFilePath}\" not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load blacklisted IPs from file \"{_blacklistFullFilePath}\": {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _fileChangeHelper.StopWatching();
    }
}