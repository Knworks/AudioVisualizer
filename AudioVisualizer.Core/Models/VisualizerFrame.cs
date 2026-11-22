using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVisualizer.Core.Models
{
    /// <summary>
    /// 1 フレーム分の解析済み音声データを表します。
    /// </summary>
    public sealed class VisualizerFrame
    {
        #region プロパティ

        /// <summary>
        /// フレームに含まれるスペクトラム値を取得します。
        /// </summary>
        public IReadOnlyList<double> SpectrumValues { get; }

        /// <summary>
        /// フレームに含まれる波形値を取得します。
        /// </summary>
        public IReadOnlyList<double> WaveformValues { get; }

        /// <summary>
        /// 間引き前の波形の最小値と最大値を、左から右の表示区間ごとに取得します。
        /// </summary>
        public IReadOnlyList<(double Minimum, double Maximum)> WaveformRanges { get; }

        /// <summary>
        /// フレームのピークレベルを取得します。
        /// </summary>
        public double PeakLevel { get; }

        /// <summary>
        /// フレームが生成された時刻を取得します。
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerFrame"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="spectrumValues">フレームに含まれるスペクトラム値です。</param>
        /// <param name="waveformValues">フレームに含まれる波形値です。</param>
        /// <param name="peakLevel">フレームのピークレベルです。</param>
        /// <param name="timestamp">フレームが生成された時刻です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="spectrumValues"/> が <see langword="null"/> の場合にスローされます。</exception>
        /// <exception cref="ArgumentNullException"><paramref name="waveformValues"/> が <see langword="null"/> の場合にスローされます。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="peakLevel"/> が負数の場合にスローされます。</exception>
        public VisualizerFrame(IEnumerable<double> spectrumValues, IEnumerable<double> waveformValues, double peakLevel, DateTimeOffset timestamp)
            : this(spectrumValues, waveformValues, peakLevel, timestamp, Array.Empty<(double, double)>())
        {
        }

        /// <summary>
        /// 解析済みフレームを波形の区間ごとの振幅範囲とともに初期化します。
        /// </summary>
        /// <param name="spectrumValues">スペクトラム値です。</param>
        /// <param name="waveformValues">波形の代表値です。</param>
        /// <param name="peakLevel">フレームのピークレベルです。</param>
        /// <param name="timestamp">フレームの生成時刻です。</param>
        /// <param name="waveformRanges">波形の区間ごとの振幅範囲です。</param>
        internal VisualizerFrame(
            IEnumerable<double> spectrumValues,
            IEnumerable<double> waveformValues,
            double peakLevel,
            DateTimeOffset timestamp,
            IReadOnlyList<(double Minimum, double Maximum)> waveformRanges)
        {
            if (spectrumValues is null)
            {
                throw new ArgumentNullException(nameof(spectrumValues));
            }

            if (waveformValues is null)
            {
                throw new ArgumentNullException(nameof(waveformValues));
            }

            if (!double.IsFinite(peakLevel) || peakLevel < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(peakLevel), "Peak level must not be negative.");
            }

            var spectrum = spectrumValues.ToArray();
            if (Array.Exists(spectrum, value => !double.IsFinite(value)))
            {
                throw new ArgumentException("Spectrum values must be finite.", nameof(spectrumValues));
            }

            var waveform = waveformValues.ToArray();
            if (Array.Exists(waveform, value => !double.IsFinite(value)))
            {
                throw new ArgumentException("Waveform values must be finite.", nameof(waveformValues));
            }

            SpectrumValues = spectrum;
            WaveformValues = waveform;
            WaveformRanges = Array.AsReadOnly(waveformRanges.ToArray());
            PeakLevel = peakLevel;
            Timestamp = timestamp;
        }

        /// <summary>
        /// <see cref="VisualizerFrame"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="spectrumValues">フレームに含まれるスペクトラム値です。</param>
        /// <param name="peakLevel">フレームのピークレベルです。</param>
        /// <param name="timestamp">フレームが生成された時刻です。</param>
        public VisualizerFrame(IEnumerable<double> spectrumValues, double peakLevel, DateTimeOffset timestamp)
            : this(spectrumValues, Array.Empty<double>(), peakLevel, timestamp)
        {
        }

        #endregion
    }
}
