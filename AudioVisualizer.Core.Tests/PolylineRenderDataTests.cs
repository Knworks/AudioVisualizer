namespace AudioVisualizer.Core.Tests
{
    using AudioVisualizer.Core.Effects;

    /// <summary>
    /// <see cref="PolylineRenderData"/> の契約を検証します。
    /// </summary>
    [TestFixture]
    public class PolylineRenderDataTests
    {
        #region 公開メソッド

        /// <summary>
        /// PolylineRenderData が正規化点列を保持することを確認します。
        /// ■入力
        /// ・1. x=0.1, y=0.8 と x=0.9, y=0.2 の NormalizedPoint
        /// ・2. 上記点列を含む PolylineRenderData
        /// ■確認内容
        /// ・1. 各点の座標が入力値を保持する
        /// ・2. Points に追加した点列と Timestamp が保持される
        /// </summary>
        [Test]
        public void Given_ValidPoints_When_CreatingPolylineRenderData_Then_PropertiesAreAssigned()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行
            var points = new[]
            {
                new NormalizedPoint(0.1, 0.8),
                new NormalizedPoint(0.9, 0.2),
            };
            var renderData = new PolylineRenderData("waveform", timestamp, points);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Points, Has.Count.EqualTo(2));
                Assert.That(renderData.Points[0].X, Is.EqualTo(0.1));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(0.8));
                Assert.That(renderData.Points[1].X, Is.EqualTo(0.9));
                Assert.That(renderData.Points[1].Y, Is.EqualTo(0.2));
                Assert.That(renderData.Timestamp, Is.EqualTo(timestamp));
            });
        }

        /// <summary>
        /// NormalizedPoint が正規化範囲外の座標を拒否することを確認します。
        /// ■入力
        /// ・1. x または y に 0.0 未満、または 1.0 超の値を指定する
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_OutOfRangePointValue_When_CreatingNormalizedPoint_Then_ThrowsArgumentOutOfRangeException()
        {
            // 準備と実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new NormalizedPoint(-0.1, 0.5), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new NormalizedPoint(0.5, 1.1), Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        /// <summary>
        /// PolylineRenderData が null の点列を拒否することを確認します。
        /// ■入力
        /// ・1. points = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullPoints_When_CreatingPolylineRenderData_Then_ThrowsArgumentNullException()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行と検証
            Assert.That(() => new PolylineRenderData("waveform", timestamp, null!), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion
    }
}
