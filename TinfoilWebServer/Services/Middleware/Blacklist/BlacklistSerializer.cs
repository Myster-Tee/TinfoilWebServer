using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TinfoilWebServer.Services.Middleware.Blacklist;

public class BlacklistSerializer : IBlacklistSerializer
{
    private readonly ILogger<BlacklistSerializer> _logger;

    private readonly object _locker = new();


    public BlacklistSerializer(ILogger<BlacklistSerializer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Serialize(string filePath, IReadOnlySet<IPAddress> ipAddresses)
    {
        lock (_locker)
        {
            var alreadySerializedIps = new HashSet<IPAddress>();

            // Deserialize existing file to preserve already saved IPs with comments
            if (File.Exists(filePath))
                DeserializeInternal(filePath, alreadySerializedIps, false);

            using var streamWriter = File.AppendText(filePath);

            foreach (var ipAddress in ipAddresses)
            {
                if (alreadySerializedIps.Contains(ipAddress))
                    continue;

                streamWriter.WriteLine(ipAddress);
            }
        }
    }

    public void Deserialize(string filePath, ISet<IPAddress> ipAddresses)
    {
        lock (_locker)
        {
            DeserializeInternal(filePath, ipAddresses, true);
        }
    }

    private void DeserializeInternal(string filePath, ISet<IPAddress> ipAddresses, bool logErrors)
    {
        const int MAX_RETRIES = 5;
        var numTry = 0;

        while (true)
        {
            try
            {
                using var textReader = File.OpenText(filePath);
                string? lineRaw;
                while ((lineRaw = textReader.ReadLine()) != null)
                {
                    var sanitizedLine = lineRaw.Split('#', 2)[0].Trim(); // Strips comments
                    if (!string.IsNullOrEmpty(sanitizedLine) && IPAddress.TryParse(sanitizedLine, out var ipAddress))
                        ipAddresses.Add(ipAddress);
                    else if (logErrors)
                        _logger.LogError($"Invalid IP address \"{sanitizedLine}\" found file in file \"{filePath}\".");
                }

                return;
            }
            catch (IOException)
            {
                numTry++;
                if (numTry > MAX_RETRIES)
                    throw;

                Thread.Sleep(100);
            }

        }

    }
}