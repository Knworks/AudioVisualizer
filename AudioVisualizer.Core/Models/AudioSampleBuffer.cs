using System;
using System.Collections.Generic;
using System.Linq;

namespace AudioVisualizer.Core.Models
{
    /// <summary>
    /// 音声取得直後の PCM サンプルバッファを表します。
    /// </summary>
    public sealed class AudioSampleBuffer
    {
        #region プロパティ

        /// <summary>
        /// PCM サンプル列を取得します。
        /// </summary>
        public IReadOnlyList<float> Samples { get; }

        /// <summary>
        /// サンプルレートを取得します。
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// チャンネル数を取得します。
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// バッファが取得された時刻を取得します。
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioSampleBuffer"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="samples">取得した PCM サンプル列です。</param>
        /// <param name="sampleRate">サンプルレートです。</param>
        /// <param name="channels">チャンネル数です。</param>
        /// <param name="timestamp">バッファが取得された時刻です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> が <see langword="null"/> の場合にスローされます。</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleRate"/> または <paramref name="channels"/> が 1 未満の場合にスローされます。</exception>
        public AudioSampleBuffer(IEnumerable<float> samples, int sampleRate, int channels, DateTimeOffset timestamp)
        {
            if (samples is null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (sampleRate < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "サンプルレートは 1 以上である必要があります。");
            }

            if (channels < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(channels), "チャンネル数は 1 以上である必要があります。");
            }

            Samples = samples.ToArray();
            SampleRate = sampleRate;
            Channels = channels;
            Timestamp = timestamp;
        }

        #endregion
    }
}
