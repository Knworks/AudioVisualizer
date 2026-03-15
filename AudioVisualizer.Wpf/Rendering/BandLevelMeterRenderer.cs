using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AudioVisualizer.Core.Effects;

namespace AudioVisualizer.Wpf.Rendering
{
    /// <summary>
    /// 固定帯域メーターレンダーデータを WPF 描画へ変換します。
    /// </summary>
    internal sealed class BandLevelMeterRenderer
    {
        #region 定数

        /// <summary>
        /// ラベル表示に確保する高さです。
        /// </summary>
        private const double LabelAreaHeight = 18.0;

        /// <summary>
        /// ラベル描画に使用する文字サイズです。
        /// </summary>
        private const double LabelFontSize = 11.0;

        /// <summary>
        /// ラベルの上側余白です。
        /// </summary>
        private const double LabelTopMargin = 2.0;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 固定帯域メーターレンダーデータを描画します。
        /// </summary>
        /// <param name="drawingContext">描画先の DrawingContext です。</param>
        /// <param name="renderData">描画対象のレンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <param name="fillBrush">メーター描画に使用するブラシです。</param>
        public void Render(DrawingContext drawingContext, VisualizerRenderData? renderData, Size renderSize, Brush fillBrush)
        {
            ArgumentNullException.ThrowIfNull(drawingContext);
            ArgumentNullException.ThrowIfNull(fillBrush);

            if (renderData is not BandLevelMeterRenderData bandLevelMeterRenderData)
            {
                return;
            }

            var rectangles = CreateMeterRectangles(bandLevelMeterRenderData, renderSize);
            for (var index = 0; index < rectangles.Count; index++)
            {
                var rectangle = rectangles[index];
                if (rectangle.Width > 0 && rectangle.Height > 0)
                {
                    drawingContext.DrawRectangle(fillBrush, null, rectangle);
                }
            }

            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                return;
            }

            var meterAreaHeight = Math.Max(0.0, renderSize.Height - LabelAreaHeight);
            for (var index = 0; index < bandLevelMeterRenderData.Bands.Count; index++)
            {
                var band = bandLevelMeterRenderData.Bands[index];
                var formattedText = CreateFormattedText(band.Label, fillBrush);
                var bandWidth = band.Width * renderSize.Width;
                var bandX = band.X * renderSize.Width;
                var textX = Math.Clamp(
                    bandX + ((bandWidth - formattedText.Width) / 2.0),
                    0.0,
                    Math.Max(0.0, renderSize.Width - formattedText.Width));
                var textY = Math.Min(renderSize.Height - formattedText.Height, meterAreaHeight + LabelTopMargin);
                drawingContext.DrawText(formattedText, new Point(textX, textY));
            }
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 固定帯域メーターレンダーデータからメーター矩形一覧を生成します。
        /// </summary>
        /// <param name="renderData">固定帯域メーターレンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <returns>メーター描画に使用する矩形一覧です。</returns>
        internal IReadOnlyList<Rect> CreateMeterRectangles(BandLevelMeterRenderData renderData, Size renderSize)
        {
            ArgumentNullException.ThrowIfNull(renderData);

            if (renderSize.Width <= 0 || renderSize.Height <= 0)
            {
                return Array.Empty<Rect>();
            }

            var meterAreaHeight = Math.Max(0.0, renderSize.Height - LabelAreaHeight);
            var rectangles = new Rect[renderData.Bands.Count];
            for (var index = 0; index < renderData.Bands.Count; index++)
            {
                var band = renderData.Bands[index];
                var width = Math.Max(0.0, band.Width * renderSize.Width);
                if (band.Level > 0 && width > 0 && width < 1.0)
                {
                    width = Math.Min(renderSize.Width, 1.0);
                }

                var height = Math.Max(0.0, band.Level * meterAreaHeight);
                if (band.Level > 0 && height > 0 && height < 1.0)
                {
                    height = Math.Min(meterAreaHeight, 1.0);
                }

                var x = Math.Clamp(band.X * renderSize.Width, 0.0, Math.Max(0.0, renderSize.Width - width));
                var y = Math.Clamp(meterAreaHeight - height, 0.0, Math.Max(0.0, meterAreaHeight - height));
                rectangles[index] = new Rect(x, y, width, height);
            }

            return rectangles;
        }

        /// <summary>
        /// 描画用のラベルテキストを生成します。
        /// </summary>
        /// <param name="label">表示ラベルです。</param>
        /// <param name="brush">文字色に使用するブラシです。</param>
        /// <returns>描画用の整形済みテキストです。</returns>
        private static FormattedText CreateFormattedText(string label, Brush brush)
        {
            return new FormattedText(
                label,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                LabelFontSize,
                brush,
                1.0);
        }

        #endregion
    }
}
