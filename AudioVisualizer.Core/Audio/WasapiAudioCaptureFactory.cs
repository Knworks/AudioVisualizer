using System;
using System.Diagnostics.CodeAnalysis;
using AudioVisualizer.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;
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
        public IAudioCaptureSession CreateCapture(InputSource inputSource, string deviceId)
        {
            using var deviceEnumerator = new MMDeviceEnumerator();
            var device = deviceEnumerator.GetDevice(deviceId);

            return inputSource switch
            {
                InputSource.SystemOutput => new WasapiCaptureSession(new WasapiLoopbackCapture(device)),
                InputSource.Microphone => new WasapiCaptureSession(new WasapiCapture(device)),
                _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
            };
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// NAudio の WASAPI キャプチャを内部契約へ適合させるラッパーです。
        /// </summary>
        private sealed class WasapiCaptureSession : IAudioCaptureSession
        {
            #region フィールド

            /// <summary>
            /// 実際の WASAPI キャプチャです。
            /// </summary>
            private readonly IWaveIn m_Capture;

            /// <summary>
            /// PCM 変換に使用する音声フォーマットです。
            /// </summary>
            private readonly WaveFormat m_WaveFormat;

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="WasapiCaptureSession"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="capture">ラップ対象の WASAPI キャプチャです。</param>
            public WasapiCaptureSession(IWaveIn capture)
            {
                m_Capture = capture ?? throw new ArgumentNullException(nameof(capture));
                m_WaveFormat = capture.WaveFormat;
                m_Capture.DataAvailable += OnDataAvailable;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 利用中のキャプチャ資源を解放します。
            /// </summary>
            public void Dispose()
            {
                m_Capture.DataAvailable -= OnDataAvailable;
                m_Capture.Dispose();
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
            /// NAudio から届いた PCM バッファを内部イベントへ変換します。
            /// </summary>
            /// <param name="sender">イベント送信元です。</param>
            /// <param name="e">取得済み PCM バッファです。</param>
            private void OnDataAvailable(object? sender, WaveInEventArgs e)
            {
                var samples = ConvertToSamples(e.Buffer, e.BytesRecorded, m_WaveFormat);
                SamplesCaptured?.Invoke(
                    this,
                    new AudioSamplesCapturedEventArgs(
                        samples,
                        m_WaveFormat.SampleRate,
                        m_WaveFormat.Channels,
                        DateTimeOffset.UtcNow));
            }

            #endregion

            #region 内部処理

            /// <summary>
            /// 受信した PCM バッファを単精度サンプル配列へ変換します。
            /// </summary>
            /// <param name="buffer">変換対象の PCM バッファです。</param>
            /// <param name="bytesRecorded">有効なバイト数です。</param>
            /// <param name="waveFormat">変換に使用する音声フォーマットです。</param>
            /// <returns>単精度サンプル配列です。</returns>
            private static float[] ConvertToSamples(byte[] buffer, int bytesRecorded, WaveFormat waveFormat)
            {
                if (waveFormat.Encoding == WaveFormatEncoding.IeeeFloat && waveFormat.BitsPerSample == 32)
                {
                    var result = new float[bytesRecorded / sizeof(float)];
                    Buffer.BlockCopy(buffer, 0, result, 0, bytesRecorded);
                    return result;
                }

                if (waveFormat.BitsPerSample == 16)
                {
                    var sampleCount = bytesRecorded / sizeof(short);
                    var result = new float[sampleCount];
                    for (var index = 0; index < sampleCount; index++)
                    {
                        var value = BitConverter.ToInt16(buffer, index * sizeof(short));
                        result[index] = value / (float)short.MaxValue;
                    }

                    return result;
                }

                throw new NotSupportedException("サポートしていない音声フォーマットです。");
            }

            #endregion
        }

        #endregion
    }
}
