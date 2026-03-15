namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// ピーク保持位置の描画情報を表します。
    /// </summary>
    public sealed class PeakMarkerItem
    {
        #region プロパティ

        /// <summary>
        /// マーカーの左端位置を取得します。
        /// </summary>
        public double X { get; }

        /// <summary>
        /// マーカー幅を取得します。
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// マーカー高さを取得します。
        /// </summary>
        public double Height { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="PeakMarkerItem"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="x">マーカーの左端位置です。</param>
        /// <param name="width">マーカー幅です。</param>
        /// <param name="height">マーカー高さです。</param>
        /// <exception cref="ArgumentOutOfRangeException">正規化範囲外の値が指定された場合にスローされます。</exception>
        public PeakMarkerItem(double x, double width, double height)
        {
            if (x < 0 || x > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(x), "X は 0.0 から 1.0 の範囲で指定してください。");
            }

            if (width < 0 || width > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width は 0.0 から 1.0 の範囲で指定してください。");
            }

            if (height < 0 || height > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height は 0.0 から 1.0 の範囲で指定してください。");
            }

            X = x;
            Width = width;
            Height = height;
        }

        #endregion
    }
}
