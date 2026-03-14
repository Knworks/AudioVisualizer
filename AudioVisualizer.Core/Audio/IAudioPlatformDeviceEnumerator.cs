using System.Collections.Generic;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// OS 依存の音声デバイス列挙を抽象化する内部契約です。
    /// </summary>
internal interface IAudioPlatformDeviceEnumerator
    {
        #region イベントハンドラ

        /// <summary>
        /// OS の既定音声デバイスが切り替わったときに発生します。
        /// </summary>
        event EventHandler<DefaultAudioDeviceChangedEventArgs>? DefaultDeviceChanged;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別に対応する利用可能デバイス一覧を取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>OS 依存情報を含むデバイス一覧です。</returns>
        IReadOnlyList<AudioDeviceSnapshot> GetDevices(InputSource inputSource);

        /// <summary>
        /// 指定した入力種別に対応する既定デバイスを取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
        AudioDeviceSnapshot? GetDefaultDevice(InputSource inputSource);

        #endregion
    }
}
