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
            var dirPath = SanitizeDir(dir.Path);

            var filePaths = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
            foreach (var filePath in filePaths)
            {
                if (!_fileFilter.IsFileAllowed(filePath))
                    continue;

                var relFilePath = filePath[dirPath.Length..]; // SubString starting at dirPath.Length to the end

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

            var dirPaths = Directory.GetDirectories(dir.Path);
            foreach (var dirPath in dirPaths)
            {
                var dirName = HttpUtility.UrlPathEncode(Path.GetFileName(dirPath));

                var newUri = new Uri(rooDirUri, dirName);
                tinfoilIndex.Directories.Add(newUri.AbsoluteUri);
            }

            foreach (var filePath in Directory.GetFiles(dir.Path))
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
            if (dirPath.EndsWith(Path.DirectorySeparatorChar))
                return dirPath;
            return dirPath + Path.DirectorySeparatorChar;
        }

        private static Uri SanitizeDirUrl(Uri url)
        {
            var rooDirUri = url.OriginalString.EndsWith('/') ? url : new Uri(url.OriginalString + "/");
            return rooDirUri;
        }
    }
}
