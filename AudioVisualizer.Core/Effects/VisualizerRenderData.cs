using System;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// ビジュアライザーエフェクトが生成する WPF 非依存の描画指示を表します。
    /// </summary>
    public abstract class VisualizerRenderData
    {
        #region プロパティ

        /// <summary>
        /// 描画指示を生成したエフェクトの識別子を取得します。
        /// </summary>
        public string EffectId { get; }

        /// <summary>
        /// 描画指示に紐づく時刻を取得します。
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerRenderData"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="effectId">描画指示を生成したエフェクトの識別子です。</param>
        /// <param name="timestamp">描画指示に紐づく時刻です。</param>
        /// <exception cref="ArgumentException"><paramref name="effectId"/> が空白の場合にスローされます。</exception>
        protected VisualizerRenderData(string effectId, DateTimeOffset timestamp)
        {
            EffectId = string.IsNullOrWhiteSpace(effectId)
                ? throw new ArgumentException("Effect ID must not be blank.", nameof(effectId))
                : effectId;
            Timestamp = timestamp;
        }

        #endregion
    }
}
