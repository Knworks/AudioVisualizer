namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// 折れ線描画用のレンダーデータを表します。
    /// </summary>
    public sealed class PolylineRenderData : VisualizerRenderData
    {
        #region プロパティ

        /// <summary>
        /// 描画対象の点列を取得します。
        /// </summary>
        public IReadOnlyList<NormalizedPoint> Points { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="PolylineRenderData"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="effectId">生成元エフェクトの識別子です。</param>
        /// <param name="timestamp">レンダーデータの生成時刻です。</param>
        /// <param name="points">描画対象の点列です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="points"/> が <see langword="null"/> の場合にスローされます。</exception>
        public PolylineRenderData(string effectId, DateTimeOffset timestamp, IEnumerable<NormalizedPoint> points)
            : base(effectId, timestamp)
        {
            if (points is null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            Points = points.ToArray();
        }

        #endregion
    }
}
