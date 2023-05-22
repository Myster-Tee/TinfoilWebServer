using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Models;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer.Services;

public class CachedTinfoilIndexBuilder : ICachedTinfoilIndexBuilder
{
    private readonly object _lock = new();
    private readonly ITinfoilIndexBuilder _tinfoilIndexBuilder;
    private readonly ILogger<CachedTinfoilIndexBuilder> _logger;
    private readonly IAppSettings _appSettings;
    private readonly Dictionary<string, CacheData> _cachePerUrl = new();

    public CachedTinfoilIndexBuilder(ITinfoilIndexBuilder tinfoilIndexBuilder, ILogger<CachedTinfoilIndexBuilder> logger, IAppSettings appSettings)
    {
        _tinfoilIndexBuilder = tinfoilIndexBuilder ?? throw new ArgumentNullException(nameof(tinfoilIndexBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    public TinfoilIndex Build(string url, IEnumerable<Dir> dirs, TinfoilIndexType indexType, string? messageOfTheDay)
    {
        lock (_lock)
        {
            var cacheExpiration = CacheExpiration;

            if (cacheExpiration <= TimeSpan.Zero)
            {
                var tinfoilIndex = _tinfoilIndexBuilder.Build(dirs, indexType, messageOfTheDay);
                _logger.LogDebug($"New index built for URL «{url}» (no cache).");
                return tinfoilIndex;
            }

            if (_cachePerUrl.TryGetValue(url, out var cacheData))
            {
                if (cacheExpiration == Timeout.InfiniteTimeSpan)
                {
                    _logger.LogDebug($"Cached index returned for URL «{url}» (no cache expiration).");
                    return cacheData.TinfoilIndex;
                }

                var cacheDuration = DateTime.Now - cacheData.CreationTime;
                if (cacheDuration < cacheExpiration)
                {
                    _logger.LogDebug($"Cached index returned for URL «{url}» (cache will expired in {cacheExpiration - cacheDuration}).");
                    return cacheData.TinfoilIndex;
                }

                var tinfoilIndex = _tinfoilIndexBuilder.Build(dirs, indexType, messageOfTheDay);
                _cachePerUrl[url] = new CacheData(DateTime.Now, tinfoilIndex);

                _logger.LogDebug($"Cached index updated for URL «{url}».");
                return tinfoilIndex;
            }
            else
            {
                var tinfoilIndex = _tinfoilIndexBuilder.Build(dirs, indexType, messageOfTheDay);
                _cachePerUrl.Add(url, new CacheData(DateTime.Now, tinfoilIndex));

                _logger.LogDebug($"New cached index created for URL «{url}».");
                return tinfoilIndex;
            }
        }
    }

    public TimeSpan CacheExpiration => _appSettings.CacheExpiration;

    private class CacheData
    {
        public CacheData(DateTime creationTime, TinfoilIndex tinfoilIndex)
        {
            CreationTime = creationTime;
            TinfoilIndex = tinfoilIndex;
        }

        public DateTime CreationTime { get; }

        public TinfoilIndex TinfoilIndex { get; }
    }
}