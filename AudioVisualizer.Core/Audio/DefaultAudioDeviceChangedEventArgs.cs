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
        public string? DeviceId { get; }

        #endregion

        #region 構築・消滅

        /// <summary>
        /// <see cref="DefaultAudioDeviceChangedEventArgs"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="inputSource">変更対象の入力種別です。</param>
        /// <param name="deviceId">新しく既定になったデバイス識別子です。既定がなくなった場合は null です。</param>
        /// <exception cref="ArgumentException"><paramref name="deviceId"/> が <see langword="null"/> でなく、空または空白のみである場合にスローされます。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="inputSource"/> がサポートされていない値である場合にスローされます。</exception>
        public DefaultAudioDeviceChangedEventArgs(InputSource inputSource, string? deviceId)
        {
            if (deviceId is not null && string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("A default device ID is required.", nameof(deviceId));
            }

            if (!Enum.IsDefined(inputSource))
            {
                throw new ArgumentOutOfRangeException(nameof(inputSource));
            }

            InputSource = inputSource;
            DeviceId = deviceId;
        }

        #endregion
    }
}
