namespace AudioVisualizer.Core.Analysis
{
    /// <summary>
    /// 1 取得セッション分の 12 ミリ秒の波形履歴を保持します。
    /// </summary>
    internal sealed class WaveformHistory
    {
        private const double WindowSeconds = 0.012;
        private int m_SampleRate;
        private int m_Channels;
        private double[] m_Samples = Array.Empty<double>();

        /// <summary>
        /// 新しい PCM を追加し、固定した時間幅で描画する波形を返します。
        /// </summary>
        /// <param name="samples">正規化済みの PCM です。</param>
        /// <param name="sampleRate">サンプルレートです。</param>
        /// <param name="channels">チャンネル数です。</param>
        /// <returns>時系列順の表示用 PCM です。</returns>
        public double[] Append(double[] samples, int sampleRate, int channels)
        {
            if (m_SampleRate != sampleRate || m_Channels != channels)
            {
                m_SampleRate = sampleRate;
                m_Channels = channels;
                m_Samples = new double[checked(Math.Max(1, (int)Math.Round(sampleRate * WindowSeconds)) * channels)];
            }

            var completeLength = samples.Length - (samples.Length % channels);
            var copyLength = Math.Min(completeLength, m_Samples.Length);
            Array.Copy(m_Samples, copyLength, m_Samples, 0, m_Samples.Length - copyLength);
            Array.Copy(samples, completeLength - copyLength, m_Samples, m_Samples.Length - copyLength, copyLength);

            // 最新窓の外に大きな単発音がある場合は、そのブロックの表示だけ単発音へ合わせます。
            var discardedLength = completeLength - copyLength;
            if (discardedLength == 0)
            {
                return m_Samples;
            }

            var earlierPeakIndex = -1;
            var earlierPeak = 0.0;
            var recentPeak = 0.0;
            for (var index = 0; index < completeLength; index++)
            {
                var amplitude = Math.Abs(samples[index]);
                if (index >= discardedLength)
                {
                    recentPeak = Math.Max(recentPeak, amplitude);
                }
                else if (amplitude > earlierPeak)
                {
                    earlierPeak = amplitude;
                    earlierPeakIndex = index;
                }
            }

            if (earlierPeakIndex >= 0 && earlierPeak > 2.0 * recentPeak)
            {
                var peakFrame = earlierPeakIndex / channels;
                var startFrame = Math.Max(0, peakFrame - (m_Samples.Length / channels / 2));
                return samples.AsSpan(startFrame * channels, m_Samples.Length).ToArray();
            }

            return m_Samples;
        }
    }
}
