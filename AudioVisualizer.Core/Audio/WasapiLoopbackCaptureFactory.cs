using System;
using System.Diagnostics.CodeAnalysis;
using NAudio.Wasapi;
using NAudio.Wave;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// NAudio の WASAPI ループバックキャプチャを生成します。
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class WasapiLoopbackCaptureFactory : IAudioCaptureFactory
    {
        #region 公開メソッド

        /// <summary>
        /// システム再生音用の WASAPI ループバックキャプチャを生成します。
        /// </summary>
        /// <returns>WASAPI ループバックキャプチャをラップしたキャプチャ実体です。</returns>
        public IAudioCaptureSession CreateSystemOutputCapture()
        {
            return new WasapiLoopbackCaptureSession(new WasapiLoopbackCapture());
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// NAudio のループバックキャプチャを内部契約へ適合させるラッパーです。
        /// </summary>
        private sealed class WasapiLoopbackCaptureSession : IAudioCaptureSession
        {
            #region フィールド

            /// <summary>
            /// 実際のシステム再生音キャプチャです。
            /// </summary>
            private readonly WasapiLoopbackCapture m_Capture;

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="WasapiLoopbackCaptureSession"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="capture">ラップ対象の WASAPI ループバックキャプチャです。</param>
            public WasapiLoopbackCaptureSession(WasapiLoopbackCapture capture)
            {
                m_Capture = capture ?? throw new ArgumentNullException(nameof(capture));
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
            /// システム再生音の録音を開始します。
            /// </summary>
            public void Start()
            {
                m_Capture.StartRecording();
            }

            /// <summary>
            /// システム再生音の録音を停止します。
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
                var samples = ConvertToSamples(e.Buffer, e.BytesRecorded, m_Capture.WaveFormat);
                SamplesCaptured?.Invoke(
                    this,
                    new AudioSamplesCapturedEventArgs(
                        samples,
                        m_Capture.WaveFormat.SampleRate,
                        m_Capture.WaveFormat.Channels,
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
