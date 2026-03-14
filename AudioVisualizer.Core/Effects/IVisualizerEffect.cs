using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// 解析済み音声フレームを描画指示へ変換するエフェクトの契約を定義します。
    /// </summary>
    public interface IVisualizerEffect
    {
        #region プロパティ

        /// <summary>
        /// エフェクトを識別するメタデータを取得します。
        /// </summary>
        EffectMetadata Metadata { get; }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 解析済みフレームと実行時コンテキストから WPF 非依存の描画指示を生成します。
        /// </summary>
        /// <param name="frame">解析済み音声フレームです。</param>
        /// <param name="context">エフェクト実行時のコンテキストです。</param>
        /// <returns>エフェクトが生成した描画指示です。</returns>
        VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context);

        #endregion
    }
}
