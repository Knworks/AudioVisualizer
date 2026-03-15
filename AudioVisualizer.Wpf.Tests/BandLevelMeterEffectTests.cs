using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;
using AudioVisualizer.Wpf.Effects;
using AudioVisualizer.Wpf.Rendering;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// <see cref="BandLevelMeterEffect"/> の変換と描画統合を検証します。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class BandLevelMeterEffectTests
    {
        #region 公開メソッド

        /// <summary>
        /// BandLevelMeterEffect が固定 4 帯域のメーターデータを生成することを確認します。
        /// ■入力
        /// ・1. 8 件の spectrumValues を含む VisualizerFrame
        /// ・2. Sensitivity = 1.0 の VisualizerEffectContext
        /// ■確認内容
        /// ・1. 4 帯域の BandLevelMeterRenderData が生成される
        /// ・2. Low から High まで固定ラベルが設定される
        /// ・3. 各帯域レベルが割り当て範囲の平均値になる
        /// </summary>
        [Test]
        public void Given_SpectrumFrame_When_BuildRenderData_Then_FixedBandMeterDataIsGenerated()
        {
            // 準備
            var effect = new BandLevelMeterEffect();
            var frame = new VisualizerFrame(
                new[] { 0.2, 0.4, 0.6, 0.8, 0.3, 0.1, 0.5, 0.7 },
                new[] { 0.0 },
                0.8,
                DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 32, SpectrumProfile.Balanced);

            // 実行
            var renderData = (BandLevelMeterRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.EffectId, Is.EqualTo("band-level-meter"));
                Assert.That(renderData.Bands, Has.Count.EqualTo(4));
                Assert.That(renderData.Bands[0].Label, Is.EqualTo("Low"));
                Assert.That(renderData.Bands[1].Label, Is.EqualTo("LowMid"));
                Assert.That(renderData.Bands[2].Label, Is.EqualTo("HighMid"));
                Assert.That(renderData.Bands[3].Label, Is.EqualTo("High"));
                Assert.That(renderData.Bands[0].Level, Is.EqualTo(0.3).Within(0.0001));
                Assert.That(renderData.Bands[1].Level, Is.EqualTo(0.7).Within(0.0001));
                Assert.That(renderData.Bands[2].Level, Is.EqualTo(0.2).Within(0.0001));
                Assert.That(renderData.Bands[3].Level, Is.EqualTo(0.6).Within(0.0001));
            });
        }

        /// <summary>
        /// BandLevelMeterEffect が空スペクトラム入力でも固定帯域を維持したゼロレベル表示を返すことを確認します。
        /// ■入力
        /// ・1. spectrumValues が空の VisualizerFrame
        /// ・2. Sensitivity = 1.0 の VisualizerEffectContext
        /// ■確認内容
        /// ・1. 4 帯域の BandLevelMeterRenderData が生成される
        /// ・2. すべての帯域レベルが 0.0 になる
        /// </summary>
        [Test]
        public void Given_EmptySpectrum_When_BuildRenderData_Then_ZeroLevelBandsAreReturned()
        {
            // 準備
            var effect = new BandLevelMeterEffect();
            var frame = new VisualizerFrame(Array.Empty<double>(), new[] { 0.0 }, 0.0, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 32, SpectrumProfile.Balanced);

            // 実行
            var renderData = (BandLevelMeterRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bands, Has.Count.EqualTo(4));
                Assert.That(renderData.Bands.Select(static band => band.Level), Is.All.EqualTo(0.0));
            });
        }

        /// <summary>
        /// BandLevelMeterEffect が入力スペクトラム数より帯域数が多い場合でも各帯域に代表値を割り当てることを確認します。
        /// ■入力
        /// ・1. 2 件の spectrumValues を含む VisualizerFrame
        /// ・2. 固定 4 帯域の VisualizerEffectContext
        /// ■確認内容
        /// ・1. 値が不足する帯域でも直近の代表値で補完される
        /// </summary>
        [Test]
        public void Given_ShortSpectrum_When_BuildRenderData_Then_EachBandUsesRepresentativeValue()
        {
            // 準備
            var effect = new BandLevelMeterEffect();
            var frame = new VisualizerFrame(new[] { 0.2, 0.8 }, new[] { 0.0 }, 0.8, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.0, 32, SpectrumProfile.Balanced);

            // 実行
            var renderData = (BandLevelMeterRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bands[0].Level, Is.EqualTo(0.2).Within(0.0001));
                Assert.That(renderData.Bands[1].Level, Is.EqualTo(0.2).Within(0.0001));
                Assert.That(renderData.Bands[2].Level, Is.EqualTo(0.8).Within(0.0001));
                Assert.That(renderData.Bands[3].Level, Is.EqualTo(0.8).Within(0.0001));
            });
        }

        /// <summary>
        /// BandLevelMeterRenderer が帯域レベルに応じた矩形へ変換できることを確認します。
        /// ■入力
        /// ・1. 2 帯域の BandLevelMeterRenderData
        /// ・2. renderSize = 100 x 60
        /// ■確認内容
        /// ・1. 矩形が 2 件生成される
        /// ・2. レベルに応じた高さで描画領域内へ収まる
        /// </summary>
        [Test]
        public void Given_BandLevelMeterRenderData_When_CreateMeterRectangles_Then_RectanglesAreScaled()
        {
            // 準備
            var renderer = new BandLevelMeterRenderer();
            var renderData = new BandLevelMeterRenderData(
                "band-level-meter",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BandLevelMeterItem(0.05, 0.2, 0.25, "Low"),
                    new BandLevelMeterItem(0.35, 0.2, 0.75, "High"),
                });

            // 実行
            var rectangles = renderer.CreateMeterRectangles(renderData, new Size(100, 60));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(rectangles, Has.Count.EqualTo(2));
                Assert.That(rectangles[0].Width, Is.EqualTo(20.0).Within(0.0001));
                Assert.That(rectangles[0].Height, Is.EqualTo(10.5).Within(0.0001));
                Assert.That(rectangles[1].Height, Is.EqualTo(31.5).Within(0.0001));
                Assert.That(rectangles[1].Y, Is.InRange(0.0, 42.0));
            });
        }

        /// <summary>
        /// BandLevelMeterRenderer が極小サイズでも最小表示幅と高さを確保することを確認します。
        /// ■入力
        /// ・1. 幅 1px 未満、高さ 1px 未満となる BandLevelMeterRenderData
        /// ・2. renderSize = 10 x 19
        /// ■確認内容
        /// ・1. 矩形の幅と高さが 1px に補正される
        /// </summary>
        [Test]
        public void Given_TinyMeterSize_When_CreateMeterRectangles_Then_MinimumVisibleSizeIsApplied()
        {
            // 準備
            var renderer = new BandLevelMeterRenderer();
            var renderData = new BandLevelMeterRenderData(
                "band-level-meter",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BandLevelMeterItem(0.0, 0.05, 0.09, "Low"),
                });

            // 実行
            var rectangles = renderer.CreateMeterRectangles(renderData, new Size(10, 19));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(rectangles[0].Width, Is.EqualTo(1.0).Within(0.0001));
                Assert.That(rectangles[0].Height, Is.EqualTo(1.0).Within(0.0001));
            });
        }

        /// <summary>
        /// BandLevelMeterRenderer が空描画領域では矩形を生成しないことを確認します。
        /// ■入力
        /// ・1. 1 帯域の BandLevelMeterRenderData
        /// ・2. renderSize = 0 x 0
        /// ■確認内容
        /// ・1. 矩形一覧が空になる
        /// </summary>
        [Test]
        public void Given_EmptyRenderSize_When_CreateMeterRectangles_Then_EmptyRectanglesAreReturned()
        {
            // 準備
            var renderer = new BandLevelMeterRenderer();
            var renderData = new BandLevelMeterRenderData(
                "band-level-meter",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BandLevelMeterItem(0.1, 0.2, 0.5, "Low"),
                });

            // 実行
            var rectangles = renderer.CreateMeterRectangles(renderData, new Size(0, 0));

            // 検証
            Assert.That(rectangles, Is.Empty);
        }

        /// <summary>
        /// BandLevelMeterRenderer が固定帯域メーターデータを描画できることを確認します。
        /// ■入力
        /// ・1. 2 帯域の BandLevelMeterRenderData
        /// ・2. renderSize = 120 x 80
        /// ■確認内容
        /// ・1. Render 実行で例外が発生しない
        /// ・2. メーター本体とラベル描画経路へ到達する
        /// </summary>
        [Test]
        public void Given_BandLevelMeterRenderData_When_Render_Then_MetersAndLabelsAreDrawnWithoutException()
        {
            // 準備
            var renderer = new BandLevelMeterRenderer();
            var renderData = new BandLevelMeterRenderData(
                "band-level-meter",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BandLevelMeterItem(0.05, 0.2, 0.25, "Low"),
                    new BandLevelMeterItem(0.35, 0.2, 0.75, "High"),
                });

            // 実行
            TestDelegate act = () =>
            {
                var drawingVisual = new DrawingVisual();
                using var drawingContext = drawingVisual.RenderOpen();
                renderer.Render(drawingContext, renderData, new Size(120, 80), Brushes.DeepSkyBlue);
            };

            // 検証
            Assert.That(act, Throws.Nothing);
        }

        /// <summary>
        /// BandLevelMeterRenderer が非対応レンダーデータや空描画領域でも例外を出さないことを確認します。
        /// ■入力
        /// ・1. 非対応レンダーデータ
        /// ・2. 固定帯域メーターデータと renderSize = 0 x 0
        /// ■確認内容
        /// ・1. どちらの Render 実行でも例外が発生しない
        /// </summary>
        [Test]
        public void Given_UnsupportedOrEmptyRenderContext_When_Render_Then_NoExceptionIsThrown()
        {
            // 準備
            var renderer = new BandLevelMeterRenderer();
            var unsupportedRenderData = new BarSpectrumRenderData(
                "bars",
                DateTimeOffset.UtcNow,
                new[] { new BarRenderItem(0.0, 0.5, 0.5, 0.5) });
            var supportedRenderData = new BandLevelMeterRenderData(
                "band-level-meter",
                DateTimeOffset.UtcNow,
                new[] { new BandLevelMeterItem(0.1, 0.2, 0.5, "Low") });

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(
                    () =>
                    {
                        var drawingVisual = new DrawingVisual();
                        using var drawingContext = drawingVisual.RenderOpen();
                        renderer.Render(drawingContext, unsupportedRenderData, new Size(120, 80), Brushes.DeepSkyBlue);
                    },
                    Throws.Nothing);
                Assert.That(
                    () =>
                    {
                        var drawingVisual = new DrawingVisual();
                        using var drawingContext = drawingVisual.RenderOpen();
                        renderer.Render(drawingContext, supportedRenderData, new Size(0, 0), Brushes.DeepSkyBlue);
                    },
                    Throws.Nothing);
            });
        }

        /// <summary>
        /// AudioVisualizerControl が BandLevelMeterEffect を指定した場合に固定帯域メーターデータを保持し描画できることを確認します。
        /// ■入力
        /// ・1. BandLevelMeterEffect と Smoothing = 0.5 を指定した AudioVisualizerControl
        /// ・2. 2 フレームの spectrumValues
        /// ■確認内容
        /// ・1. CurrentRenderData が BandLevelMeterRenderData になる
        /// ・2. 2 フレーム目で帯域レベルが平滑化される
        /// ・3. OnRender 実行で例外が発生しない
        /// </summary>
        [Test]
        public void Given_ControlWithBandLevelMeterEffect_When_FrameProduced_Then_BandMeterRenderDataIsSmoothedAndRendered()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new TestableAudioVisualizerControl(
                provider,
                new SpectrumBarEffect(),
                new BarSpectrumRenderer(),
                new PolylineRenderer(),
                new BandLevelMeterRenderer())
            {
                IsActive = true,
                Effect = new BandLevelMeterEffect(),
                Smoothing = 0.5,
                Width = 120,
                Height = 80,
            };
            var firstFrame = new VisualizerFrame(
                new[] { 1.0, 1.0, 0.2, 0.2, 0.1, 0.1, 0.0, 0.0 },
                new[] { 0.0 },
                1.0,
                DateTimeOffset.UtcNow);
            var secondFrame = new VisualizerFrame(
                new[] { 0.0, 0.0, 0.8, 0.8, 0.4, 0.4, 0.2, 0.2 },
                new[] { 0.0 },
                0.8,
                DateTimeOffset.UtcNow.AddMilliseconds(16));

            // 実行
            provider.RaiseFrame(firstFrame);
            provider.RaiseFrame(secondFrame);

            // 検証
            var renderData = (BandLevelMeterRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bands, Has.Count.EqualTo(4));
                Assert.That(renderData.Bands[0].Level, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bands[1].Level, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bands[2].Level, Is.EqualTo(0.25).Within(0.0001));
                Assert.That(renderData.Bands[3].Level, Is.EqualTo(0.1).Within(0.0001));
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
        /// AudioVisualizerControl が帯域数の増えた BandLevelMeterRenderData を平滑化する場合は追加帯域を最新値のまま採用することを確認します。
        /// ■入力
        /// ・1. Smoothing = 0.5 の AudioVisualizerControl
        /// ・2. 2 帯域の後に 4 帯域を返すエフェクト
        /// ■確認内容
        /// ・1. 既存帯域は平滑化される
        /// ・2. 追加帯域は最新値のまま反映される
        /// </summary>
        [Test]
        public void Given_BandMeterRenderData_When_BandCountIncreasesDuringSmoothing_Then_NewBandsUseLatestValues()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var effect = new SequenceBandLevelMeterEffect(
                new BandLevelMeterRenderData(
                    "band-level-meter",
                    DateTimeOffset.UtcNow,
                    new[]
                    {
                        new BandLevelMeterItem(0.1, 0.2, 1.0, "Low"),
                        new BandLevelMeterItem(0.4, 0.2, 0.0, "High"),
                    }),
                new BandLevelMeterRenderData(
                    "band-level-meter",
                    DateTimeOffset.UtcNow.AddMilliseconds(16),
                    new[]
                    {
                        new BandLevelMeterItem(0.05, 0.15, 0.0, "Low"),
                        new BandLevelMeterItem(0.30, 0.15, 1.0, "LowMid"),
                        new BandLevelMeterItem(0.55, 0.15, 0.5, "HighMid"),
                        new BandLevelMeterItem(0.80, 0.15, 0.25, "High"),
                    }));
            var control = new TestableAudioVisualizerControl(
                provider,
                new SpectrumBarEffect(),
                new BarSpectrumRenderer(),
                new PolylineRenderer(),
                new BandLevelMeterRenderer())
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
            var renderData = (BandLevelMeterRenderData)control.CurrentRenderData!;
            Assert.Multiple(() =>
            {
                Assert.That(renderData.Bands, Has.Count.EqualTo(4));
                Assert.That(renderData.Bands[0].Level, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bands[1].Level, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bands[2].Level, Is.EqualTo(0.5).Within(0.0001));
                Assert.That(renderData.Bands[3].Level, Is.EqualTo(0.25).Within(0.0001));
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
            /// キャプチャを開始します。
            /// </summary>
            /// <param name="settings">キャプチャ設定です。</param>
            /// <returns>開始結果です。</returns>
            public AudioInputStartResult Start(VisualizerSettings settings)
            {
                IsCapturing = true;
                return AudioInputStartResult.Success();
            }

            /// <summary>
            /// キャプチャを停止します。
            /// </summary>
            public void Stop()
            {
                IsCapturing = false;
            }

            /// <summary>
            /// テスト用フレームを通知します。
            /// </summary>
            /// <param name="frame">通知する解析済みフレームです。</param>
            public void RaiseFrame(VisualizerFrame frame)
            {
                m_FrameProduced?.Invoke(this, new VisualizerFrameEventArgs(frame));
            }

            #endregion
        }

        /// <summary>
        /// テストから描画を呼び出すための派生コントロールです。
        /// </summary>
        private sealed class TestableAudioVisualizerControl : AudioVisualizerControl
        {
            #region 構築 / 消滅

            /// <summary>
            /// テスト用の依存を注入して初期化します。
            /// </summary>
            /// <param name="audioInputProvider">テスト用入力プロバイダーです。</param>
            /// <param name="defaultEffect">既定エフェクトです。</param>
            /// <param name="renderer">バー描画レンダラーです。</param>
            /// <param name="polylineRenderer">折れ線描画レンダラーです。</param>
            /// <param name="bandLevelMeterRenderer">固定帯域メーター描画レンダラーです。</param>
            public TestableAudioVisualizerControl(
                IAudioInputProvider audioInputProvider,
                IVisualizerEffect defaultEffect,
                BarSpectrumRenderer renderer,
                PolylineRenderer polylineRenderer,
                BandLevelMeterRenderer bandLevelMeterRenderer)
                : base(audioInputProvider, defaultEffect, renderer, polylineRenderer, bandLevelMeterRenderer)
            {
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 描画処理をテストから実行します。
            /// </summary>
            /// <param name="drawingContext">描画先コンテキストです。</param>
            public void InvokeOnRender(DrawingContext drawingContext)
            {
                OnRender(drawingContext);
            }

            #endregion
        }

        /// <summary>
        /// 呼び出し回数ごとに返却値を切り替える帯域メーター用エフェクトです。
        /// </summary>
        private sealed class SequenceBandLevelMeterEffect : IVisualizerEffect
        {
            #region フィールド

            /// <summary>
            /// 1 回目に返すレンダーデータです。
            /// </summary>
            private readonly BandLevelMeterRenderData m_FirstRenderData;

            /// <summary>
            /// 2 回目以降に返すレンダーデータです。
            /// </summary>
            private readonly BandLevelMeterRenderData m_SecondRenderData;

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
                "sequence-band-meter",
                "Sequence Band Meter",
                "1.0.0",
                "AudioVisualizer.Tests",
                "テスト用に帯域数を切り替えるエフェクトです。",
                new[] { InputSource.SystemOutput });

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// 返却する 2 種類のレンダーデータを指定して初期化します。
            /// </summary>
            /// <param name="firstRenderData">1 回目に返すレンダーデータです。</param>
            /// <param name="secondRenderData">2 回目以降に返すレンダーデータです。</param>
            public SequenceBandLevelMeterEffect(BandLevelMeterRenderData firstRenderData, BandLevelMeterRenderData secondRenderData)
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
