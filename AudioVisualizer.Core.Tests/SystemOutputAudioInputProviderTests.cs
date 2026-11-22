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
        public async Task Given_SystemOutputSettings_When_StartAndSamplesArrive_Then_FrameProducedIsRaised()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true) },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true),
                defaultMicrophoneDevice: null);
            var analyzer = new AudioFrameAnalyzer();
            using var sut = new SystemOutputAudioInputProvider(analyzer, deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
            var receivedFrame = new TaskCompletionSource<VisualizerFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
            sut.FrameProduced += (_, eventArgs) => receivedFrame.TrySetResult(eventArgs.Frame);

            // 実行
            var startResult = sut.Start(settings);
            captureSession.RaiseSamples(new float[] { 0.1f, -0.4f, 0.7f, -0.2f }, 48000, 2);
            var producedFrame = await receivedFrame.Task.WaitAsync(TimeSpan.FromSeconds(5));
            sut.Stop();

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(sut.IsCapturing, Is.False);
                Assert.That(producedFrame, Is.Not.Null);
                Assert.That(producedFrame!.SpectrumValues.Count, Is.EqualTo(settings.BarCount));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(1));
                Assert.That(captureFactory.LastInputSource, Is.EqualTo(InputSource.SystemOutput));
                Assert.That(captureFactory.LastDeviceId, Is.EqualTo("render-1"));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider がマイク入力へ切り替えて開始できることを確認します。
        /// ■入力
        /// ・1. Microphone の VisualizerSettings
        /// ・2. Microphone の既定デバイスを返す FakeAudioDeviceService
        /// ■確認内容
        /// ・1. Start が成功結果を返す
        /// ・2. マイク入力種別でキャプチャ生成が呼ばれる
        /// </summary>
        [Test]
        public void Given_MicrophoneSettings_When_Start_Then_MicrophoneCaptureIsCreated()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: Array.Empty<AudioDeviceInfo>(),
                microphoneDevices: new[] { new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true) },
                defaultRenderDevice: null,
                defaultMicrophoneDevice: new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true));
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.Microphone, null, true, true, 1.0, 0.5, 8);

            // 実行
            var result = sut.Start(settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(sut.IsCapturing, Is.True);
                Assert.That(captureFactory.LastInputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(captureFactory.LastDeviceId, Is.EqualTo("mic-1"));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が既定デバイス利用中に既定再生デバイス変更通知を受けると再接続することを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = true の VisualizerSettings
        /// ・2. 既定再生デバイスを render-1 から render-2 に切り替える FakeAudioDeviceService
        /// ■確認内容
        /// ・1. キャプチャ生成が 2 回呼ばれる
        /// ・2. 2 回目は新しい既定デバイス render-2 が使われる
        /// ・3. 既存セッションへ Stop が 1 回呼ばれる
        /// </summary>
        [Test]
        public async Task Given_DefaultDeviceMode_When_DefaultRenderDeviceChanges_Then_CaptureRestartsWithNewDefaultDevice()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var renderDevice1 = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var renderDevice2 = new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { renderDevice1, renderDevice2 },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: renderDevice1,
                defaultMicrophoneDevice: null);
            var work = new Queue<Action>();
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory, work.Enqueue);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
            var receivedFrame = new TaskCompletionSource<VisualizerFrameEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            sut.FrameProduced += (_, args) => receivedFrame.TrySetResult(args);

            // 実行
            var startResult = sut.Start(settings);
            captureSession.RaiseSamples(new[] { 0.1f, 0.2f }, 48000, 1);
            var oldFrame = await receivedFrame.Task.WaitAsync(TimeSpan.FromSeconds(5));
            deviceService.SetDefaultDevice(InputSource.SystemOutput, renderDevice2);
            deviceService.RaiseDefaultDeviceChanged(InputSource.SystemOutput, "render-2");
            Assert.That(captureFactory.CreateCallCount, Is.EqualTo(1));
            work.Dequeue()();
            Assert.That(oldFrame.IsCurrent, Is.False);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(2));
                Assert.That(captureFactory.LastDeviceId, Is.EqualTo("render-2"));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(1));
                Assert.That(sut.IsCapturing, Is.True);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が明示デバイス利用中は既定デバイス変更通知で再接続しないことを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = false の VisualizerSettings
        /// ・2. 既定再生デバイス変更通知
        /// ■確認内容
        /// ・1. キャプチャ生成回数が増えない
        /// ・2. 既存セッションへ Stop が呼ばれない
        /// </summary>
        [Test]
        public void Given_ExplicitDeviceMode_When_DefaultRenderDeviceChanges_Then_CaptureDoesNotRestart()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var renderDevice1 = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var renderDevice2 = new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { renderDevice1, renderDevice2 },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: renderDevice1,
                defaultMicrophoneDevice: null);
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, "render-1", false, true, 1.0, 0.5, 8);

            // 実行
            var startResult = sut.Start(settings);
            deviceService.SetDefaultDevice(InputSource.SystemOutput, renderDevice2);
            deviceService.RaiseDefaultDeviceChanged(InputSource.SystemOutput, "render-2");

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(1));
                Assert.That(captureFactory.LastDeviceId, Is.EqualTo("render-1"));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(0));
                Assert.That(sut.IsCapturing, Is.True);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が同一既定デバイス通知では再接続しないことを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = true の VisualizerSettings
        /// ・2. 現在接続中と同じデバイス識別子の既定デバイス変更通知
        /// ■確認内容
        /// ・1. キャプチャ生成回数が増えない
        /// ・2. 既存セッションへ Stop が呼ばれない
        /// </summary>
        [Test]
        public void Given_DefaultDeviceMode_When_DefaultDeviceChangeKeepsSameDeviceId_Then_CaptureDoesNotRestart()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var renderDevice = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { renderDevice },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: renderDevice,
                defaultMicrophoneDevice: null);
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            // 実行
            var startResult = sut.Start(settings);
            deviceService.RaiseDefaultDeviceChanged(InputSource.SystemOutput, "render-1");

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(1));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(0));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が別入力種別の既定デバイス変更通知を無視することを確認します。
        /// ■入力
        /// ・1. SystemOutput で開始済みの UseDefaultDevice = true の VisualizerSettings
        /// ・2. Microphone の既定デバイス変更通知
        /// ■確認内容
        /// ・1. キャプチャ生成回数が増えない
        /// ・2. 既存セッションへ Stop が呼ばれない
        /// </summary>
        [Test]
        public void Given_DefaultDeviceMode_When_DefaultDeviceChangeTargetsOtherInputSource_Then_CaptureDoesNotRestart()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var renderDevice = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var microphoneDevice = new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { renderDevice },
                microphoneDevices: new[] { microphoneDevice },
                defaultRenderDevice: renderDevice,
                defaultMicrophoneDevice: microphoneDevice);
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            // 実行
            var startResult = sut.Start(settings);
            deviceService.RaiseDefaultDeviceChanged(InputSource.Microphone, "mic-1");

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(startResult.Succeeded, Is.True);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(1));
                Assert.That(captureSession.StopCallCount, Is.EqualTo(0));
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が開始失敗時に停止状態を維持することを確認します。
        /// ■入力
        /// ・1. CreateCapture で例外を送出する FakeAudioCaptureFactory
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
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true) },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true),
                defaultMicrophoneDevice: null);
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
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
        /// SystemOutputAudioInputProvider が開始失敗後でも次回開始を再試行できることを確認します。
        /// ■入力
        /// ・1. 初回だけ例外を送出する FakeAudioCaptureFactory
        /// ・2. 同じ VisualizerSettings で 2 回 Start を呼ぶ
        /// ■確認内容
        /// ・1. 1 回目は失敗結果を返す
        /// ・2. 2 回目は成功結果を返す
        /// ・3. 2 回目の後に IsCapturing が true になる
        /// </summary>
        [Test]
        public void Given_TransientStartFailure_When_StartingAgain_Then_CaptureCanRetrySuccessfully()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            captureFactory.SetNextException(new InvalidOperationException("boom"));
            var defaultRenderDevice = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { defaultRenderDevice },
                microphoneDevices: Array.Empty<AudioDeviceInfo>(),
                defaultRenderDevice: defaultRenderDevice,
                defaultMicrophoneDevice: null);
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            // 実行
            var firstResult = sut.Start(settings);
            var secondResult = sut.Start(settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(firstResult.Succeeded, Is.False);
                Assert.That(secondResult.Succeeded, Is.True);
                Assert.That(captureFactory.CreateCallCount, Is.EqualTo(2));
                Assert.That(sut.IsCapturing, Is.True);
            });
        }

        /// <summary>
        /// SystemOutputAudioInputProvider が存在しない明示デバイス指定を拒否することを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = false, DeviceId = "missing-device" の VisualizerSettings
        /// ■確認内容
        /// ・1. Start が失敗結果を返す
        /// ・2. キャプチャファクトリが呼び出されない
        /// </summary>
        [Test]
        public void Given_MissingExplicitDevice_When_Start_Then_FailureResultIsReturned()
        {
            // 準備
            var captureFactory = new FakeAudioCaptureFactory(new FakeAudioCaptureSession());
            var deviceService = new FakeAudioDeviceService(
                renderDevices: new[] { new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true) },
                microphoneDevices: new[] { new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true) },
                defaultRenderDevice: new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true),
                defaultMicrophoneDevice: new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true));
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.Microphone, "missing-device", false, true, 1.0, 0.5, 8);

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
        /// SystemOutputAudioInputProvider が明示デバイス指定で対象デバイスを使用することを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = false, DeviceId = "mic-2" の VisualizerSettings
        /// ■確認内容
        /// ・1. Start が成功結果を返す
        /// ・2. 指定した DeviceId でキャプチャ生成が呼ばれる
        /// </summary>
        [Test]
        public void Given_ExplicitDeviceId_When_Start_Then_RequestedDeviceIsUsed()
        {
            // 準備
            var captureSession = new FakeAudioCaptureSession();
            var captureFactory = new FakeAudioCaptureFactory(captureSession);
            var deviceService = new FakeAudioDeviceService(
                renderDevices: Array.Empty<AudioDeviceInfo>(),
                microphoneDevices: new[]
                {
                    new AudioDeviceInfo("mic-1", "Microphone 1", InputSource.Microphone, true),
                    new AudioDeviceInfo("mic-2", "Microphone 2", InputSource.Microphone, false),
                },
                defaultRenderDevice: null,
                defaultMicrophoneDevice: new AudioDeviceInfo("mic-1", "Microphone 1", InputSource.Microphone, true));
            using var sut = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), deviceService, captureFactory);
            var settings = new VisualizerSettings(InputSource.Microphone, "mic-2", false, true, 1.0, 0.5, 8);

            // 実行
            var result = sut.Start(settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(captureFactory.LastInputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(captureFactory.LastDeviceId, Is.EqualTo("mic-2"));
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
            using var sut = new SystemOutputAudioInputProvider(
                new AudioFrameAnalyzer(),
                new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>(), null, null),
                new FakeAudioCaptureFactory(new FakeAudioCaptureSession()));

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
            using var sut = new SystemOutputAudioInputProvider(
                new AudioFrameAnalyzer(),
                new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>(), null, null),
                new FakeAudioCaptureFactory(new FakeAudioCaptureSession()));
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

        /// <summary>
        /// 停止と再開をまたいだ既定デバイス通知は、後から実行されても再接続しません。
        /// </summary>
        [Test]
        public void QueuedDefaultChange_WhenCaptureRequestIsReplaced_IsIgnored()
        {
            var first = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var second = new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false);
            var devices = new FakeAudioDeviceService(new[] { first, second }, Array.Empty<AudioDeviceInfo>(), first, null);
            var captures = new FakeAudioCaptureFactory(new FakeAudioCaptureSession());
            var work = new Queue<Action>();
            using var provider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), devices, captures, work.Enqueue);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            Assert.That(provider.Start(settings).Succeeded, Is.True);
            devices.SetDefaultDevice(InputSource.SystemOutput, second);
            devices.RaiseDefaultDeviceChanged(InputSource.SystemOutput, second.DeviceId);
            provider.Stop();
            Assert.That(provider.Start(settings).Succeeded, Is.True);
            devices.SetDefaultDevice(InputSource.SystemOutput, first);

            work.Dequeue()();

            Assert.That(captures.CreateCallCount, Is.EqualTo(2));
            Assert.That(captures.LastDeviceId, Is.EqualTo(second.DeviceId));
        }

        /// <summary>
        /// 既定デバイス消失後に再接続が失敗しても、次の変更時に取得が復旧します。
        /// </summary>
        [Test]
        public void DefaultCapture_WhenDeviceDisappearsAndReconnectFails_RecoversOnNextChange()
        {
            var first = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var second = new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false);
            var devices = new FakeAudioDeviceService(new[] { first, second }, Array.Empty<AudioDeviceInfo>(), first, null);
            var captures = new FakeAudioCaptureFactory(new FakeAudioCaptureSession());
            var work = new Queue<Action>();
            using var provider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), devices, captures, work.Enqueue);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            Assert.That(provider.Start(settings).Succeeded, Is.True);
            devices.SetDefaultDevice(InputSource.SystemOutput, null);
            devices.RaiseDefaultDeviceChanged(InputSource.SystemOutput, null);
            work.Dequeue()();
            Assert.That(provider.IsCapturing, Is.False);
            Assert.That(provider.CaptureError, Is.Not.Null);

            captures.SetNextException(new InvalidOperationException());
            devices.SetDefaultDevice(InputSource.SystemOutput, second);
            devices.RaiseDefaultDeviceChanged(InputSource.SystemOutput, second.DeviceId);
            work.Dequeue()();
            Assert.That(provider.IsCapturing, Is.False);
            Assert.That(provider.CaptureError, Is.Not.Null);

            devices.SetDefaultDevice(InputSource.SystemOutput, first);
            devices.RaiseDefaultDeviceChanged(InputSource.SystemOutput, first.DeviceId);
            work.Dequeue()();
            Assert.That(provider.IsCapturing, Is.True);
            Assert.That(provider.CaptureError, Is.Null);
        }

        /// <summary>
        /// 開始処理で例外が発生したセッションから通知購読を外し、セッションを破棄します。
        /// </summary>
        [Test]
        public void Start_WhenSessionStartThrows_DetachesAndDisposesSession()
        {
            var device = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var devices = new FakeAudioDeviceService(new[] { device }, Array.Empty<AudioDeviceInfo>(), device, null);
            var session = new FakeAudioCaptureSession { StartException = new InvalidOperationException() };
            using var provider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), devices, new FakeAudioCaptureFactory(session));
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);

            var result = provider.Start(settings);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(provider.IsCapturing, Is.False);
            Assert.That(session.HasSubscribers, Is.False);
            Assert.That(session.DisposeCallCount, Is.EqualTo(1));
        }

        /// <summary>
        /// 取得側の予期しない終了が、状態とエラーとして利用側に伝わります。
        /// </summary>
        [Test]
        public void RecordingStopped_WhenCaptureIsActive_UpdatesPublicStatus()
        {
            var device = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var devices = new FakeAudioDeviceService(new[] { device }, Array.Empty<AudioDeviceInfo>(), device, null);
            var session = new FakeAudioCaptureSession();
            var work = new Queue<Action>();
            using var provider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), devices, new FakeAudioCaptureFactory(session), work.Enqueue);
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
            Assert.That(provider.Start(settings).Succeeded, Is.True);
            var reportedStates = new List<(bool IsCapturing, string? Error)>();
            provider.CaptureStatusChanged += (_, _) => reportedStates.Add((provider.IsCapturing, provider.CaptureError));

            session.RaiseRecordingStopped();
            work.Dequeue()();

            Assert.That(reportedStates, Has.Count.EqualTo(1));
            Assert.That(reportedStates[0].IsCapturing, Is.False);
            Assert.That(reportedStates[0].Error, Is.Not.Null);
        }

        /// <summary>
        /// フレーム通知中に停止や破棄を行っても、サンプル通知元の終了を待てます。
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public async Task FrameHandler_WhenStoppingOrDisposing_AllowsCaptureThreadToExit(bool dispose)
        {
            var device = new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true);
            var devices = new FakeAudioDeviceService(new[] { device }, Array.Empty<AudioDeviceInfo>(), device, null);
            var producerExited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var session = new FakeAudioCaptureSession
            {
                OnDispose = () => producerExited.Task.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult(),
            };
            using var provider = new SystemOutputAudioInputProvider(new AudioFrameAnalyzer(), devices, new FakeAudioCaptureFactory(session));
            provider.FrameProduced += (_, _) =>
            {
                try
                {
                    if (dispose)
                    {
                        provider.Dispose();
                    }
                    else
                    {
                        provider.Stop();
                    }

                    stopped.SetResult();
                }
                catch (Exception exception)
                {
                    stopped.SetException(exception);
                }
            };

            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
            Assert.That(provider.Start(settings).Succeeded, Is.True);
            await Task.Run(() =>
            {
                session.RaiseSamples(new[] { 0.1f, 0.2f }, 48000, 1);
                producerExited.SetResult();
            });

            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(provider.IsCapturing, Is.False);
            Assert.That(session.DisposeCallCount, Is.EqualTo(1));
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
            private Exception? m_Exception;

            #endregion

            #region プロパティ

            /// <summary>
            /// キャプチャ生成要求を受けた回数を取得します。
            /// </summary>
            public int CreateCallCount { get; private set; }

            /// <summary>
            /// 最後に要求された入力種別を取得します。
            /// </summary>
            public InputSource? LastInputSource { get; private set; }

            /// <summary>
            /// 最後に要求されたデバイス識別子を取得します。
            /// </summary>
            public string? LastDeviceId { get; private set; }

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
            /// 指定条件に応じた偽キャプチャ実体を返すか、設定された例外を送出します。
            /// </summary>
            /// <param name="inputSource">要求された入力種別です。</param>
            /// <param name="deviceId">要求されたデバイス識別子です。</param>
            /// <returns>偽のキャプチャ実体です。</returns>
            public IAudioCaptureSession CreateCapture(InputSource inputSource, string deviceId)
            {
                CreateCallCount++;
                LastInputSource = inputSource;
                LastDeviceId = deviceId;
                if (m_Exception is not null)
                {
                    var exception = m_Exception;
                    m_Exception = null;
                    throw exception;
                }

                return m_CaptureSession!;
            }

            /// <summary>
            /// 次回の CreateCapture 呼び出しで送出する例外を設定します。
            /// </summary>
            /// <param name="exception">送出する例外です。</param>
            public void SetNextException(Exception exception)
            {
                m_Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            }

            #endregion
        }

        /// <summary>
        /// 入力種別ごとのデバイス一覧と既定デバイスを返す偽のデバイスサービスです。
        /// </summary>
        private sealed class FakeAudioDeviceService : IAudioDeviceService
        {
            #region フィールド

            /// <summary>
            /// 再生デバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_RenderDevices;

            /// <summary>
            /// マイクデバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_MicrophoneDevices;

            /// <summary>
            /// 既定の再生デバイスです。
            /// </summary>
            private AudioDeviceInfo? m_DefaultRenderDevice;

            /// <summary>
            /// 既定のマイクデバイスです。
            /// </summary>
            private AudioDeviceInfo? m_DefaultMicrophoneDevice;

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeAudioDeviceService"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="renderDevices">再生デバイス一覧です。</param>
            /// <param name="microphoneDevices">マイクデバイス一覧です。</param>
            /// <param name="defaultRenderDevice">既定の再生デバイスです。</param>
            /// <param name="defaultMicrophoneDevice">既定のマイクデバイスです。</param>
            public FakeAudioDeviceService(
                IReadOnlyList<AudioDeviceInfo> renderDevices,
                IReadOnlyList<AudioDeviceInfo> microphoneDevices,
                AudioDeviceInfo? defaultRenderDevice,
                AudioDeviceInfo? defaultMicrophoneDevice)
            {
                m_RenderDevices = renderDevices;
                m_MicrophoneDevices = microphoneDevices;
                m_DefaultRenderDevice = defaultRenderDevice;
                m_DefaultMicrophoneDevice = defaultMicrophoneDevice;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 指定入力種別に対応するデバイス一覧を返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>対応するデバイス一覧です。</returns>
            public IReadOnlyList<AudioDeviceInfo> GetDevices(InputSource inputSource)
            {
                return inputSource switch
                {
                    InputSource.SystemOutput => m_RenderDevices,
                    InputSource.Microphone => m_MicrophoneDevices,
                    _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
                };
            }

            /// <summary>
            /// 指定入力種別に対応する既定デバイスを返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
            public AudioDeviceInfo? GetDefaultDevice(InputSource inputSource)
            {
                return inputSource switch
                {
                    InputSource.SystemOutput => m_DefaultRenderDevice,
                    InputSource.Microphone => m_DefaultMicrophoneDevice,
                    _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
                };
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// 既定音声デバイスが切り替わったときに発生します。
            /// </summary>
            public event EventHandler<DefaultAudioDeviceChangedEventArgs>? DefaultDeviceChanged;

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 指定入力種別の既定デバイスを更新します。
            /// </summary>
            /// <param name="inputSource">更新対象の入力種別です。</param>
            /// <param name="deviceInfo">新しい既定デバイスです。</param>
            public void SetDefaultDevice(InputSource inputSource, AudioDeviceInfo? deviceInfo)
            {
                switch (inputSource)
                {
                    case InputSource.SystemOutput:
                        m_DefaultRenderDevice = deviceInfo;
                        break;
                    case InputSource.Microphone:
                        m_DefaultMicrophoneDevice = deviceInfo;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。");
                }
            }

            /// <summary>
            /// 任意の既定デバイス変更通知を発生させます。
            /// </summary>
            /// <param name="inputSource">通知対象の入力種別です。</param>
            /// <param name="deviceId">新しい既定デバイス識別子です。</param>
            public void RaiseDefaultDeviceChanged(InputSource inputSource, string? deviceId)
            {
                DefaultDeviceChanged?.Invoke(this, new DefaultAudioDeviceChangedEventArgs(inputSource, deviceId));
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

            /// <summary>
            /// 開始時に送出する例外を指定します。
            /// </summary>
            public Exception? StartException { get; init; }

            /// <summary>
            /// 通知の購読が残っているかを取得します。
            /// </summary>
            public bool HasSubscribers => SamplesCaptured is not null || RecordingStopped is not null;

            /// <summary>
            /// 解放要求を受けた回数を取得します。
            /// </summary>
            public int DisposeCallCount { get; private set; }

            /// <summary>
            /// 解放時に実行する処理を指定します。
            /// </summary>
            public Action? OnDispose { get; init; }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// キャプチャ開始のダミー実装です。
            /// </summary>
            public void Start()
            {
                if (StartException is not null)
                {
                    throw StartException;
                }
            }

            /// <summary>
            /// 停止要求回数を記録します。
            /// </summary>
            public void Stop()
            {
                StopCallCount++;
            }

            /// <summary>
            /// 解放要求の回数を記録します。
            /// </summary>
            public void Dispose()
            {
                DisposeCallCount++;
                OnDispose?.Invoke();
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

            /// <summary>
            /// 取得終了を通知します。
            /// </summary>
            public event EventHandler? RecordingStopped;

            /// <summary>
            /// 取得側からの終了イベントを発行します。
            /// </summary>
            public void RaiseRecordingStopped()
            {
                RecordingStopped?.Invoke(this, EventArgs.Empty);
            }

            #endregion
        }

        #endregion
    }
}
