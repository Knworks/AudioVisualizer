using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// OS の既定音声デバイスが切り替わったときの通知データを表します。
    /// </summary>
    public sealed class DefaultAudioDeviceChangedEventArgs : EventArgs
    {
        #region プロパティ

        /// <summary>
        /// 変更対象の入力種別を取得します。
        /// </summary>
        public InputSource InputSource { get; }

        /// <summary>
        /// 新しく既定になったデバイス識別子を取得します。
        /// </summary>
        public string DeviceId { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="DefaultAudioDeviceChangedEventArgs"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="inputSource">変更対象の入力種別です。</param>
        /// <param name="deviceId">新しく既定になったデバイス識別子です。</param>
        public DefaultAudioDeviceChangedEventArgs(InputSource inputSource, string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("A default device ID is required.", nameof(deviceId));
            }

            InputSource = inputSource;
            DeviceId = deviceId;
        }

        #endregion
    }
}
