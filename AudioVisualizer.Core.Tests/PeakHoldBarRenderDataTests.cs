namespace AudioVisualizer.Core.Tests
{
    using AudioVisualizer.Core.Effects;

    /// <summary>
    /// <see cref="PeakHoldBarRenderData"/> の契約を検証します。
    /// </summary>
    [TestFixture]
    public class PeakHoldBarRenderDataTests
    {
        #region 公開メソッド

        /// <summary>
        /// PeakHoldBarRenderData がバーとピーク保持マーカーを保持することを確認します。
        /// ■入力
        /// ・1. 有効な BarRenderItem と PeakMarkerItem を各 1 件
        /// ・2. 上記を含む PeakHoldBarRenderData
        /// ■確認内容
        /// ・1. Bars と PeakMarkers に入力した要素が保持される
        /// ・2. Timestamp が保持される
        /// </summary>
        [Test]
        public void Given_ValidPeakHoldData_When_CreatingRenderData_Then_PropertiesAreAssigned()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;
            var bars = new[] { new BarRenderItem(0.1, 0.2, 0.6, 0.6) };
            var peakMarkers = new[] { new PeakMarkerItem(0.1, 0.2, 0.65) };

            // 実行
            var renderData = new PeakHoldBarRenderData("peak-hold", timestamp, bars, peakMarkers);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bars, Has.Count.EqualTo(1));
                Assert.That(renderData.PeakMarkers, Has.Count.EqualTo(1));
                Assert.That(renderData.Bars[0], Is.SameAs(bars[0]));
                Assert.That(renderData.PeakMarkers[0], Is.SameAs(peakMarkers[0]));
                Assert.That(renderData.PeakMarkers[0].X, Is.EqualTo(0.1));
                Assert.That(renderData.PeakMarkers[0].Width, Is.EqualTo(0.2));
                Assert.That(renderData.PeakMarkers[0].Height, Is.EqualTo(0.65));
                Assert.That(renderData.Timestamp, Is.EqualTo(timestamp));
            });
        }

        /// <summary>
        /// PeakMarkerItem が正規化範囲外の値を拒否することを確認します。
        /// ■入力
        /// ・1. x, width, height のいずれかに範囲外の値を指定する
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_OutOfRangePeakMarkerValue_When_CreatingPeakMarkerItem_Then_ThrowsArgumentOutOfRangeException()
        {
            // 準備と実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new PeakMarkerItem(-0.1, 0.2, 0.3), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new PeakMarkerItem(0.1, 1.2, 0.3), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new PeakMarkerItem(0.1, 0.2, 1.3), Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        /// <summary>
        /// PeakHoldBarRenderData が null の一覧を拒否することを確認します。
        /// ■入力
        /// ・1. bars = null または peakMarkers = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullCollections_When_CreatingPeakHoldBarRenderData_Then_ThrowsArgumentNullException()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;
            var bars = new[] { new BarRenderItem(0.1, 0.2, 0.3, 0.3) };
            var peakMarkers = new[] { new PeakMarkerItem(0.1, 0.2, 0.3) };

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new PeakHoldBarRenderData("peak-hold", timestamp, null!, peakMarkers), Throws.TypeOf<ArgumentNullException>());
                Assert.That(() => new PeakHoldBarRenderData("peak-hold", timestamp, bars, null!), Throws.TypeOf<ArgumentNullException>());
            });
        }

        #endregion
    }
}
