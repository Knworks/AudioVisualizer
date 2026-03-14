using System.Collections.Generic;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声デバイス一覧と既定デバイス情報を取得する公開サービス契約です。
    /// </summary>
    public interface IAudioDeviceService
    {
        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別に対応する利用可能デバイス一覧を取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>指定入力種別に対応するデバイス一覧です。</returns>
        IReadOnlyList<AudioDeviceInfo> GetDevices(InputSource inputSource);

        /// <summary>
        /// 指定した入力種別に対応する既定デバイスを取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
        AudioDeviceInfo? GetDefaultDevice(InputSource inputSource);

        #endregion
    }
}
