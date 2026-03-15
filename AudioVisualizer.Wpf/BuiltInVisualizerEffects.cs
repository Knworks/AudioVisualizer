using AudioVisualizer.Core.Effects;
using AudioVisualizer.Wpf.Effects;

namespace AudioVisualizer.Wpf
{
    /// <summary>
    /// 組込エフェクトの生成を提供します。
    /// </summary>
    public static class BuiltInVisualizerEffects
    {
        #region 公開メソッド

        /// <summary>
        /// 指定した組込エフェクト種別に対応するエフェクトを生成します。
        /// </summary>
        /// <param name="effectKind">生成対象の組込エフェクト種別です。</param>
        /// <returns>組込エフェクトの新しいインスタンスです。</returns>
        public static IVisualizerEffect Create(BuiltInVisualizerEffectKind effectKind)
        {
            return effectKind switch
            {
                BuiltInVisualizerEffectKind.SpectrumBar => new SpectrumBarEffect(),
                BuiltInVisualizerEffectKind.WaveformLine => new WaveformLineEffect(),
                BuiltInVisualizerEffectKind.MirrorBar => new MirrorBarEffect(),
                BuiltInVisualizerEffectKind.PeakHoldBar => new PeakHoldBarEffect(),
                BuiltInVisualizerEffectKind.BandLevelMeter => new BandLevelMeterEffect(),
                _ => throw new ArgumentOutOfRangeException(nameof(effectKind), effectKind, "未対応の組込エフェクト種別です。"),
            };
        }

        #endregion
    }
}
