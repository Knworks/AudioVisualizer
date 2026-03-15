namespace AudioVisualizer.Core.Tests
{
    using AudioVisualizer.Core.Effects;

    /// <summary>
    /// <see cref="BandLevelMeterRenderData"/> の契約を検証します。
    /// </summary>
    [TestFixture]
    public class BandLevelMeterRenderDataTests
    {
        #region 公開メソッド

        /// <summary>
        /// BandLevelMeterRenderData が帯域メーター情報を保持することを確認します。
        /// ■入力
        /// ・1. x=0.1, width=0.25, level=0.7, label=\"Mid\" の BandLevelMeterItem
        /// ・2. 上記帯域を 1 件含む BandLevelMeterRenderData
        /// ■確認内容
        /// ・1. 各プロパティが入力値を保持する
        /// ・2. Bands に追加した帯域が保持される
        /// </summary>
        [Test]
        public void Given_ValidBandData_When_CreatingRenderData_Then_PropertiesAreAssigned()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行
            var band = new BandLevelMeterItem(0.1, 0.25, 0.7, "Mid");
            var renderData = new BandLevelMeterRenderData("meter", timestamp, new[] { band });

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(band.X, Is.EqualTo(0.1));
                Assert.That(band.Width, Is.EqualTo(0.25));
                Assert.That(band.Level, Is.EqualTo(0.7));
                Assert.That(band.Label, Is.EqualTo("Mid"));
                Assert.That(renderData.Bands, Has.Count.EqualTo(1));
                Assert.That(renderData.Bands[0], Is.SameAs(band));
                Assert.That(renderData.Timestamp, Is.EqualTo(timestamp));
            });
        }

        /// <summary>
        /// BandLevelMeterItem が不正な値を拒否することを確認します。
        /// ■入力
        /// ・1. x, width, level に範囲外の値を指定する
        /// ・2. label に空白文字列を指定する
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException または ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidBandValue_When_CreatingBandLevelMeterItem_Then_ThrowsException()
        {
            // 準備と実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new BandLevelMeterItem(-0.1, 0.2, 0.3, "Low"), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BandLevelMeterItem(0.1, 1.2, 0.3, "Low"), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BandLevelMeterItem(0.1, 0.2, 1.3, "Low"), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BandLevelMeterItem(0.1, 0.2, 0.3, " "), Throws.TypeOf<ArgumentException>());
            });
        }

        /// <summary>
        /// BandLevelMeterRenderData が null の帯域一覧を拒否することを確認します。
        /// ■入力
        /// ・1. bands = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullBands_When_CreatingBandLevelMeterRenderData_Then_ThrowsArgumentNullException()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行と検証
            Assert.That(() => new BandLevelMeterRenderData("meter", timestamp, null!), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion
    }
}
