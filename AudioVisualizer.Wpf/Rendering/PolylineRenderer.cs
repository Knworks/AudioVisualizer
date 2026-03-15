using System.Windows;
using System.Windows.Media;
using AudioVisualizer.Core.Effects;

namespace AudioVisualizer.Wpf.Rendering
{
    /// <summary>
    /// 折れ線レンダーデータを WPF 描画へ変換します。
    /// </summary>
    internal sealed class PolylineRenderer
    {
        #region 定数

        /// <summary>
        /// 線の端切れを抑えるために確保する最小余白です。
        /// </summary>
        private const double EdgePadding = 0.5;

        /// <summary>
        /// 折れ線描画に使用する線幅です。
        /// </summary>
        private const double StrokeThickness = 1.5;

        #endregion

        #region フィールド

        /// <summary>
        /// 直近に点列化した折れ線レンダーデータです。
        /// </summary>
        private PolylineRenderData? m_LastRenderData;

        /// <summary>
        /// 直近に点列化した描画領域サイズです。
        /// </summary>
        private Size m_LastRenderSize;

        /// <summary>
        /// 直近に生成した実描画用の点列です。
        /// </summary>
        private IReadOnlyList<Point>? m_LastRenderPoints;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 折れ線レンダーデータを描画します。
        /// </summary>
        /// <param name="drawingContext">描画先の DrawingContext です。</param>
        /// <param name="renderData">描画対象のレンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <param name="strokeBrush">線描画に使用するブラシです。</param>
        public void Render(DrawingContext drawingContext, VisualizerRenderData? renderData, Size renderSize, Brush strokeBrush)
        {
            ArgumentNullException.ThrowIfNull(drawingContext);
            ArgumentNullException.ThrowIfNull(strokeBrush);

            if (renderData is not PolylineRenderData polylineRenderData)
            {
                return;
            }

            var renderPoints = CreateRenderPoints(polylineRenderData, renderSize);
            if (renderPoints.Count < 2)
            {
                return;
            }

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(renderPoints[0], false, false);
                context.PolyLineTo(renderPoints.Skip(1).ToArray(), true, false);
            }

            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            var pen = new Pen(strokeBrush, StrokeThickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };
            if (pen.CanFreeze)
            {
                pen.Freeze();
            }

            drawingContext.DrawGeometry(null, pen, geometry);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 折れ線レンダーデータを実描画用の点列へ変換します。
        /// </summary>
        /// <param name="renderData">折れ線レンダーデータです。</param>
        /// <param name="renderSize">描画領域サイズです。</param>
        /// <returns>実描画に使用する点列です。</returns>
        internal IReadOnlyList<Point> CreateRenderPoints(PolylineRenderData renderData, Size renderSize)
        {
            ArgumentNullException.ThrowIfNull(renderData);

            if (ReferenceEquals(m_LastRenderData, renderData) &&
                m_LastRenderSize.Equals(renderSize) &&
                m_LastRenderPoints is not null)
            {
                return m_LastRenderPoints;
            }

            if (renderSize.Width <= 0 || renderSize.Height <= 0 || renderData.Points.Count == 0)
            {
                m_LastRenderData = renderData;
                m_LastRenderSize = renderSize;
                m_LastRenderPoints = Array.Empty<Point>();
                return m_LastRenderPoints;
            }

            var points = new Point[renderData.Points.Count];
            for (var index = 0; index < renderData.Points.Count; index++)
            {
                var point = renderData.Points[index];
                points[index] = new Point(
                    MapToPixelCoordinate(point.X, renderSize.Width),
                    MapToPixelCoordinate(point.Y, renderSize.Height));
            }

            m_LastRenderData = renderData;
            m_LastRenderSize = renderSize;
            m_LastRenderPoints = points;
            return m_LastRenderPoints;
        }

        /// <summary>
        /// 正規化座標を描画領域内の実座標へ変換します。
        /// </summary>
        /// <param name="normalizedValue">0.0 から 1.0 の正規化値です。</param>
        /// <param name="length">変換先の辺長です。</param>
        /// <returns>描画領域内に収まる実座標です。</returns>
        private static double MapToPixelCoordinate(double normalizedValue, double length)
        {
            if (length <= 0)
            {
                return 0;
            }

            var clampedValue = Math.Clamp(normalizedValue, 0.0, 1.0);
            if (length <= 1.0)
            {
                return clampedValue * length;
            }

            return EdgePadding + (clampedValue * Math.Max(0.0, length - (EdgePadding * 2.0)));
        }

        #endregion
    }
}
