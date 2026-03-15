using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// バー本体に加えてピーク保持線を生成する組込エフェクトです。
    /// </summary>
    internal sealed class PeakHoldBarEffect : IVisualizerEffect, IResettableVisualizerEffect
    {
        #region 定数

        /// <summary>
        /// フレームごとのピーク減衰量です。
        /// </summary>
        private const double PeakDecayPerFrame = 0.04;

        /// <summary>
        /// ピーク保持線の最小表示高さです。
        /// </summary>
        private const double MinimumMarkerHeight = 0.01;

        #endregion

        #region フィールド

        /// <summary>
        /// 高さ計算を再利用するための既定バーエフェクトです。
        /// </summary>
        private readonly SpectrumBarEffect m_SourceEffect = new();

        /// <summary>
        /// 直近フレームのピーク保持高さです。
        /// </summary>
        private double[] m_PeakHeights = Array.Empty<double>();

        #endregion

        #region プロパティ

        /// <summary>
        /// PeakHoldBar エフェクトのメタデータを取得します。
        /// </summary>
        public EffectMetadata Metadata { get; } = new(
            "peak-hold-bars",
            "Peak Hold Bars",
            "1.0.0",
            "AudioVisualizer",
            "バー本体とピーク保持線を描画する組込エフェクトです。",
            new[] { InputSource.SystemOutput, InputSource.Microphone });

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームをピーク保持バー描画用レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>ピーク保持バー描画用のレンダーデータです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> または <paramref name="context"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(context);

            var sourceRenderData = (BarSpectrumRenderData)m_SourceEffect.BuildRenderData(frame, context);
            if (sourceRenderData.Bars.Count == 0)
            {
                m_PeakHeights = Array.Empty<double>();
                return new PeakHoldBarRenderData(Metadata.Id, frame.Timestamp, Array.Empty<BarRenderItem>(), Array.Empty<PeakMarkerItem>());
            }

            EnsurePeakBufferSize(sourceRenderData.Bars.Count);

            var peakMarkers = new PeakMarkerItem[sourceRenderData.Bars.Count];
            for (var index = 0; index < sourceRenderData.Bars.Count; index++)
            {
                var bar = sourceRenderData.Bars[index];
                var nextPeakHeight = CalculateNextPeakHeight(m_PeakHeights[index], bar.Height);
                m_PeakHeights[index] = nextPeakHeight;
                peakMarkers[index] = new PeakMarkerItem(bar.X, bar.Width, Math.Max(nextPeakHeight, MinimumMarkerHeight));
            }

            return new PeakHoldBarRenderData(Metadata.Id, frame.Timestamp, sourceRenderData.Bars, peakMarkers);
        }

        /// <summary>
        /// 内部状態を初期化します。
        /// </summary>
        public void Reset()
        {
            m_PeakHeights = Array.Empty<double>();
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// ピーク保持バッファのサイズを現在のバー本数に合わせます。
        /// </summary>
        /// <param name="barCount">現在のバー本数です。</param>
        private void EnsurePeakBufferSize(int barCount)
        {
            if (m_PeakHeights.Length == barCount)
            {
                return;
            }

            m_PeakHeights = new double[barCount];
        }

        /// <summary>
        /// 次フレームのピーク保持高さを算出します。
        /// </summary>
        /// <param name="currentPeakHeight">現在のピーク保持高さです。</param>
        /// <param name="currentBarHeight">現在フレームのバー高さです。</param>
        /// <returns>次フレームのピーク保持高さです。</returns>
        private static double CalculateNextPeakHeight(double currentPeakHeight, double currentBarHeight)
        {
            if (currentBarHeight >= currentPeakHeight)
            {
                return currentBarHeight;
            }

            return Math.Max(currentBarHeight, currentPeakHeight - PeakDecayPerFrame);
        }

        #endregion
    }
}
