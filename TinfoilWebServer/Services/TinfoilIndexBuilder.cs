using System;
using System.Collections.Generic;
using System.IO;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public class TinfoilIndexBuilder : ITinfoilIndexBuilder
    {
        private readonly IFileFilter _fileFilter;
        private readonly IUrlCombinerFactory _urlCombinerFactory;

        public TinfoilIndexBuilder(IFileFilter fileFilter, IUrlCombinerFactory urlCombinerFactory)
        {
            _fileFilter = fileFilter ?? throw new ArgumentNullException(nameof(fileFilter));
            _urlCombinerFactory = urlCombinerFactory ?? throw new ArgumentNullException(nameof(urlCombinerFactory));
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
            var urlCombiner = _urlCombinerFactory.Create(dir.CorrespondingUrl);

            var localDirPath = dir.Path;

            if (!Directory.Exists(localDirPath))
                return;

            var filePaths = Directory.GetFiles(localDirPath, "*.*", SearchOption.AllDirectories);
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
        }

        private void AppendHierarchical(Dir dir, TinfoilIndex tinfoilIndex)
        {
            var urlCombiner = _urlCombinerFactory.Create(dir.CorrespondingUrl);

            var localDirPath = dir.Path;

            if (!Directory.Exists(localDirPath))
                return;

            var dirPaths = Directory.GetDirectories(localDirPath);
            foreach (var subDirPath in dirPaths)
            {
                var dirName = Path.GetFileName(subDirPath);
                var newUri = urlCombiner.CombineLocalPath(dirName);
                tinfoilIndex.Directories.Add(newUri.AbsoluteUri);
            }

            foreach (var filePath in Directory.GetFiles(localDirPath))
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

    }
}
