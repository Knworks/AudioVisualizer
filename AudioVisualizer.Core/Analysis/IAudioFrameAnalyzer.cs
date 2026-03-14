using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Analysis
{
    /// <summary>
    /// PCM サンプルバッファから解析済みフレームを生成する契約を定義します。
    /// </summary>
    public interface IAudioFrameAnalyzer
    {
        #region 公開メソッド

        /// <summary>
        /// PCM サンプルバッファと設定から解析済みフレームを生成します。
        /// </summary>
        /// <param name="sampleBuffer">解析対象のサンプルバッファです。</param>
        /// <param name="settings">解析に使用する設定です。</param>
        /// <returns>生成された解析済みフレームです。</returns>
        VisualizerFrame CreateFrame(AudioSampleBuffer sampleBuffer, VisualizerSettings settings);

        #endregion
    }
}
