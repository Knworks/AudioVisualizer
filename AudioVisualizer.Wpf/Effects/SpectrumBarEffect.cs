using System;
using System.Collections.Generic;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Wpf.Effects
{
    /// <summary>
    /// スペクトラム値をバー型レンダーデータへ変換する既定エフェクトです。
    /// </summary>
    internal sealed class SpectrumBarEffect : IVisualizerEffect
    {
        #region 定数

        /// <summary>
        /// 各バー区画に対する実バー幅の比率です。
        /// </summary>
        private const double BarWidthRatio = 0.8;

        /// <summary>
        /// Balanced プロファイルで使用する位置補正係数です。
        /// `1.0` に近いほど元の分布を残し、少しだけ高域側の割当を広げます。
        /// </summary>
        private const double BalancedPositionExponent = 0.8;

        /// <summary>
        /// Balanced プロファイルで使用する圧縮係数です。
        /// 小さい成分を控えめに持ち上げ、HighBoost ほど強くは補正しません。
        /// </summary>
        private const double BalancedCompressionExponent = 0.82;

        /// <summary>
        /// Balanced プロファイルで使用する高域補正倍率です。
        /// 高域の見え方を少し改善するための穏やかなゲインです。
        /// </summary>
        private const double BalancedHighBandGain = 0.3;

        /// <summary>
        /// Balanced プロファイルで使用する中域補正倍率です。
        /// 中域が沈み込みやすい音源でも、真ん中のバーが埋もれにくいように補正します。
        /// </summary>
        private const double BalancedMidBandGain = 0.22;

        /// <summary>
        /// HighBoost プロファイルで使用する位置補正係数です。
        /// </summary>
        private const double HighBoostPositionExponent = 0.58;

        /// <summary>
        /// HighBoost プロファイルで使用する圧縮係数です。
        /// </summary>
        private const double HighBoostCompressionExponent = 0.58;

        /// <summary>
        /// HighBoost プロファイルで使用する高域補正倍率です。
        /// </summary>
        private const double HighBoostHighBandGain = 1.35;

        #endregion

        #region プロパティ

        /// <summary>
        /// 既定バーエフェクトのメタデータを取得します。
        /// </summary>
        public EffectMetadata Metadata { get; } = new(
            "spectrum-bars",
            "Spectrum Bars",
            "1.0.0",
            "AudioVisualizer",
            "スペクトラム値を等間隔のバーとして描画する既定エフェクトです。",
            new[] { InputSource.SystemOutput, InputSource.Microphone });

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームをバー型レンダーデータへ変換します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>バー型スペクトラムのレンダーデータです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="frame"/> または <paramref name="context"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
        {
            ArgumentNullException.ThrowIfNull(frame);
            ArgumentNullException.ThrowIfNull(context);

            var barCount = context.BarCount;
            var bars = new List<BarRenderItem>(barCount);
            if (barCount == 0 || frame.SpectrumValues.Count == 0)
            {
                return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, bars);
            }

            var slotWidth = 1.0 / barCount;
            var actualBarWidth = slotWidth * BarWidthRatio;
            var xOffset = (slotWidth - actualBarWidth) / 2.0;

            for (var index = 0; index < barCount; index++)
            {
                var level = Math.Clamp(
                    SampleSpectrumValue(frame.SpectrumValues, index, barCount, context.SpectrumProfile) * context.Sensitivity,
                    0.0,
                    1.0);
                bars.Add(new BarRenderItem((slotWidth * index) + xOffset, actualBarWidth, level, level));
            }

            return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, bars);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 任意本数の描画用にスペクトラム値を再サンプリングします。
        /// </summary>
        /// <param name="spectrumValues">元のスペクトラム値です。</param>
        /// <param name="targetIndex">取得対象のバー位置です。</param>
        /// <param name="targetCount">描画対象のバー本数です。</param>
        /// <param name="spectrumProfile">再サンプリング時に適用する計算プロファイルです。</param>
        /// <returns>再サンプリング後のスペクトラム値です。</returns>
        private static double SampleSpectrumValue(
            IReadOnlyList<double> spectrumValues,
            int targetIndex,
            int targetCount,
            SpectrumProfile spectrumProfile)
        {
            return spectrumProfile switch
            {
                SpectrumProfile.Raw => SampleLinearSpectrumValue(spectrumValues, targetIndex, targetCount),
                SpectrumProfile.Balanced => SampleBalancedSpectrumValue(
                    spectrumValues,
                    targetIndex,
                    targetCount,
                    BalancedPositionExponent,
                    BalancedCompressionExponent,
                    BalancedHighBandGain,
                    BalancedMidBandGain),
                SpectrumProfile.HighBoost => SampleBalancedSpectrumValue(
                    spectrumValues,
                    targetIndex,
                    targetCount,
                    HighBoostPositionExponent,
                    HighBoostCompressionExponent,
                    HighBoostHighBandGain,
                    0.0),
                _ => SampleBalancedSpectrumValue(
                    spectrumValues,
                    targetIndex,
                    targetCount,
                    BalancedPositionExponent,
                    BalancedCompressionExponent,
                    BalancedHighBandGain,
                    BalancedMidBandGain),
            };
        }

        /// <summary>
        /// 元のスペクトラム分布をそのまま線形補間します。
        /// </summary>
        /// <param name="spectrumValues">元のスペクトラム値です。</param>
        /// <param name="targetIndex">取得対象のバー位置です。</param>
        /// <param name="targetCount">描画対象のバー本数です。</param>
        /// <returns>線形補間したスペクトラム値です。</returns>
        private static double SampleLinearSpectrumValue(IReadOnlyList<double> spectrumValues, int targetIndex, int targetCount)
        {
            if (targetCount <= 1 || spectrumValues.Count == 1)
            {
                return spectrumValues[0];
            }

            var position = targetIndex * (spectrumValues.Count - 1.0) / (targetCount - 1.0);
            var lowerIndex = (int)Math.Floor(position);
            var upperIndex = Math.Min(spectrumValues.Count - 1, lowerIndex + 1);
            if (lowerIndex == upperIndex)
            {
                return spectrumValues[lowerIndex];
            }

            var weight = position - lowerIndex;
            return (spectrumValues[lowerIndex] * (1.0 - weight)) + (spectrumValues[upperIndex] * weight);
        }

        /// <summary>
        /// 帯域平均と高域補正を組み合わせて見やすい分布へ変換します。
        /// </summary>
        /// <param name="spectrumValues">元のスペクトラム値です。</param>
        /// <param name="targetIndex">取得対象のバー位置です。</param>
        /// <param name="targetCount">描画対象のバー本数です。</param>
        /// <param name="positionExponent">高域側へ寄せる位置補正係数です。`1.0` 未満ほど高域を広く使います。</param>
        /// <param name="compressionExponent">小さい値を持ち上げる圧縮係数です。`1.0` 未満ほど弱い成分も見えやすくなります。</param>
        /// <param name="highBandGain">右側バーへ追加する補正倍率です。</param>
        /// <param name="midBandGain">中央付近のバーへ追加する補正倍率です。</param>
        /// <returns>補正後のスペクトラム値です。</returns>
        private static double SampleBalancedSpectrumValue(
            IReadOnlyList<double> spectrumValues,
            int targetIndex,
            int targetCount,
            double positionExponent,
            double compressionExponent,
            double highBandGain,
            double midBandGain)
        {
            if (targetCount <= 1 || spectrumValues.Count == 1)
            {
                return spectrumValues[0];
            }

            var startProgress = targetIndex / (double)targetCount;
            var endProgress = (targetIndex + 1.0) / targetCount;
            var startPosition = MapSpectrumPosition(startProgress, spectrumValues.Count, positionExponent);
            var endPosition = MapSpectrumPosition(endProgress, spectrumValues.Count, positionExponent);
            var averagedValue = SampleAverageSpectrumValue(spectrumValues, startPosition, endPosition);
            var normalizedIndex = targetCount == 1 ? 1.0 : targetIndex / (double)(targetCount - 1);
            var midBandWeight = CalculateMidBandWeight(normalizedIndex);
            var compressedValue = Math.Pow(Math.Clamp(averagedValue, 0.0, 1.0), compressionExponent);
            var adjustedValue = compressedValue * (1.0 + (normalizedIndex * highBandGain) + (midBandWeight * midBandGain));
            return Math.Clamp(adjustedValue, 0.0, 1.0);
        }

        /// <summary>
        /// 指定した進行度をスペクトラム配列上の位置へ変換します。
        /// </summary>
        /// <param name="progress">0.0 から 1.0 の範囲で表した進行度です。</param>
        /// <param name="spectrumCount">スペクトラム配列長です。</param>
        /// <param name="positionExponent">位置補正係数です。</param>
        /// <returns>スペクトラム配列上の位置です。</returns>
        private static double MapSpectrumPosition(double progress, int spectrumCount, double positionExponent)
        {
            return Math.Pow(progress, positionExponent) * (spectrumCount - 1.0);
        }

        /// <summary>
        /// 中域付近のバーへ適用する重みを返します。
        /// </summary>
        /// <param name="normalizedIndex">0.0 から 1.0 の範囲に正規化したバー位置です。</param>
        /// <returns>中央に近いほど大きい重みです。</returns>
        private static double CalculateMidBandWeight(double normalizedIndex)
        {
            return 1.0 - Math.Abs((normalizedIndex * 2.0) - 1.0);
        }

        /// <summary>
        /// 指定範囲に含まれるスペクトラム値の平均を返します。
        /// </summary>
        /// <param name="spectrumValues">元のスペクトラム値です。</param>
        /// <param name="startPosition">平均開始位置です。</param>
        /// <param name="endPosition">平均終了位置です。</param>
        /// <returns>平均化したスペクトラム値です。</returns>
        private static double SampleAverageSpectrumValue(IReadOnlyList<double> spectrumValues, double startPosition, double endPosition)
        {
            var startIndex = Math.Clamp((int)Math.Floor(startPosition), 0, spectrumValues.Count - 1);
            var endIndex = Math.Clamp((int)Math.Ceiling(endPosition), 0, spectrumValues.Count - 1);
            if (startIndex == endIndex)
            {
                return spectrumValues[startIndex];
            }

            var total = 0.0;
            var count = 0;
            for (var index = startIndex; index <= endIndex; index++)
            {
                total += spectrumValues[index];
                count++;
            }

            return count == 0 ? 0.0 : total / count;
        }

        #endregion
    }
}
