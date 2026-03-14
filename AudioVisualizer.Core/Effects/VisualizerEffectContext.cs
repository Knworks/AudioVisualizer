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
        /// `SystemOutput` は再生中のシステム音声、`Microphone` は録音入力を表します。
        /// </summary>
        public InputSource InputSource { get; }

        /// <summary>
        /// 入力感度の倍率を取得します。
        /// `1.0` が基準で、値を上げるほど小さな音も強く反映します。
        /// エフェクト側ではバー高さや強度補正の係数として利用します。
        /// </summary>
        public double Sensitivity { get; }

        /// <summary>
        /// 平滑化係数を取得します。
        /// `0.0` は即時反応、`1.0` に近いほど前回状態を強く残します。
        /// エフェクトや描画側で補間係数として使う前提の値です。
        /// </summary>
        public double Smoothing { get; }

        /// <summary>
        /// バー型エフェクトで使用するバー本数を取得します。
        /// 値を増やすほど高密度な表示、値を減らすほど大きく分かりやすい表示になります。
        /// </summary>
        public int BarCount { get; }

        /// <summary>
        /// スペクトラム値をバーへ割り当てる計算プロファイルを取得します。
        /// `Raw` は元の分布、`Balanced` は見た目のバランス重視、`HighBoost` は高域強調です。
        /// バーの偏りを抑えたい場合は `Balanced` か `HighBoost` を使用します。
        /// </summary>
        public SpectrumProfile SpectrumProfile { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerEffectContext"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="inputSource">現在の音声入力元です。</param>
        /// <param name="sensitivity">入力感度の倍率です。`1.0` が基準で、値を上げるほど小さな音も強く扱います。</param>
        /// <param name="smoothing">`0.0` から `1.0` の範囲で指定する平滑化係数です。大きいほど表示がなめらかになります。</param>
        /// <param name="barCount">バー型エフェクトで使用するバー本数です。多いほど細かく、少ないほど動きが目立ちやすくなります。</param>
        /// <param name="spectrumProfile">スペクトラム値をバーへ割り当てる計算プロファイルです。</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sensitivity"/>、<paramref name="smoothing"/>、<paramref name="barCount"/>、<paramref name="spectrumProfile"/> が許容範囲外の場合にスローされます。
        /// </exception>
        public VisualizerEffectContext(
            InputSource inputSource,
            double sensitivity,
            double smoothing,
            int barCount,
            SpectrumProfile spectrumProfile)
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

            if (!Enum.IsDefined(spectrumProfile))
            {
                throw new ArgumentOutOfRangeException(nameof(spectrumProfile), "Spectrum profile is not supported.");
            }

            InputSource = inputSource;
            Sensitivity = sensitivity;
            Smoothing = smoothing;
            BarCount = barCount;
            SpectrumProfile = spectrumProfile;
        }

        #endregion
    }
}
