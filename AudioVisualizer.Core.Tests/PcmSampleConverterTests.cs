using AudioVisualizer.Core.Audio;
using NAudio.Wave;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// 実機を使用せずに PCM 変換を検証します。
    /// </summary>
    [TestFixture]
    public class PcmSampleConverterTests
    {
        /// <summary>
        /// 対応する整数 PCM で符号とスケールを確認します。
        /// </summary>
        [TestCase(8, new byte[] { 0, 128, 192 })]
        [TestCase(16, new byte[] { 0, 128, 0, 0, 0, 64 })]
        [TestCase(24, new byte[] { 0, 0, 128, 0, 0, 0, 0, 0, 64 })]
        [TestCase(32, new byte[] { 0, 0, 0, 128, 0, 0, 0, 0, 0, 0, 0, 64 })]
        public void Convert_WhenIntegerPcm_PreservesPolarityAndLevel(int bits, byte[] buffer)
        {
            Assert.That(PcmSampleConverter.Convert(buffer, new WaveFormat(48000, bits, 1)), Is.EqualTo(new[] { -1f, 0f, 0.5f }));
        }

        /// <summary>
        /// 浮動小数点のサンプルを IEEE Float として読み取ります。
        /// </summary>
        [Test]
        public void Convert_WhenFloatPcm_PreservesValue()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            Assert.That(PcmSampleConverter.Convert(new byte[] { 0, 0, 128, 190 }, format), Is.EqualTo(new[] { -0.25f }));
        }

        /// <summary>IEEE Floatの非有限値が無音に置き換わることを確認します。</summary>
        [Test]
        public void Convert_WhenFloatDataContainsNonFiniteSamples_ProducesSilence()
        {
            var format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 1);
            var buffer = new byte[] { 0, 0, 192, 127, 0, 0, 128, 127, 0, 0, 128, 255 };

            Assert.That(PcmSampleConverter.Convert(buffer, format), Is.EqualTo(new[] { 0f, 0f, 0f }));
        }

        /// <summary>
        /// ビット幅が合っていても対応外のエンコードを開始前に拒否します。
        /// </summary>
        [Test]
        public void ValidateFormat_WhenEncodingIsUnsupported_Throws()
        {
            var format = WaveFormat.CreateCustomFormat(WaveFormatEncoding.ALaw, 48000, 1, 96000, 2, 16);
            Assert.That(() => PcmSampleConverter.ValidateFormat(format), Throws.TypeOf<NotSupportedException>());
        }
    }
}
