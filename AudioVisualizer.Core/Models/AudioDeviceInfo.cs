using System;

namespace AudioVisualizer.Core.Models
{
    /// <summary>
    /// ビジュアライザー入力として利用できる音声デバイスを表します。
    /// </summary>
    public sealed class AudioDeviceInfo
    {
        #region プロパティ

        /// <summary>
        /// 音声デバイスの一意な識別子を取得します。
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// デバイスの表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// デバイスが属する入力元の種別を取得します。
        /// </summary>
        public InputSource InputSource { get; }

        /// <summary>
        /// デバイスが現在の既定デバイスかどうかを示す値を取得します。
        /// </summary>
        public bool IsDefault { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioDeviceInfo"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="deviceId">音声デバイスの一意な識別子です。</param>
        /// <param name="displayName">利用者へ表示するデバイス名です。</param>
        /// <param name="inputSource">デバイスが属する入力元の種別です。</param>
        /// <param name="isDefault">現在の既定デバイスであるかどうかを示します。</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="deviceId"/> または <paramref name="displayName"/> が空白の場合にスローされます。
        /// </exception>
        public AudioDeviceInfo(string deviceId, string displayName, InputSource inputSource, bool isDefault)
        {
            DeviceId = string.IsNullOrWhiteSpace(deviceId)
                ? throw new ArgumentException("Device ID must not be blank.", nameof(deviceId))
                : deviceId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Display name must not be blank.", nameof(displayName))
                : displayName;
            InputSource = inputSource;
            IsDefault = isDefault;
        }

        #endregion
    }
}
