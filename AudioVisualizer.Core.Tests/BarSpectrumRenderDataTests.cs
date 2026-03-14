using System;
using AudioVisualizer.Core.Effects;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// バー型レンダーデータの生成条件を検証します。
    /// </summary>
    [TestFixture]
    public class BarSpectrumRenderDataTests
    {
        #region 公開メソッド

        /// <summary>
        /// BarRenderItem と BarSpectrumRenderData が正規化バー情報を保持することを確認します。
        /// ■入力
        /// ・1. x=0.1, width=0.2, height=0.3, level=0.3 の BarRenderItem
        /// ・2. 上記バーを 1 件含む BarSpectrumRenderData
        /// ■確認内容
        /// ・1. 各プロパティが入力値を保持する
        /// ・2. Bars に追加したバーが保持される
        /// </summary>
        [Test]
        public void Given_ValidBarData_When_CreatingRenderData_Then_PropertiesAreAssigned()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行
            var bar = new BarRenderItem(0.1, 0.2, 0.3, 0.3);
            var renderData = new BarSpectrumRenderData("bars", timestamp, new[] { bar });

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(bar.X, Is.EqualTo(0.1));
                Assert.That(bar.Width, Is.EqualTo(0.2));
                Assert.That(bar.Height, Is.EqualTo(0.3));
                Assert.That(bar.Level, Is.EqualTo(0.3));
                Assert.That(renderData.Bars, Has.Count.EqualTo(1));
                Assert.That(renderData.Bars[0], Is.SameAs(bar));
                Assert.That(renderData.Timestamp, Is.EqualTo(timestamp));
            });
        }

        /// <summary>
        /// BarRenderItem が正規化範囲外の値を拒否することを確認します。
        /// ■入力
        /// ・1. x, width, height, level のいずれかに範囲外の値を指定する
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_OutOfRangeBarValue_When_CreatingBarRenderItem_Then_ThrowsArgumentOutOfRangeException()
        {
            // 準備と実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new BarRenderItem(-0.1, 0.2, 0.3, 0.3), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BarRenderItem(0.1, 1.2, 0.3, 0.3), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BarRenderItem(0.1, 0.2, 1.3, 0.3), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new BarRenderItem(0.1, 0.2, 0.3, 1.3), Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        /// <summary>
        /// BarSpectrumRenderData が null のバー一覧を拒否することを確認します。
        /// ■入力
        /// ・1. bars = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullBars_When_CreatingRenderData_Then_ThrowsArgumentNullException()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行と検証
            Assert.That(() => new BarSpectrumRenderData("bars", timestamp, null!), Throws.TypeOf<ArgumentNullException>());
        }

        #endregion
    }
}
