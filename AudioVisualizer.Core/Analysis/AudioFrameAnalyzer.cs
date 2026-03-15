using System;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Analysis
{
    /// <summary>
    /// PCM サンプルからスペクトラム値、波形値、ピーク値を生成します。
    /// </summary>
    public sealed class AudioFrameAnalyzer : IAudioFrameAnalyzer
    {
        #region 定数

        /// <summary>
        /// FFT 近似計算に使用する最大サンプル数です。
        /// </summary>
        private const int MaxAnalysisWindowSize = 1024;

        /// <summary>
        /// スペクトラム値を UI 向けの 0..1 範囲へ拡大する係数です。
        /// </summary>
        private const double SpectrumScale = 8.0;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// PCM サンプルバッファと設定から解析済みフレームを生成します。
        /// </summary>
        /// <param name="sampleBuffer">解析対象のサンプルバッファです。</param>
        /// <param name="settings">解析に使用する設定です。</param>
        /// <returns>生成された解析済みフレームです。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sampleBuffer"/> または <paramref name="settings"/> が <see langword="null"/> の場合にスローされます。</exception>
        public VisualizerFrame CreateFrame(AudioSampleBuffer sampleBuffer, VisualizerSettings settings)
        {
            ArgumentNullException.ThrowIfNull(sampleBuffer);
            ArgumentNullException.ThrowIfNull(settings);

            var normalizedSamples = ToNormalizedSamples(sampleBuffer);
            var peakLevel = CalculatePeakLevel(normalizedSamples);
            var waveformValues = BuildWaveformValues(normalizedSamples, settings.BarCount, settings.Sensitivity);
            var spectrumValues = BuildSpectrumValues(normalizedSamples, settings.BarCount, settings.Sensitivity);

            return new VisualizerFrame(spectrumValues, waveformValues, peakLevel, sampleBuffer.Timestamp);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// PCM サンプル列を -1.0..1.0 範囲の倍精度サンプル列へ正規化します。
        /// </summary>
        /// <param name="sampleBuffer">変換対象のサンプルバッファです。</param>
        /// <returns>正規化後のサンプル列です。</returns>
        private static double[] ToNormalizedSamples(AudioSampleBuffer sampleBuffer)
        {
            var result = new double[sampleBuffer.Samples.Count];
            for (var index = 0; index < sampleBuffer.Samples.Count; index++)
            {
                result[index] = Math.Clamp(sampleBuffer.Samples[index], -1.0f, 1.0f);
            }

            return result;
        }

        /// <summary>
        /// サンプル列からピークレベルを算出します。
        /// </summary>
        /// <param name="samples">解析対象のサンプル列です。</param>
        /// <returns>最大絶対値として求めたピークレベルです。</returns>
        private static double CalculatePeakLevel(double[] samples)
        {
            var peak = 0.0;
            for (var index = 0; index < samples.Length; index++)
            {
                var absoluteValue = Math.Abs(samples[index]);
                if (absoluteValue > peak)
                {
                    peak = absoluteValue;
                }
            }

            return peak;
        }

        /// <summary>
        /// サンプル列から表示用の波形配列を生成します。
        /// </summary>
        /// <param name="samples">解析対象のサンプル列です。</param>
        /// <param name="outputLength">出力する波形要素数です。</param>
        /// <param name="sensitivity">感度係数です。</param>
        /// <returns>表示用に整形した波形配列です。</returns>
        private static double[] BuildWaveformValues(double[] samples, int outputLength, double sensitivity)
        {
            var result = new double[outputLength];
            if (samples.Length == 0 || outputLength <= 0)
            {
                return result;
            }

            var bucketSize = Math.Max(1, samples.Length / outputLength);
            for (var bucketIndex = 0; bucketIndex < outputLength; bucketIndex++)
            {
                var startIndex = bucketIndex * bucketSize;
                if (startIndex >= samples.Length)
                {
                    break;
                }

                var endIndex = Math.Min(samples.Length, startIndex + bucketSize);
                var representativeValue = 0.0;
                for (var sampleIndex = startIndex; sampleIndex < endIndex; sampleIndex++)
                {
                    var currentSample = samples[sampleIndex];
                    if (Math.Abs(currentSample) > Math.Abs(representativeValue))
                    {
                        representativeValue = currentSample;
                    }
                }

                result[bucketIndex] = Math.Clamp(representativeValue * sensitivity, -1.0, 1.0);
            }

            return result;
        }

        /// <summary>
        /// サンプル列から表示用のスペクトラム配列を生成します。
        /// </summary>
        /// <param name="samples">解析対象のサンプル列です。</param>
        /// <param name="outputLength">出力するスペクトラム要素数です。</param>
        /// <param name="sensitivity">感度係数です。</param>
        /// <returns>表示用に整形したスペクトラム配列です。</returns>
        private static double[] BuildSpectrumValues(double[] samples, int outputLength, double sensitivity)
        {
            var result = new double[outputLength];
            if (samples.Length == 0)
            {
                return result;
            }

            var windowSize = Math.Min(samples.Length, MaxAnalysisWindowSize);
            if (windowSize < 2)
            {
                result[0] = Math.Clamp(Math.Abs(samples[0]) * sensitivity, 0.0, 1.0);
                return result;
            }

            var windowedSamples = ApplyHannWindow(samples, windowSize);
            var maxFrequencyBin = Math.Max(1, windowSize / 2);
            var denominator = Math.Max(1, outputLength - 1);

            for (var barIndex = 0; barIndex < outputLength; barIndex++)
            {
                var frequencyBin = 1 + ((maxFrequencyBin - 1) * barIndex / denominator);
                var magnitude = CalculateMagnitude(windowedSamples, frequencyBin);
                result[barIndex] = Math.Clamp(magnitude * sensitivity * SpectrumScale, 0.0, 1.0);
            }

            return result;
        }

        /// <summary>
        /// サンプル列へハン窓を適用します。
        /// </summary>
        /// <param name="samples">窓関数適用前のサンプル列です。</param>
        /// <param name="windowSize">適用対象のサンプル数です。</param>
        /// <returns>ハン窓適用後のサンプル列です。</returns>
        private static double[] ApplyHannWindow(double[] samples, int windowSize)
        {
            var result = new double[windowSize];
            for (var index = 0; index < windowSize; index++)
            {
                var window = 0.5 - (0.5 * Math.Cos((2.0 * Math.PI * index) / (windowSize - 1)));
                result[index] = samples[index] * window;
            }

            return result;
        }

        /// <summary>
        /// 指定周波数ビンの振幅を近似計算します。
        /// </summary>
        /// <param name="samples">窓関数適用済みサンプル列です。</param>
        /// <param name="frequencyBin">算出対象の周波数ビンです。</param>
        /// <returns>周波数ビンに対応する振幅です。</returns>
        private static double CalculateMagnitude(double[] samples, int frequencyBin)
        {
            var real = 0.0;
            var imaginary = 0.0;
            for (var sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
            {
                var angle = (2.0 * Math.PI * frequencyBin * sampleIndex) / samples.Length;
                real += samples[sampleIndex] * Math.Cos(angle);
                imaginary -= samples[sampleIndex] * Math.Sin(angle);
            }

            return Math.Sqrt((real * real) + (imaginary * imaginary)) / samples.Length;
        }

        #endregion
    }
}
