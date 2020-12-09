using System;
using System.IO;
using System.Threading.Tasks;
using ElMariachi.Http.Header.Managed;
using Microsoft.AspNetCore.Http;
using Range = ElMariachi.Http.Header.Managed.Range;

namespace TinfoilWebServer
{
    public class FileSender
    {
        private readonly HttpResponse _response;
        private readonly string _contentType;
        private readonly Stream _fileStream;
        private readonly long _contentLength;
        private readonly long _startOffset;
        private int _bufferSize = 4 * 1024 * 1024; // 4 MiB
        private readonly long _fileSize;

        public FileSender(HttpResponse response, string filePath, string contentType = "application/octet-stream", IRange? range = null)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
            if (filePath == null)
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));

            _fileStream = File.OpenRead(filePath);
            try
            {
                _fileSize = _fileStream.Length;
                ComputeCopyInfo(range, _fileSize, out _contentLength, out _startOffset);
            }
            catch (Exception)
            {
                _fileStream.Dispose();
                throw;
            }
        }

        public bool IsPartialContent => _contentLength != _fileSize;

        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(BufferSize), value, "Buffer can't be less than or equal to zero.");
                _bufferSize = value;
            }
        }

        public async Task Send()
        {
            FillHeaders(_response.Headers);

            var bufferSize = BufferSize;
            await using (_fileStream)
            {
                var buffer = new byte[bufferSize];

                var nbRemainingBytes = _contentLength;
                _fileStream.Position = _startOffset;

                int nbBytesRead;
                do
                {
                    var nbBytesToRead = (int)Math.Min(bufferSize, nbRemainingBytes);

                    nbBytesRead = _fileStream.Read(buffer, 0, nbBytesToRead);

                    await _response.Body.WriteAsync(buffer, 0, nbBytesRead);

                    nbRemainingBytes -= nbBytesRead;
                } while (nbRemainingBytes > 0 || nbBytesRead == 0);

                if (nbRemainingBytes > 0)
                    throw new Exception($"Unexpected error: the number of bytes to write could not be honored, {nbRemainingBytes} byte(s) missing.");

            }
        }

        private void FillHeaders(IHeaderDictionary headers)
        {
            headers["Content-Type"] = _contentType;
            headers.ContentLength = _contentLength;

            if (IsPartialContent)
            {
                var contentRangeHeader = new ContentRangeHeader
                {
                    Unit = "bytes",
                    Range = new Range(_startOffset, _startOffset + _contentLength - 1),
                    Size = _fileSize,
                };
                headers[contentRangeHeader.Name] = contentRangeHeader.RawValue;
            }
        }

        private static void ComputeCopyInfo(IRange? range, long fileSize, out long contentLength, out long startOffset)
        {
            if (range == null)
            {
                contentLength = fileSize;
                startOffset = 0;
                return;
            }

            var start = range.Start;
            var end = range.End;
            if (start == null && end == null)
                throw new ArgumentException("Invalid range, start and end value can't be both undefined.", nameof(range));

            if (start == null && end != null)
            {
                if (end.Value < 0)
                    throw new ArgumentException($"Invalid range, end value «{end.Value}» can't be less than zero.", nameof(range));

                if (end.Value > fileSize)
                {
                    startOffset = 0;
                    contentLength = fileSize;
                }
                else
                {
                    startOffset = fileSize - end.Value;
                    contentLength = end.Value;
                }
                return;
            }

            if (start != null && end == null)
            {
                if (start.Value < 0)
                    throw new ArgumentException($"Invalid range, start value «{start.Value}» can't be less than zero.", nameof(range));

                if (start.Value >= fileSize)
                    throw new ArgumentException($"Invalid range, when end is undefined, start «{start.Value}» can't be greater than or equal to file size «{fileSize}» (bytes).", nameof(range));

                startOffset = start.Value;
                contentLength = fileSize - start.Value;
                return;
            }

            if (start.Value > end.Value)
                throw new ArgumentException($"Invalid range, start value «{start.Value}» can't be greater than end value «{end.Value}».", nameof(range));

            if (start.Value < 0)
                throw new ArgumentException("Invalid range, start value can't be less than zero.", nameof(range));

            if (end.Value < 0)
                throw new ArgumentException("Invalid range, end value can't be less than zero.", nameof(range));

            long realEnd;
            if (end.Value >= fileSize)
            {
                realEnd = fileSize - 1;
            }
            else
            {
                realEnd = end.Value;
            }
            contentLength = realEnd - start.Value + 1;

            startOffset = start.Value;

        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }
    }
}
