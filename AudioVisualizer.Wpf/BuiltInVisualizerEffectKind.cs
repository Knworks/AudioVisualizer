namespace AudioVisualizer.Wpf
{
    /// <summary>
    /// AudioVisualizer.Wpf が提供する組込エフェクト種別を表します。
    /// </summary>
    public enum BuiltInVisualizerEffectKind
    {
        #region 定数

        /// <summary>
        /// バー型スペクトラムを描画します。
        /// </summary>
        SpectrumBar,

        /// <summary>
        /// 波形を折れ線で描画します。
        /// </summary>
        WaveformLine,

        /// <summary>
        /// 中心対称のバーを描画します。
        /// </summary>
        MirrorBar,

        /// <summary>
        /// ピーク保持線付きバーを描画します。
        /// </summary>
        PeakHoldBar,

        /// <summary>
        /// 固定帯域メーターを描画します。
        /// </summary>
        BandLevelMeter,

        #endregion
    }
}
