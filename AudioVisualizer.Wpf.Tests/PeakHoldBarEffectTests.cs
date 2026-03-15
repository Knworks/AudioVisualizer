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
    /// <see cref="PeakHoldBarEffect"/> の保持、減衰、停止時リセットを検証します。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class PeakHoldBarEffectTests
    {
        #region 公開メソッド

        /// <summary>
        /// PeakHoldBarEffect がバー本体とピーク保持線を生成することを確認します。
        /// ■入力
        /// ・1. 4 本表示の VisualizerEffectContext
        /// ・2. 複数スペクトラム値を含む VisualizerFrame
        /// ■確認内容
        /// ・1. PeakHoldBarRenderData が生成される
        /// ・2. Bars と PeakMarkers が同じ本数を持つ
        /// </summary>
        [Test]
        public void Given_SpectrumFrame_When_BuildRenderData_Then_BarsAndPeakMarkersAreGenerated()
        {
            // 準備
            var effect = new PeakHoldBarEffect();
            var frame = new VisualizerFrame(new[] { 0.2, 0.4, 0.8, 0.6 }, new[] { 0.0 }, 0.8, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 4, SpectrumProfile.Balanced);

            // 実行
            var result = (PeakHoldBarRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.EffectId, Is.EqualTo("peak-hold-bars"));
                Assert.That(result.Bars, Has.Count.EqualTo(4));
                Assert.That(result.PeakMarkers, Has.Count.EqualTo(4));
            });
        }

        /// <summary>
        /// PeakHoldBarEffect が連続フレームでピーク保持と減衰を行うことを確認します。
        /// ■入力
        /// ・1. 高レベルフレームの後に低レベルフレームを渡す
        /// ■確認内容
        /// ・1. 2 フレーム目のピーク保持線がバー本体より高い
        /// ・2. 1 フレーム目よりは低くなり、減衰している
        /// </summary>
        [Test]
        public void Given_SequentialFrames_When_BuildRenderData_Then_PeakMarkersHoldAndDecay()
        {
            // 準備
            var effect = new PeakHoldBarEffect();
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 4, SpectrumProfile.Balanced);
            var highFrame = new VisualizerFrame(new[] { 1.0, 1.0, 1.0, 1.0 }, new[] { 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var lowFrame = new VisualizerFrame(new[] { 0.2, 0.2, 0.2, 0.2 }, new[] { 0.0 }, 0.2, DateTimeOffset.UtcNow.AddMilliseconds(16));

            // 実行
            var firstResult = (PeakHoldBarRenderData)effect.BuildRenderData(highFrame, context);
            var secondResult = (PeakHoldBarRenderData)effect.BuildRenderData(lowFrame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(secondResult.PeakMarkers[0].Height, Is.GreaterThan(secondResult.Bars[0].Height));
                Assert.That(secondResult.PeakMarkers[0].Height, Is.LessThan(firstResult.PeakMarkers[0].Height));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が PeakHoldBarEffect でも Smoothing をバー本体へだけ適用することを確認します。
        /// ■入力
        /// ・1. PeakHoldBarEffect と Smoothing = 0.5 を指定した AudioVisualizerControl
        /// ・2. 高レベルフレームの後に低レベルフレームを渡す
        /// ■確認内容
        /// ・1. 2 フレーム目のバー本体が中間値へ補間される
        /// ・2. ピーク保持線はエフェクト側の減衰結果をそのまま維持する
        /// </summary>
        [Test]
        public void Given_PeakHoldBarEffect_When_SmoothingIsEnabled_Then_OnlyBarsAreInterpolated()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new TestableAudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer(), new PolylineRenderer())
            {
                IsActive = true,
                Effect = new PeakHoldBarEffect(),
                Smoothing = 0.5,
            };
            var firstFrame = new VisualizerFrame(new[] { 1.0, 1.0, 1.0, 1.0 }, new[] { 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var secondFrame = new VisualizerFrame(new[] { 0.2, 0.2, 0.2, 0.2 }, new[] { 0.0 }, 0.2, DateTimeOffset.UtcNow.AddMilliseconds(16));
            var verificationEffect = new PeakHoldBarEffect();
            var effectContext = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.5, 32, SpectrumProfile.Balanced);
            var firstRenderData = (PeakHoldBarRenderData)verificationEffect.BuildRenderData(firstFrame, effectContext);
            var secondRenderData = (PeakHoldBarRenderData)verificationEffect.BuildRenderData(secondFrame, effectContext);
            var expectedBarHeight = (firstRenderData.Bars[0].Height * 0.5) + (secondRenderData.Bars[0].Height * 0.5);
            var expectedPeakHeight = secondRenderData.PeakMarkers[0].Height;

            // 実行
            provider.RaiseFrame(firstFrame);
            provider.RaiseFrame(secondFrame);

            // 検証
            var renderData = (PeakHoldBarRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bars[0].Height, Is.EqualTo(expectedBarHeight).Within(0.0001));
                Assert.That(renderData.Bars[0].Level, Is.EqualTo(expectedBarHeight).Within(0.0001));
                Assert.That(renderData.PeakMarkers[0].Height, Is.EqualTo(expectedPeakHeight).Within(0.0001));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が帯域数の増えた PeakHoldBarRenderData を平滑化する場合は追加バーを最新値のまま採用することを確認します。
        /// ■入力
        /// ・1. Smoothing = 0.5 の AudioVisualizerControl
        /// ・2. 2 本の後に 4 本を返す PeakHoldBarRenderData
        /// ■確認内容
        /// ・1. 既存バーは平滑化される
        /// ・2. 追加バーとピーク保持線は最新値のまま反映される
        /// </summary>
        [Test]
        public void Given_PeakHoldRenderData_When_BarCountIncreasesDuringSmoothing_Then_NewBarsUseLatestValues()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var effect = new SequencePeakHoldEffect(
                new PeakHoldBarRenderData(
                    "peak-hold-bars",
                    DateTimeOffset.UtcNow,
                    new[]
                    {
                        new BarRenderItem(0.1, 0.2, 1.0, 1.0),
                        new BarRenderItem(0.4, 0.2, 0.0, 0.0),
                    },
                    new[]
                    {
                        new PeakMarkerItem(0.1, 0.2, 1.0),
                        new PeakMarkerItem(0.4, 0.2, 0.2),
                    }),
                new PeakHoldBarRenderData(
                    "peak-hold-bars",
                    DateTimeOffset.UtcNow.AddMilliseconds(16),
                    new[]
                    {
                        new BarRenderItem(0.05, 0.15, 0.0, 0.0),
                        new BarRenderItem(0.30, 0.15, 1.0, 1.0),
                        new BarRenderItem(0.55, 0.15, 0.5, 0.5),
                        new BarRenderItem(0.80, 0.15, 0.25, 0.25),
                    },
                    new[]
                    {
                        new PeakMarkerItem(0.05, 0.15, 0.9),
                        new PeakMarkerItem(0.30, 0.15, 0.8),
                        new PeakMarkerItem(0.55, 0.15, 0.7),
                        new PeakMarkerItem(0.80, 0.15, 0.6),
                    }));
            var control = new TestableAudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer(), new PolylineRenderer())
            {
                IsActive = true,
                Effect = effect,
                Smoothing = 0.5,
            };
            var frame = new VisualizerFrame(new[] { 0.0 }, new[] { 0.0 }, 0.0, DateTimeOffset.UtcNow);

            // 実行
            provider.RaiseFrame(frame);
            provider.RaiseFrame(frame);

            // 検証
            var renderData = (PeakHoldBarRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bars, Has.Count.EqualTo(4));
                Assert.That(renderData.Bars[0].Height, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bars[1].Height, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bars[2].Height, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bars[3].Height, Is.EqualTo(0.25).Within(0.0001));
                Assert.That(renderData.PeakMarkers[2].Height, Is.EqualTo(0.7).Within(0.0001));
                Assert.That(renderData.PeakMarkers[3].Height, Is.EqualTo(0.6).Within(0.0001));
            });
        }

        /// <summary>
        /// PeakHoldBarEffect が空スペクトラム入力を空の描画データとして扱うことを確認します。
        /// ■入力
        /// ・1. 直前にピーク保持状態を持つ PeakHoldBarEffect
        /// ・2. spectrumValues が空の VisualizerFrame
        /// ■確認内容
        /// ・1. Bars と PeakMarkers が空になる
        /// ・2. 以前のピーク保持状態がクリアされる
        /// </summary>
        [Test]
        public void Given_EmptySpectrumAfterPeakHold_When_BuildRenderData_Then_EmptyRenderDataIsReturned()
        {
            // 準備
            var effect = new PeakHoldBarEffect();
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 4, SpectrumProfile.Balanced);
            var filledFrame = new VisualizerFrame(new[] { 1.0, 0.8, 0.6, 0.4 }, new[] { 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var emptyFrame = new VisualizerFrame(Array.Empty<double>(), new[] { 0.0 }, 0.0, DateTimeOffset.UtcNow.AddMilliseconds(16));

            // 実行
            effect.BuildRenderData(filledFrame, context);
            var result = (PeakHoldBarRenderData)effect.BuildRenderData(emptyFrame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Bars, Is.Empty);
                Assert.That(result.PeakMarkers, Is.Empty);
            });
        }

        /// <summary>
        /// PeakHoldBarEffect が Reset 呼び出しで保持状態を破棄することを確認します。
        /// ■入力
        /// ・1. 高レベルフレームの後に Reset を呼び出す
        /// ・2. 低レベルフレームを渡す
        /// ■確認内容
        /// ・1. ピーク保持線が過去の高レベルを引き継がない
        /// </summary>
        [Test]
        public void Given_ResetAfterHighPeak_When_BuildRenderDataAgain_Then_PreviousPeakIsCleared()
        {
            // 準備
            var effect = new PeakHoldBarEffect();
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 4, SpectrumProfile.Balanced);
            var highFrame = new VisualizerFrame(new[] { 1.0, 1.0, 1.0, 1.0 }, new[] { 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var lowFrame = new VisualizerFrame(new[] { 0.2, 0.2, 0.2, 0.2 }, new[] { 0.0 }, 0.2, DateTimeOffset.UtcNow.AddMilliseconds(16));

            // 実行
            effect.BuildRenderData(highFrame, context);
            effect.Reset();
            var resetResult = (PeakHoldBarRenderData)effect.BuildRenderData(lowFrame, context);

            // 検証
            Assert.That(resetResult.PeakMarkers[0].Height, Is.EqualTo(resetResult.Bars[0].Height).Within(0.0001));
        }

        /// <summary>
        /// AudioVisualizerControl が停止時に PeakHoldBarEffect の保持状態をリセットすることを確認します。
        /// ■入力
        /// ・1. PeakHoldBarEffect を指定した AudioVisualizerControl
        /// ・2. 高レベルフレーム後に停止し、再開して低レベルフレームを渡す
        /// ■確認内容
        /// ・1. 再開後のピーク保持線が過去の高レベルを引き継がない
        /// ・2. 描画実行で例外が発生しない
        /// </summary>
        [Test]
        public void Given_ControlStops_When_UsingPeakHoldEffect_Then_PeakStateIsReset()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var effect = new PeakHoldBarEffect();
            var control = new TestableAudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer(), new PolylineRenderer())
            {
                IsActive = true,
                Effect = effect,
            };
            var highFrame = new VisualizerFrame(new[] { 1.0, 1.0, 1.0, 1.0 }, new[] { 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var lowFrame = new VisualizerFrame(new[] { 0.2, 0.2, 0.2, 0.2 }, new[] { 0.0 }, 0.2, DateTimeOffset.UtcNow.AddMilliseconds(16));

            // 実行
            provider.RaiseFrame(highFrame);
            control.IsActive = false;
            control.IsActive = true;
            provider.RaiseFrame(lowFrame);

            // 検証
            var renderData = (PeakHoldBarRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.PeakMarkers[0].Height, Is.EqualTo(renderData.Bars[0].Height).Within(0.0001));
                Assert.That(
                    () =>
                    {
                        var drawingVisual = new DrawingVisual();
                        using var drawingContext = drawingVisual.RenderOpen();
                        control.InvokeOnRender(drawingContext);
                    },
                    Throws.Nothing);
            });
        }

        /// <summary>
        /// BarSpectrumRenderer が PeakHoldBarRenderData のピーク保持線矩形を生成できることを確認します。
        /// ■入力
        /// ・1. 1 本のバーとピーク保持線を含む PeakHoldBarRenderData
        /// ・2. renderSize = 100 x 50
        /// ■確認内容
        /// ・1. ピーク保持線矩形が 1 件生成される
        /// ・2. 矩形が描画領域内に収まる
        /// </summary>
        [Test]
        public void Given_PeakHoldRenderData_When_CreatePeakMarkerRectangles_Then_RectanglesAreGenerated()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var renderData = new PeakHoldBarRenderData(
                "peak-hold-bars",
                DateTimeOffset.UtcNow,
                new[] { new BarRenderItem(0.1, 0.2, 0.5, 0.5) },
                new[] { new PeakMarkerItem(0.1, 0.2, 0.7) });

            // 実行
            var rectangles = renderer.CreatePeakMarkerRectangles(renderData, new System.Windows.Size(100, 50));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(rectangles, Has.Count.EqualTo(1));
                Assert.That(rectangles[0].X, Is.InRange(0.0, 100.0));
                Assert.That(rectangles[0].Y, Is.InRange(0.0, 50.0));
                Assert.That(rectangles[0].Height, Is.GreaterThan(0.0));
            });
        }

        /// <summary>
        /// BarSpectrumRenderer が PeakHoldBarRenderData を描画できることを確認します。
        /// ■入力
        /// ・1. バー本体とピーク保持線を含む PeakHoldBarRenderData
        /// ・2. renderSize = 120 x 60
        /// ■確認内容
        /// ・1. Render 実行で例外が発生しない
        /// ・2. ピーク保持線経路が描画処理へ到達する
        /// </summary>
        [Test]
        public void Given_PeakHoldRenderData_When_Render_Then_BarsAndPeakMarkersAreDrawnWithoutException()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var renderData = new PeakHoldBarRenderData(
                "peak-hold-bars",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BarRenderItem(0.1, 0.2, 0.5, 0.5),
                    new BarRenderItem(0.4, 0.2, 0.7, 0.7),
                },
                new[]
                {
                    new PeakMarkerItem(0.1, 0.2, 0.6),
                    new PeakMarkerItem(0.4, 0.2, 0.8),
                });

            // 実行
            TestDelegate act = () =>
            {
                var drawingVisual = new DrawingVisual();
                using var drawingContext = drawingVisual.RenderOpen();
                renderer.Render(drawingContext, renderData, new System.Windows.Size(120, 60), Brushes.DeepSkyBlue);
            };

            // 検証
            Assert.That(act, Throws.Nothing);
        }

        /// <summary>
        /// BarSpectrumRenderer が極小幅のピーク保持線でも最小表示幅を確保することを確認します。
        /// ■入力
        /// ・1. 幅が 1px 未満になる PeakHoldBarRenderData
        /// ・2. renderSize = 10 x 20
        /// ■確認内容
        /// ・1. 生成されたピーク保持線矩形の幅が 1px になる
        /// </summary>
        [Test]
        public void Given_TinyPeakMarkerWidth_When_CreatePeakMarkerRectangles_Then_MinimumVisibleWidthIsApplied()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var renderData = new PeakHoldBarRenderData(
                "peak-hold-bars",
                DateTimeOffset.UtcNow,
                new[] { new BarRenderItem(0.0, 0.05, 0.5, 0.5) },
                new[] { new PeakMarkerItem(0.0, 0.05, 0.7) });

            // 実行
            var rectangles = renderer.CreatePeakMarkerRectangles(renderData, new System.Windows.Size(10, 20));

            // 検証
            Assert.That(rectangles[0].Width, Is.EqualTo(1.0).Within(0.0001));
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

        /// <summary>
        /// 呼び出し回数ごとに返却値を切り替えるピーク保持バー用エフェクトです。
        /// </summary>
        private sealed class SequencePeakHoldEffect : IVisualizerEffect
        {
            #region フィールド

            /// <summary>
            /// 1 回目に返すレンダーデータです。
            /// </summary>
            private readonly PeakHoldBarRenderData m_FirstRenderData;

            /// <summary>
            /// 2 回目以降に返すレンダーデータです。
            /// </summary>
            private readonly PeakHoldBarRenderData m_SecondRenderData;

            /// <summary>
            /// BuildRenderData の呼び出し回数です。
            /// </summary>
            private int m_CallCount;

            #endregion

            #region プロパティ

            /// <summary>
            /// テスト用エフェクトのメタデータです。
            /// </summary>
            public EffectMetadata Metadata { get; } = new(
                "sequence-peak-hold",
                "Sequence Peak Hold",
                "1.0.0",
                "AudioVisualizer.Tests",
                "テスト用にバー本数を切り替える PeakHold エフェクトです。",
                new[] { InputSource.SystemOutput });

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// 返却する 2 種類のレンダーデータを指定して初期化します。
            /// </summary>
            /// <param name="firstRenderData">1 回目に返すレンダーデータです。</param>
            /// <param name="secondRenderData">2 回目以降に返すレンダーデータです。</param>
            public SequencePeakHoldEffect(PeakHoldBarRenderData firstRenderData, PeakHoldBarRenderData secondRenderData)
            {
                m_FirstRenderData = firstRenderData;
                m_SecondRenderData = secondRenderData;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 呼び出し回数に応じたレンダーデータを返します。
            /// </summary>
            /// <param name="frame">未使用の解析済みフレームです。</param>
            /// <param name="context">未使用のエフェクトコンテキストです。</param>
            /// <returns>切り替え対象のレンダーデータです。</returns>
            public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
            {
                m_CallCount++;
                return m_CallCount == 1 ? m_FirstRenderData : m_SecondRenderData;
            }

            #endregion
        }

        #endregion
    }
}
