using System;
using System.Threading;
using System.Windows.Media;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;
using AudioVisualizer.Wpf.Effects;
using AudioVisualizer.Wpf.Rendering;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// <see cref="WaveformLineEffect"/> の変換とコントロール統合を検証します。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WaveformLineEffectTests
    {
        #region 公開メソッド

        /// <summary>
        /// WaveformLineEffect が波形値を折れ線用の正規化点列へ変換することを確認します。
        /// ■入力
        /// ・1. waveformValues = [-1.0, 0.0, 1.0] を含む VisualizerFrame
        /// ・2. 既定値の VisualizerEffectContext
        /// ■確認内容
        /// ・1. PolylineRenderData が生成される
        /// ・2. 左から右へ均等な X 座標と、上中下に対応した Y 座標へ変換される
        /// </summary>
        [Test]
        public void Given_WaveformFrame_When_BuildRenderData_Then_PolylinePointsAreGenerated()
        {
            // 準備
            var effect = new WaveformLineEffect();
            var frame = new VisualizerFrame(new[] { 0.1, 0.2, 0.3 }, new[] { -1.0, 0.0, 1.0 }, 1.0, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.Microphone, 1.0, 0.0, 3, SpectrumProfile.Balanced);

            // 実行
            var renderData = (PolylineRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.EffectId, Is.EqualTo("waveform-line"));
                Assert.That(renderData.Points, Has.Count.EqualTo(3));
                Assert.That(renderData.Points[0].X, Is.EqualTo(0.0).Within(0.0001));
                Assert.That(renderData.Points[1].X, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Points[2].X, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(renderData.Points[1].Y, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Points[2].Y, Is.EqualTo(0.0).Within(0.0001));
            });
        }

        /// <summary>
        /// WaveformLineEffect が単一点の波形値を両端に広げて線描画可能にすることを確認します。
        /// ■入力
        /// ・1. waveformValues = [0.4] を含む VisualizerFrame
        /// ・2. 既定値の VisualizerEffectContext
        /// ■確認内容
        /// ・1. 2 点の PolylineRenderData が生成される
        /// ・2. 始点と終点の Y 座標が同じである
        /// </summary>
        [Test]
        public void Given_SingleWaveformValue_When_BuildRenderData_Then_PointIsExpandedToEndpoints()
        {
            // 準備
            var effect = new WaveformLineEffect();
            var frame = new VisualizerFrame(new[] { 0.1 }, new[] { 0.4 }, 0.4, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.Microphone, 1.0, 0.0, 1, SpectrumProfile.Balanced);

            // 実行
            var renderData = (PolylineRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Points, Has.Count.EqualTo(2));
                Assert.That(renderData.Points[0].X, Is.EqualTo(0.0).Within(0.0001));
                Assert.That(renderData.Points[1].X, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(renderData.Points[1].Y).Within(0.0001));
            });
        }

        /// <summary>
        /// WaveformLineEffect が空波形入力を空の点列として扱うことを確認します。
        /// ■入力
        /// ・1. waveformValues が空の VisualizerFrame
        /// ・2. 既定値の VisualizerEffectContext
        /// ■確認内容
        /// ・1. PolylineRenderData が生成される
        /// ・2. Points は空になる
        /// </summary>
        [Test]
        public void Given_EmptyWaveform_When_BuildRenderData_Then_EmptyPolylineIsReturned()
        {
            // 準備
            var effect = new WaveformLineEffect();
            var frame = new VisualizerFrame(new[] { 0.1 }, Array.Empty<double>(), 0.1, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.Microphone, 1.0, 0.0, 1, SpectrumProfile.Balanced);

            // 実行
            var renderData = (PolylineRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.That(renderData.Points, Is.Empty);
        }

        /// <summary>
        /// AudioVisualizerControl が WaveformLineEffect を指定した場合に折れ線レンダーデータを保持することを確認します。
        /// ■入力
        /// ・1. WaveformLineEffect を指定した AudioVisualizerControl
        /// ・2. waveformValues を含む VisualizerFrame
        /// ■確認内容
        /// ・1. CurrentRenderData が PolylineRenderData になる
        /// ・2. OnRender 実行で例外が発生しない
        /// </summary>
        [Test]
        public void Given_ControlWithWaveformEffect_When_FrameProduced_Then_PolylineRenderDataIsUsed()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new TestableAudioVisualizerControl(
                provider,
                new SpectrumBarEffect(),
                new BarSpectrumRenderer(),
                new PolylineRenderer())
            {
                IsActive = true,
                Effect = new WaveformLineEffect(),
                Width = 120,
                Height = 60,
            };
            var frame = new VisualizerFrame(new[] { 0.2, 0.4, 0.6 }, new[] { -0.5, 0.0, 0.5 }, 0.6, DateTimeOffset.UtcNow);

            // 実行
            provider.RaiseFrame(frame);

            // 検証
            Assert.That(control.CurrentRenderData, Is.TypeOf<PolylineRenderData>());
            Assert.That(
                () =>
                {
                    var drawingVisual = new DrawingVisual();
                    using var drawingContext = drawingVisual.RenderOpen();
                    control.InvokeOnRender(drawingContext);
                },
                Throws.Nothing);
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// テスト用の音声入力プロバイダーです。
        /// </summary>
        private sealed class FakeAudioInputProvider : IAudioInputProvider
        {
            #region フィールド

            /// <summary>
            /// フレーム生成イベントの実体です。
            /// </summary>
            private EventHandler<VisualizerFrameEventArgs>? m_FrameProduced;

            #endregion

            #region プロパティ

            /// <summary>
            /// キャプチャ中かどうかを取得します。
            /// </summary>
            public bool IsCapturing { get; private set; }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// フレーム生成イベントです。
            /// </summary>
            public event EventHandler<VisualizerFrameEventArgs>? FrameProduced
            {
                add => m_FrameProduced += value;
                remove => m_FrameProduced -= value;
            }

            /// <summary>
            /// 入力開始を模擬します。
            /// </summary>
            /// <param name="settings">可視化設定です。</param>
            /// <returns>開始結果です。</returns>
            public AudioInputStartResult Start(VisualizerSettings settings)
            {
                IsCapturing = true;
                return AudioInputStartResult.Success();
            }

            /// <summary>
            /// 入力停止を模擬します。
            /// </summary>
            public void Stop()
            {
                IsCapturing = false;
            }

            /// <summary>
            /// テスト用にフレーム生成イベントを発火します。
            /// </summary>
            /// <param name="frame">通知するフレームです。</param>
            public void RaiseFrame(VisualizerFrame frame)
            {
                m_FrameProduced?.Invoke(this, new VisualizerFrameEventArgs(frame));
            }

            #endregion
        }

        /// <summary>
        /// テストから OnRender を呼び出すためのコントロールです。
        /// </summary>
        private sealed class TestableAudioVisualizerControl : AudioVisualizerControl
        {
            #region 構築 / 消滅

            /// <summary>
            /// <see cref="TestableAudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="audioInputProvider">音声入力プロバイダーです。</param>
            /// <param name="defaultEffect">既定エフェクトです。</param>
            /// <param name="barRenderer">バー描画レンダラーです。</param>
            /// <param name="polylineRenderer">折れ線描画レンダラーです。</param>
            public TestableAudioVisualizerControl(
                IAudioInputProvider audioInputProvider,
                IVisualizerEffect defaultEffect,
                BarSpectrumRenderer barRenderer,
                PolylineRenderer polylineRenderer)
                : base(audioInputProvider, defaultEffect, barRenderer, polylineRenderer)
            {
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// テストから OnRender を呼び出します。
            /// </summary>
            /// <param name="drawingContext">描画先の DrawingContext です。</param>
            public void InvokeOnRender(DrawingContext drawingContext)
            {
                OnRender(drawingContext);
            }

            #endregion
        }

        #endregion
    }
}
