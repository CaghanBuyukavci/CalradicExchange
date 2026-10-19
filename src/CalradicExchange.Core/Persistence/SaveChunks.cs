using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CalradicExchange.Core.Persistence
{
    /// <summary>
    /// Splits the state JSON into save-safe strings (ADR-028). Bannerlord's save system stores each string's size as a
    /// 16-bit number (SaveContext.SaveStringTo), so a string over 32767 bytes corrupts the whole save. The JSON is
    /// deflated, base64-encoded and cut into chunks of at most <see cref="MaxChunkChars"/> ASCII characters, each
    /// written under its own key. Chunk format: "CXZ1|index|count|payload". A first chunk starting with '{' is a
    /// plain JSON state from before ADR-028.
    /// </summary>
    public static class SaveChunks
    {
        public const string Marker = "CXZ1";
        public const int MaxChunkChars = 24000;

        public static List<string> Pack(string json, int maxChunkChars = MaxChunkChars)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (maxChunkChars < 1) throw new ArgumentOutOfRangeException(nameof(maxChunkChars));
            string payload = Convert.ToBase64String(Deflate(Encoding.UTF8.GetBytes(json)));
            int count = Math.Max(1, (payload.Length + maxChunkChars - 1) / maxChunkChars);
            var chunks = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                int start = i * maxChunkChars;
                string part = start < payload.Length ? payload.Substring(start, Math.Min(maxChunkChars, payload.Length - start)) : "";
                chunks.Add(string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", Marker, i, count, part));
            }
            return chunks;
        }

        /// <summary>Number of chunks announced by the first chunk; 1 for a plain JSON state, 0 for nothing.</summary>
        public static int PartCount(string firstChunk)
        {
            if (string.IsNullOrEmpty(firstChunk)) return 0;
            if (!firstChunk.StartsWith(Marker + "|", StringComparison.Ordinal)) return 1;
            return ParseHeader(firstChunk, out _, out int count, out _) ? count : 0;
        }

        /// <summary>Rebuilds the JSON. Throws <see cref="StateFormatException"/> on missing or damaged chunks.</summary>
        public static string Unpack(IList<string> chunks)
        {
            if (chunks == null || chunks.Count == 0 || string.IsNullOrEmpty(chunks[0])) throw new StateFormatException("No saved exchange state.");
            if (!chunks[0].StartsWith(Marker + "|", StringComparison.Ordinal)) return chunks[0];

            var payload = new StringBuilder();
            int expected = -1;
            for (int i = 0; i < chunks.Count; i++)
            {
                if (!ParseHeader(chunks[i], out int index, out int count, out string part) || index != i)
                {
                    throw new StateFormatException($"Exchange state chunk {i} is missing or damaged.");
                }
                if (expected < 0) expected = count;
                if (count != expected) throw new StateFormatException("Exchange state chunks disagree on their count.");
                payload.Append(part);
            }
            if (chunks.Count != expected) throw new StateFormatException($"Exchange state has {chunks.Count} of {expected} chunks.");

            try
            {
                return Encoding.UTF8.GetString(Inflate(Convert.FromBase64String(payload.ToString())));
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidDataException)
            {
                throw new StateFormatException("Exchange state could not be decompressed: " + ex.Message);
            }
        }

        private static bool ParseHeader(string chunk, out int index, out int count, out string payload)
        {
            index = count = 0;
            payload = null;
            if (chunk == null) return false;
            string[] parts = chunk.Split(new[] { '|' }, 4);
            if (parts.Length != 4 || parts[0] != Marker) return false;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out index)) return false;
            if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out count) || count < 1) return false;
            payload = parts[3];
            return index < count;
        }

        private static byte[] Deflate(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true)) deflate.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        private static byte[] Inflate(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
