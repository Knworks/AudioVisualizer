using AudioVisualizer.Wpf.Effects;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// <see cref="BuiltInVisualizerEffects"/> の組込エフェクト生成を検証します。
    /// </summary>
    [TestFixture]
    public class BuiltInVisualizerEffectsTests
    {
        #region 公開メソッド

        /// <summary>
        /// BuiltInVisualizerEffects が 5 種類の組込エフェクトを正しく生成することを確認します。
        /// ■入力
        /// ・1. 定義済みの 5 種類の BuiltInVisualizerEffectKind
        /// ■確認内容
        /// ・1. 生成結果が null にならない
        /// ・2. 生成結果の実装型が期待どおりである
        /// ・3. 生成結果の Metadata.Id が期待どおりである
        /// </summary>
        [Test]
        [TestCase(BuiltInVisualizerEffectKind.SpectrumBar, typeof(SpectrumBarEffect), "spectrum-bars")]
        [TestCase(BuiltInVisualizerEffectKind.WaveformLine, typeof(WaveformLineEffect), "waveform-line")]
        [TestCase(BuiltInVisualizerEffectKind.MirrorBar, typeof(MirrorBarEffect), "mirror-bars")]
        [TestCase(BuiltInVisualizerEffectKind.PeakHoldBar, typeof(PeakHoldBarEffect), "peak-hold-bars")]
        [TestCase(BuiltInVisualizerEffectKind.BandLevelMeter, typeof(BandLevelMeterEffect), "band-level-meter")]
        public void Given_BuiltInEffectKind_When_CreatingEffect_Then_MatchingEffectIsReturned(
            BuiltInVisualizerEffectKind effectKind,
            Type expectedType,
            string expectedEffectId)
        {
            // 準備なし

            // 実行
            var effect = BuiltInVisualizerEffects.Create(effectKind);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(effect, Is.Not.Null);
                Assert.That(effect, Is.InstanceOf(expectedType));
                Assert.That(effect.Metadata.Id, Is.EqualTo(expectedEffectId));
            });
        }

        /// <summary>
        /// BuiltInVisualizerEffects が未対応の組込エフェクト種別を拒否することを確認します。
        /// ■入力
        /// ・1. 定義外の BuiltInVisualizerEffectKind
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidBuiltInEffectKind_When_CreatingEffect_Then_ArgumentOutOfRangeExceptionIsThrown()
        {
            // 準備
            var invalidKind = (BuiltInVisualizerEffectKind)(-1);

            // 実行と検証
            Assert.That(
                () => BuiltInVisualizerEffects.Create(invalidKind),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        #endregion
    }
}
