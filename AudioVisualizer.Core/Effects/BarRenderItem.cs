using System;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// バー型スペクトラム描画で使用する 1 本分の正規化情報を表します。
    /// </summary>
    public sealed class BarRenderItem
    {
        #region プロパティ

        /// <summary>
        /// バーの左端位置を取得します。
        /// </summary>
        public double X { get; }

        /// <summary>
        /// バー幅を取得します。
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// バー高さを取得します。
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// 元のレベル値を取得します。
        /// </summary>
        public double Level { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="BarRenderItem"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="x">バーの左端位置です。</param>
        /// <param name="width">バー幅です。</param>
        /// <param name="height">バー高さです。</param>
        /// <param name="level">元のレベル値です。</param>
        /// <exception cref="ArgumentOutOfRangeException">正規化範囲外の値が指定された場合にスローされます。</exception>
        public BarRenderItem(double x, double width, double height, double level)
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

            if (level < 0 || level > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level), "Level は 0.0 から 1.0 の範囲で指定してください。");
            }

            X = x;
            Width = width;
            Height = height;
            Level = level;
        }

        #endregion
    }
}
