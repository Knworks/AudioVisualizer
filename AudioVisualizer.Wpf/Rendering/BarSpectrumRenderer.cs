using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using AudioVisualizer.Core.Effects;

namespace AudioVisualizer.Wpf.Rendering
{
    /// <summary>
    /// バー型スペクトラムレンダーデータを WPF 描画へ変換します。
    /// </summary>
    internal sealed class BarSpectrumRenderer
    {
        #region 公開メソッド

        /// <summary>
        /// バー型スペクトラムレンダーデータを描画します。
        /// </summary>
        /// <param name="drawingContext">描画先の DrawingContext です。</param>
        /// <param name="renderData">描画対象のレンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <param name="fillBrush">バー描画に使用するブラシです。</param>
        public void Render(DrawingContext drawingContext, VisualizerRenderData? renderData, Size renderSize, Brush fillBrush)
        {
            ArgumentNullException.ThrowIfNull(drawingContext);
            ArgumentNullException.ThrowIfNull(fillBrush);

            if (renderData is not BarSpectrumRenderData barSpectrumRenderData)
            {
                return;
            }

            var rectangles = CreateBarRectangles(barSpectrumRenderData, renderSize);
            for (var index = 0; index < rectangles.Count; index++)
            {
                drawingContext.DrawRectangle(fillBrush, null, rectangles[index]);
            }
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// バー型レンダーデータから実描画用矩形一覧を生成します。
        /// </summary>
        /// <param name="renderData">バー型スペクトラムのレンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <returns>実描画に使用する矩形一覧です。</returns>
        internal IReadOnlyList<Rect> CreateBarRectangles(BarSpectrumRenderData renderData, Size renderSize)
        {
            ArgumentNullException.ThrowIfNull(renderData);

            var rectangles = new List<Rect>(renderData.Bars.Count);
            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                return rectangles;
            }

            for (var index = 0; index < renderData.Bars.Count; index++)
            {
                var bar = renderData.Bars[index];
                var width = Math.Max(0, bar.Width * renderSize.Width);
                var height = Math.Max(0, bar.Height * renderSize.Height);
                var x = bar.X * renderSize.Width;
                var y = renderSize.Height - height;
                rectangles.Add(new Rect(x, y, width, height));
            }

            return rectangles;
        }

        #endregion
    }
}
