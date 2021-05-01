using System;

namespace TinfoilWebServer.Services
{
    public class PhysicalPathConverter : IPhysicalPathConverter
    {
        private readonly IServedDirsAliasMapper _servedDirsAliasMapper;

        public PhysicalPathConverter(IServedDirsAliasMapper servedDirsAliasMapper)
        {
            _servedDirsAliasMapper = servedDirsAliasMapper ?? throw new ArgumentNullException(nameof(servedDirsAliasMapper));
        }

        public string? Convert(string url)
        {
            var uri = new Uri(url, UriKind.Absolute);

            if (uri.Segments.Length >= 2)
            {
                var servedDirAlias = uri.Segments[1].TrimEnd('/');
                var servedDir = _servedDirsAliasMapper.GetServedDir(servedDirAlias);
                if (servedDir == null)
                    return null;

            }

            return null;
        }
    }
}