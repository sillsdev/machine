using System.IO;
using System.IO.Compression;

namespace SIL.Machine.Utils
{
    public static class ZipArchiveEntryExtensions
    {
        private const long DefaultMaxUncompressedSize = 100 * 1024 * 1024; // 100 MB
        private const double DefaultMaxCompressionRatio = 100.0; // 100:1 ratio limit

        /// <summary>
        /// Opens a bounded stream that will not be larger than the specified maximum uncompressed size.
        /// </summary>
        /// <param name="entry">The zip archive entry.</param>
        /// <param name="maxUncompressedSize">The maximum uncompressed size allowed in bytes.</param>
        /// <param name="maxCompressionRatio">The maximum compression ratio allowed.</param>
        /// <returns>A bounded stream.</returns>
        /// <exception cref="InvalidDataException">
        /// The entry's uncompressed size or ratio exceeds the maximum allowed limit.
        /// </exception>
        public static BoundedStream OpenBoundedStream(
            this ZipArchiveEntry entry,
            long maxUncompressedSize = DefaultMaxUncompressedSize,
            double maxCompressionRatio = DefaultMaxCompressionRatio
        )
        {
            if (entry == null)
                return null;

            if (entry.Length > maxUncompressedSize)
                throw new InvalidDataException("Entry uncompressed size exceeds maximum allowed limit.");

            if (entry.CompressedLength > 0)
            {
                double ratio = (double)entry.Length / entry.CompressedLength;
                if (ratio > maxCompressionRatio)
                    throw new InvalidDataException("Compression ratio exceeds safe threshold.");
            }

            return new BoundedStream(entry.Open(), maxUncompressedSize);
        }
    }
}
