using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public class TinfoilIndexBuilder : ITinfoilIndexBuilder
    {
        private readonly IFileFilter _fileFilter;

        public TinfoilIndexBuilder(IFileFilter fileFilter)
        {
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
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

        private void AppendFlatten(Dir dir, TinfoilIndex tinfoilIndex)
        {
            var rooDirUri = SanitizeDirUrl(dir.CorrespondingUrl);
            var sanitizedDir = SanitizeDir(dir.Path);

            if (!Directory.Exists(sanitizedDir))
                return;

            var filePaths = Directory.GetFiles(sanitizedDir, "*.*", SearchOption.AllDirectories);
            foreach (var filePath in filePaths)
            {
                if (!_fileFilter.IsFileAllowed(filePath))
                    continue;

                var relFilePath = filePath[(sanitizedDir.Length + 1)..]; // SubString starting at dirPath.Length+1 to the end

                var encodedFileName = HttpUtility.UrlPathEncode(relFilePath);
                var newUri = new Uri(rooDirUri, new Uri(encodedFileName, UriKind.Relative));

                tinfoilIndex.Files.Add(new FileNfo
                {
                    Size = new FileInfo(filePath).Length,
                    Url = newUri.AbsoluteUri
                });
            }
        }

        private void AppendHierarchical(Dir dir, TinfoilIndex tinfoilIndex)
        {
            var rooDirUri = SanitizeDirUrl(dir.CorrespondingUrl);

            var sanitizedDir = SanitizeDir(dir.Path);

            if (!Directory.Exists(sanitizedDir))
                return;

            var dirPaths = Directory.GetDirectories(sanitizedDir);
            foreach (var subDirPath in dirPaths)
            {
                var dirName = HttpUtility.UrlPathEncode(Path.GetFileName(subDirPath));

                var newUri = new Uri(rooDirUri, dirName);
                tinfoilIndex.Directories.Add(newUri.AbsoluteUri);
            }

            foreach (var filePath in Directory.GetFiles(sanitizedDir))
            {
                if (!_fileFilter.IsFileAllowed(filePath))
                    continue;

                var fileName = Path.GetFileName(filePath);
                var encodedFileName = HttpUtility.UrlPathEncode(fileName);
                var newUri = new Uri(rooDirUri, new Uri(encodedFileName, UriKind.Relative));
                tinfoilIndex.Files.Add(new FileNfo
                {
                    Size = new FileInfo(filePath).Length,
                    Url = newUri.AbsoluteUri
                });
            }
        }

        private static string SanitizeDir(string dirPath)
        {
            var sanitizedDir = dirPath.TrimEnd(Path.DirectorySeparatorChar, '/');
            return sanitizedDir;
        }

        private static Uri SanitizeDirUrl(Uri url)
        {
            var rooDirUri = url.OriginalString.EndsWith('/') ? url : new Uri(url.OriginalString + "/");
            return rooDirUri;
        }
    }
}
