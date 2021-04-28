using System;
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

        public TinfoilIndex Build(string directory, Uri correspondingUri, TinfoilIndexType indexType, string? messageOfTheDay)
        {
            var rooDirUri = correspondingUri.OriginalString.EndsWith('/') ? correspondingUri : new Uri(correspondingUri.OriginalString + "/");

            var tinfoilIndex = new TinfoilIndex
            {
                Success = messageOfTheDay,
            };

            switch (indexType)
            {
                case TinfoilIndexType.Flatten:
                    var filePaths = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                    foreach (var filePath in filePaths)
                    {
                        if (!_fileFilter.IsFileAllowed(filePath))
                            continue;

                        var relFilePath = filePath[directory.Length..];

                        var encodedFileName = HttpUtility.UrlPathEncode(relFilePath);
                        var newUri = new Uri(rooDirUri, new Uri(encodedFileName, UriKind.Relative));
                        tinfoilIndex.Files.Add(new FileNfo
                        {
                            Size = new FileInfo(filePath).Length,
                            Url = newUri.AbsoluteUri
                        });
                    }

                    break;
                case TinfoilIndexType.Hierarchical:

                    var dirPaths = Directory.GetDirectories(directory);
                    foreach (var dirPath in dirPaths)
                    {
                        var dirName = HttpUtility.UrlPathEncode(Path.GetFileName(dirPath));

                        var newUri = new Uri(rooDirUri, dirName);
                        tinfoilIndex.Directories.Add(newUri.AbsoluteUri);
                    }

                    foreach (var filePath in Directory.GetFiles(directory))
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

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null);
            }

            return tinfoilIndex;
        }
    }
}
