using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.VirtualFS;

public class VirtualFileSystemBuilder : IVirtualFileSystemBuilder
{

    private readonly IFileFilter _fileFilter;
    private readonly ILogger<VirtualFileSystemBuilder> _logger;


    private class SegmentGenerator
    {
        private readonly string _baseFileName;
        private int? _num;

        public SegmentGenerator(string baseFileName)
        {
            _baseFileName = baseFileName;
            _num = null;
        }

        public DirectoryUriSegment GetNextDirectory()
        {
            return new DirectoryUriSegment(GetNextFileName());
        }

        public FileUriSegment GetNextFile()
        {
            return new FileUriSegment(GetNextFileName());
        }


        private string GetNextFileName()
        {
            return _num == null ? _baseFileName : $"{_baseFileName}{++_num}";
        }
    }

    public VirtualFileSystemBuilder(IFileFilter fileFilter, ILogger<VirtualFileSystemBuilder> logger)
    {
        _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static void SafePopulateSubDir(VirtualDirectory parent, string subDirPath)
    {
        var fileName = Path.GetFileName(subDirPath.TrimEnd('/', '\\'));

        // Can be null or empty when serving the root of a drive, but in this case we really need to define a name to avoid empty URI path segment
        fileName = string.IsNullOrEmpty(fileName) ? "root" : fileName;

        var segmentGenerator = new SegmentGenerator(fileName);

        DirectoryUriSegment uriSegment;
        do
        {
            uriSegment = segmentGenerator.GetNextDirectory();
        } while (parent.ChildExists(uriSegment.UriSegment));

        parent.AddDirectory(new VirtualDirectory(uriSegment, subDirPath));
    }

    private static void SafePopulateFile(VirtualDirectory parent, string subFilePath)
    {
        var fileInfo = new FileInfo(subFilePath);

        var segmentGenerator = new SegmentGenerator(fileInfo.Name);

        FileUriSegment uriSegment;
        do
        {
            uriSegment = segmentGenerator.GetNextFile();
        } while (parent.ChildExists(uriSegment.UriSegment));

        parent.AddFile(new VirtualFile(uriSegment, subFilePath, fileInfo.Length));
    }


    public VirtualFileSystemRoot BuildHierarchical(string[] servedDirectories)
    {
        var virtualFileSystemRoot = new VirtualFileSystemRoot();

        foreach (var servedDirectory in servedDirectories)
        {
            if (Directory.Exists(servedDirectory))
                SafePopulateSubDir(virtualFileSystemRoot, servedDirectory);
            else
                _logger.LogError($"Served directory \"{servedDirectory}\" not found.");
        }

        var remainingDirsToBrowse = new List<VirtualDirectory>(virtualFileSystemRoot.Directories);
        while (remainingDirsToBrowse.Count > 0)
        {
            var virtualDir = remainingDirsToBrowse[0];
            remainingDirsToBrowse.RemoveAt(0);

            string[] subDirectoryPaths;
            try
            {
                subDirectoryPaths = Directory.GetDirectories(virtualDir.FullLocalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list directories of \"{virtualDir.FullLocalPath}\": {ex.Message}");
                continue;
            }

            foreach (var subDirectory in subDirectoryPaths)
            {
                SafePopulateSubDir(virtualDir, subDirectory);
            }
            remainingDirsToBrowse.AddRange(virtualDir.Directories);

            string[] subFilePaths;
            try
            {
                subFilePaths = Directory.GetFiles(virtualDir.FullLocalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to list files of \"{virtualDir.FullLocalPath}\": {ex.Message}");
                continue;
            }
            foreach (var subFilePath in subFilePaths)
            {
                if (_fileFilter.IsFileAllowed(subFilePath))
                    SafePopulateFile(virtualDir, subFilePath);
            }
        }

        return virtualFileSystemRoot;

    }

}