using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;

namespace TinfoilWebServer.Utils;

public static class HashHelper
{
    [Pure]
    public static string ComputeSha256(string text)
    {
        if (text == null) 
            throw new ArgumentNullException(nameof(text));

        var bytes = Encoding.UTF8.GetBytes(text);

        var hashedBytes = SHA256.Create().ComputeHash(bytes);

        return hashedBytes.ToHexString();
    }
}