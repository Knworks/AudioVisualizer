using System;
using System.Numerics;
using AudioVisualizer.Core.Models;
using NAudio.Dsp;
using Complex = NAudio.Dsp.Complex;

namespace AudioVisualizer.Core.Analysis
{
    /// <summary>
    /// PCM サンプルからスペクトラム値、波形値、ピーク値を生成します。
    /// </summary>
    public sealed class AudioFrameAnalyzer : IAudioFrameAnalyzer
    {
        #region 定数

        /// <summary>
        /// FFTに使用する最大サンプル数です。
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
            var spectrumValues = BuildSpectrumValues(normalizedSamples, sampleBuffer.Channels, settings.BarCount, settings.Sensitivity);

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
        /// チャンネルごとにスペクトラムを求め、各周波数帯域の最大振幅を採用します。
        /// </summary>
        /// <param name="samples">チャンネルが交互に並ぶ解析対象のサンプル列です。</param>
        /// <param name="channels">入力のチャンネル数です。</param>
        /// <param name="outputLength">出力するスペクトラム要素数です。</param>
        /// <param name="sensitivity">感度係数です。</param>
        /// <returns>表示用に整形したスペクトラム配列です。</returns>
        private static double[] BuildSpectrumValues(double[] samples, int channels, int outputLength, double sensitivity)
        {
            var result = new double[outputLength];
            var windowSize = Math.Min(samples.Length / channels, MaxAnalysisWindowSize);
            if (windowSize == 0)
            {
                return result;
            }

            if (windowSize == 1)
            {
                var peak = 0.0;
                for (var channel = 0; channel < channels; channel++)
                {
                    peak = Math.Max(peak, Math.Abs(samples[channel]));
                }

                result[0] = Math.Clamp(peak * sensitivity, 0.0, 1.0);
                return result;
            }

            var fftSize = (int)BitOperations.RoundUpToPowerOf2((uint)windowSize);
            var bins = new Complex[fftSize];
            var positiveBinCount = fftSize / 2;
            var maximumPowerByBin = new double[positiveBinCount + 1];
            for (var channel = 0; channel < channels; channel++)
            {
                Array.Clear(bins);
                for (var index = 0; index < windowSize; index++)
                {
                    var window = windowSize == 2 ? 1.0 : FastFourierTransform.HannWindow(index, windowSize);
                    bins[index].X = (float)(samples[(index * channels) + channel] * window);
                }

                FastFourierTransform.FFT(true, BitOperations.Log2((uint)fftSize), bins);
                for (var bin = 1; bin <= positiveBinCount; bin++)
                {
                    var real = (double)bins[bin].X;
                    var imaginary = (double)bins[bin].Y;
                    maximumPowerByBin[bin] = Math.Max(maximumPowerByBin[bin], (real * real) + (imaginary * imaginary));
                }
            }

            var scale = (double)fftSize / windowSize * sensitivity * SpectrumScale;
            for (var barIndex = 0; barIndex < outputLength; barIndex++)
            {
                var startBin = 1 + (int)((long)positiveBinCount * barIndex / outputLength);
                var endBin = 1 + (int)((long)positiveBinCount * (barIndex + 1) / outputLength);
                var maximumPower = 0.0;
                for (var bin = startBin; bin < Math.Max(startBin + 1, endBin); bin++)
                {
                    maximumPower = Math.Max(maximumPower, maximumPowerByBin[bin]);
                }

                result[barIndex] = Math.Clamp(Math.Sqrt(maximumPower) * scale, 0.0, 1.0);
            }

            return result;
        }

        #endregion
    }
}
