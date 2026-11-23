using System;
using System.Diagnostics;
using AudioVisualizer.Core.Analysis;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// システム再生音またはマイク入力を取得し、解析済みフレームを生成する入力プロバイダーです。
    /// </summary>
    public sealed class SystemOutputAudioInputProvider : IAudioInputProvider, IDisposable
    {
        #region フィールド

        /// <summary>
        /// PCM サンプルを可視化フレームへ変換する解析サービスです。
        /// </summary>
        private readonly IAudioFrameAnalyzer m_FrameAnalyzer;

        /// <summary>
        /// 音声入力キャプチャを生成するファクトリです。
        /// </summary>
        private readonly IAudioCaptureFactory m_CaptureFactory;

        /// <summary>
        /// 利用可能デバイス一覧と既定デバイスを取得するサービスです。
        /// </summary>
        private readonly IAudioDeviceService m_AudioDeviceService;

        /// <summary>
        /// 現在利用中の音声キャプチャ実体です。
        /// </summary>
        private IAudioCaptureSession? m_CaptureSession;

        /// <summary>
        /// フレーム生成に使用する現在の設定です。
        /// </summary>
        private volatile CaptureState? m_ActiveCapture;

        /// <summary>
        /// 開始、停止、破棄、再接続を直列化するためのロックです。
        /// </summary>
        private readonly object m_CaptureSync = new();

        /// <summary>
        /// 利用者が開始または停止を指示するたびに更新される識別子です。
        /// </summary>
        private long m_RequestVersion;

        /// <summary>
        /// 既定デバイスの変更を通知元スレッドの外で処理します。
        /// </summary>
        private readonly Action<Action> m_QueueReconnect;

        /// <summary>
        /// プロバイダーが破棄されたかどうかを示します。
        /// </summary>
        private bool m_IsDisposed;

        /// <summary>
        /// 自動復旧に使用する、現在の取得要求です。
        /// </summary>
        private VisualizerSettings? m_RequestedSettings;

        /// <summary>
        /// 最後に通知した取得エラーです。
        /// </summary>
        private volatile string? m_CaptureError;

        /// <summary>
        /// フレーム通知の予約を保護します。キャプチャ操作のロックとは独立しています。
        /// </summary>
        private readonly object m_FrameDispatchSync = new();

        /// <summary>
        /// 通知待ちの最新フレームです。
        /// </summary>
        private VisualizerFrameEventArgs? m_PendingFrame;

        /// <summary>
        /// フレーム通知の作業が予約済みか実行中かを示します。
        /// </summary>
        private bool m_IsFrameDispatchQueued;

        /// <summary>
        /// 現在接続している音声デバイス識別子です。
        /// </summary>
        private string? m_CurrentDeviceId;

        /// <summary>
        /// 注入されたデバイスサービスを破棄するかどうかを示す値です。
        /// </summary>
        private readonly bool m_OwnsAudioDeviceService;

        #endregion

        #region プロパティ

        /// <summary>
        /// 現在音声入力が有効かどうかを示す値を取得します。
        /// </summary>
        public bool IsCapturing => m_ActiveCapture is not null;

        /// <summary>
        /// 取得開始または継続ができない場合のエラーを取得します。
        /// </summary>
        public string? CaptureError => m_CaptureError;

        #endregion

        #region 構築・消滅

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public SystemOutputAudioInputProvider()
            : this(new AudioFrameAnalyzer(), new WindowsAudioDeviceService(), new WasapiAudioCaptureFactory(), true)
        {
        }

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frameAnalyzer">PCM サンプルをフレームへ変換する解析サービスです。</param>
        public SystemOutputAudioInputProvider(IAudioFrameAnalyzer frameAnalyzer)
            : this(frameAnalyzer, new WindowsAudioDeviceService(), new WasapiAudioCaptureFactory(), true)
        {
        }

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frameAnalyzer">PCM サンプルをフレームへ変換する解析サービスです。</param>
        /// <param name="audioDeviceService">利用可能デバイス一覧と既定デバイスを取得するサービスです。</param>
        /// <param name="captureFactory">音声入力キャプチャを生成するファクトリです。</param>
        /// <param name="queueReconnect">再接続処理を予約する仕組みです。</param>
        internal SystemOutputAudioInputProvider(
            IAudioFrameAnalyzer frameAnalyzer,
            IAudioDeviceService audioDeviceService,
            IAudioCaptureFactory captureFactory,
            Action<Action>? queueReconnect = null)
            : this(frameAnalyzer, audioDeviceService, captureFactory, false, queueReconnect)
        {
        }

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frameAnalyzer">PCM サンプルをフレームへ変換する解析サービスです。</param>
        /// <param name="audioDeviceService">利用可能デバイス一覧と既定デバイスを取得するサービスです。</param>
        /// <param name="captureFactory">音声入力キャプチャを生成するファクトリです。</param>
        /// <param name="ownsAudioDeviceService">デバイスサービスの破棄責務をこのインスタンスが持つかどうかを示します。</param>
        /// <param name="queueReconnect">再接続処理を予約する仕組みです。</param>
        /// <exception cref="ArgumentNullException"><paramref name="frameAnalyzer"/> または <paramref name="audioDeviceService"/> または <paramref name="captureFactory"/> が <see langword="null"/> である場合にスローされます。</exception>
        private SystemOutputAudioInputProvider(
            IAudioFrameAnalyzer frameAnalyzer,
            IAudioDeviceService audioDeviceService,
            IAudioCaptureFactory captureFactory,
            bool ownsAudioDeviceService,
            Action<Action>? queueReconnect = null)
        {
            m_FrameAnalyzer = frameAnalyzer ?? throw new ArgumentNullException(nameof(frameAnalyzer));
            m_AudioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
            m_CaptureFactory = captureFactory ?? throw new ArgumentNullException(nameof(captureFactory));
            m_OwnsAudioDeviceService = ownsAudioDeviceService;
            m_QueueReconnect = queueReconnect ?? (static reconnect => ThreadPool.QueueUserWorkItem(static action => action(), reconnect, false));
            m_AudioDeviceService.DefaultDeviceChanged += OnDefaultDeviceChanged;
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 利用中のキャプチャ資源を解放します。
        /// </summary>
        public void Dispose()
        {
            lock (m_CaptureSync)
            {
                if (m_IsDisposed)
                {
                    return;
                }

                m_IsDisposed = true;
                m_AudioDeviceService.DefaultDeviceChanged -= OnDefaultDeviceChanged;
                try
                {
                    Stop();
                }
                finally
                {
                    if (m_OwnsAudioDeviceService && m_AudioDeviceService is IDisposable disposableAudioDeviceService)
                    {
                        disposableAudioDeviceService.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// 指定設定が有効なら音声入力を開始し、無効なら停止します。
        /// </summary>
        /// <param name="settings">開始に使用する設定です。</param>
        /// <returns>開始処理の結果です。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> が <see langword="null"/> である場合にスローされます。</exception>
        /// <exception cref="ObjectDisposedException">このインスタンスが破棄済みである場合にスローされます。</exception>
        public AudioInputStartResult Start(VisualizerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            lock (m_CaptureSync)
            {
                ObjectDisposedException.ThrowIf(m_IsDisposed, this);
                Interlocked.Increment(ref m_RequestVersion);
                m_RequestedSettings = settings.IsActive ? settings : null;
                var result = StartCapture(settings);
                PublishCaptureStatus(result.ErrorMessage);
                return result;
            }
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// ロックを保持した状態で取得セッションを切り替えます。
        /// </summary>
        /// <param name="settings">開始に使用する設定です。</param>
        /// <param name="reconnect">既定の変更による再接続かどうかを示します。</param>
        /// <returns>開始処理の結果です。</returns>
        private AudioInputStartResult StartCapture(VisualizerSettings settings, bool reconnect = false)
        {
            try
            {
                if (!settings.IsActive)
                {
                    StopCapture();
                    return AudioInputStartResult.Success();
                }

                var targetDevice = ResolveTargetDevice(settings);
                if (targetDevice is null)
                {
                    StopCapture();
                    return AudioInputStartResult.Failed("指定された音声デバイスが見つかりません。");
                }

                if (reconnect && string.Equals(m_CurrentDeviceId, targetDevice.DeviceId, StringComparison.Ordinal))
                {
                    return AudioInputStartResult.Success();
                }

                StopCapture();
                var captureSession = m_CaptureFactory.CreateCapture(settings.InputSource, targetDevice.DeviceId);
                m_CaptureSession = captureSession;
                captureSession.SamplesCaptured += OnSamplesCaptured;
                captureSession.RecordingStopped += OnRecordingStopped;
                captureSession.Start();

                m_CurrentDeviceId = targetDevice.DeviceId;
                m_ActiveCapture = new CaptureState(captureSession, settings);
                return AudioInputStartResult.Success();
            }
            catch (Exception exception)
            {
                Trace.TraceError("{0}", exception);
                try
                {
                    StopCapture();
                }
                catch (Exception cleanupException)
                {
                    Trace.TraceError("{0}", cleanupException);
                }
                return AudioInputStartResult.Failed("音声入力の開始に失敗しました。");
            }
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 音声入力を停止します。
        /// </summary>
        public void Stop()
        {
            lock (m_CaptureSync)
            {
                Interlocked.Increment(ref m_RequestVersion);
                m_RequestedSettings = null;
                try
                {
                    StopCapture();
                }
                finally
                {
                    PublishCaptureStatus(null);
                }
            }
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// ロックを保持した状態で取得を停止し、処理中のフレームを無効にします。
        /// </summary>
        private void StopCapture()
        {
            m_ActiveCapture = null;
            lock (m_FrameDispatchSync)
            {
                m_PendingFrame = null;
            }

            try
            {
                m_CaptureSession?.Stop();
            }
            finally
            {
                m_CurrentDeviceId = null;
                ReleaseCaptureSession();
            }
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 取得スレッドとは別のスレッドで最新の解析済みフレームを通知します。
        /// </summary>
        public event EventHandler<VisualizerFrameEventArgs>? FrameProduced;

        /// <summary>
        /// 取得状態または取得エラーを確認できるタイミングで発生します。
        /// </summary>
        public event EventHandler? CaptureStatusChanged;

        /// <summary>
        /// 既定音声デバイスの変更通知を受け取り、必要時だけ再接続します。
        /// </summary>
        /// <param name="sender">通知送信元です。</param>
        /// <param name="e">既定デバイス変更情報です。</param>
        private void OnDefaultDeviceChanged(object? sender, DefaultAudioDeviceChangedEventArgs e)
        {
            var requestVersion = Interlocked.Read(ref m_RequestVersion);
            m_QueueReconnect(() => Reconnect(requestVersion, e.InputSource));
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// 通知後も同じ取得要求が有効な場合に、現在の既定デバイスを再確認します。
        /// </summary>
        /// <param name="requestVersion">通知時点の利用者要求です。</param>
        /// <param name="inputSource">通知対象の入力種別です。</param>
        private void Reconnect(long requestVersion, InputSource inputSource)
        {
            lock (m_CaptureSync)
            {
                if (m_IsDisposed || requestVersion != m_RequestVersion ||
                    m_RequestedSettings is not { UseDefaultDevice: true } settings || settings.InputSource != inputSource)
                {
                    return;
                }

                var result = StartCapture(settings, reconnect: true);
                PublishCaptureStatus(result.ErrorMessage);
            }
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 取得側からの停止通知を受け取り、同じセッションがまだ有効なら終了を反映します。
        /// </summary>
        /// <param name="sender">停止した取得セッションです。</param>
        /// <param name="e">取得終了時の情報です。</param>
        private void OnRecordingStopped(object? sender, EventArgs e)
        {
            m_QueueReconnect(() =>
            {
                lock (m_CaptureSync)
                {
                    if (m_ActiveCapture is not { } capture || !ReferenceEquals(sender, capture.Session))
                    {
                        return;
                    }

                    try
                    {
                        StopCapture();
                    }
                    catch (Exception exception)
                    {
                        Trace.TraceError("{0}", exception);
                    }

                    PublishCaptureStatus("音声入力が停止しました。");
                }
            });
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// 最新のキャプチャ状態を購読者へ通知します。
        /// </summary>
        /// <param name="error">取得エラーです。正常な場合は null です。</param>
        private void PublishCaptureStatus(string? error)
        {
            m_CaptureError = error;
            CaptureStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// キャプチャ済み PCM サンプルを受け取り、解析済みフレーム通知へ変換します。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">取得した PCM サンプルを含むイベントデータです。</param>
        private void OnSamplesCaptured(object? sender, AudioSamplesCapturedEventArgs e)
        {
            var capture = m_ActiveCapture;
            if (capture is null || !ReferenceEquals(sender, capture.Session))
            {
                return;
            }

            var sampleBuffer = new AudioSampleBuffer(e.Samples, e.SampleRate, e.Channels, e.Timestamp);
            var frame = m_FrameAnalyzer is AudioFrameAnalyzer analyzer
                ? analyzer.CreateFrame(sampleBuffer, capture.Settings, capture.WaveformHistory)
                : m_FrameAnalyzer.CreateFrame(sampleBuffer, capture.Settings);
            var args = new VisualizerFrameEventArgs(frame, () => ReferenceEquals(m_ActiveCapture, capture));
            lock (m_FrameDispatchSync)
            {
                if (!args.IsCurrent)
                {
                    return;
                }

                m_PendingFrame = args;
                if (m_IsFrameDispatchQueued)
                {
                    return;
                }

                m_IsFrameDispatchQueued = true;
            }

            QueueFrameDispatch();
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// 取得スレッドから独立した作業として、保留フレームの通知を予約します。
        /// </summary>
        private void QueueFrameDispatch()
        {
            ThreadPool.QueueUserWorkItem(static provider => provider.DispatchFrame(), this, false);
        }

        /// <summary>
        /// 取得がまだ有効な最新フレームを購読者へ通知します。
        /// </summary>
        private void DispatchFrame()
        {
            VisualizerFrameEventArgs? frame;
            lock (m_FrameDispatchSync)
            {
                frame = m_PendingFrame;
                m_PendingFrame = null;
            }

            try
            {
                if (frame is { IsCurrent: true })
                {
                    FrameProduced?.Invoke(this, frame);
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("{0}", exception);
            }
            finally
            {
                bool shouldSchedule;
                lock (m_FrameDispatchSync)
                {
                    shouldSchedule = m_PendingFrame is not null;
                    m_IsFrameDispatchQueued = shouldSchedule;
                }

                if (shouldSchedule)
                {
                    QueueFrameDispatch();
                }
            }
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// 稼働中のセッションと、そのセッションに対応する解析設定です。
        /// </summary>
        /// <param name="Session">音声取得セッションです。</param>
        /// <param name="Settings">フレームの解析設定です。</param>
        private sealed record CaptureState(IAudioCaptureSession Session, VisualizerSettings Settings)
        {
            #region プロパティ

            /// <summary>
            /// 同じ取得セッションの PCM を保持する波形履歴です。
            /// </summary>
            public WaveformHistory WaveformHistory { get; } = new();

            #endregion
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// 現在利用中のキャプチャ実体を解放します。
        /// </summary>
        private void ReleaseCaptureSession()
        {
            if (m_CaptureSession is null)
            {
                return;
            }

            var session = m_CaptureSession;
            m_CaptureSession = null;
            session.SamplesCaptured -= OnSamplesCaptured;
            session.RecordingStopped -= OnRecordingStopped;
            session.Dispose();
        }

        /// <summary>
        /// 現在設定に対して実際に使用する音声デバイスを解決します。
        /// </summary>
        /// <param name="settings">開始に使用する設定です。</param>
        /// <returns>解決できた音声デバイス。見つからない場合は <see langword="null"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="settings"/> が <see langword="null"/> である場合にスローされます。</exception>
        private AudioDeviceInfo? ResolveTargetDevice(VisualizerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (settings.UseDefaultDevice)
            {
                return m_AudioDeviceService.GetDefaultDevice(settings.InputSource);
            }

            var devices = m_AudioDeviceService.GetDevices(settings.InputSource);
            for (var index = 0; index < devices.Count; index++)
            {
                var device = devices[index];
                if (string.Equals(device.DeviceId, settings.DeviceId, StringComparison.Ordinal))
                {
                    return device;
                }
            }

            return null;
        }

        #endregion
    }
}
