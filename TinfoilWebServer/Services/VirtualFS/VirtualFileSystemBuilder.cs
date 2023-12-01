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


    private void SafePopulateSubDir(VirtualDirectory parent, DirectoryInfo subDirectory)
    {
        try
        {
            var dirName = subDirectory.Name;

            // Can be null or empty when serving the root of a drive, but in this case we really need to define a name to avoid empty URI path segment
            dirName = string.IsNullOrEmpty(dirName) ? "root" : dirName;

            var keyGenerator = new KeyGenerator(dirName);

            string key;
            do
            {
                key = keyGenerator.GetNextKey();
            } while (parent.ChildExists(key));

            parent.AddDirectory(new VirtualDirectory(key, subDirectory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to populate directory \"{subDirectory}\" to served files.");
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

            parent.AddFile(new VirtualFile(key, subFile, subFile.Length)); // Length property can throw when file doesn't exist anymore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to populate file \"{subFile}\" to served files.");
        }
    }


    public VirtualFileSystemRoot BuildFlat(IReadOnlyList<DirectoryInfo> servedDirectories)
    {
        var virtualFileSystemRoot = new VirtualFileSystemRoot();

        var remainingDirsToBrowse = servedDirectories.ToList();

        while (remainingDirsToBrowse.TryRemoveFirst(out var dir))
        {
            FileInfo[]? subFiles = null;
            try
            {
                subFiles = dir.GetFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list files of \"{dir.FullName}\": {ex.Message}");
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
                _logger.LogError(ex, $"Failed to list directories of \"{dir.FullName}\": {ex.Message}");
            }

            if (subDirs != null)
                remainingDirsToBrowse.AddRange(subDirs);
        }

        return virtualFileSystemRoot;
    }


    public VirtualFileSystemRoot BuildHierarchical(IReadOnlyList<DirectoryInfo> servedDirectories, bool excludeEmptyDirectories)
    {
        var virtualFileSystemRoot = new VirtualFileSystemRoot();

        foreach (var servedDirectory in servedDirectories)
        {
            // Trims end separator to avoid having an empty name while calling Path.GetFileName
            //var safeDirPath = servedDirectory.TrimEnd(Path.DirectorySeparatorChar);

            SafePopulateSubDir(virtualFileSystemRoot, servedDirectory);
        }

        var remainingDirsToBrowse = new List<VirtualDirectory>(virtualFileSystemRoot.Directories);
        while (remainingDirsToBrowse.TryRemoveFirst(out var virtualDir))
        {

            DirectoryInfo[]? subDirectories = null;
            try
            {
                subDirectories = virtualDir.Directory!.GetDirectories();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list directories of \"{virtualDir.Directory}\": {ex.Message}");
            }

            if (subDirectories != null)
            {
                foreach (var subDirectory in subDirectories)
                {
                    SafePopulateSubDir(virtualDir, subDirectory);
                }

                remainingDirsToBrowse.AddRange(virtualDir.Directories);
            }

            FileInfo[]? subFiles = null;
            try
            {
                subFiles = virtualDir.Directory!.GetFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list files of \"{virtualDir.Directory}\": {ex.Message}");
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
                _logger.LogDebug($"Empty served directories removed:{removedDirectories.Select(directory => directory.Directory!.FullName).ToMultilineString()}");
        }

        return virtualFileSystemRoot;

    }

}