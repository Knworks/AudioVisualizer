using System;
using AudioVisualizer.Core.Analysis;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// SystemOutputAudioInputProvider の状態遷移とフレーム通知を検証します。
    /// </summary>
    [TestFixture]
    public class SystemOutputAudioInputProviderTests
    {
        #region 公開メソッド

        /// <summary>
        /// SystemOutputAudioInputProvider が開始成功後にフレーム通知を行うことを確認します。
        /// ■入力
        /// ・1. SystemOutput の VisualizerSettings
        /// ・2. サンプル通知可能な FakeAudioCaptureSession
        /// ■確認内容
        /// ・1. Start が成功結果を返す
        /// ・2. FrameProduced が 1 回発火する
        /// ・3. Stop 後に IsCapturing が false になる
        /// </summary>
        [Test]
        public void Given_SystemOutputSettings_When_StartAndSamplesArrive_Then_FrameProducedIsRaised()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var analyzer = new AudioFrameAnalyzer();
            using var sut = new SystemOutputAudioInputProvider(analyzer, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
            VisualizerFrame? producedFrame = null;
            sut.FrameProduced += (_, eventArgs) => producedFrame = eventArgs.Frame;

            // 実行
            var startResult = sut.Start(settings);
            captureSession.RaiseSamples(new float[] { 0.1f, -0.4f, 0.7f, -0.2f }, 48000, 2);
            sut.Stop();

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(sut.IsCapturing, Is.False);
                Assert.That(producedFrame, Is.Not.Null);
                Assert.That(producedFrame!.SpectrumValues.Count, Is.EqualTo(settings.BarCount));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が開始失敗時に停止状態を維持することを確認します。
        /// ■入力
        /// ・1. CreateSystemOutputCapture で例外を送出する FakeAudioCaptureFactory
        /// ■確認内容
        /// ・1. Start が失敗結果を返す
        /// ・2. IsCapturing が false のままである
        /// ・3. エラーメッセージが返される
        /// </summary>
        [Test]
        public void Given_FactoryThrows_When_Start_Then_FailureResultKeepsStoppedState()
        {
            // 準備
            var captureFactory = new FakeAudioCaptureFactory(new InvalidOperationException("boom"));
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            // 実行
            var result = sut.Start(settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorMessage, Is.Not.Null.And.Not.Empty);
                Assert.That(sut.IsCapturing, Is.False);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が未対応入力元を拒否することを確認します。
        /// ■入力
        /// ・1. InputSource = Microphone の VisualizerSettings
        /// ■確認内容
        /// ・1. Start が失敗結果を返す
        /// ・2. キャプチャファクトリが呼び出されない
        /// </summary>
        [Test]
        public void Given_UnsupportedInputSource_When_Start_Then_FailureResultIsReturned()
        {
            // 準備
            var captureFactory = new FakeAudioCaptureFactory(new FakeAudioCaptureSession());
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), captureFactory);
            var settings = new VisualizerSettings(InputSource.Microphone, null, true, true, 1.0, 0.5, 8);

            // 実行
            var result = sut.Start(settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(sut.IsCapturing, Is.False);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(0));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が未開始状態の停止呼び出しを安全に処理することを確認します。
        /// ■入力
        /// ・1. Start を呼び出していない provider
        /// ■確認内容
        /// ・1. 例外を送出しない
        /// ・2. IsCapturing が false のままである
        /// </summary>
        [Test]
        public void Given_NotStartedProvider_When_Stop_Then_StateRemainsStopped()
        {
            // 準備
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), new FakeAudioCaptureFactory(new FakeAudioCaptureSession()));

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => sut.Stop(), Throws.Nothing);
                Assert.That(sut.IsCapturing, Is.False);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider の公開コンストラクタが利用可能であることを確認します。
        /// ■入力
        /// ・1. 引数なしコンストラクタ
        /// ・2. IAudioFrameAnalyzer 指定コンストラクタ
        /// ■確認内容
        /// ・1. どちらのインスタンスも生成できる
        /// ・2. Stop と Dispose を呼んでも例外を送出しない
        /// </summary>
        [Test]
        public void Given_PublicConstructors_When_CreatingProvider_Then_InstanceCanBeDisposedSafely()
        {
            // 準備
            using var defaultProvider = new SystemOutputAudioInputProvider();
            using var analyzerProvider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer());

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(defaultProvider, Is.Not.Null);
                Assert.That(analyzerProvider, Is.Not.Null);
                Assert.That(() => defaultProvider.Stop(), Throws.Nothing);
                Assert.That(() => analyzerProvider.Stop(), Throws.Nothing);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が設定未保持時のサンプル通知を無視することを確認します。
        /// ■入力
        /// ・1. 開始していない provider
        /// ・2. AudioSamplesCapturedEventArgs
        /// ■確認内容
        /// ・1. FrameProduced が発火しない
        /// ・2. 例外を送出しない
        /// </summary>
        [Test]
        public void Given_NoCurrentSettings_When_SamplesCaptured_Then_FrameIsNotProduced()
        {
            // 準備
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), new FakeAudioCaptureFactory(new FakeAudioCaptureSession()));
            var eventArgs = new AudioSamplesCapturedEventArgs(new[] { 0.1f, 0.2f }, 48000, 2, DateTimeOffset.UtcNow);
            var frameProduced = false;
            sut.FrameProduced += (_, _) => frameProduced = true;

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => InvokeOnSamplesCaptured(sut, eventArgs), Throws.Nothing);
                Assert.That(frameProduced, Is.False);
            });
        }

        /// <summary>
        /// AudioSampleBuffer と VisualizerFrameEventArgs の基本生成を確認します。
        /// ■入力
        /// ・1. 有効な PCM サンプル
        /// ・2. VisualizerFrame
        /// ■確認内容
        /// ・1. AudioSampleBuffer がサンプル情報を保持する
        /// ・2. VisualizerFrameEventArgs がフレームを保持する
        /// </summary>
        [Test]
        public void Given_ValidBufferAndFrame_When_CreatingSupportModels_Then_PropertiesAreAssigned()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行
            var buffer = new AudioSampleBuffer(new float[] { 0.1f, 0.2f }, 48000, 2, timestamp);
            var frame = new VisualizerFrame(new[] { 0.2 }, new[] { 0.1 }, 0.2, timestamp);
            var eventArgs = new VisualizerFrameEventArgs(frame);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(buffer.Samples.Count, Is.EqualTo(2));
                Assert.That(buffer.SampleRate, Is.EqualTo(48000));
                Assert.That(buffer.Channels, Is.EqualTo(2));
                Assert.That(eventArgs.Frame, Is.SameAs(frame));
            });
        }

        /// <summary>
        /// AudioSampleBuffer と VisualizerFrameEventArgs の入力制約を確認します。
        /// ■入力
        /// ・1. samples = null
        /// ・2. sampleRate = 0, channels = 0
        /// ・3. frame = null
        /// ■確認内容
        /// ・1. それぞれ適切な例外が送出される
        /// </summary>
        [Test]
        public void Given_InvalidSupportModelInput_When_CreatingSupportModels_Then_ThrowsArgumentException()
        {
            // 準備
            var timestamp = DateTimeOffset.UtcNow;

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => new AudioSampleBuffer(null!, 48000, 2, timestamp), Throws.TypeOf<ArgumentNullException>());
                Assert.That(() => new AudioSampleBuffer(new float[1], 0, 2, timestamp), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new AudioSampleBuffer(new float[1], 48000, 0, timestamp), Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(() => new VisualizerFrameEventArgs(null!), Throws.TypeOf<ArgumentNullException>());
            });
        }

        /// <summary>
        /// AudioInputStartResult の成功と失敗結果を確認します。
        /// ■入力
        /// ・1. Success と Failed をそれぞれ生成する
        /// ■確認内容
        /// ・1. Success では Succeeded が true になる
        /// ・2. Failed では Succeeded が false となり ErrorMessage を保持する
        /// </summary>
        [Test]
        public void Given_StartResultFactoryMethods_When_CreatingResults_Then_ResultStateIsAssigned()
        {
            // 準備と実行
            var success = AudioInputStartResult.Success();
            var failure = AudioInputStartResult.Failed("error");

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(success.Succeeded, Is.True);
                Assert.That(success.ErrorMessage, Is.Null);
                Assert.That(failure.Succeeded, Is.False);
                Assert.That(failure.ErrorMessage, Is.EqualTo("error"));
            });
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 非公開のサンプル受信ハンドラをテストから呼び出します。
        /// </summary>
        /// <param name="provider">呼び出し対象の入力プロバイダーです。</param>
        /// <param name="eventArgs">サンプル受信イベントデータです。</param>
        private static void InvokeOnSamplesCaptured(SystemOutputAudioInputProvider provider, AudioSamplesCapturedEventArgs eventArgs)
        {
            var method = typeof(SystemOutputAudioInputProvider).GetMethod("OnSamplesCaptured", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method!.Invoke(provider, new object?[] { null, eventArgs });
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// 入力プロバイダー用のキャプチャファクトリ代替です。
        /// </summary>
        private sealed class FakeAudioCaptureFactory : IAudioCaptureFactory
        {
            #region フィールド

            /// <summary>
            /// 生成対象として返す偽のキャプチャ実体です。
            /// </summary>
            private readonly FakeAudioCaptureSession? m_CaptureSession;

            /// <summary>
            /// 生成時に送出する例外です。
            /// </summary>
            private readonly Exception? m_Exception;

            #endregion

            #region プロパティ

            /// <summary>
            /// キャプチャ生成要求を受けた回数を取得します。
            /// </summary>
            public int CreateCallCount { get; private set; }

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeAudioCaptureFactory"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="captureSession">生成時に返す偽のキャプチャ実体です。</param>
            public FakeAudioCaptureFactory(FakeAudioCaptureSession captureSession)
            {
                m_CaptureSession = captureSession;
            }

            /// <summary>
            /// <see cref="FakeAudioCaptureFactory"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="exception">生成時に送出する例外です。</param>
            public FakeAudioCaptureFactory(Exception exception)
            {
                m_Exception = exception;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// システム再生音用キャプチャ実体を返すか、設定された例外を送出します。
            /// </summary>
            /// <returns>偽のキャプチャ実体です。</returns>
            public IAudioCaptureSession CreateSystemOutputCapture()
            {
                CreateCallCount++;
                if (m_Exception is not null)
                {
                    throw m_Exception;
                }

                return m_CaptureSession!;
            }

            #endregion
        }

        /// <summary>
        /// 音声サンプル通知と停止回数記録を行う偽のキャプチャ実体です。
        /// </summary>
        private sealed class FakeAudioCaptureSession : IAudioCaptureSession
        {
            #region プロパティ

            /// <summary>
            /// 停止要求を受けた回数を取得します。
            /// </summary>
            public int StopCallCount { get; private set; }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// キャプチャ開始のダミー実装です。
            /// </summary>
            public void Start()
            {
            }

            /// <summary>
            /// 停止要求回数を記録します。
            /// </summary>
            public void Stop()
            {
                StopCallCount++;
            }

            /// <summary>
            /// 解放処理のダミー実装です。
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// 任意の PCM サンプル通知を発生させます。
            /// </summary>
            /// <param name="samples">通知する PCM サンプル列です。</param>
            /// <param name="sampleRate">サンプルレートです。</param>
            /// <param name="channels">チャンネル数です。</param>
            public void RaiseSamples(float[] samples, int sampleRate, int channels)
            {
                SamplesCaptured?.Invoke(this, new AudioSamplesCapturedEventArgs(samples, sampleRate, channels, DateTimeOffset.UtcNow));
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// PCM サンプル通知イベントです。
            /// </summary>
            public event EventHandler<AudioSamplesCapturedEventArgs>? SamplesCaptured;

            #endregion
        }

        #endregion
    }
}
