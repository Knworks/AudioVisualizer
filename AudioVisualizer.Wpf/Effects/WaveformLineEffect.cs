using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// 波形値を折れ線描画用のレンダーデータへ変換する組込エフェクトです。
    /// </summary>
    internal sealed class WaveformLineEffect : IVisualizerEffect
    {
        #region プロパティ

        /// <summary>
        /// 波形線エフェクトのメタデータを取得します。
        /// </summary>
        public EffectMetadata Metadata { get; } = new(
            "waveform-line",
            "Waveform Line",
            "1.0.0",
            "AudioVisualizer",
            "波形値を折れ線で描画する組込エフェクトです。",
            new[] { InputSource.SystemOutput, InputSource.Microphone });

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームを折れ線描画用レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>折れ線描画用のレンダーデータです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> または <paramref name="context"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(context);

            if (frame.WaveformRanges.Count > 1)
            {
                return BuildEnvelope(frame, context.Sensitivity);
            }

            if (frame.WaveformValues.Count == 0)
            {
                return new PolylineRenderData(Metadata.Id, frame.Timestamp, Array.Empty<NormalizedPoint>());
            }

            if (frame.WaveformValues.Count == 1)
            {
                var y = ConvertToNormalizedY(frame.WaveformValues[0] * context.Sensitivity);
                return new PolylineRenderData(
                    Metadata.Id,
                    frame.Timestamp,
                    new[]
                    {
                        new NormalizedPoint(0.0, y),
                        new NormalizedPoint(1.0, y),
                    });
            }

            var points = new NormalizedPoint[frame.WaveformValues.Count];
            for (var index = 0; index < frame.WaveformValues.Count; index++)
            {
                var x = index / (double)(frame.WaveformValues.Count - 1);
                var y = ConvertToNormalizedY(frame.WaveformValues[index] * context.Sensitivity);
                points[index] = new NormalizedPoint(x, y);
            }

            return new PolylineRenderData(Metadata.Id, frame.Timestamp, points);
        }

        #endregion

        #region 内部メソッド

        /// <summary>
        /// 間引き前の最大値と最小値を、時間順に上下の輪郭として描画します。
        /// </summary>
        /// <param name="frame">波形範囲を格納したフレームです。</param>
        /// <param name="sensitivity">描画時の感度です。</param>
        /// <returns>振幅の上下の輪郭を結んだ折れ線です。</returns>
        private PolylineRenderData BuildEnvelope(VisualizerFrame frame, double sensitivity)
        {
            var count = frame.WaveformRanges.Count;
            var points = new NormalizedPoint[(count * 2) + 1];
            for (var index = 0; index < count; index++)
            {
                var range = frame.WaveformRanges[index];
                var x = index / (double)(count - 1);
                points[index] = new NormalizedPoint(x, ConvertToNormalizedY(range.Maximum * sensitivity));
                points[(2 * count) - 1 - index] = new NormalizedPoint(x, ConvertToNormalizedY(range.Minimum * sensitivity));
            }

            points[^1] = points[0];
            return new PolylineRenderData(Metadata.Id, frame.Timestamp, points);
        }

        /// <summary>
        /// 波形値を描画用の正規化 Y 座標へ変換します。
        /// </summary>
        /// <param name="waveformValue">-1.0 から 1.0 を想定した波形値です。</param>
        /// <returns>上端 0.0、下端 1.0 の正規化 Y 座標です。</returns>
        private static double ConvertToNormalizedY(double waveformValue)
        {
            return Math.Clamp(0.5 - (Math.Clamp(waveformValue, -1.0, 1.0) / 2.0), 0.0, 1.0);
        }

        #endregion
    }
}
