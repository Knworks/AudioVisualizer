using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// 公開データが無効な値を境界で拒否することを検証します。
    /// </summary>
    [TestFixture]
    public class PublicInputValidationTests
    {
        #region 公開メソッド

        /// <summary>
        /// 有限値が必要な設定と正規化座標を検証します。
        /// </summary>
        /// <param name="value">各公開 API へ渡す非有限値。</param>
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void NumericInputs_WhenNotFinite_AreRejected(double value)
        {
            Assert.That(() => new VisualizerSettings(InputSource.Microphone, null, true, true, value, 0, 16),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new VisualizerEffectContext(InputSource.Microphone, 1, value, 16, SpectrumProfile.Raw),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new NormalizedPoint(0, value), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new VisualizerFrame(new[] { value }, 0, DateTimeOffset.UtcNow),
                Throws.TypeOf<ArgumentException>());
        }

        /// <summary>
        /// 解析と描画でバー数の上限を共有することを検証します。
        /// </summary>
        [Test]
        public void BarCount_WhenExceedingSharedLimit_IsRejected()
        {
            const int tooManyBars = VisualizerSettings.MaximumBarCount + 1;
            Assert.That(() => new VisualizerSettings(InputSource.Microphone, null, true, true, 1, 0, tooManyBars),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new VisualizerEffectContext(InputSource.Microphone, 1, 0, tooManyBars, SpectrumProfile.Raw),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// 未定義の入力種別を検証します。
        /// </summary>
        [Test]
        public void InputSource_WhenUndefined_IsRejected()
        {
            Assert.That(() => new VisualizerSettings((InputSource)99, null, true, true, 1, 0, 16),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new VisualizerEffectContext((InputSource)99, 1, 0, 16, SpectrumProfile.Raw),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// 描画用コレクションの null 要素を検証します。
        /// </summary>
        [Test]
        public void RenderData_WhenCollectionContainsNull_IsRejected()
        {
            Assert.That(() => new PolylineRenderData("test", DateTimeOffset.UtcNow, new NormalizedPoint[] { null! }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new BarSpectrumRenderData("test", DateTimeOffset.UtcNow, new BarRenderItem[] { null! }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new BandLevelMeterRenderData("test", DateTimeOffset.UtcNow, new BandLevelMeterItem[] { null! }), Throws.TypeOf<ArgumentException>());
            Assert.That(() => new PeakHoldBarRenderData("test", DateTimeOffset.UtcNow, Array.Empty<BarRenderItem>(), new PeakMarkerItem[] { null! }),
                Throws.TypeOf<ArgumentException>());
        }

        #endregion
    }
}
