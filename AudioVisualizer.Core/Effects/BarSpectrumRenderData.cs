using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// バー型スペクトラム描画用のレンダーデータを表します。
    /// </summary>
    public sealed class BarSpectrumRenderData : VisualizerRenderData
    {
        #region プロパティ

        /// <summary>
        /// 描画対象のバー一覧を取得します。
        /// </summary>
        public IReadOnlyList<BarRenderItem> Bars { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="BarSpectrumRenderData"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="effectId">生成元エフェクトの識別子です。</param>
        /// <param name="timestamp">レンダーデータの生成時刻です。</param>
        /// <param name="bars">描画対象のバー一覧です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="bars"/> が <see langword="null"/> の場合にスローされます。</exception>
        public BarSpectrumRenderData(string effectId, DateTimeOffset timestamp, IEnumerable<BarRenderItem> bars)
            : base(effectId, timestamp)
        {
            if (bars is null)
            {
                throw new ArgumentNullException(nameof(bars));
            }

            Bars = bars.ToArray();
        }

        #endregion
    }
}
