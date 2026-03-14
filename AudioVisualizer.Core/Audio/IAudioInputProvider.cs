using System;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声入力の開始、停止、および解析済みフレーム通知を提供する契約を定義します。
    /// </summary>
    public interface IAudioInputProvider
    {
        #region プロパティ

        /// <summary>
        /// 現在音声入力が有効かどうかを示す値を取得します。
        /// </summary>
        bool IsCapturing { get; }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 音声入力を開始します。
        /// </summary>
        /// <param name="settings">入力開始に使用する設定です。</param>
        /// <returns>開始処理の結果です。</returns>
        AudioInputStartResult Start(VisualizerSettings settings);

        /// <summary>
        /// 音声入力を停止します。
        /// </summary>
        void Stop();

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 新しい解析済みフレームが生成されたときに発生します。
        /// </summary>
        event EventHandler<VisualizerFrameEventArgs>? FrameProduced;

        #endregion
    }
}
