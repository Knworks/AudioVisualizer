using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;
using AudioVisualizer.Wpf.Effects;
using AudioVisualizer.Wpf.Rendering;

namespace AudioVisualizer.Wpf.Tests
{
    /// <summary>
    /// AudioVisualizerControl、既定エフェクト、レンダラーの最小動作を検証します。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class AudioVisualizerControlTests
    {
        #region 公開メソッド

        /// <summary>
        /// AudioVisualizerControl の公開コンストラクタでインスタンス生成できることを確認します。
        /// ■入力
        /// ・1. 引数なしコンストラクタ
        /// ■確認内容
        /// ・1. AudioVisualizerControl を生成できる
        /// </summary>
        [Test]
        public void Given_PublicConstructor_When_CreatingControl_Then_InstanceIsCreated()
        {
            // 準備と実行
            var control = new AudioVisualizerControl();

            // 検証
            Assert.That(control, Is.Not.Null);
        }

        /// <summary>
        /// AudioVisualizerControl が IsActive=false の間は入力開始しないことを確認します。
        /// ■入力
        /// ・1. IsActive = false の AudioVisualizerControl
        /// ・2. InputSource と UseDefaultDevice の変更
        /// ■確認内容
        /// ・1. IAudioInputProvider.Start が呼ばれない
        /// </summary>
        [Test]
        public void Given_InactiveControl_When_ConnectionSettingsChange_Then_InputDoesNotStart()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer());

            // 実行
            control.InputSource = InputSource.Microphone;
            control.UseDefaultDevice = false;

            // 検証
            Assert.That(provider.StartCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// AudioVisualizerControl が有効化時に入力開始し、無効化時に停止することを確認します。
        /// ■入力
        /// ・1. IsActive を false から true、true から false に変更する
        /// ■確認内容
        /// ・1. Start が 1 回呼ばれる
        /// ・2. Stop が 1 回以上呼ばれる
        /// </summary>
        [Test]
        public void Given_Control_When_IsActiveChanges_Then_InputStartsAndStops()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer());

            // 実行
            control.IsActive = true;
            control.IsActive = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(provider.StartCallCount, Is.EqualTo(1));
                Assert.That(provider.StopCallCount, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が有効中の接続設定変更で再接続することを確認します。
        /// ■入力
        /// ・1. IsActive = true の AudioVisualizerControl
        /// ・2. InputSource の変更
        /// ■確認内容
        /// ・1. Stop が呼ばれる
        /// ・2. Start が再度呼ばれる
        /// </summary>
        [Test]
        public void Given_ActiveControl_When_InputSourceChanges_Then_InputRestarts()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
            };

            // 実行
            control.InputSource = InputSource.Microphone;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(provider.StartCallCount, Is.EqualTo(2));
                Assert.That(provider.StopCallCount, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が UseDefaultDevice=false の場合に開始を抑止することを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = false の AudioVisualizerControl
        /// ・2. IsActive = true への変更
        /// ■確認内容
        /// ・1. Start が呼ばれない
        /// </summary>
        [Test]
        public void Given_NonDefaultDeviceMode_When_Activating_Then_InputDoesNotStart()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                UseDefaultDevice = false,
            };

            // 実行
            control.IsActive = true;

            // 検証
            Assert.That(provider.StartCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// AudioVisualizerControl が既にキャプチャ中の入力プロバイダーへ重複開始しないことを確認します。
        /// ■入力
        /// ・1. IsCapturing = true の偽入力プロバイダー
        /// ・2. IsActive = true への変更
        /// ■確認内容
        /// ・1. Start が追加で呼ばれない
        /// </summary>
        [Test]
        public void Given_AlreadyCapturingProvider_When_Activating_Then_StartIsSkipped()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            provider.SetCapturing(true);
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer());

            // 実行
            control.IsActive = true;

            // 検証
            Assert.That(provider.StartCallCount, Is.EqualTo(0));
        }

        /// <summary>
        /// AudioVisualizerControl がフレーム未保持時のエフェクト変更で空レンダーデータのまま維持することを確認します。
        /// ■入力
        /// ・1. フレーム未受信の AudioVisualizerControl
        /// ・2. Effect の変更
        /// ■確認内容
        /// ・1. CurrentRenderData が null のままである
        /// </summary>
        [Test]
        public void Given_NoFrame_When_EffectChanges_Then_CurrentRenderDataRemainsNull()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer());

            // 実行
            control.Effect = new SpectrumBarEffect();

            // 検証
            Assert.That(control.CurrentRenderData, Is.Null);
        }

        /// <summary>
        /// AudioVisualizerControl が Effect 未指定時に既定バーエフェクトでレンダーデータを生成することを確認します。
        /// ■入力
        /// ・1. Effect = null の AudioVisualizerControl
        /// ・2. SpectrumValues を含む VisualizerFrame
        /// ■確認内容
        /// ・1. 現在レンダーデータが BarSpectrumRenderData になる
        /// ・2. バー本数が入力スペクトラム数と一致する
        /// </summary>
        [Test]
        public void Given_NullEffect_When_FrameProduced_Then_DefaultBarRenderDataIsUsed()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
                Effect = null,
            };
            var frame = new VisualizerFrame(new[] { 0.2, 0.5, 0.8 }, new[] { 0.1, 0.2, 0.3 }, 0.8, DateTimeOffset.UtcNow);

            // 実行
            provider.RaiseFrame(frame);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(control.CurrentRenderData, Is.TypeOf<BarSpectrumRenderData>());
                Assert.That(((BarSpectrumRenderData)control.CurrentRenderData!).Bars.Count, Is.EqualTo(3));
            });
        }

        /// <summary>
        /// AudioVisualizerControl が別スレッドからのフレーム通知を Dispatcher 経由で反映できることを確認します。
        /// ■入力
        /// ・1. 別スレッドから RaiseFrame する偽入力プロバイダー
        /// ■確認内容
        /// ・1. CurrentRenderData が非 null になる
        /// </summary>
        [Test]
        public void Given_FrameRaisedFromWorkerThread_When_FrameProduced_Then_RenderDataIsUpdatedViaDispatcher()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new AudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                IsActive = true,
            };
            var frame = new VisualizerFrame(new[] { 0.3, 0.6 }, new[] { 0.0, 0.0 }, 0.6, DateTimeOffset.UtcNow);

            // 実行
            Task.Run(() => provider.RaiseFrame(frame)).Wait();
            PumpDispatcher();

            // 検証
            Assert.That(control.CurrentRenderData, Is.Not.Null);
        }

        /// <summary>
        /// SpectrumBarEffect がスペクトラム値から正規化バー情報を生成することを確認します。
        /// ■入力
        /// ・1. 3 本のスペクトラム値を含む VisualizerFrame
        /// ・2. barCount = 3 の VisualizerEffectContext
        /// ■確認内容
        /// ・1. 3 本の BarRenderItem が生成される
        /// ・2. 各 Height が 0.0 から 1.0 の範囲に収まる
        /// </summary>
        [Test]
        public void Given_SpectrumFrame_When_BuildRenderData_Then_BarItemsAreGenerated()
        {
            // 準備
            var effect = new SpectrumBarEffect();
            var frame = new VisualizerFrame(new[] { 0.2, 0.5, 1.4 }, new[] { 0.0, 0.0, 0.0 }, 1.0, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.5, 3);

            // 実行
            var result = (BarSpectrumRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Bars.Count, Is.EqualTo(3));
                Assert.That(result.Bars[0].Height, Is.EqualTo(0.2).Within(1e-10));
                Assert.That(result.Bars[2].Height, Is.EqualTo(1.0).Within(1e-10));
            });
        }

        /// <summary>
        /// SpectrumBarEffect が空スペクトラムでも空のバー一覧を返すことを確認します。
        /// ■入力
        /// ・1. SpectrumValues が空の VisualizerFrame
        /// ■確認内容
        /// ・1. Bars が空になる
        /// </summary>
        [Test]
        public void Given_EmptySpectrum_When_BuildRenderData_Then_EmptyBarsAreReturned()
        {
            // 準備
            var effect = new SpectrumBarEffect();
            var frame = new VisualizerFrame(Array.Empty<double>(), Array.Empty<double>(), 0.0, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.5, 3);

            // 実行
            var result = (BarSpectrumRenderData)effect.BuildRenderData(frame, context);

            // 検証
            Assert.That(result.Bars, Is.Empty);
        }

        /// <summary>
        /// BarSpectrumRenderer が正規化バー情報を実座標へ変換できることを確認します。
        /// ■入力
        /// ・1. 2 本のバーを含む BarSpectrumRenderData
        /// ・2. 幅 200、高さ 100 の描画領域
        /// ■確認内容
        /// ・1. 2 件の Rect が生成される
        /// ・2. 高さと位置が期待どおりに変換される
        /// </summary>
        [Test]
        public void Given_BarSpectrumRenderData_When_CreateBarRectangles_Then_RectanglesAreScaled()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var renderData = new BarSpectrumRenderData(
                "bars",
                DateTimeOffset.UtcNow,
                new[]
                {
                    new BarRenderItem(0.1, 0.2, 0.5, 0.5),
                    new BarRenderItem(0.4, 0.1, 1.0, 1.0),
                });

            // 実行
            var result = renderer.CreateBarRectangles(renderData, new Size(200, 100));

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That(result[0], Is.EqualTo(new Rect(20, 50, 40, 50)));
                Assert.That(result[1], Is.EqualTo(new Rect(80, 0, 20, 100)));
            });
        }

        /// <summary>
        /// BarSpectrumRenderer が不正な描画サイズでは矩形を生成しないことを確認します。
        /// ■入力
        /// ・1. 幅 0、高さ 0 の描画領域
        /// ■確認内容
        /// ・1. 返却矩形一覧が空になる
        /// </summary>
        [Test]
        public void Given_EmptyRenderSize_When_CreateBarRectangles_Then_NoRectangleIsGenerated()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var renderData = new BarSpectrumRenderData("bars", DateTimeOffset.UtcNow, new[] { new BarRenderItem(0.0, 0.5, 0.5, 0.5) });

            // 実行
            var result = renderer.CreateBarRectangles(renderData, new Size(0, 0));

            // 検証
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// BarSpectrumRenderer が非対応レンダーデータを無視することを確認します。
        /// ■入力
        /// ・1. 非対応の VisualizerRenderData
        /// ■確認内容
        /// ・1. 例外を送出しない
        /// </summary>
        [Test]
        public void Given_UnsupportedRenderData_When_Render_Then_NoExceptionIsThrown()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var visual = new DrawingVisual();

            // 実行と検証
            Assert.That(
                () =>
                {
                    using var drawingContext = visual.RenderOpen();
                    renderer.Render(drawingContext, new FakeRenderData("fake", DateTimeOffset.UtcNow), new Size(100, 50), Brushes.DeepSkyBlue);
                },
                Throws.Nothing);
        }

        /// <summary>
        /// BarSpectrumRenderer が対応レンダーデータを DrawingContext へ描画できることを確認します。
        /// ■入力
        /// ・1. BarSpectrumRenderData
        /// ・2. DrawingVisual の DrawingContext
        /// ■確認内容
        /// ・1. 例外を送出しない
        /// </summary>
        [Test]
        public void Given_BarSpectrumRenderData_When_Render_Then_RectanglesAreDrawnWithoutException()
        {
            // 準備
            var renderer = new BarSpectrumRenderer();
            var visual = new DrawingVisual();
            var renderData = new BarSpectrumRenderData("bars", DateTimeOffset.UtcNow, new[] { new BarRenderItem(0.1, 0.2, 0.5, 0.5) });

            // 実行と検証
            Assert.That(
                () =>
                {
                    using var drawingContext = visual.RenderOpen();
                    renderer.Render(drawingContext, renderData, new Size(100, 50), Brushes.DeepSkyBlue);
                },
                Throws.Nothing);
        }

        /// <summary>
        /// AudioVisualizerControl が OnRender 呼び出しで例外なく描画できることを確認します。
        /// ■入力
        /// ・1. BarSpectrumRenderData を保持した AudioVisualizerControl
        /// ・2. DrawingVisual の DrawingContext
        /// ■確認内容
        /// ・1. OnRender 相当の描画処理が例外なく完了する
        /// </summary>
        [Test]
        public void Given_ControlWithRenderData_When_Rendering_Then_NoExceptionIsThrown()
        {
            // 準備
            var provider = new FakeAudioInputProvider();
            var control = new TestableAudioVisualizerControl(provider, new SpectrumBarEffect(), new BarSpectrumRenderer())
            {
                Width = 200,
                Height = 100,
                IsActive = true,
            };
            provider.RaiseFrame(new VisualizerFrame(new[] { 0.4, 0.7 }, new[] { 0.0, 0.0 }, 0.7, DateTimeOffset.UtcNow));
            var visual = new DrawingVisual();

            // 実行と検証
            Assert.That(
                () =>
                {
                    using var drawingContext = visual.RenderOpen();
                    control.InvokeOnRender(drawingContext);
                },
                Throws.Nothing);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// Dispatcher キューに積まれた非同期処理を消化します。
        /// </summary>
        private static void PumpDispatcher()
        {
            var dispatcherFrame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new DispatcherOperationCallback(
                    state =>
                    {
                        ((DispatcherFrame)state!).Continue = false;
                        return null;
                    }),
                dispatcherFrame);
            Dispatcher.PushFrame(dispatcherFrame);
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// 入力開始回数とフレーム通知を制御できる偽の音声入力プロバイダーです。
        /// </summary>
        private sealed class FakeAudioInputProvider : IAudioInputProvider
        {
            #region プロパティ

            /// <summary>
            /// 入力開始が呼ばれた回数を取得します。
            /// </summary>
            public int StartCallCount { get; private set; }

            /// <summary>
            /// 入力停止が呼ばれた回数を取得します。
            /// </summary>
            public int StopCallCount { get; private set; }

            /// <summary>
            /// 現在音声入力が有効かどうかを示す値を取得します。
            /// </summary>
            public bool IsCapturing { get; private set; }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 音声入力開始要求を記録し、成功結果を返します。
            /// </summary>
            /// <param name="settings">入力開始に使用する設定です。</param>
            /// <returns>成功結果です。</returns>
            public AudioInputStartResult Start(VisualizerSettings settings)
            {
                StartCallCount++;
                IsCapturing = true;
                return AudioInputStartResult.Success();
            }

            /// <summary>
            /// 音声入力停止要求を記録します。
            /// </summary>
            public void Stop()
            {
                StopCallCount++;
                IsCapturing = false;
            }

            /// <summary>
            /// 現在のキャプチャ状態をテストから設定します。
            /// </summary>
            /// <param name="isCapturing">設定するキャプチャ状態です。</param>
            public void SetCapturing(bool isCapturing)
            {
                IsCapturing = isCapturing;
            }

            /// <summary>
            /// 指定した解析済みフレームの通知を発生させます。
            /// </summary>
            /// <param name="frame">通知対象の解析済みフレームです。</param>
            public void RaiseFrame(VisualizerFrame frame)
            {
                FrameProduced?.Invoke(this, new VisualizerFrameEventArgs(frame));
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// 新しい解析済みフレームが生成されたときに発生します。
            /// </summary>
            public event EventHandler<VisualizerFrameEventArgs>? FrameProduced;

            #endregion
        }

        /// <summary>
        /// 非対応レンダーデータ分岐の確認に使うダミー実装です。
        /// </summary>
        private sealed class FakeRenderData : VisualizerRenderData
        {
            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeRenderData"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="effectId">エフェクト識別子です。</param>
            /// <param name="timestamp">生成時刻です。</param>
            public FakeRenderData(string effectId, DateTimeOffset timestamp)
                : base(effectId, timestamp)
            {
            }

            #endregion
        }

        /// <summary>
        /// テストから OnRender を呼び出すための派生コントロールです。
        /// </summary>
        private sealed class TestableAudioVisualizerControl : AudioVisualizerControl
        {
            #region 構築 / 消滅

            /// <summary>
            /// <see cref="TestableAudioVisualizerControl"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="audioInputProvider">音声入力プロバイダーです。</param>
            /// <param name="defaultEffect">既定エフェクトです。</param>
            /// <param name="renderer">レンダラーです。</param>
            public TestableAudioVisualizerControl(IAudioInputProvider audioInputProvider, IVisualizerEffect defaultEffect, BarSpectrumRenderer renderer)
                : base(audioInputProvider, defaultEffect, renderer)
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
