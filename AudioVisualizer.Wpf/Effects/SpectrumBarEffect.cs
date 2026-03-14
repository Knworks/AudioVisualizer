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

            var barCount = Math.Min(context.BarCount, frame.SpectrumValues.Count);
            var bars = new List<BarRenderItem>(barCount);
            if (barCount == 0)
            {
                return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, bars);
            }

            var slotWidth = 1.0 / barCount;
            var actualBarWidth = slotWidth * BarWidthRatio;
            var xOffset = (slotWidth - actualBarWidth) / 2.0;

            for (var index = 0; index < barCount; index++)
            {
                var level = Math.Clamp(frame.SpectrumValues[index], 0.0, 1.0);
                bars.Add(new BarRenderItem((slotWidth * index) + xOffset, actualBarWidth, level, level));
            }

            return new BarSpectrumRenderData(Metadata.Id, frame.Timestamp, bars);
        }

        #endregion
    }
}
