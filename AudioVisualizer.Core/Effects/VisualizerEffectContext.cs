using System;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// ビジュアライザーエフェクトの実行に必要な実行時コンテキストを提供します。
    /// </summary>
    public sealed class VisualizerEffectContext
    {
        #region プロパティ

        /// <summary>
        /// 現在の音声入力元を取得します。
        /// </summary>
        public InputSource InputSource { get; }

        /// <summary>
        /// 入力感度の倍率を取得します。
        /// </summary>
        public double Sensitivity { get; }

        /// <summary>
        /// 平滑化係数を取得します。
        /// </summary>
        public double Smoothing { get; }

        /// <summary>
        /// バー型エフェクトで使用するバー本数を取得します。
        /// </summary>
        public int BarCount { get; }

        #endregion

        #region  構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerEffectContext"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="inputSource">現在の音声入力元です。</param>
        /// <param name="sensitivity">入力感度の倍率です。</param>
        /// <param name="smoothing">0.0 から 1.0 の範囲で指定する平滑化係数です。</param>
        /// <param name="barCount">バー型エフェクトで使用するバー本数です。</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sensitivity"/>、<paramref name="smoothing"/>、<paramref name="barCount"/> が許容範囲外の場合にスローされます。
        /// </exception>
        public VisualizerEffectContext(InputSource inputSource, double sensitivity, double smoothing, int barCount)
        {
            if (sensitivity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sensitivity), "Sensitivity must be greater than zero.");
            }

            if (smoothing < 0 || smoothing > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(smoothing), "Smoothing must be between 0.0 and 1.0.");
            }

            if (barCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(barCount), "Bar count must be greater than zero.");
            }

            InputSource = inputSource;
            Sensitivity = sensitivity;
            Smoothing = smoothing;
            BarCount = barCount;
        }

        #endregion
    }
}
