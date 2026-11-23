using System.Buffers.Binary;
using NAudio.Wave;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// WASAPI の音声データを単精度サンプルへ変換します。
    /// </summary>
    internal static class PcmSampleConverter
    {
        #region 公開メソッド

        /// <summary>
        /// 取得開始前に変換可能なフォーマットであることを確認します。
        /// </summary>
        /// <param name="format">変換対象のフォーマットです。</param>
        /// <exception cref="NotSupportedException">音声形式がサポートされていない場合にスローされます。</exception>
        public static void ValidateFormat(WaveFormat format)
        {
            if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample is 8 or 16 or 24 or 32 ||
                format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
            {
                return;
            }

            throw new NotSupportedException($"サポートしていない音声フォーマットです ({format.Encoding}, {format.BitsPerSample} bit)。");
        }

        /// <summary>
        /// リトルエンディアンのサンプル列を単精度へ変換します。
        /// </summary>
        /// <param name="buffer">取得したサンプル列です。</param>
        /// <param name="format">開始時に検証したフォーマットです。</param>
        /// <returns>変換したサンプル列です。</returns>
        public static float[] Convert(ReadOnlySpan<byte> buffer, WaveFormat format)
        {
            ValidateFormat(format);
            var bytesPerSample = format.BitsPerSample / 8;
            var result = new float[buffer.Length / bytesPerSample];
            for (var index = 0; index < result.Length; index++)
            {
                var bytes = buffer[(index * bytesPerSample)..];
                var sample = (format.Encoding, bytesPerSample) switch
                {
                    (WaveFormatEncoding.IeeeFloat, _) => BinaryPrimitives.ReadSingleLittleEndian(bytes),
                    (_, 1) => (bytes[0] - 128) / 128f,
                    (_, 2) => BinaryPrimitives.ReadInt16LittleEndian(bytes) / 32768f,
                    (_, 3) => (bytes[0] | (bytes[1] << 8) | ((sbyte)bytes[2] << 16)) / 8388608f,
                    _ => BinaryPrimitives.ReadInt32LittleEndian(bytes) / 2147483648f,
                };
                result[index] = float.IsFinite(sample) ? sample : 0f;
            }

            return result;
        }

        #endregion
    }
}
