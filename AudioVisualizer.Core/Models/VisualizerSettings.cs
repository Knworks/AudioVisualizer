using System;

namespace AudioVisualizer.Core.Models
{
    /// <summary>
    /// 音声ビジュアライザーの実行時設定を表します。
    /// </summary>
    public sealed class VisualizerSettings
    {
        #region プロパティ

        /// <summary>
        /// 選択された音声入力元を取得します。
        /// </summary>
        public InputSource InputSource { get; }

        /// <summary>
        /// 明示的に選択されたデバイス識別子を取得します。
        /// </summary>
        public string? DeviceId { get; }

        /// <summary>
        /// システムの既定デバイスを利用するかどうかを示す値を取得します。
        /// </summary>
        public bool UseDefaultDevice { get; }

        /// <summary>
        /// 音声取得が有効かどうかを示す値を取得します。
        /// </summary>
        public bool IsActive { get; }

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

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="VisualizerSettings"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="inputSource">選択された音声入力元です。</param>
        /// <param name="deviceId">明示的に選択されたデバイス識別子です。</param>
        /// <param name="useDefaultDevice">既定デバイスを利用するかどうかを示します。</param>
        /// <param name="isActive">音声取得を有効化するかどうかを示します。</param>
        /// <param name="sensitivity">入力感度の倍率です。</param>
        /// <param name="smoothing">0.0 から 1.0 の範囲で指定する平滑化係数です。</param>
        /// <param name="barCount">バー型エフェクトで使用するバー本数です。</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sensitivity"/>、<paramref name="smoothing"/>、<paramref name="barCount"/> が許容範囲外の場合にスローされます。
        /// </exception>
        /// <exception cref="ArgumentException">
        /// 明示的なデバイス指定が必須なのに <paramref name="deviceId"/> が空白の場合にスローされます。
        /// </exception>
        public VisualizerSettings(
            InputSource inputSource,
            string? deviceId,
            bool useDefaultDevice,
            bool isActive,
            double sensitivity,
            double smoothing,
            int barCount)
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

            if (!useDefaultDevice && string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("A device ID is required when the default device is not used.", nameof(deviceId));
            }

            InputSource = inputSource;
            DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId;
            UseDefaultDevice = useDefaultDevice;
            IsActive = isActive;
            Sensitivity = sensitivity;
            Smoothing = smoothing;
            BarCount = barCount;
        }

        #endregion
    }
}
