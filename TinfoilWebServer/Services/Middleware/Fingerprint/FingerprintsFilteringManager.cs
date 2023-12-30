using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Booting;
using TinfoilWebServer.Services.FSChangeDetection;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services.Middleware.Fingerprint;

public class FingerprintsFilteringManager : IFingerprintsFilteringManager
{
    private readonly IFingerprintsFilterSettings _fingerprintsFilterSettings;
    private readonly IAuthenticationSettings _authenticationSettings;
    private readonly ILogger<FingerprintsFilteringManager> _logger;
    private readonly IBootInfo _bootInfo;
    private readonly IFingerprintsSerializer _fingerprintsSerializer;
    private readonly IFileChangeHelper _fileChangeHelper;
    private AllowedFingerprints _allowedFingerprints = new();
    private FileInfo? _fingerprintsFile;
    private IWatchedFile? _fingerprintsWatchedFile;

    public FingerprintsFilteringManager(IFingerprintsFilterSettings fingerprintsFilterSettings, IAuthenticationSettings authenticationSettings, ILogger<FingerprintsFilteringManager> logger, IBootInfo bootInfo, IFingerprintsSerializer fingerprintsSerializer, IFileChangeHelper fileChangeHelper)
    {
        _fingerprintsFilterSettings = fingerprintsFilterSettings ?? throw new ArgumentNullException(nameof(fingerprintsFilterSettings));
        _authenticationSettings = authenticationSettings ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bootInfo = bootInfo ?? throw new ArgumentNullException(nameof(bootInfo));
        _fingerprintsSerializer = fingerprintsSerializer ?? throw new ArgumentNullException(nameof(fingerprintsSerializer));
        _fileChangeHelper = fileChangeHelper ?? throw new ArgumentNullException(nameof(fileChangeHelper));

        _fingerprintsFilterSettings.PropertyChanged += OnFingerprintsFilterSettingsChanged;
        _authenticationSettings.PropertyChanged += OnAuthenticationSettingsChanged;
    }

    private void OnFingerprintsFilterSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IFingerprintsFilterSettings.Enabled))
        {
            if (_fingerprintsFilterSettings.Enabled)
                _logger.LogInformation("Fingerprints filtering feature enabled.");
            else
                _logger.LogWarning("Fingerprints filtering feature disabled.");

            Initialize();
        }
        else if (e.PropertyName == nameof(IFingerprintsFilterSettings.FingerprintsFilePath))
        {
            _logger.LogInformation("Fingerprints file path changed, reloading fingerprints.");
            Initialize();
        }       
        else if (e.PropertyName == nameof(IFingerprintsFilterSettings.MaxFingerprints))
        {
            _logger.LogInformation($"Max global allowed fingerprints updated to {_fingerprintsFilterSettings.MaxFingerprints}.");
            CheckSettingsConsistency();
        }
    }

    private void OnAuthenticationSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IAuthenticationSettings.WebBrowserAuthEnabled))
        {
            CheckSettingsConsistency();
        }
        else if (e.PropertyName == nameof(IAuthenticationSettings.Enabled))
        {
            CheckSettingsConsistency();
        }
    }

    private void CheckSettingsConsistency()
    {
        if (_fingerprintsFilterSettings.Enabled && _authenticationSettings is { Enabled: true, WebBrowserAuthEnabled: true })
        {
            _logger.LogWarning($"Inconsistent configuration: Web Browser authentication is enabled ({nameof(IAuthenticationSettings.WebBrowserAuthEnabled)}) " +
                               $"as well as fingerprints filtering ({nameof(IFingerprintsFilterSettings.MaxFingerprints)}), " +
                               $"but Web Browsers never send fingerprints, only Tinfoil do.");
        }

        if (_fingerprintsFilterSettings is { Enabled: true, MaxFingerprints: <= 0 } &&
            _authenticationSettings.Users.All(user => user.MaxFingerprints <= 0))
        {
            _logger.LogWarning("Inconsistent configuration: fingerprints filtering is enabled but the number of allowed fingerprints is 0.");
        }
    }

    public bool AcceptFingerprint(string? fingerprint, IUserInfo? userInfo, string traceId)
    {
        if (!_fingerprintsFilterSettings.Enabled)
            return true;

        lock (_allowedFingerprints)
        {

            if (userInfo != null)
            {
                // User authenticated

                if (fingerprint == null)
                {
                    _logger.LogWarning($"Request [{traceId}] from authenticated user \"{userInfo.Name}\" received without fingerprint, request rejected.");
                    return false;
                }

                if (!_allowedFingerprints.PerUser.TryGetValue(userInfo.Name, out var allowedUserFingerprints))
                {
                    allowedUserFingerprints = new List<string>();
                    _allowedFingerprints.PerUser.Add(userInfo.Name, allowedUserFingerprints);
                }

                if (allowedUserFingerprints.Contains(fingerprint))
                {
                    // Fingerprint allowed
                    _logger.LogDebug($"Request [{traceId}] from authenticated user \"{userInfo.Name}\" passed fingerprint validation.");
                    return true;
                }

                var allowedGlobalFingerprints = _allowedFingerprints.Global;
                if (allowedGlobalFingerprints.Contains(fingerprint))
                {
                    // Fingerprint allowed
                    _logger.LogDebug($"Request [{traceId}] from authenticated user \"{userInfo.Name}\" passed global fingerprint validation.");
                    return true;
                }

                var maxUserFingerprints = userInfo.MaxFingerprints;

                if (allowedUserFingerprints.Count < maxUserFingerprints)
                {
                    // New fingerprint still allowed for user
                    allowedUserFingerprints.Add(fingerprint);

                    SafeSaveFingerprintsToFileAsync();

                    _logger.LogInformation($"New fingerprint \"{fingerprint}\" added to user \"{userInfo.Name}\" ({allowedUserFingerprints.Count}/{maxUserFingerprints}).");
                    return true;
                }

                // No more fingerprint allowed
                _logger.LogWarning($"Request [{traceId}] rejected, fingerprint of user \"{userInfo.Name}\" couldn't be added, maximum reached ({allowedUserFingerprints.Count}/{maxUserFingerprints}).");
                return false;

            }
            else
            {
                // No authenticated user

                if (fingerprint == null)
                {
                    // Fingerprint undefined
                    _logger.LogWarning($"Request [{traceId}] received without fingerprint, request rejected.");
                    return false;
                }

                var allowedGlobalFingerprints = _allowedFingerprints.Global;
                if (allowedGlobalFingerprints.Contains(fingerprint))
                {
                    // Fingerprint allowed
                    _logger.LogDebug($"Request [{traceId}] passed global fingerprint validation.");
                    return true;
                }

                var maxAllowedFingerprints = _fingerprintsFilterSettings.MaxFingerprints;
                if (allowedGlobalFingerprints.Count < maxAllowedFingerprints)
                {
                    // New fingerprint still allowed
                    allowedGlobalFingerprints.Add(fingerprint);

                    SafeSaveFingerprintsToFileAsync();

                    _logger.LogInformation($"New global fingerprint \"{fingerprint}\" added ({allowedGlobalFingerprints.Count}/{maxAllowedFingerprints}).");
                    return true;
                }

                // No more fingerprint allowed
                _logger.LogWarning($"Request [{traceId}] rejected, global fingerprint \"{fingerprint}\" couldn't be added, maximum reached ({allowedGlobalFingerprints.Count}/{maxAllowedFingerprints}).");
                return false;
            }
        }

    }

    private void SafeSaveFingerprintsToFileAsync()
    {
        Task.Run(() =>
        {
            if (_fingerprintsFile == null)
                return;

            try
            {
                if (_fingerprintsWatchedFile != null)
                {
                    lock (_fingerprintsWatchedFile)
                    {
                        _fingerprintsWatchedFile.FileChangedEventEnabled = false;
                        _fingerprintsSerializer.Serialize(_fingerprintsWatchedFile.File, _allowedFingerprints);
                        _fingerprintsWatchedFile.FileChangedEventEnabled = true;
                    }
                }
                else
                {
                    _fingerprintsSerializer.Serialize(_fingerprintsFile, _allowedFingerprints);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save fingerprints file to \"{_fingerprintsFile.FullName}\": {ex.Message}");
            }
        });
    }

    public void Initialize()
    {
        SafeInitializeFingerprintsFile();
        SafeLoadFingerprintsFromFile();
        CheckSettingsConsistency();
    }

    private void SafeLoadFingerprintsFromFile()
    {
        if (_fingerprintsFile == null)
            return;

        try
        {
            _fingerprintsFile.Refresh();
            if (_fingerprintsFile.Exists)
                _allowedFingerprints = _fingerprintsSerializer.Deserialize(_fingerprintsFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load fingerprints from file \"{_fingerprintsFile.FullName}\": {ex.Message}");
        }
    }


    /// <summary>
    /// Intialize members <see cref="_fingerprintsWatchedFile"/> and <see cref="_fingerprintsFile"/>
    /// and starts watching fingerprints file changes
    /// </summary>
    private void SafeInitializeFingerprintsFile()
    {
        try
        {
            _fingerprintsFile = null;
            if (_fingerprintsWatchedFile != null)
            {
                _fingerprintsWatchedFile.FileChanged -= OnFingerprintsFileChanged;
                _fingerprintsWatchedFile.Dispose();
                _fingerprintsWatchedFile = null;
            }

            if (!_fingerprintsFilterSettings.Enabled)
                return;

            var filePath = _fingerprintsFilterSettings.FingerprintsFilePath;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning($"Fingerprints filtering is enabled but file path is empty in configuration file \"{_bootInfo.ConfigFileFullPath}\", fingerprints won't be saved.");
                return;
            }

            _fingerprintsFile = new FileInfo(filePath);

            if (_fingerprintsFile.Directory == null)
                throw new Exception($"The directory of fingerprints file \"{_fingerprintsFile}\" can't be determined.");

            var fingerprintsFileDirectory = _fingerprintsFile.Directory;
            fingerprintsFileDirectory.Refresh();
            if (!fingerprintsFileDirectory.Exists)
            {
                _fingerprintsFile.Directory.Create();
                _logger.LogInformation($"Fingerprints directory \"{fingerprintsFileDirectory.FullName}\" created.");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize fingerprints file: {ex.Message}");
            _fingerprintsFile = null;
            return;
        }

        try
        {
            _fingerprintsWatchedFile = _fileChangeHelper.WatchFile(_fingerprintsFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to watch changes of file \"{_fingerprintsFile.FullName}\": {ex.Message}");
            return;
        }
        _fingerprintsWatchedFile.FileChanged += OnFingerprintsFileChanged;
    }

    private void OnFingerprintsFileChanged(object sender, FileChangedEventHandlerArgs args)
    {
        _logger.LogInformation($"Reloading allowed fingerprints.");
        SafeLoadFingerprintsFromFile();
    }
}