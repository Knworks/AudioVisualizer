using System;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 解析済みフレーム通知イベントのデータを表します。
    /// </summary>
    public sealed class VisualizerFrameEventArgs : EventArgs
    {
        /// <summary>
        /// フレームの取得セッションがまだ有効かを判定します。
        /// </summary>
        private readonly Func<bool>? m_IsCurrent;

        #region プロパティ

        /// <summary>
        /// 通知対象の解析済みフレームを取得します。
        /// </summary>
        public VisualizerFrame Frame { get; }

        /// <summary>
        /// 通知元の取得セッションがまだ有効かどうかを取得します。
        /// </summary>
        public bool IsCurrent => m_IsCurrent?.Invoke() ?? true;

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerFrameEventArgs"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="frame">通知対象の解析済みフレームです。</param>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerFrameEventArgs(VisualizerFrame frame)
            : this(frame, null)
        {
        }

        /// <summary>
        /// 取得セッションの有効性を確認できるフレーム通知を作成します。
        /// </summary>
        /// <param name="frame">通知対象のフレームです。</param>
        /// <param name="isCurrent">通知元の取得セッションが有効かを判定する処理です。</param>
        internal VisualizerFrameEventArgs(VisualizerFrame frame, Func<bool>? isCurrent)
        {
            Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_IsCurrent = isCurrent;
        }

        #endregion
    }
}
