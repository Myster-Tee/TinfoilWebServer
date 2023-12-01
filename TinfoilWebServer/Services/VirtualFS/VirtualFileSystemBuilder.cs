using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.Logging;
using TinfoilWebServer.Utils;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualFileSystemBuilder : IVirtualFileSystemBuilder
{

    private readonly IFileFilter _fileFilter;
    private readonly ILogger<VirtualFileSystemBuilder> _logger;

    public VirtualFileSystemBuilder(IFileFilter fileFilter, ILogger<VirtualFileSystemBuilder> logger)
    {
        _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Helper to ensure we never have duplicated key
    /// </summary>
    private class KeyGenerator
    {
        private readonly string _baseFileNameWithoutExt;
        private readonly string _fileExt;
        private int? _num;

        public KeyGenerator(string baseFileName)
        {
            _baseFileNameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
            _fileExt = Path.GetExtension(baseFileName);
            _num = null;
        }

        public string GetNextKey()
        {
            string nextKey;
            if (_num == null)
            {
                nextKey = $"{_baseFileNameWithoutExt}{_fileExt}";
                _num = 1;
            }
            else
            {
                nextKey = $"{_baseFileNameWithoutExt}_{_num}{_fileExt}";
                _num++;
            }

            return nextKey;
        }
    }


    private void SafePopulateSubDir(VirtualDirectory parent, string subDirPath)
    {
        try
        {
            var dirName = Path.GetFileName(subDirPath);

            // Can be null or empty when serving the root of a drive, but in this case we really need to define a name to avoid empty URI path segment
            dirName = string.IsNullOrEmpty(dirName) ? "root" : dirName;

            var keyGenerator = new KeyGenerator(dirName);

            string key;
            do
            {
                key = keyGenerator.GetNextKey();
            } while (parent.ChildExists(key));

            parent.AddDirectory(new VirtualDirectory(key, subDirPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to populate directory \"{subDirPath}\" to served files.");
        }
    }

    /// <summary>
    /// Add the specified file to the virtual parent directory and ensure the uniqueness of the mapped key
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="subFile"></param>
    private void SafePopulateFile(VirtualDirectory parent, FileInfo subFile)
    {
        try
        {
            var keyGenerator = new KeyGenerator(subFile.Name);

            string key;
            do
            {
                key = keyGenerator.GetNextKey();
            } while (parent.ChildExists(key));

            parent.AddFile(new VirtualFile(key, subFile.FullName, subFile.Length)); // Length property can throw when file doesn't exist anymore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to populate file \"{subFile}\" to served files.");
        }
    }


    public VirtualFileSystemRoot BuildFlat(IReadOnlyList<string> servedDirectories)
    {
        var virtualFileSystemRoot = new VirtualFileSystemRoot();

        var remainingDirsToBrowse = new List<DirectoryInfo>();

        foreach (var servedDirectoryFullPath in servedDirectories.Select(ToFullPath))
        {
            var directory = new DirectoryInfo(servedDirectoryFullPath);
            if (directory.Exists)
                remainingDirsToBrowse.Add(directory);
            else
                _logger.LogError($"Served directory \"{servedDirectoryFullPath}\" not found (or access denied).");
        }

        while (remainingDirsToBrowse.TryRemoveFirst(out var dir))
        {
            FileInfo[]? subFiles = null;
            try
            {
                subFiles = dir.GetFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list files of \"{dir}\": {ex.Message}");
            }

            if (subFiles != null)
            {
                foreach (var subFile in subFiles)
                {
                    if (_fileFilter.IsFileAllowed(subFile.Name))
                        SafePopulateFile(virtualFileSystemRoot, subFile);
                }
            }

            DirectoryInfo[]? subDirs = null;
            try
            {
                subDirs = dir.GetDirectories();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list directories of \"{dir}\": {ex.Message}");
            }

            if (subDirs != null)
                remainingDirsToBrowse.AddRange(subDirs);
        }

        return virtualFileSystemRoot;
    }


    public VirtualFileSystemRoot BuildHierarchical(IReadOnlyList<string> servedDirectories, bool excludeEmptyDirectories)
    {
        var virtualFileSystemRoot = new VirtualFileSystemRoot();

        foreach (var servedDirectoryFullPath in servedDirectories.Select(ToFullPath))
        {
            // Trims end separator to avoid having an empty name while calling Path.GetFileName
            var safeDirPath = servedDirectoryFullPath.TrimEnd(Path.DirectorySeparatorChar);

            if (Directory.Exists(safeDirPath))
                SafePopulateSubDir(virtualFileSystemRoot, safeDirPath);
            else
                _logger.LogError($"Served directory \"{servedDirectoryFullPath}\" not found.");
        }

        var remainingDirsToBrowse = new List<VirtualDirectory>(virtualFileSystemRoot.Directories);
        while (remainingDirsToBrowse.TryRemoveFirst(out var virtualDir))
        {

            string[]? subDirectoryPaths = null;
            try
            {
                subDirectoryPaths = Directory.GetDirectories(virtualDir.FullLocalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list directories of \"{virtualDir.FullLocalPath}\": {ex.Message}");
            }

            if (subDirectoryPaths != null)
            {
                foreach (var subDirectory in subDirectoryPaths)
                {
                    SafePopulateSubDir(virtualDir, subDirectory);
                }

                remainingDirsToBrowse.AddRange(virtualDir.Directories);
            }

            FileInfo[]? subFiles = null;
            try
            {
                subFiles = new DirectoryInfo(virtualDir.FullLocalPath).GetFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list files of \"{virtualDir.FullLocalPath}\": {ex.Message}");
            }

            if (subFiles != null)
            {
                foreach (var subFile in subFiles)
                {
                    if (_fileFilter.IsFileAllowed(subFile.Name))
                        SafePopulateFile(virtualDir, subFile);
                }
            }
        }

        if (excludeEmptyDirectories)
        {
            var removedDirectories = virtualFileSystemRoot.RemoveDirectoriesWithoutFile();

            if (_logger.IsEnabled(LogLevel.Debug) && removedDirectories.Count > 0)
                _logger.LogDebug($"Empty served directories removed:{removedDirectories.Select(directory => directory.FullLocalPath).ToMultilineString()}");
        }

        return virtualFileSystemRoot;

    }


    private static string ToFullPath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(path);
    }

}