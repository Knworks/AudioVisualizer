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
        #region フィールド

        /// <summary>
        /// 直近に矩形化したバー型レンダーデータです。
        /// </summary>
        private BarSpectrumRenderData? m_LastRenderData;

        /// <summary>
        /// 直近に矩形化した描画領域サイズです。
        /// </summary>
        private Size m_LastRenderSize;

        /// <summary>
        /// 直近に生成した実描画用矩形一覧です。
        /// </summary>
        private IReadOnlyList<Rect>? m_LastRectangles;

        #endregion

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

            if (ReferenceEquals(m_LastRenderData, renderData) &&
                m_LastRenderSize.Equals(renderSize) &&
                m_LastRectangles is not null)
            {
                return m_LastRectangles;
            }

            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                m_LastRenderData = renderData;
                m_LastRenderSize = renderSize;
                m_LastRectangles = Array.Empty<Rect>();
                return m_LastRectangles;
            }

            var rectangles = new Rect[renderData.Bars.Count];
            for (var index = 0; index < renderData.Bars.Count; index++)
            {
                var bar = renderData.Bars[index];
                var width = Math.Max(0, bar.Width * renderSize.Width);
                if (bar.Height > 0 && width > 0 && width < 1.0)
                {
                    width = Math.Min(renderSize.Width, 1.0);
                }

                var height = Math.Max(0, bar.Height * renderSize.Height);
                if (bar.Height > 0 && height > 0 && height < 1.0)
                {
                    height = Math.Min(renderSize.Height, 1.0);
                }

                var x = Math.Clamp(bar.X * renderSize.Width, 0, Math.Max(0, renderSize.Width - width));
                var y = Math.Clamp(renderSize.Height - height, 0, Math.Max(0, renderSize.Height - height));
                rectangles[index] = new Rect(x, y, width, height);
            }

            m_LastRenderData = renderData;
            m_LastRenderSize = renderSize;
            m_LastRectangles = rectangles;
            return m_LastRectangles;
        }

        #endregion
    }
}
