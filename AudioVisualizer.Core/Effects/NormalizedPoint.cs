namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// 正規化座標系で扱う 1 点を表します。
    /// </summary>
    public sealed class NormalizedPoint
    {
        #region プロパティ

        /// <summary>
        /// X 座標を取得します。
        /// 0.0 が左端、1.0 が右端です。
        /// </summary>
        public double X { get; }

        /// <summary>
        /// Y 座標を取得します。
        /// 0.0 が上端、1.0 が下端です。
        /// </summary>
        public double Y { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="NormalizedPoint"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="x">X 座標です。0.0 から 1.0 の範囲で指定します。</param>
        /// <param name="y">Y 座標です。0.0 から 1.0 の範囲で指定します。</param>
        /// <exception cref="ArgumentOutOfRangeException">正規化範囲外の値が指定された場合にスローされます。</exception>
        public NormalizedPoint(double x, double y)
        {
            if (x < 0 || x > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "X は 0.0 から 1.0 の範囲で指定してください。");
            }

            if (y < 0 || y > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "Y は 0.0 から 1.0 の範囲で指定してください。");
            }

            X = x;
            Y = y;
        }

        #endregion
    }
}
