namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// バー本体とピーク保持マーカーを併せ持つレンダーデータを表します。
    /// </summary>
    public sealed class PeakHoldBarRenderData : VisualizerRenderData
    {
        #region プロパティ

        /// <summary>
        /// 描画対象のバー一覧を取得します。
        /// </summary>
        public IReadOnlyList<BarRenderItem> Bars { get; }

        /// <summary>
        /// 描画対象のピーク保持マーカー一覧を取得します。
        /// </summary>
        public IReadOnlyList<PeakMarkerItem> PeakMarkers { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="PeakHoldBarRenderData"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="effectId">生成元エフェクトの識別子です。</param>
        /// <param name="timestamp">レンダーデータの生成時刻です。</param>
        /// <param name="bars">描画対象のバー一覧です。</param>
        /// <param name="peakMarkers">描画対象のピーク保持マーカー一覧です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="bars"/> または <paramref name="peakMarkers"/> が <see langword="null"/> の場合にスローされます。</exception>
        public PeakHoldBarRenderData(
            string effectId,
            DateTimeOffset timestamp,
            IEnumerable<BarRenderItem> bars,
            IEnumerable<PeakMarkerItem> peakMarkers)
            : base(effectId, timestamp)
        {
            if (bars is null)
            {
                throw new ArgumentNullException(nameof(bars));
            }

            if (peakMarkers is null)
            {
                throw new ArgumentNullException(nameof(peakMarkers));
            }

            Bars = bars.ToArray();
            PeakMarkers = peakMarkers.ToArray();
        }

        #endregion
    }
}
