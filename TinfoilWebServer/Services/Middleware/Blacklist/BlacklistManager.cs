using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Blacklist;

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
    private FileInfo? _blacklistFile;
    private IWatchedFile? _watchedFile;


    public BlacklistManager(IBlacklistSettings blacklistSettings, IBlacklistSerializer blacklistSerializer, IFileChangeHelper fileChangeHelper, ILogger<BlacklistManager> logger, IBootInfo bootInfo)
    {
        _blacklistSettings = blacklistSettings ?? throw new ArgumentNullException(nameof(blacklistSettings));
        _blacklistSerializer = blacklistSerializer ?? throw new ArgumentNullException(nameof(blacklistSerializer));
        _fileChangeHelper = fileChangeHelper ?? throw new ArgumentNullException(nameof(fileChangeHelper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));

        _blacklistSettings.PropertyChanged += OnBlacklistSettingsChanged;
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
        if (_watchedFile != null)
            _watchedFile.FileChangedEventEnabled = false;

        try
        {
            HashSet<IPAddress> blacklistedIpsCopy;
            lock (_lock)
            {
                blacklistedIpsCopy = _blacklistedIps.ToHashSet();
            }

            if (_blacklistFile != null)
                _blacklistSerializer.Serialize(_blacklistFile.FullName, blacklistedIpsCopy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to save blacklisted IPs to \"{_blacklistFile}\": {ex.Message}");
        }

        if (_watchedFile != null)
            _watchedFile.FileChangedEventEnabled = true;
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
        SafeInitializeBlacklistFile();
        SafeInitializeBlacklistFileChangeDetection();
        SafeLoadBlacklistedIps();
    }


    private void SafeInitializeBlacklistFile()
    {
        try
        {
            _blacklistFile = null;
            if (!_blacklistSettings.Enabled)
                return;

            var blacklistFilePath = _blacklistSettings.FilePath;

            if (string.IsNullOrWhiteSpace(blacklistFilePath))
            {
                _logger.LogWarning($"IP blacklisting is enabled but blacklist file path is empty in configuration file \"{_bootInfo.ConfigFileFullPath}\", blacklisted IPs won't be saved.");
                return;
            }

            _blacklistFile = new FileInfo(blacklistFilePath);

            if (_blacklistFile.Directory == null)
                throw new Exception($"The directory of blacklist file \"{_blacklistFile}\" can't be determined.");

            var blacklistFileDir = _blacklistFile.Directory;
            blacklistFileDir.Refresh();
            if (!blacklistFileDir.Exists)
            {
                _blacklistFile.Directory.Create();
                _logger.LogInformation($"Blacklist directory \"{blacklistFileDir.FullName}\" created.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize blacklist file: {ex.Message}");
            _blacklistFile = null;
        }
    }

    private void SafeInitializeBlacklistFileChangeDetection()
    {
        _watchedFile?.Dispose();

        var blacklistFullFilePath = _blacklistFile;

        if (blacklistFullFilePath == null)
            return;

        try
        {
            _watchedFile = _fileChangeHelper.WatchFile(blacklistFullFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to watch changes of file \"{blacklistFullFilePath}\": {ex.Message}");
            return;
        }

        _watchedFile.FileChanged += OnBlacklistFileChanged;
    }

    private void SafeLoadBlacklistedIps()
    {
        lock (_lock)
        {
            try
            {
                _blacklistedIps.Clear();

                if (_blacklistFile == null)
                    return;

                _blacklistFile.Refresh();
                if (_blacklistFile.Exists)
                {
                    _blacklistSerializer.Deserialize(_blacklistFile.FullName, _blacklistedIps);

                    _logger.LogInformation($"{_blacklistedIps.Count} blacklisted IPs successfully loaded.");
                }
                else
                {
                    _logger.LogInformation($"Blacklist file \"{_blacklistFile.FullName}\" not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load blacklisted IPs from file \"{_blacklistFile}\": {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _watchedFile?.Dispose();
    }
}