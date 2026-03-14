using System;
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
        private VisualizerSettings? m_CurrentSettings;

        #endregion

        #region プロパティ

        /// <summary>
        /// 現在音声入力が有効かどうかを示す値を取得します。
        /// </summary>
        public bool IsCapturing { get; private set; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public SystemOutputAudioInputProvider()
            : this(new AudioFrameAnalyzer(), new WindowsAudioDeviceService(), new WasapiAudioCaptureFactory())
        {
        }

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frameAnalyzer">PCM サンプルをフレームへ変換する解析サービスです。</param>
        public SystemOutputAudioInputProvider(IAudioFrameAnalyzer frameAnalyzer)
            : this(frameAnalyzer, new WindowsAudioDeviceService(), new WasapiAudioCaptureFactory())
        {
        }

        /// <summary>
        /// <see cref="SystemOutputAudioInputProvider"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frameAnalyzer">PCM サンプルをフレームへ変換する解析サービスです。</param>
        /// <param name="audioDeviceService">利用可能デバイス一覧と既定デバイスを取得するサービスです。</param>
        /// <param name="captureFactory">音声入力キャプチャを生成するファクトリです。</param>
        internal SystemOutputAudioInputProvider(IAudioFrameAnalyzer frameAnalyzer, IAudioDeviceService audioDeviceService, IAudioCaptureFactory captureFactory)
        {
            m_FrameAnalyzer = frameAnalyzer ?? throw new ArgumentNullException(nameof(frameAnalyzer));
            m_AudioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
            m_CaptureFactory = captureFactory ?? throw new ArgumentNullException(nameof(captureFactory));
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 利用中のキャプチャ資源を解放します。
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// 指定設定に従って音声入力の取得を開始します。
        /// </summary>
        /// <param name="settings">開始に使用する設定です。</param>
        /// <returns>開始処理の結果です。</returns>
        public AudioInputStartResult Start(VisualizerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var targetDevice = ResolveTargetDevice(settings);
            if (targetDevice is null)
            {
                return AudioInputStartResult.Failed("指定された音声デバイスが見つかりません。");
            }

            try
            {
                Stop();
                var captureSession = m_CaptureFactory.CreateCapture(settings.InputSource, targetDevice.DeviceId);
                captureSession.SamplesCaptured += OnSamplesCaptured;
                captureSession.Start();

                m_CaptureSession = captureSession;
                m_CurrentSettings = settings;
                IsCapturing = true;
                return AudioInputStartResult.Success();
            }
            catch (Exception)
            {
                ReleaseCaptureSession();
                m_CurrentSettings = null;
                IsCapturing = false;
                return AudioInputStartResult.Failed("音声入力の開始に失敗しました。");
            }
        }

        /// <summary>
        /// 音声入力を停止します。
        /// </summary>
        public void Stop()
        {
            try
            {
                m_CaptureSession?.Stop();
            }
            finally
            {
                ReleaseCaptureSession();
                m_CurrentSettings = null;
                IsCapturing = false;
            }
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 新しい解析済みフレームが生成されたときに発生します。
        /// </summary>
        public event EventHandler<VisualizerFrameEventArgs>? FrameProduced;

        /// <summary>
        /// キャプチャ済み PCM サンプルを受け取り、解析済みフレーム通知へ変換します。
        /// </summary>
        /// <param name="sender">イベント送信元です。</param>
        /// <param name="e">取得した PCM サンプルを含むイベントデータです。</param>
        private void OnSamplesCaptured(object? sender, AudioSamplesCapturedEventArgs e)
        {
            if (m_CurrentSettings is null)
            {
                return;
            }

            var sampleBuffer = new AudioSampleBuffer(e.Samples, e.SampleRate, e.Channels, e.Timestamp);
            var frame = m_FrameAnalyzer.CreateFrame(sampleBuffer, m_CurrentSettings);
            FrameProduced?.Invoke(this, new VisualizerFrameEventArgs(frame));
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 現在利用中のキャプチャ実体を解放します。
        /// </summary>
        private void ReleaseCaptureSession()
        {
            if (m_CaptureSession is null)
            {
                return;
            }

            m_CaptureSession.SamplesCaptured -= OnSamplesCaptured;
            m_CaptureSession.Dispose();
            m_CaptureSession = null;
        }

        /// <summary>
        /// 現在設定に対して実際に使用する音声デバイスを解決します。
        /// </summary>
        /// <param name="settings">開始に使用する設定です。</param>
        /// <returns>解決できた音声デバイス。見つからない場合は <see langword="null"/>。</returns>
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
