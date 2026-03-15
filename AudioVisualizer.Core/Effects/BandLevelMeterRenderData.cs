namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// 固定帯域メーター描画用のレンダーデータを表します。
    /// </summary>
    public sealed class BandLevelMeterRenderData : VisualizerRenderData
    {
        #region プロパティ

        /// <summary>
        /// 描画対象の帯域一覧を取得します。
        /// </summary>
        public IReadOnlyList<BandLevelMeterItem> Bands { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="BandLevelMeterRenderData"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="effectId">生成元エフェクトの識別子です。</param>
        /// <param name="timestamp">レンダーデータの生成時刻です。</param>
        /// <param name="bands">描画対象の帯域一覧です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="bands"/> が <see langword="null"/> の場合にスローされます。</exception>
        public BandLevelMeterRenderData(string effectId, DateTimeOffset timestamp, IEnumerable<BandLevelMeterItem> bands)
            : base(effectId, timestamp)
        {
            if (bands is null)
            {
                throw new ArgumentNullException(nameof(bands));
            }

            Bands = bands.ToArray();
        }

        #endregion
    }
}
