namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// 帯域メーター 1 本分の描画情報を表します。
    /// </summary>
    public sealed class BandLevelMeterItem
    {
        #region プロパティ

        /// <summary>
        /// メーターの左端位置を取得します。
        /// </summary>
        public double X { get; }

        /// <summary>
        /// メーター幅を取得します。
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// 帯域レベルを取得します。
        /// </summary>
        public double Level { get; }

        /// <summary>
        /// 表示ラベルを取得します。
        /// </summary>
        public string Label { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="BandLevelMeterItem"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="x">メーターの左端位置です。</param>
        /// <param name="width">メーター幅です。</param>
        /// <param name="level">帯域レベルです。</param>
        /// <param name="label">表示ラベルです。</param>
        /// <exception cref="ArgumentException"><paramref name="label"/> が空白の場合にスローされます。</exception>
        /// <exception cref="ArgumentOutOfRangeException">正規化範囲外の値が指定された場合にスローされます。</exception>
        public BandLevelMeterItem(double x, double width, double level, string label)
        {
            if (x < 0 || x > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "X は 0.0 から 1.0 の範囲で指定してください。");
            }

            if (width < 0 || width > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width は 0.0 から 1.0 の範囲で指定してください。");
            }

            if (level < 0 || level > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level は 0.0 から 1.0 の範囲で指定してください。");
            }

            Label = string.IsNullOrWhiteSpace(label)
                ? throw new ArgumentException("Label は空白にできません。", nameof(label))
                : label;

            X = x;
            Width = width;
            Level = level;
        }

        #endregion
    }
}
