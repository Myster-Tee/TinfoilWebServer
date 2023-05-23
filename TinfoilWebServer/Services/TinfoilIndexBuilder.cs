using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services;

public class TinfoilIndexBuilder : ITinfoilIndexBuilder
{
    private readonly IFileFilter _fileFilter;
    private readonly IUrlCombinerFactory _urlCombinerFactory;
    private readonly ILogger<TinfoilIndexBuilder> _logger;

    public TinfoilIndexBuilder(IFileFilter fileFilter, IUrlCombinerFactory urlCombinerFactory, ILogger<TinfoilIndexBuilder> logger)
    {
        _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        _urlCombinerFactory = urlCombinerFactory ?? throw new ArgumentNullException(nameof(urlCombinerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public TinfoilIndex Build(IEnumerable<Dir> dirs, TinfoilIndexType indexType, string? messageOfTheDay)
    {
        var tinfoilIndex = new TinfoilIndex
        {
            Success = messageOfTheDay,
        };

        switch (indexType)
        {
            case TinfoilIndexType.Flatten:
                foreach (var dir in dirs)
                {
                    AppendFlatten(dir, tinfoilIndex);
                }
                break;
            case TinfoilIndexType.Hierarchical:
                foreach (var dir in dirs)
                {
                    AppendHierarchical(dir, tinfoilIndex);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null);
        }

        return tinfoilIndex;
    }

    /// <summary>
    /// Build a single index containing all files found in all served directories
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="tinfoilIndex"></param>
    private void AppendFlatten(Dir dir, TinfoilIndex tinfoilIndex)
    {
        var urlCombiner = _urlCombinerFactory.Create(dir.CorrespondingUrl);

        var localDirPath = dir.Path;

        if (!Directory.Exists(localDirPath))
        {
            _logger.LogError($"Directory \"{localDirPath}\" not found.");
            return;
        }

        var remainingDirsToBrowse = new List<string> { localDirPath };

        while (remainingDirsToBrowse.Count > 0)
        {
            var dirPath = remainingDirsToBrowse[0];
            remainingDirsToBrowse.RemoveAt(0);

            string[] filePaths;
            try
            {
                filePaths = SafeGetFiles(dirPath);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to list files of directory \"{dirPath}\": {ex.Message}");
                continue;
            }

            foreach (var filePath in filePaths)
            {
                if (!_fileFilter.IsFileAllowed(filePath))
                    continue;

                var relFilePath = filePath[localDirPath.Length..]; // SubString from dirPath.Length to the end

                var newUri = urlCombiner.CombineLocalPath(relFilePath);

                tinfoilIndex.Files.Add(new FileNfo
                {
                    Size = new FileInfo(filePath).Length,
                    Url = newUri.AbsoluteUri
                });
            }

            try
            {
                remainingDirsToBrowse.AddRange(SafeGetSubDirectories(dirPath));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to list sub-directories of \"{dirPath}\": {ex.Message}");
                continue;
            }
        }
    }

    /// <summary>
    /// Build an index containing only the files and sub-directories at the root of the specified directory
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="tinfoilIndex"></param>
    private void AppendHierarchical(Dir dir, TinfoilIndex tinfoilIndex)
    {
        var urlCombiner = _urlCombinerFactory.Create(dir.CorrespondingUrl);

        var localDirPath = dir.Path;

        if (!Directory.Exists(localDirPath))
        {
            _logger.LogError($"Directory \"{localDirPath}\" not found.");
            return;
        }

        var dirPaths = SafeGetSubDirectories(localDirPath);
        foreach (var subDirPath in dirPaths)
        {
            var dirName = Path.GetFileName(subDirPath);
            var newUri = urlCombiner.CombineLocalPath(dirName);
            tinfoilIndex.Directories.Add(newUri.AbsoluteUri);
        }

        foreach (var filePath in SafeGetFiles(localDirPath))
        {
            if (!_fileFilter.IsFileAllowed(filePath))
                continue;

            var fileName = Path.GetFileName(filePath);
            var newUri = urlCombiner.CombineLocalPath(fileName);
            tinfoilIndex.Files.Add(new FileNfo
            {
                Size = new FileInfo(filePath).Length,
                Url = newUri.AbsoluteUri
            });
        }
    }

    private string[] SafeGetSubDirectories(string dirPath)
    {
        try
        {
            return Directory.GetDirectories(dirPath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to list sub-directories of \"{dirPath}\": {ex.Message}");
            return  Array.Empty<string>();
        }
    }

    private string[] SafeGetFiles(string dirPath)
    {
        try
        {
            return Directory.GetFiles(dirPath);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to list files of \"{dirPath}\": {ex.Message}");
            return Array.Empty<string>();
        }
    }

}