namespace NServiceBus;

using System;
using System.Security.Cryptography;
using System.Text;

static class DeterministicGuid
{
    public static Guid Create(string data)
    {
        var inputBytes = Encoding.Default.GetBytes(data);

        // use MD5 hash to get a 16-byte hash of the string
        var hashBytes = MD5.HashData(inputBytes);

        // generate a guid from the hash:
        return new Guid(hashBytes);
    }
}