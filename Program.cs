using System;
using System.Collections.Generic;
using System.Runtime;
using System.Runtime.InteropServices;
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
public unsafe class HexBench
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

    // From: https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
    private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
    private static readonly uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_lookup32Unsafe, GCHandleType.Pinned).AddrOfPinnedObject();

    private static uint[] CreateLookup32Unsafe()
    {
        var result = new uint[256];

        for (int i = 0; i < 256; i++)
        {
            string s = i.ToString("X2");
            if (BitConverter.IsLittleEndian)
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            else
                result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
        }

        return result;
    }

    public static string ByteArrayToHexViaLookup32Unsafe(byte[] bytes)
    {
        var lookupP = _lookup32UnsafeP;
        var result = new char[bytes.Length * 2];

        fixed (byte* bytesP = bytes)
        fixed (char* resultP = result)
        {
            uint* resultP2 = (uint*)resultP;

            for (int i = 0; i < bytes.Length; i++)
            {
                resultP2[i] = lookupP[bytesP[i]];
            }
        }

        return new string(result);
    }

    public static string ByteArrayToHexViaLookup32UnsafeDirect(byte[] bytes)
    {
        var lookupP = _lookup32UnsafeP;
        var result = new string((char)0, bytes.Length * 2);
        fixed (byte* bytesP = bytes)
        fixed (char* resultP = result)
        {
            uint* resultP2 = (uint*)resultP;
            for (int i = 0; i < bytes.Length; i++)
            {
                resultP2[i] = lookupP[bytesP[i]];
            }
        }

        return result;
    }

    [Benchmark(Baseline = true)]
    public string BitConverter_Benchmark()
    {
        return BitConverter.ToString(this.data).Replace("-", string.Empty);
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

    [Benchmark]
    public string ByteArrayToHex_Benchmark()
    {
        return ByteArrayToHexViaLookup32Unsafe(this.data);
    }

    [Benchmark]
    public string ByteArrayToHexDirect_Benchmark()
    {
        return ByteArrayToHexViaLookup32UnsafeDirect(this.data);
    }
}
