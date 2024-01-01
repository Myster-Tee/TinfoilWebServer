using System;
using System.Security.Cryptography;
using System.Text;
using TinfoilWebServer.Utils;

namespace TinfoilWebServer.Services;

public class HashHelper : IHashHelper
{
    public string ComputeSha256(string text)
    {
        if (text == null) 
            throw new ArgumentNullException(nameof(text));

        var bytes = Encoding.UTF8.GetBytes(text);

        var hashedBytes = SHA256.Create().ComputeHash(bytes);

        return hashedBytes.ToHexString();
    }
}