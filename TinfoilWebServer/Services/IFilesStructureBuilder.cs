using System;
using TinfoilWebServer.Models;

namespace TinfoilWebServer.Services
{
    public interface IFilesStructureBuilder
    {
        public FilesStructure Build(string directory, Uri correspondingUri);
    }
}