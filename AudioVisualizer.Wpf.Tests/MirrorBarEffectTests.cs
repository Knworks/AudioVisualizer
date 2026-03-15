using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;
using AudioVisualizer.Wpf.Effects;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// <see cref="MirrorBarEffect"/> のレンダーデータ生成を検証します。
    /// </summary>
    [TestFixture]
    public class MirrorBarEffectTests
    {
        #region 公開メソッド

        /// <summary>
        /// MirrorBarEffect が左右対称のバーを生成することを確認します。
        /// ■入力
        /// ・1. 6 本表示の VisualizerEffectContext
        /// ・2. 複数スペクトラム値を含む VisualizerFrame
        /// ■確認内容
        /// ・1. 生成バー数が 6 本になる
        /// ・2. 左右の対応バーが同じ高さを持つ
        /// ・3. 左右の X 座標が中心対称になる
        /// </summary>
        [Test]
        public void Given_EvenBarCount_When_BuildRenderData_Then_MirroredBarsAreGenerated()
        {
            // 準備
            var effect = new MirrorBarEffect();
            var frame = new VisualizerFrame(new[] { 0.2, 0.4, 0.8, 0.6, 0.3, 0.1 }, new[] { 0.0 }, 0.8, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 6, SpectrumProfile.Balanced);

            // 実行
            var result = (BarSpectrumRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.EffectId, Is.EqualTo("mirror-bars"));
                Assert.That(result.Bars, Has.Count.EqualTo(6));
                Assert.That(result.Bars[0].Height, Is.EqualTo(result.Bars[5].Height).Within(0.0001));
                Assert.That(result.Bars[1].Height, Is.EqualTo(result.Bars[4].Height).Within(0.0001));
                Assert.That(result.Bars[2].Height, Is.EqualTo(result.Bars[3].Height).Within(0.0001));
                Assert.That(result.Bars[0].X + result.Bars[5].X + result.Bars[5].Width, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(result.Bars[1].X + result.Bars[4].X + result.Bars[4].Width, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(result.Bars[2].X + result.Bars[3].X + result.Bars[3].Width, Is.EqualTo(1.0).Within(0.0001));
            });
        }

        /// <summary>
        /// MirrorBarEffect が奇数本指定で中央バーを保持することを確認します。
        /// ■入力
        /// ・1. 5 本表示の VisualizerEffectContext
        /// ・2. 複数スペクトラム値を含む VisualizerFrame
        /// ■確認内容
        /// ・1. 中央インデックスにバーが配置される
        /// ・2. 中央バーの左右が対称になる
        /// </summary>
        [Test]
        public void Given_OddBarCount_When_BuildRenderData_Then_CenterBarIsPlacedAtMiddle()
        {
            // 準備
            var effect = new MirrorBarEffect();
            var frame = new VisualizerFrame(new[] { 0.3, 0.6, 0.9, 0.2, 0.1 }, new[] { 0.0 }, 0.9, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 5, SpectrumProfile.Balanced);

            // 実行
            var result = (BarSpectrumRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Bars, Has.Count.EqualTo(5));
                Assert.That(result.Bars[2].X + (result.Bars[2].Width / 2.0), Is.EqualTo(0.5).Within(0.0001));
                Assert.That(result.Bars[0].Height, Is.EqualTo(result.Bars[4].Height).Within(0.0001));
                Assert.That(result.Bars[1].Height, Is.EqualTo(result.Bars[3].Height).Within(0.0001));
            });
        }

        /// <summary>
        /// MirrorBarEffect が空スペクトラム入力を安全に空バーとして扱うことを確認します。
        /// ■入力
        /// ・1. SpectrumValues が空の VisualizerFrame
        /// ・2. 6 本表示の VisualizerEffectContext
        /// ■確認内容
        /// ・1. BarSpectrumRenderData が生成される
        /// ・2. Bars は空になる
        /// </summary>
        [Test]
        public void Given_EmptySpectrum_When_BuildRenderData_Then_EmptyBarsAreReturned()
        {
            // 準備
            var effect = new MirrorBarEffect();
            var frame = new VisualizerFrame(Array.Empty<double>(), Array.Empty<double>(), 0.0, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 6, SpectrumProfile.Balanced);

            // 実行
            var result = (BarSpectrumRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.That(result.Bars, Is.Empty);
        }

        #endregion
    }
}
