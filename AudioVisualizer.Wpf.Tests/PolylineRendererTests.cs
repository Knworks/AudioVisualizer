using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Wpf.Rendering;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// <see cref="PolylineRenderer"/> の点列変換と描画を検証します。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class PolylineRendererTests
    {
        #region 公開メソッド

        /// <summary>
        /// PolylineRenderer が正規化点列を実座標へ変換できることを確認します。
        /// ■入力
        /// ・1. 3 点の PolylineRenderData
        /// ・2. renderSize = 100 x 40
        /// ■確認内容
        /// ・1. X 座標が横幅へ比例して変換される
        /// ・2. Y 座標が上下端に貼り付きすぎない位置へ変換される
        /// </summary>
        [Test]
        public void Given_PolylineRenderData_When_CreateRenderPoints_Then_PointsAreScaled()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var renderData = new PolylineRenderData(
                "waveform",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new NormalizedPoint(0.0, 0.0),
                    new NormalizedPoint(0.5, 0.5),
                    new NormalizedPoint(1.0, 1.0),
                });

            // 実行
            var points = renderer.CreateRenderPoints(renderData, new Size(100, 40));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(points, Has.Count.EqualTo(3));
                Assert.That(points[0].X, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(points[1].X, Is.EqualTo(50.0).Within(0.0001));
                Assert.That(points[2].X, Is.EqualTo(99.5).Within(0.0001));
                Assert.That(points[0].Y, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(points[1].Y, Is.EqualTo(20.0).Within(0.0001));
                Assert.That(points[2].Y, Is.EqualTo(39.5).Within(0.0001));
            });
        }

        /// <summary>
        /// PolylineRenderer が極小描画領域でも領域内の点列を返すことを確認します。
        /// ■入力
        /// ・1. 上端と下端を含む PolylineRenderData
        /// ・2. renderSize = 1 x 1
        /// ■確認内容
        /// ・1. 点列が 0 から 1 の範囲内に収まる
        /// </summary>
        [Test]
        public void Given_TinyRenderSize_When_CreateRenderPoints_Then_PointsRemainInsideBounds()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var renderData = new PolylineRenderData(
                "waveform",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new NormalizedPoint(0.0, 0.0),
                    new NormalizedPoint(1.0, 1.0),
                });

            // 実行
            var points = renderer.CreateRenderPoints(renderData, new Size(1, 1));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(points, Has.Count.EqualTo(2));
                Assert.That(points[0].X, Is.InRange(0.0, 1.0));
                Assert.That(points[0].Y, Is.InRange(0.0, 1.0));
                Assert.That(points[1].X, Is.InRange(0.0, 1.0));
                Assert.That(points[1].Y, Is.InRange(0.0, 1.0));
            });
        }

        /// <summary>
        /// PolylineRenderer の座標変換が辺長 0 以下を安全に 0 へ丸めることを確認します。
        /// ■入力
        /// ・1. normalizedValue = 0.5
        /// ・2. length = 0
        /// ■確認内容
        /// ・1. 返却値が 0 になる
        /// </summary>
        [Test]
        public void Given_NonPositiveLength_When_MapToPixelCoordinate_Then_ZeroIsReturned()
        {
            // 準備
            var method = typeof(PolylineRenderer).GetMethod("MapToPixelCoordinate", BindingFlags.NonPublic | BindingFlags.Static);

            // 実行
            var result = (double)method!.Invoke(null, new object[] { 0.5, 0.0 })!;

            // 検証
            Assert.That(result, Is.EqualTo(0.0));
        }

        /// <summary>
        /// PolylineRenderer が空点列や不正サイズでは空結果を返すことを確認します。
        /// ■入力
        /// ・1. 点列が空の PolylineRenderData
        /// ・2. renderSize = 0 x 40
        /// ■確認内容
        /// ・1. 点列が空で返る
        /// </summary>
        [Test]
        public void Given_EmptyInputOrInvalidSize_When_CreateRenderPoints_Then_EmptyResultIsReturned()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var renderData = new PolylineRenderData("waveform", DateTimeOffset.UtcNow, Array.Empty<NormalizedPoint>());

            // 実行
            var emptyPoints = renderer.CreateRenderPoints(renderData, new Size(100, 40));
            var invalidSizePoints = renderer.CreateRenderPoints(
                new PolylineRenderData(
                    "waveform",
                    DateTimeOffset.UtcNow,
                    new[]
                    {
                        new NormalizedPoint(0.0, 0.5),
                        new NormalizedPoint(1.0, 0.5),
                    }),
                new Size(0, 40));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(emptyPoints, Is.Empty);
                Assert.That(invalidSizePoints, Is.Empty);
            });
        }

        /// <summary>
        /// PolylineRenderer が同じレンダーデータとサイズで点列計算結果を再利用することを確認します。
        /// ■入力
        /// ・1. 同一インスタンスの PolylineRenderData
        /// ・2. 同一の renderSize
        /// ■確認内容
        /// ・1. 返却される点列参照が再利用される
        /// </summary>
        [Test]
        public void Given_SameRenderDataAndSize_When_CreateRenderPointsRepeatedly_Then_CachedResultIsReused()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var renderData = new PolylineRenderData(
                "waveform",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new NormalizedPoint(0.0, 0.5),
                    new NormalizedPoint(1.0, 0.5),
                });
            var renderSize = new Size(120, 60);

            // 実行
            var first = renderer.CreateRenderPoints(renderData, renderSize);
            var second = renderer.CreateRenderPoints(renderData, renderSize);

            // 検証
            Assert.That(second, Is.SameAs(first));
        }

        /// <summary>
        /// PolylineRenderer が対応レンダーデータを DrawingContext へ描画できることを確認します。
        /// ■入力
        /// ・1. 2 点の PolylineRenderData
        /// ・2. DrawingVisual の DrawingContext
        /// ■確認内容
        /// ・1. Render 実行で例外が発生しない
        /// </summary>
        [Test]
        public void Given_PolylineRenderData_When_Render_Then_NoExceptionIsThrown()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var renderData = new PolylineRenderData(
                "waveform",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new NormalizedPoint(0.0, 0.5),
                    new NormalizedPoint(1.0, 0.5),
                });

            // 実行と検証
            Assert.That(
                () =>
                {
                    var drawingVisual = new DrawingVisual();
                    using var drawingContext = drawingVisual.RenderOpen();
                    renderer.Render(drawingContext, renderData, new Size(120, 60), Brushes.DeepSkyBlue);
                },
                Throws.Nothing);
        }

        /// <summary>
        /// PolylineRenderer が非対応レンダーデータや空点列を無視できることを確認します。
        /// ■入力
        /// ・1. 非対応 RenderData
        /// ・2. 空点列の PolylineRenderData
        /// ■確認内容
        /// ・1. Render 実行で例外が発生しない
        /// </summary>
        [Test]
        public void Given_UnsupportedOrEmptyRenderData_When_Render_Then_NoExceptionIsThrown()
        {
            // 準備
            var renderer = new PolylineRenderer();
            var emptyRenderData = new PolylineRenderData("waveform", DateTimeOffset.UtcNow, Array.Empty<NormalizedPoint>());

            // 実行と検証
            Assert.That(
                () =>
                {
                    var drawingVisual = new DrawingVisual();
                    using var drawingContext = drawingVisual.RenderOpen();
                    renderer.Render(drawingContext, new FakeRenderData("fake", DateTimeOffset.UtcNow), new Size(120, 60), Brushes.DeepSkyBlue);
                    renderer.Render(drawingContext, emptyRenderData, new Size(120, 60), Brushes.DeepSkyBlue);
                },
                Throws.Nothing);
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// 非対応レンダーデータの代替型です。
        /// </summary>
        private sealed class FakeRenderData : VisualizerRenderData
        {
            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeRenderData"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="effectId">エフェクト識別子です。</param>
            /// <param name="timestamp">描画データ作成時刻です。</param>
            public FakeRenderData(string effectId, DateTimeOffset timestamp)
                : base(effectId, timestamp)
            {
            }

            #endregion
        }

        #endregion
    }
}
