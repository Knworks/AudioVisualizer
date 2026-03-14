namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声キャプチャ実体を生成する内部ファクトリ契約です。
    /// </summary>
    internal interface IAudioCaptureFactory
    {
        #region 公開メソッド

        /// <summary>
        /// システム再生音を取得するキャプチャ実体を生成します。
        /// </summary>
        /// <returns>システム再生音用のキャプチャ実体です。</returns>
        IAudioCaptureSession CreateSystemOutputCapture();

        #endregion
    }
}
