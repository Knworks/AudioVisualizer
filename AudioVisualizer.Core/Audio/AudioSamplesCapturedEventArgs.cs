using System;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// 音声キャプチャから取得した PCM サンプルを表します。
    /// </summary>
    internal sealed class AudioSamplesCapturedEventArgs : EventArgs
    {
        #region プロパティ

        /// <summary>
        /// 取得した PCM サンプル列を取得します。
        /// </summary>
        public float[] Samples { get; }

        /// <summary>
        /// サンプルレートを取得します。
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// チャンネル数を取得します。
        /// </summary>
        public int Channels { get; }

        /// <summary>
        /// サンプル取得時刻を取得します。
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="AudioSamplesCapturedEventArgs"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="samples">取得した PCM サンプル列です。</param>
        /// <param name="sampleRate">サンプルレートです。</param>
        /// <param name="channels">チャンネル数です。</param>
        /// <param name="timestamp">サンプル取得時刻です。</param>
        /// <exception cref="ArgumentNullException"><paramref name="samples"/> が <see langword="null"/> の場合にスローされます。</exception>
        public AudioSamplesCapturedEventArgs(float[] samples, int sampleRate, int channels, DateTimeOffset timestamp)
        {
            Samples = samples ?? throw new ArgumentNullException(nameof(samples));
            SampleRate = sampleRate;
            Channels = channels;
            Timestamp = timestamp;
        }

        #endregion
    }
}
