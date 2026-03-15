using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// スペクトラム値を中心対称のバー配置へ変換する組込エフェクトです。
    /// </summary>
    internal sealed class MirrorBarEffect : IVisualizerEffect
    {
        #region 定数

        /// <summary>
        /// 各バー区画に対する実バー幅の比率です。
        /// </summary>
        private const double BarWidthRatio = 0.8;

        #endregion

        #region フィールド

        /// <summary>
        /// 高さ計算を再利用するための既定バーエフェクトです。
        /// </summary>
        private readonly SpectrumBarEffect m_SourceEffect = new();

        #endregion

        #region プロパティ

        /// <summary>
        /// MirrorBar エフェクトのメタデータを取得します。
        /// </summary>
        public EffectMetadata Metadata { get; } = new(
            "mirror-bars",
            "Mirror Bars",
            "1.0.0",
            "AudioVisualizer",
            "中心から左右対称にバーを配置する組込エフェクトです。",
            new[] { InputSource.SystemOutput, InputSource.Microphone });

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームを中心対称バー描画用レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>中心対称バー描画用のレンダーデータです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> または <paramref name="context"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(context);

            if (context.BarCount == 0 || frame.SpectrumValues.Count == 0)
            {
                return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, Array.Empty<BarRenderItem>());
            }

            var pairCount = context.BarCount / 2;
            var hasCenterBar = (context.BarCount % 2) == 1;
            var sourceBarCount = pairCount + (hasCenterBar ? 1 : 0);
            var mirrorContext = new VisualizerEffectContext(
                context.InputSource,
                context.Sensitivity,
                context.Smoothing,
                sourceBarCount,
                context.SpectrumProfile);
            var sourceBars = ((BarSpectrumRenderData)m_SourceEffect.BuildRenderData(frame, mirrorContext)).Bars;

            var slotWidth = 1.0 / context.BarCount;
            var actualBarWidth = slotWidth * BarWidthRatio;
            var xOffset = (slotWidth - actualBarWidth) / 2.0;
            var mirroredBars = new BarRenderItem[context.BarCount];

            for (var pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                var sourceBar = sourceBars[pairIndex];
                var leftIndex = pairCount - pairIndex - 1;
                var rightIndex = hasCenterBar ? pairCount + pairIndex + 1 : pairCount + pairIndex;
                mirroredBars[leftIndex] = new BarRenderItem((slotWidth * leftIndex) + xOffset, actualBarWidth, sourceBar.Height, sourceBar.Level);
                mirroredBars[rightIndex] = new BarRenderItem((slotWidth * rightIndex) + xOffset, actualBarWidth, sourceBar.Height, sourceBar.Level);
            }

            if (hasCenterBar)
            {
                var centerIndex = pairCount;
                var centerBar = sourceBars[^1];
                mirroredBars[centerIndex] = new BarRenderItem((slotWidth * centerIndex) + xOffset, actualBarWidth, centerBar.Height, centerBar.Level);
            }

            return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, mirroredBars);
        }

        #endregion
    }
}
