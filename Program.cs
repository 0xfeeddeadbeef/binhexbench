using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(HexBench));
    }
}

[MemoryDiagnoser]
// [DisassemblyDiagnoser(true, false, false, false, 1)]
public class HexBench
{
    // From small arrays to big enough to go into LOH
    [Params(4, 40, 400, 4000, 8000, 40000, 80000, 120000, 240000)]
    public int N;
    private byte[] data;

    [GlobalSetup]
    public void Setup()
    {
        this.data = new byte[N];
        new Random(42).NextBytes(this.data);
    }

    // From: https://github.com/aspnet/AspNetWebStack/blob/master/src/System.Web.Helpers/Crypto.cs#L146
    // Also: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
    internal static string BinaryToHex(byte[] data, bool lowerCase = true)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        byte addByte = 0x37;
        if (lowerCase) addByte = 0x57;
        char[] hex = new char[data.Length * 2];

        for (int iter = 0; iter < data.Length; iter++)
        {
            byte hexChar = ((byte)(data[iter] >> 4));
            hex[iter * 2] = (char)(hexChar > 9 ? hexChar + addByte : hexChar + 0x30);
            hexChar = ((byte)(data[iter] & 0xF));
            hex[(iter * 2) + 1] = (char)(hexChar > 9 ? hexChar + addByte : hexChar + 0x30);
        }

        return new string(hex);
    }

    // From: https://github.com/aspnet/Razor/blob/dev/src/Microsoft.AspNetCore.Razor.Language/Checksum.cs
    internal static string BytesToString(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var result = new StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            result.Append(bytes[i].ToString("x2"));
        }

        return result.ToString();
    }

    [Benchmark]
    public string BinaryToHex_Benchmark()
    {
        return BinaryToHex(this.data);
    }

    [Benchmark]
    public string BytesToString_Benchmark()
    {
        return BytesToString(this.data);
    }
}
