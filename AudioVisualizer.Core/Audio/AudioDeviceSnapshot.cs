using System;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// OS 依存のデバイス列挙結果を表す内部スナップショットです。
    /// </summary>
    internal sealed class AudioDeviceSnapshot
    {
        #region プロパティ

        /// <summary>
        /// 音声デバイスの一意な識別子を取得します。
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// 利用者向けの表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioDeviceSnapshot"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="deviceId">音声デバイスの一意な識別子です。</param>
        /// <param name="displayName">利用者向けの表示名です。</param>
        /// <exception cref="ArgumentException"><paramref name="deviceId"/> または <paramref name="displayName"/> が空白の場合にスローされます。</exception>
        public AudioDeviceSnapshot(string deviceId, string displayName)
        {
            DeviceId = string.IsNullOrWhiteSpace(deviceId)
                ? throw new ArgumentException("Device ID must not be blank.", nameof(deviceId))
                : deviceId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Display name must not be blank.", nameof(displayName))
                : displayName;
        }

        #endregion
    }
}
