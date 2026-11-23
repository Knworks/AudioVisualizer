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
        /// 点数が同じでも X 座標が変わった場合は最新値を使用します。
        /// </summary>
        [Test]
        public void Polyline_WhenPointPositionsChange_UsesLatestPoints()
        {
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new ShiftedPolylineEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
                Smoothing = 0.5,
            };
            var timestamp = DateTimeOffset.UnixEpoch;
            provider.RaiseFrame(new VisualizerFrame(new[] { 0.25 }, 0, timestamp));
            provider.RaiseFrame(new VisualizerFrame(new[] { 0.75 }, 1, timestamp.AddMilliseconds(16)));

            var points = ((PolylineRenderData)control.CurrentRenderData!).Points;
            Assert.That(points[1].X, Is.EqualTo(0.75));
            Assert.That(points[1].Y, Is.EqualTo(1));
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// フレームが指定する X 座標で折れ線を作るテスト用エフェクトです。
        /// </summary>
        private sealed class ShiftedPolylineEffect : IVisualizerEffect
        {
            #region プロパティ

            /// <summary>
            /// 可変位置の折れ線を作るテスト用エフェクトのメタデータを取得します。
            /// </summary>
            public EffectMetadata Metadata { get; } =
                new("shift", "Shift", "1.0.0", null, null, new[] { InputSource.SystemOutput });

            #endregion

            #region 公開メソッド

            /// <summary>
            /// フレームの値で中央点の位置と各点の高さを決め、折れ線を生成します。
            /// </summary>
            /// <param name="frame">中央点の X 座標と各点の Y 座標を決める値を含むフレーム。</param>
            /// <param name="context">エフェクトの実行時設定。このテスト用エフェクトでは座標計算に使用しません。</param>
            /// <returns>フレームから生成した 3 点の折れ線描画データ。</returns>
            public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
            {
                var points = new[]
                {
                    new NormalizedPoint(0, frame.PeakLevel),
                    new NormalizedPoint(frame.SpectrumValues[0], frame.PeakLevel),
                    new NormalizedPoint(1, frame.PeakLevel),
                };
                return new PolylineRenderData(Metadata.Id, frame.Timestamp, points);
            }

            #endregion
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 平滑化が前フレームからの実時間に応じて進むことを確認します。
        /// </summary>
        /// <param name="milliseconds">先行フレームから次のフレームまでの間隔。単位はミリ秒です。</param>
        /// <param name="expectedY">平滑化後に期待する最初の点の正規化 Y 座標。</param>
        [TestCase(16, 0.5)]
        [TestCase(32, 0.25)]
        public void Smoothing_WithLongerInterval_KeepsLessOfPreviousFrame(int milliseconds, double expectedY)
        {
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new WaveformLineEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
                BarCount = 2,
                Smoothing = 0.5,
            };
            var timestamp = DateTimeOffset.UnixEpoch;
            provider.RaiseFrame(new VisualizerFrame(Array.Empty<double>(), new[] { -1.0, -1.0 }, 1, timestamp));
            provider.RaiseFrame(new VisualizerFrame(Array.Empty<double>(), new[] { 1.0, 1.0 }, 1, timestamp.AddMilliseconds(milliseconds)));

            Assert.That(((PolylineRenderData)control.CurrentRenderData!).Points[0].Y, Is.EqualTo(expectedY).Within(1e-9));
        }

        /// <summary>
        /// コントロールの感度変更を、符号を保った波形の拡大とクリップに反映します。
        /// </summary>
        [Test]
        public void Control_WhenSensitivityChanges_ScalesAndClipsWaveform()
        {
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
                VisualizerEffect = new WaveformLineEffect(),
                Smoothing = 0,
            };
            provider.RaiseFrame(new VisualizerFrame(Array.Empty<double>(), new[] { -0.4, -0.1, 0.1, 0.4 }, 0.4, DateTimeOffset.UtcNow));

            control.Sensitivity = 5.0;

            var data = (PolylineRenderData)control.CurrentRenderData!;
            Assert.That(data.Points.Select(point => point.Y), Is.EqualTo(new[] { 1.0, 0.75, 0.25, 0.0 }).Within(1e-10));
        }

        /// <summary>
        /// 表示点より周期の短い音は、各区間の上下の振幅を含む包絡線として表示されます。
        /// </summary>
        [Test]
        public void Waveform_WhenHighFrequencyIsDownsampled_DrawsAmplitudeEnvelope()
        {
            const int sampleRate = 48000;
            const int pointCount = 32;
            var samples = Enumerable.Range(0, 576)
                .Select(index => (float)(0.2 * Math.Sin(2 * Math.PI * 2000 * index / sampleRate)))
                .ToArray();
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.0, pointCount);
            var frame = new AudioVisualizer.Core.Analysis.AudioFrameAnalyzer().CreateFrame(
                new AudioSampleBuffer(samples, sampleRate, 1, DateTimeOffset.UtcNow), settings);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, pointCount, SpectrumProfile.Raw);
            var data = (PolylineRenderData)new WaveformLineEffect().BuildRenderData(frame, context);

            Assert.That(data.Points, Has.Count.EqualTo((pointCount * 2) + 1));
            Assert.That(data.Points.Take(pointCount).Select(point => point.Y), Is.All.LessThan(0.5));
            Assert.That(data.Points.Skip(pointCount).Take(pointCount).Select(point => point.Y), Is.All.GreaterThan(0.5));
        }

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
                VisualizerEffect = new WaveformLineEffect(),
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

        /// <summary>
        /// AudioVisualizerControl が WaveformLineEffect でも Smoothing を適用することを確認します。
        /// ■入力
        /// ・1. Smoothing = 0.5 の AudioVisualizerControl
        /// ・2. Y 座標が異なる 2 フレーム
        /// ■確認内容
        /// ・1. 2 フレーム目の CurrentRenderData が中間値へ補間される
        /// </summary>
        [Test]
        public void Given_WaveformRenderData_When_SmoothingIsEnabled_Then_PolylinePointsAreInterpolated()
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
                VisualizerEffect = new WaveformLineEffect(),
                Smoothing = 0.5,
            };
            var firstFrame = new VisualizerFrame(new[] { 0.1, 0.2 }, new[] { -1.0, -1.0 }, 1.0, DateTimeOffset.UnixEpoch);
            var secondFrame = new VisualizerFrame(new[] { 0.1, 0.2 }, new[] { 1.0, 1.0 }, 1.0, DateTimeOffset.UnixEpoch.AddMilliseconds(16));

            // 実行
            provider.RaiseFrame(firstFrame);
            provider.RaiseFrame(secondFrame);

            // 検証
            var renderData = (PolylineRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Points, Has.Count.EqualTo(2));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Points[1].Y, Is.EqualTo(0.5).Within(0.0001));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が Smoothing = 0 の場合は WaveformLineEffect の描画値をそのまま採用することを確認します。
        /// ■入力
        /// ・1. Smoothing = 0 の AudioVisualizerControl
        /// ・2. Y 座標が異なる 2 フレーム
        /// ■確認内容
        /// ・1. 2 フレーム目の CurrentRenderData が補間されず最新値になる
        /// </summary>
        [Test]
        public void Given_WaveformRenderData_When_SmoothingIsZero_Then_LatestPolylinePointsAreUsed()
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
                VisualizerEffect = new WaveformLineEffect(),
                Smoothing = 0.0,
            };
            var firstFrame = new VisualizerFrame(new[] { 0.1, 0.2 }, new[] { -1.0, -1.0 }, 1.0, DateTimeOffset.UnixEpoch);
            var secondFrame = new VisualizerFrame(new[] { 0.1, 0.2 }, new[] { 1.0, 1.0 }, 1.0, DateTimeOffset.UnixEpoch.AddMilliseconds(16));

            // 実行
            provider.RaiseFrame(firstFrame);
            provider.RaiseFrame(secondFrame);

            // 検証
            var renderData = (PolylineRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Points, Has.Count.EqualTo(2));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(0.0).Within(0.0001));
                Assert.That(renderData.Points[1].Y, Is.EqualTo(0.0).Within(0.0001));
            });
        }

        /// <summary>
        /// 点数が異なる折れ線では最新の点列をそのまま採用することを確認します。
        /// ■入力
        /// ・1. Smoothing = 0.5 の AudioVisualizerControl
        /// ・2. 2 点の後に 3 点となる 2 フレーム
        /// ■確認内容
        /// ・1. 全点が最新値のまま反映される
        /// </summary>
        [Test]
        public void Waveform_WhenPointCountChanges_UsesLatestPoints()
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
                VisualizerEffect = new WaveformLineEffect(),
                Smoothing = 0.5,
            };
            var firstFrame = new VisualizerFrame(new[] { 0.1, 0.2 }, new[] { -1.0, 1.0 }, 1.0, DateTimeOffset.UnixEpoch);
            var secondFrame = new VisualizerFrame(new[] { 0.1, 0.2, 0.3 }, new[] { 1.0, 0.0, -1.0 }, 1.0, DateTimeOffset.UnixEpoch.AddMilliseconds(16));

            // 実行
            provider.RaiseFrame(firstFrame);
            provider.RaiseFrame(secondFrame);

            // 検証
            var renderData = (PolylineRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Points, Has.Count.EqualTo(3));
                Assert.That(renderData.Points[0].Y, Is.EqualTo(0.0).Within(0.0001));
                Assert.That(renderData.Points[1].Y, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Points[2].Y, Is.EqualTo(1.0).Within(0.0001));
            });
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

            #region イベントハンドラ

            /// <summary>
            /// フレーム生成イベントです。
            /// </summary>
            public event EventHandler<VisualizerFrameEventArgs>? FrameProduced
            {
                add => m_FrameProduced += value;
                remove => m_FrameProduced -= value;
            }

            #endregion

            #region 公開メソッド

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
            #region 構築・消滅

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
