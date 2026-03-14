using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声キャプチャ実体を生成する内部ファクトリ契約です。
    /// </summary>
    internal interface IAudioCaptureFactory
    {
        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別とデバイス識別子に対応するキャプチャ実体を生成します。
        /// </summary>
        /// <param name="inputSource">入力種別です。</param>
        /// <param name="deviceId">対象デバイスの識別子です。</param>
        /// <returns>指定入力向けのキャプチャ実体です。</returns>
        IAudioCaptureSession CreateCapture(InputSource inputSource, string deviceId);

        #endregion
    }
}
