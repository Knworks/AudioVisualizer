using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// スペクトラム値を固定帯域メーター表示へ変換する組込エフェクトです。
    /// </summary>
    internal sealed class BandLevelMeterEffect : IVisualizerEffect
    {
        #region 定数

        /// <summary>
        /// 帯域メーター幅の比率です。
        /// </summary>
        private const double MeterWidthRatio = 0.72;

        /// <summary>
        /// 固定表示する帯域ラベルです。
        /// </summary>
        private static readonly string[] s_BandLabels =
        {
            "Low",
            "LowMid",
            "HighMid",
            "High",
        };

        #endregion

        #region プロパティ

        /// <summary>
        /// BandLevelMeter エフェクトのメタデータを取得します。
        /// </summary>
        public EffectMetadata Metadata { get; } = new(
            "band-level-meter",
            "Band Level Meter",
            "1.0.0",
            "AudioVisualizer",
            "固定帯域ごとのレベルをメーターで表示する組込エフェクトです。",
            new[] { InputSource.SystemOutput, InputSource.Microphone });

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームを固定帯域メーター描画用レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>固定帯域メーター描画用のレンダーデータです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> または <paramref name="context"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(context);

            var slotWidth = 1.0 / s_BandLabels.Length;
            var actualMeterWidth = slotWidth * MeterWidthRatio;
            var xOffset = (slotWidth - actualMeterWidth) / 2.0;
            var bands = new BandLevelMeterItem[s_BandLabels.Length];

            for (var index = 0; index < s_BandLabels.Length; index++)
            {
                var level = Math.Clamp(
                    SampleBandLevel(frame.SpectrumValues, index, s_BandLabels.Length) * context.Sensitivity,
                    0.0,
                    1.0);
                bands[index] = new BandLevelMeterItem((slotWidth * index) + xOffset, actualMeterWidth, level, s_BandLabels[index]);
            }

            return new BandLevelMeterRenderData(Metadata.Id, frame.Timestamp, bands);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// スペクトラム値から対象帯域の平均レベルを抽出します。
        /// </summary>
        /// <param name="spectrumValues">解析済みスペクトラム値です。</param>
        /// <param name="bandIndex">抽出対象の帯域番号です。</param>
        /// <param name="bandCount">帯域総数です。</param>
        /// <returns>0.0 から 1.0 の帯域レベルです。</returns>
        private static double SampleBandLevel(IReadOnlyList<double> spectrumValues, int bandIndex, int bandCount)
        {
            if (spectrumValues.Count == 0 || bandCount <= 0)
            {
                return 0.0;
            }

            var startIndex = (int)Math.Floor((double)(bandIndex * spectrumValues.Count) / bandCount);
            var endIndex = (int)Math.Floor((double)((bandIndex + 1) * spectrumValues.Count) / bandCount);
            if (endIndex <= startIndex)
            {
                endIndex = Math.Min(spectrumValues.Count, startIndex + 1);
            }

            var total = 0.0;
            var count = 0;
            for (var index = startIndex; index < endIndex; index++)
            {
                total += spectrumValues[index];
                count++;
            }

            return count == 0
                ? 0.0
                : total / count;
        }

        #endregion
    }
}
