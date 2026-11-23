using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using AudioVisualizer.Core.Models;
using NAudio.CoreAudioApi;

using NAudio.Wave;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// NAudio の WASAPI キャプチャを生成する内部ファクトリです。
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class WasapiAudioCaptureFactory : IAudioCaptureFactory
    {
        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別とデバイス識別子に対応するキャプチャ実体を生成します。
        /// </summary>
        /// <param name="inputSource">入力種別です。</param>
        /// <param name="deviceId">対象デバイスの識別子です。</param>
        /// <returns>指定入力向けのキャプチャ実体です。</returns>
        /// <exception cref="PlatformNotSupportedException">実行環境が Windows でない場合にスローされます。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSource"/> がサポートされていない値である場合にスローされます。</exception>
        public IAudioCaptureSession CreateCapture(InputSource inputSource, string deviceId)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            using var deviceEnumerator = new MMDeviceEnumerator();
            var device = deviceEnumerator.GetDevice(deviceId);
            try
            {
                var builder = new WasapiRecorderBuilder().WithDevice(device).WithPollingSync();
                switch (inputSource)
                {
                    case InputSource.SystemOutput:
                        builder.WithLoopbackCapture();
                        break;
                    case InputSource.Microphone:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。");
                }

                var recorder = builder.Build();
                try
                {
                    var format = recorder.WaveFormat.AsStandardWaveFormat();
                    PcmSampleConverter.ValidateFormat(format);
                    return new WasapiCaptureSession(recorder, device, format);
                }
                catch
                {
                    recorder.Dispose();
                    throw;
                }
            }
            catch
            {
                device.Dispose();
                throw;
            }
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// NAudio の WASAPI キャプチャを内部契約へ適合させるラッパーです。
        /// </summary>
        [SupportedOSPlatform("windows")]
        private sealed class WasapiCaptureSession : IAudioCaptureSession
        {
            #region フィールド

            /// <summary>
            /// 実際の WASAPI キャプチャです。
            /// </summary>
            private readonly WasapiRecorder m_Capture;

            /// <summary>
            /// 録音機が使用する音声デバイスです。
            /// </summary>
            private readonly MMDevice m_Device;

            /// <summary>
            /// PCM 変換に使用する音声フォーマットです。
            /// </summary>
            private readonly WaveFormat m_WaveFormat;

            #endregion

            #region 構築・消滅

            /// <summary>
            /// <see cref="WasapiCaptureSession"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="capture">ラップ対象の WASAPI キャプチャです。</param>
            /// <param name="device">録音機が使用する音声デバイスです。</param>
            /// <param name="format">検証済みの取得フォーマットです。</param>
            public WasapiCaptureSession(WasapiRecorder capture, MMDevice device, WaveFormat format)
            {
                m_Capture = capture;
                m_Device = device;
                m_WaveFormat = format;
                m_Capture.DataAvailable += OnDataAvailable;
                m_Capture.RecordingStopped += OnRecordingStopped;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 利用中のキャプチャ資源を解放します。
            /// </summary>
            public void Dispose()
            {
                m_Capture.DataAvailable -= OnDataAvailable;
                m_Capture.RecordingStopped -= OnRecordingStopped;
                try
                {
                    m_Capture.Dispose();
                }
                finally
                {
                    m_Device.Dispose();
                }
            }

            /// <summary>
            /// 音声録音を開始します。
            /// </summary>
            public void Start()
            {
                m_Capture.StartRecording();
            }

            /// <summary>
            /// 音声録音を停止します。
            /// </summary>
            public void Stop()
            {
                m_Capture.StopRecording();
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// PCM サンプルが取得されたときに発生します。
            /// </summary>
            public event EventHandler<AudioSamplesCapturedEventArgs>? SamplesCaptured;

            /// <summary>
            /// 音声録音処理が終了したときに発生します。
            /// </summary>
            public event EventHandler? RecordingStopped;

            /// <summary>
            /// NAudio からの録音終了を取得管理側へ伝えます。
            /// </summary>
            /// <param name="sender">停止した NAudio の録音機です。</param>
            /// <param name="e">録音が停止した理由です。</param>
            private void OnRecordingStopped(object? sender, StoppedEventArgs e)
            {
                if (e.Exception is not null)
                {
                    System.Diagnostics.Trace.TraceError("{0}", e.Exception);
                }

                RecordingStopped?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// NAudio から届いた PCM バッファを内部イベントへ変換します。
            /// </summary>
            /// <param name="buffer">取得済み PCM バッファです。</param>
            /// <param name="flags">取得したパケットのフラグです。</param>
            /// <param name="devicePosition">デバイスでのサンプル位置です。</param>
            /// <param name="qpcPosition">パケットの計測時刻です。</param>
            private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
            {
                var samples = PcmSampleConverter.Convert(buffer, m_WaveFormat);
                SamplesCaptured?.Invoke(
                    this,
                    new AudioSamplesCapturedEventArgs(
                        samples,
                        m_WaveFormat.SampleRate,
                        m_WaveFormat.Channels,
                        DateTimeOffset.UtcNow));
            }

            #endregion
        }

        #endregion
    }
}
