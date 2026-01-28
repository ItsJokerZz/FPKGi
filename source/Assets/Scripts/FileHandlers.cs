/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SfoHandler
{
    public class SfoEntry
    {
        public string Key { get; set; } = "";
        public object Value { get; set; } = "";
        public ushort DataFormat { get; set; }
        public uint DataLength { get; set; }
    }

    public class SfoInfo
    {
        public string TITLE { get; set; } = "";
        public string TITLE_ID { get; set; } = "";
        public string VERSION { get; set; } = "";
        public string APP_VER { get; set; } = "";
        public string CATEGORY { get; set; } = "";
        public string APP_TYPE { get; set; } = "";
        public string CONTENT_ID { get; set; } = "";
        public string SDK_VER { get; set; } = "";
        public List<SfoEntry> AllEntries { get; set; } = new List<SfoEntry>();
    }

    public static SfoInfo ParseSfo(byte[] sfoData)
    {
        if (sfoData.Length < 0x14)
            throw new Exception("SFO file too small.");

        uint magic = BitConverter.ToUInt32(sfoData, 0);
        if (magic != 0x46535000)
            throw new Exception($"Invalid SFO file magic number. Got: 0x{magic:X8}, Expected: 0x46535000");

        uint keyTableOffset = BitConverter.ToUInt32(sfoData, 8);
        uint dataTableOffset = BitConverter.ToUInt32(sfoData, 12);
        uint entryCount = BitConverter.ToUInt32(sfoData, 16);

        var info = new SfoInfo();
        int entrySize = 16;

        for (int i = 0; i < entryCount; i++)
        {
            int entryOffset = 0x14 + i * entrySize;
            if (entryOffset + entrySize > sfoData.Length) break;

            ushort keyOffset = BitConverter.ToUInt16(sfoData, entryOffset);
            ushort dataFormat = BitConverter.ToUInt16(sfoData, entryOffset + 2);
            uint dataLength = BitConverter.ToUInt32(sfoData, entryOffset + 4);
            uint dataOffset = BitConverter.ToUInt32(sfoData, entryOffset + 12);

            if (keyTableOffset + keyOffset >= sfoData.Length ||
                dataTableOffset + dataOffset + dataLength > sfoData.Length)
                continue;

            string key = ReadNullTerminatedString(sfoData, (int)(keyTableOffset + keyOffset));
            object value = ReadValue(sfoData, (int)(dataTableOffset + dataOffset), dataLength, dataFormat);

            var prop = typeof(SfoInfo).GetProperty(key);
            if (prop != null)
            {
                info.AllEntries.Add(new SfoEntry
                {
                    Key = key,
                    Value = value,
                    DataFormat = dataFormat,
                    DataLength = dataLength
                });
            }

            ParseKnownKey(info, key, value);
        }

        return info;
    }

    private static void ParseKnownKey(SfoInfo info, string key, object value)
    {
        var val = ConvertValueToString(value);

        if (key == "PUBTOOLINFO")
        {
            string input = val ?? "";
            string sdkVer = "";
            var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var kept = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i].Trim();
                if (part.StartsWith("sdk_ver=", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = part.Split(new[] { '=' }, 2);
                    if (kv.Length == 2) sdkVer = kv[1];
                    continue;
                }
                kept.Add(part);
            }

            info.SDK_VER = FormatSdkVer(sdkVer);
            if (!string.IsNullOrEmpty(sdkVer))
            {
                info.AllEntries.Add(new SfoEntry
                {
                    Key = "SDK_VER",
                    Value = info.SDK_VER,
                    DataFormat = 0,
                    DataLength = (uint)info.SDK_VER.Length
                });
            }

            return;
        }

        var property = typeof(SfoInfo).GetProperty(key);
        property?.SetValue(info, val);
    }

    private static string FormatSdkVer(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        var digits = new System.Text.StringBuilder();
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c >= '0' && c <= '9') digits.Append(c);
        }

        if (digits.Length < 4) return raw;

        var majorStr = digits.ToString(0, 2);
        var minorStr = digits.ToString(2, 2);

        int major;
        if (!int.TryParse(majorStr, out major)) return raw;

        return major.ToString() + "." + minorStr;
    }

    private static string ConvertValueToString(object value)
    {
        if (value is uint)
            return ((uint)value).ToString();
        if (value is int)
            return ((int)value).ToString();
        if (value == null)
            return "???";
        return value.ToString() ?? "???";
    }

    private static string ReadNullTerminatedString(byte[] buf, int offset)
    {
        int end = offset;
        while (end < buf.Length && buf[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(buf, offset, end - offset);
    }

    private static object ReadValue(byte[] buf, int offset, uint size, ushort format)
    {
        if (offset >= buf.Length || size == 0) return "";

        switch (format)
        {
            case 0x0402: // Integer (4 bytes)
                if (size >= 4 && offset + 4 <= buf.Length)
                    return BitConverter.ToUInt32(buf, offset);
                break;
            case 0x0404: // Int32 (4 bytes)
                if (size >= 4 && offset + 4 <= buf.Length)
                    return BitConverter.ToInt32(buf, offset);
                break;
            case 0x0204: // String (null-terminated)
                return ReadNullTerminatedString(buf, offset);
            case 0x0004: // UTF8 string
            case 0x0000: // Binary data (treat as string)
            default:
                // For any other type, try to read as string
                if (size > 0 && offset + size <= buf.Length)
                {
                    int end = offset;
                    int maxEnd = offset + (int)size;
                    while (end < maxEnd && end < buf.Length && buf[end] != 0)
                        end++;
                    return System.Text.Encoding.UTF8.GetString(buf, offset, end - offset);
                }
                break;
        }

        return "";
    }
}

public class PkgHandler
{
    private static readonly byte[] PKG_MAGIC_BYTES = new byte[] { 0x7F, 0x43, 0x4E, 0x54 };
    private const uint PARAM_SFO_ID = 0x00001000;
    private const uint NAMES_TABLE_ID = 0x00000200;
    public static async Task<SfoHandler.SfoInfo> DownloadAndParseParamSfo(string url)
    {
        var entries = await ParsePkg(url);
        var sfoEntry = FindSfoEntry(entries);
        if (sfoEntry.id == 0)
            throw new Exception("param.sfo not found in PKG");

        var sfoBytes = await DownloadFile(url, sfoEntry.offset, sfoEntry.size);
        return SfoHandler.ParseSfo(sfoBytes);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PkgTableEntry
    {
        public uint id;
        public uint filename_offset;
        public uint flags1;
        public uint flags2;
        public uint offset;
        public uint size;
        public ulong padding;
    }

    public static async Task<byte[]> DownloadFileWithWebHelper(string url, uint offset, uint size)
    {
        try
        {
            var dataStr = await UOBWrapper.DownloadRangeBytes(Utilities.URL.ProperFormatUrl(url), offset, size);
            var data = dataStr != null ? Encoding.GetEncoding("ISO-8859-1").GetBytes(dataStr) : null;
            if (data == null) throw new Exception("No data returned");

            if (data.Length != size && size > 0)
            {
                if (size < 1000)
                {
                    UOBWrapper.Print("Warning: Downloaded " + data.Length + " bytes, expected " + size + " bytes. Continuing anyway.", LogType.Warning);
                }
                else
                {
                    throw new Exception("Downloaded " + data.Length + " bytes, expected " + size + " bytes.");
                }
            }

            return data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to download file range from {url}:\nError: {ex.GetType().Name}\nMessage: {ex.Message}");
            throw;
        }
    }

    public static async Task<List<PkgTableEntry>> ParsePkg(string url)
    {
        url = Utilities.URL.ProperFormatUrl(url);
        string headerStr = await UOBWrapper.DownloadRangeBytes(url, 0, 32);
        byte[] header = headerStr != null ? Encoding.GetEncoding("ISO-8859-1").GetBytes(headerStr) : null;
        if (header == null || header.Length < 32)
            throw new Exception("Failed to read PKG header");

        if (!StartsWithBytes(header, PKG_MAGIC_BYTES))
            throw new Exception("Invalid PKG file format - magic bytes don't match.");

        uint fileCount = SwapEndian(BitConverter.ToUInt32(header, 0x0C));
        uint tableOffset = SwapEndian(BitConverter.ToUInt32(header, 0x18));
        int entrySize = Marshal.SizeOf<PkgTableEntry>();
        uint tableBytesLength = fileCount * (uint)entrySize;

        string tableStr = await UOBWrapper.DownloadRangeBytes(url, tableOffset, tableBytesLength);
        byte[] tableBytes = tableStr != null ? Encoding.GetEncoding("ISO-8859-1").GetBytes(tableStr) : null;
        if (tableBytes == null || tableBytes.Length == 0)
            throw new Exception("Failed to read PKG table");

        var entries = new List<PkgTableEntry>();
        for (int i = 0; i < fileCount; i++)
        {
            int entryStart = i * entrySize;
            if (entryStart + entrySize > tableBytes.Length) break;

            byte[] entryBytes = new byte[entrySize];
            Array.Copy(tableBytes, entryStart, entryBytes, 0, entrySize);

            GCHandle handle = GCHandle.Alloc(entryBytes, GCHandleType.Pinned);
            PkgTableEntry entry = Marshal.PtrToStructure<PkgTableEntry>(handle.AddrOfPinnedObject());
            handle.Free();

            uint id = SwapEndian(entry.id);
            uint offset = SwapEndian(entry.offset);
            uint size = SwapEndian(entry.size);

            if (id != 0 || offset != 0 || size != 0)
            {
                entries.Add(new PkgTableEntry
                {
                    id = id,
                    filename_offset = SwapEndian(entry.filename_offset),
                    flags1 = SwapEndian(entry.flags1),
                    flags2 = SwapEndian(entry.flags2),
                    offset = offset,
                    size = size,
                    padding = 0
                });
            }
        }

        return entries;
    }

    public static async Task<byte[]> GetNamesTable(string url, List<PkgTableEntry> entries)
    {
        var namesEntry = entries.FirstOrDefault(e => e.id == NAMES_TABLE_ID);
        if (namesEntry.id == 0) return new byte[0];

        return await DownloadFile(url, namesEntry.offset, namesEntry.size);
    }

    public static string GetFilename(byte[] namesTable, uint nameOffset)
    {
        if (namesTable == null || nameOffset == 0 || nameOffset >= namesTable.Length)
            return "";

        int endIndex = Array.IndexOf(namesTable, (byte)0, (int)nameOffset);
        if (endIndex == -1)
            endIndex = namesTable.Length;

        int length = endIndex - (int)nameOffset;
        if (length <= 0)
            return "";

        return System.Text.Encoding.UTF8.GetString(namesTable, (int)nameOffset, length);
    }

    public static async Task<byte[]> DownloadFile(string url, uint offset, uint size)
    {
        return await DownloadFileWithWebHelper(url, offset, size);
    }

    public static async Task<byte[]> DownloadFileSample(string url, uint offset, uint sampleSize)
    {
        return await DownloadFileWithWebHelper(url, offset, sampleSize);
    }

    public static PkgTableEntry FindSfoEntry(List<PkgTableEntry> entries)
    {
        var entry = entries.FirstOrDefault(e => e.id == PARAM_SFO_ID);
        return entry.id == 0 ? new PkgTableEntry() : entry;
    }


    private static string GenerateHmacSha1Hash(string hexKey, string data)
    {
        var keyBytes = new byte[hexKey.Length / 2];
        for (int i = 0; i < keyBytes.Length; i++)
        {
            keyBytes[i] = Convert.ToByte(hexKey.Substring(i * 2, 2), 16);
        }

        using (var hmac = new System.Security.Cryptography.HMACSHA1(keyBytes))
        {
            var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            var hex = BitConverter.ToString(hashBytes).Replace("-", "");
            return hex.ToUpperInvariant();
        }
    }

    private static bool StartsWithBytes(byte[] buffer, byte[] prefix)
    {
        if (buffer == null || prefix == null) return false;
        if (buffer.Length < prefix.Length) return false;
        for (int i = 0; i < prefix.Length; i++)
        {
            if (buffer[i] != prefix[i]) return false;
        }
        return true;
    }

    private static uint SwapEndian(uint value)
    {
        return (value >> 24) |
               ((value >> 8) & 0x0000FF00) |
               ((value << 8) & 0x00FF0000) |
               (value << 24);
    }
}

public class PngHandler
{
    private static readonly byte[] PNG_MAGIC_BYTES = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private const int BUFFER_SIZE = 65536;
    private const int OVERLAP_SIZE = 7;

    public static async Task<long?> FindPngOffset(string url)
    {
        var latin1 = Encoding.GetEncoding("ISO-8859-1");
        var tail = Array.Empty<byte>();
        long done = 0;

        while (true)
        {
            string data = await UOBWrapper.DownloadRangeBytes(url, (uint)done, (uint)BUFFER_SIZE);
            if (string.IsNullOrEmpty(data)) break;

            var chunk = latin1.GetBytes(data);

            var s = new byte[tail.Length + chunk.Length];
            Array.Copy(tail, 0, s, 0, tail.Length);
            Array.Copy(chunk, 0, s, tail.Length, chunk.Length);

            var pngIndex = FindPngMagic(s);
            if (pngIndex != -1)
                return done - tail.Length + pngIndex;

            if (s.Length >= OVERLAP_SIZE)
            {
                tail = new byte[OVERLAP_SIZE];
                Array.Copy(s, s.Length - OVERLAP_SIZE, tail, 0, OVERLAP_SIZE);
            }
            else
            {
                tail = s;
            }
            done += chunk.Length;

            if (chunk.Length < BUFFER_SIZE) break;
        }

        return null;
    }

    public static async Task<bool> GrabPng(string url, long offset, string outputPath)
    {
        try
        {
            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                var latin1 = Encoding.GetEncoding("ISO-8859-1");

                string sig = await UOBWrapper.DownloadRangeBytes(url, (uint)offset, 8);
                if (string.IsNullOrEmpty(sig)) throw new Exception("Failed to read PNG signature");
                var magicBuffer = latin1.GetBytes(sig);
                if (magicBuffer.Length != 8 || !SequenceEqualPrefix(magicBuffer, PNG_MAGIC_BYTES, 8))
                    throw new Exception("Ranged body didn't start with PNG");

                fileStream.Write(magicBuffer, 0, magicBuffer.Length);

                long currentOffset = offset + 8;

                while (true)
                {
                    string headerStr = await UOBWrapper.DownloadRangeBytes(url, (uint)currentOffset, 8);
                    if (string.IsNullOrEmpty(headerStr)) throw new Exception("Unexpected EOF");
                    var headerBuffer = latin1.GetBytes(headerStr);
                    if (headerBuffer.Length != 8) throw new Exception("Unexpected EOF");

                    uint length = (uint)((headerBuffer[0] << 24) | (headerBuffer[1] << 16) | (headerBuffer[2] << 8) | headerBuffer[3]);
                    var chunkType = new byte[4];
                    Array.Copy(headerBuffer, 4, chunkType, 0, 4);

                    fileStream.Write(headerBuffer, 0, headerBuffer.Length);

                    int dataSize = checked((int)length + 4);
                    if (dataSize > 0)
                    {
                        string dataStr = await UOBWrapper.DownloadRangeBytes(url, (uint)(currentOffset + 8), (uint)dataSize);
                        if (string.IsNullOrEmpty(dataStr) || latin1.GetByteCount(dataStr) != dataSize)
                            throw new Exception("Unexpected EOF");
                        var dataBuffer = latin1.GetBytes(dataStr);
                        fileStream.Write(dataBuffer, 0, dataBuffer.Length);
                    }

                    currentOffset += 8 + dataSize;

                    if (IsIend(chunkType) && length == 0)
                        break;
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static async Task<byte[]> ReadPngBytes(string url, long offset)
    {
        try
        {
            var latin1 = Encoding.GetEncoding("ISO-8859-1");
            using (var ms = new MemoryStream())
            {
                string sig = await UOBWrapper.DownloadRangeBytes(url, (uint)offset, 8);
                if (string.IsNullOrEmpty(sig)) return null;
                var magicBuffer = latin1.GetBytes(sig);
                if (magicBuffer.Length != 8 || !SequenceEqualPrefix(magicBuffer, PNG_MAGIC_BYTES, 8))
                    return null;

                ms.Write(magicBuffer, 0, magicBuffer.Length);

                long currentOffset = offset + 8;

                while (true)
                {
                    string headerStr = await UOBWrapper.DownloadRangeBytes(url, (uint)currentOffset, 8);
                    if (string.IsNullOrEmpty(headerStr)) return null;
                    var headerBuffer = latin1.GetBytes(headerStr);
                    if (headerBuffer.Length != 8) return null;

                    uint length = (uint)((headerBuffer[0] << 24) | (headerBuffer[1] << 16) | (headerBuffer[2] << 8) | headerBuffer[3]);
                    var chunkType = new byte[4];
                    Array.Copy(headerBuffer, 4, chunkType, 0, 4);

                    ms.Write(headerBuffer, 0, headerBuffer.Length);

                    int dataSize = checked((int)length + 4);
                    if (dataSize > 0)
                    {
                        string dataStr = await UOBWrapper.DownloadRangeBytes(url, (uint)(currentOffset + 8), (uint)dataSize);
                        if (string.IsNullOrEmpty(dataStr) || latin1.GetByteCount(dataStr) != dataSize) return null;
                        var dataBuffer = latin1.GetBytes(dataStr);
                        ms.Write(dataBuffer, 0, dataBuffer.Length);
                    }

                    currentOffset += 8 + dataSize;

                    if (IsIend(chunkType) && length == 0)
                        break;
                }

                return ms.ToArray();
            }
        }
        catch
        {
            return null;
        }
    }

    private static int FindPngMagic(byte[] data)
    {
        for (int i = 0; i <= data.Length - PNG_MAGIC_BYTES.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < PNG_MAGIC_BYTES.Length; j++)
            {
                if (data[i + j] != PNG_MAGIC_BYTES[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                return i;
            }
        }
        return -1;
    }

    private static bool SequenceEqualPrefix(byte[] a, byte[] b, int count)
    {
        if (a == null || b == null) return false;
        if (a.Length < count || b.Length < count) return false;
        for (int i = 0; i < count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private static bool IsIend(byte[] chunkType)
    {
        return chunkType != null && chunkType.Length == 4
            && chunkType[0] == (byte)'I' && chunkType[1] == (byte)'E'
            && chunkType[2] == (byte)'N' && chunkType[3] == (byte)'D';
    }
}
*/