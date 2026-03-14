using System;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 解析済みフレーム通知イベントのデータを表します。
    /// </summary>
    public sealed class VisualizerFrameEventArgs : EventArgs
    {
        #region プロパティ

        /// <summary>
        /// 通知対象の解析済みフレームを取得します。
        /// </summary>
        public VisualizerFrame Frame { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerFrameEventArgs"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frame">通知対象の解析済みフレームです。</param>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerFrameEventArgs(VisualizerFrame frame)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        #endregion
    }
}
