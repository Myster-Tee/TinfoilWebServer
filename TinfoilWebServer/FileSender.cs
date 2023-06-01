using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace TinfoilWebServer;

public class FileSender : IDisposable, IAsyncDisposable
{
    private readonly HttpResponse _response;
    private readonly string _contentType;
    private readonly Stream _fileStream;
    private readonly long _contentLength;
    private readonly long _startOffset;
    private int _bufferSize = 4 * 1024 * 1024; // 4 MiB
    private readonly long _fileSize;

    public FileSender(HttpResponse response, string filePath, string contentType = "application/octet-stream", RangeItemHeaderValue? range = null)
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

    public async Task Send(CancellationToken cancellationToken)
    {
        FillHeaders(_response.Headers);

        var bufferSize = BufferSize;
        await using (_fileStream)
        {
            var buffer = new byte[bufferSize];

            var nbRemainingBytes = _contentLength;
            _fileStream.Position = _startOffset;

            while (nbRemainingBytes > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nbBytesToRead = (int)Math.Min(bufferSize, nbRemainingBytes);

                var nbBytesRead = await _fileStream.ReadAsync(buffer, 0, nbBytesToRead, cancellationToken);
                if (nbBytesRead <= 0)
                    break;

                await _response.Body.WriteAsync(buffer, 0, nbBytesRead, cancellationToken);

                nbRemainingBytes -= nbBytesRead;
            }

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
            var contentRangeHeader = new ContentRangeHeaderValue(_startOffset, _startOffset + _contentLength - 1, _fileSize)
            {
                Unit = "bytes"
            };
            headers["Content-Range"] = contentRangeHeader.ToString();
        }
    }

    private static void ComputeCopyInfo(RangeItemHeaderValue? range, long fileSize, out long contentLength, out long startOffset)
    {
        if (range == null)
        {
            contentLength = fileSize;
            startOffset = 0;
            return;
        }

        var from = range.From;
        var to = range.To;
        if (from == null && to == null)
            throw new ArgumentException("Invalid range, start and end value can't be both undefined.", nameof(range));

        if (from == null && to != null)
        {
            if (to.Value < 0)
                throw new ArgumentException($"Invalid range, end value {to.Value} can't be less than zero.", nameof(range));

            if (to.Value > fileSize)
            {
                startOffset = 0;
                contentLength = fileSize;
            }
            else
            {
                startOffset = fileSize - to.Value;
                contentLength = to.Value;
            }
            return;
        }

        if (from != null && to == null)
        {
            if (from.Value < 0)
                throw new ArgumentException($"Invalid range, start value {from.Value} can't be less than zero.", nameof(range));

            if (from.Value >= fileSize)
                throw new ArgumentException($"Invalid range, when end is undefined, start {from.Value} can't be greater than or equal to file size {fileSize} (bytes).", nameof(range));

            startOffset = from.Value;
            contentLength = fileSize - from.Value;
            return;
        }

        if (from.Value > to.Value)
            throw new ArgumentException($"Invalid range, start value {from.Value} can't be greater than end value {to.Value}.", nameof(range));

        if (from.Value < 0)
            throw new ArgumentException("Invalid range, start value can't be less than zero.", nameof(range));

        if (to.Value < 0)
            throw new ArgumentException("Invalid range, end value can't be less than zero.", nameof(range));

        long realEnd;
        if (to.Value >= fileSize)
        {
            realEnd = fileSize - 1;
        }
        else
        {
            realEnd = to.Value;
        }
        contentLength = realEnd - from.Value + 1;

        startOffset = from.Value;

    }

    public void Dispose()
    {
        _fileStream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _fileStream.DisposeAsync();
    }
}