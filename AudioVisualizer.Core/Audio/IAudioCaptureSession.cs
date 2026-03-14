using System;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声キャプチャ実体の開始、停止、および PCM サンプル通知を提供する内部契約です。
    /// </summary>
    internal interface IAudioCaptureSession : IDisposable
    {
        #region 公開メソッド

        /// <summary>
        /// 音声キャプチャを開始します。
        /// </summary>
        void Start();

        /// <summary>
        /// 音声キャプチャを停止します。
        /// </summary>
        void Stop();

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// PCM サンプルが取得されたときに発生します。
        /// </summary>
        event EventHandler<AudioSamplesCapturedEventArgs>? SamplesCaptured;

        #endregion
    }
}
