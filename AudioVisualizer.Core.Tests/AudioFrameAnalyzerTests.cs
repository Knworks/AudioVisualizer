using System;
using System.Linq;
using AudioVisualizer.Core.Analysis;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// AudioFrameAnalyzer の解析結果を検証します。
    /// </summary>
    [TestFixture]
    public class AudioFrameAnalyzerTests
    {
        #region フィールド

        /// <summary>
        /// テスト対象の解析サービスです。
        /// </summary>
        private AudioFrameAnalyzer m_Sut = null!;

        /// <summary>
        /// 解析に使用する共通設定です。
        /// </summary>
        private VisualizerSettings m_Settings = null!;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 各テストで使用する解析サービスと設定を初期化します。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_Sut = new AudioFrameAnalyzer();
            m_Settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
        }

        /// <summary>
        /// AudioFrameAnalyzer が可聴サンプルからスペクトラムとピークを生成することを確認します。
        /// ■入力
        /// ・1. 440Hz の疑似サイン波を含む AudioSampleBuffer
        /// ・2. barCount = 8 の VisualizerSettings
        /// ■確認内容
        /// ・1. SpectrumValues が barCount 分生成される
        /// ・2. PeakLevel が 0 より大きくなる
        /// ・3. WaveformValues が barCount 分生成される
        /// </summary>
        [Test]
        public void Given_AudibleSamples_When_CreateFrame_Then_SpectrumAndPeakAreGenerated()
        {
            // 準備
            var samples = Enumerable.Range(0, 512)
                .Select(index => (float)Math.Sin((2.0 * Math.PI * 440.0 * index) / 48000.0))
                .ToArray();
            var sampleBuffer = new AudioSampleBuffer(samples, 48000, 2, DateTimeOffset.UtcNow);

            // 実行
            var result = m_Sut.CreateFrame(sampleBuffer, m_Settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.SpectrumValues.Count, Is.EqualTo(m_Settings.BarCount));
                Assert.That(result.WaveformValues.Count, Is.EqualTo(m_Settings.BarCount));
                Assert.That(result.PeakLevel, Is.GreaterThan(0.0));
                Assert.That(result.SpectrumValues.Any(value => value > 0.0), Is.True);
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が無音サンプルでもゼロ埋めされたフレームを返すことを確認します。
        /// ■入力
        /// ・1. すべて 0 の AudioSampleBuffer
        /// ■確認内容
        /// ・1. SpectrumValues と WaveformValues がゼロで埋められる
        /// ・2. PeakLevel が 0 となる
        /// </summary>
        [Test]
        public void Given_SilentSamples_When_CreateFrame_Then_ZeroFilledFrameIsGenerated()
        {
            // 準備
            var sampleBuffer = new AudioSampleBuffer(new float[128], 48000, 2, DateTimeOffset.UtcNow);

            // 実行
            var result = m_Sut.CreateFrame(sampleBuffer, m_Settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.SpectrumValues, Is.All.EqualTo(0.0));
                Assert.That(result.WaveformValues, Is.All.EqualTo(0.0));
                Assert.That(result.PeakLevel, Is.EqualTo(0.0));
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が空サンプルでも barCount 分のゼロ埋め結果を返すことを確認します。
        /// ■入力
        /// ・1. 長さ 0 の AudioSampleBuffer
        /// ■確認内容
        /// ・1. SpectrumValues と WaveformValues が barCount 分生成される
        /// ・2. すべての値が 0 となる
        /// </summary>
        [Test]
        public void Given_EmptySamples_When_CreateFrame_Then_ZeroFilledFrameIsGenerated()
        {
            // 準備
            var sampleBuffer = new AudioSampleBuffer(Array.Empty<float>(), 48000, 2, DateTimeOffset.UtcNow);

            // 実行
            var result = m_Sut.CreateFrame(sampleBuffer, m_Settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.SpectrumValues.Count, Is.EqualTo(m_Settings.BarCount));
                Assert.That(result.WaveformValues.Count, Is.EqualTo(m_Settings.BarCount));
                Assert.That(result.SpectrumValues, Is.All.EqualTo(0.0));
                Assert.That(result.WaveformValues, Is.All.EqualTo(0.0));
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が単一サンプル入力でも最小スペクトラムを生成することを確認します。
        /// ■入力
        /// ・1. 1 サンプルのみを含む AudioSampleBuffer
        /// ・2. sensitivity = 1.0 の VisualizerSettings
        /// ■確認内容
        /// ・1. 先頭スペクトラム値がサンプル絶対値に基づいて設定される
        /// ・2. PeakLevel がサンプル絶対値と一致する
        /// </summary>
        [Test]
        public void Given_SingleSample_When_CreateFrame_Then_FirstSpectrumValueIsGenerated()
        {
            // 準備
            var sampleBuffer = new AudioSampleBuffer(new[] { -0.25f }, 48000, 1, DateTimeOffset.UtcNow);

            // 実行
            var result = m_Sut.CreateFrame(sampleBuffer, m_Settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.SpectrumValues[0], Is.EqualTo(0.25).Within(1e-10));
                Assert.That(result.PeakLevel, Is.EqualTo(0.25).Within(1e-10));
                Assert.That(result.WaveformValues[0], Is.EqualTo(-0.25).Within(1e-10));
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が波形生成で各区画の代表ピークを採用することを確認します。
        /// ■入力
        /// ・1. 正負が打ち消し合う 4 サンプルを含む AudioSampleBuffer
        /// ・2. barCount = 2 の VisualizerSettings
        /// ■確認内容
        /// ・1. 各区画の最大絶対値サンプルが WaveformValues に反映される
        /// </summary>
        [Test]
        public void Given_CancelingSamples_When_CreateFrame_Then_WaveformUsesRepresentativePeak()
        {
            // 準備
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 2);
            var sampleBuffer = new AudioSampleBuffer(new[] { 0.1f, 0.8f, -0.2f, -0.9f }, 48000, 1, DateTimeOffset.UtcNow);

            // 実行
            var result = m_Sut.CreateFrame(sampleBuffer, settings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.WaveformValues.Count, Is.EqualTo(2));
                Assert.That(result.WaveformValues[0], Is.EqualTo(0.8).Within(1e-6));
                Assert.That(result.WaveformValues[1], Is.EqualTo(-0.9).Within(1e-6));
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が波形生成時にも感度係数を適用することを確認します。
        /// ■入力
        /// ・1. 代表ピーク 0.3 を含む AudioSampleBuffer
        /// ・2. sensitivity = 2.0 の VisualizerSettings
        /// ■確認内容
        /// ・1. WaveformValues が感度係数に応じて拡大される
        /// ・2. 1.0 を超える場合はクリップされる
        /// </summary>
        [Test]
        public void Given_WaveformSensitivity_When_CreateFrame_Then_WaveformAmplitudeIsScaled()
        {
            // 準備
            var boostedSettings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 2.0, 0.5, 2);
            var clippedSettings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 4.0, 0.5, 2);
            var sampleBuffer = new AudioSampleBuffer(new[] { 0.3f, 0.1f, -0.2f, -0.1f }, 48000, 1, DateTimeOffset.UtcNow);

            // 実行
            var boostedFrame = m_Sut.CreateFrame(sampleBuffer, boostedSettings);
            var clippedFrame = m_Sut.CreateFrame(sampleBuffer, clippedSettings);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(boostedFrame.WaveformValues[0], Is.EqualTo(0.6).Within(1e-6));
                Assert.That(clippedFrame.WaveformValues[0], Is.EqualTo(1.0).Within(1e-6));
            });
        }

        /// <summary>
        /// AudioFrameAnalyzer が null 入力を拒否することを確認します。
        /// ■入力
        /// ・1. sampleBuffer = null
        /// ・2. settings = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullInput_When_CreateFrame_Then_ThrowsArgumentNullException()
        {
            // 準備
            var sampleBuffer = new AudioSampleBuffer(new float[16], 48000, 2, DateTimeOffset.UtcNow);

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(() => m_Sut.CreateFrame(null!, m_Settings), Throws.TypeOf<ArgumentNullException>());
                Assert.That(() => m_Sut.CreateFrame(sampleBuffer, null!), Throws.TypeOf<ArgumentNullException>());
            });
        }

        /// <summary>
        /// バーの代表周波数から外れた正弦波も、該当する線形周波数帯に現れることを確認します。
        /// </summary>
        /// <param name="frequency">入力する正弦波の周波数。単位は Hz です。</param>
        [TestCase(468.75)]
        [TestCase(796.875)]
        [TestCase(2500)]
        [TestCase(5400)]
        [TestCase(9150)]
        [TestCase(12300)]
        [TestCase(17000)]
        [TestCase(22700)]
        public void Spectrum_WhenToneFrequencyVaries_ShowsItInTheExpectedBand(double frequency)
        {
            const int sampleRate = 48000;
            const int sampleCount = 1024;
            const int barCount = 32;
            var samples = new float[sampleCount];
            for (var index = 0; index < samples.Length; index++)
            {
                samples[index] = (float)(0.2 * Math.Sin(2 * Math.PI * frequency * index / sampleRate));
            }

            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, barCount);
            var frame = new AudioFrameAnalyzer().CreateFrame(
                new AudioSampleBuffer(samples, sampleRate, 1, DateTimeOffset.UtcNow), settings);
            var bandIndex = (int)(frequency * barCount / (sampleRate / 2));

            Assert.That(frame.SpectrumValues[bandIndex], Is.GreaterThan(0.30));
        }

        /// <summary>
        /// 2 サンプルの窓でも交互に振れる入力を検出できることを確認します。
        /// </summary>
        [Test]
        public void Spectrum_WhenInputHasTwoSamples_DetectsAlternatingSignal()
        {
            var frame = new AudioFrameAnalyzer().CreateFrame(
                new AudioSampleBuffer(new[] { 0.05f, -0.05f }, 48000, 1, DateTimeOffset.UtcNow), m_Settings);

            Assert.That(frame.SpectrumValues.Max(), Is.GreaterThan(0.3));
        }

        /// <summary>
        /// 同相、片側だけ、逆相のステレオでも、同じ音の周波数と振幅を保ちます。
        /// </summary>
        /// <param name="rightGain">基準波形に対する右チャンネルの倍率。0 は無音、-1 は逆相です。</param>
        [TestCase(1)]
        [TestCase(0)]
        [TestCase(-1)]
        public void Spectrum_WhenStereoContainsSameTone_MatchesMonoSpectrum(int rightGain)
        {
            const int sampleRate = 48000;
            const int sampleCount = 1024;
            const double frequency = 3000;
            var mono = new float[sampleCount];
            var stereo = new float[sampleCount * 2];

            for (var index = 0; index < sampleCount; index++)
            {
                mono[index] = (float)(0.2 * Math.Sin(2 * Math.PI * frequency * index / sampleRate));
                stereo[index * 2] = mono[index];
                stereo[(index * 2) + 1] = mono[index] * rightGain;
            }

            var analyzer = new AudioFrameAnalyzer();
            var timestamp = DateTimeOffset.UtcNow;
            var monoFrame = analyzer.CreateFrame(new AudioSampleBuffer(mono, sampleRate, 1, timestamp), m_Settings);
            var stereoFrame = analyzer.CreateFrame(new AudioSampleBuffer(stereo, sampleRate, 2, timestamp), m_Settings);

            Assert.That(stereoFrame.SpectrumValues, Is.EqualTo(monoFrame.SpectrumValues).Within(1e-7));
        }

        /// <summary>
        /// 逆相で打ち消されず、右側だけに音がある場合も正しい時間軸で波形を生成します。
        /// </summary>
        /// <param name="leftGain">基準波形に対する左チャンネルの倍率。0 は無音を表します。</param>
        /// <param name="rightGain">基準波形に対する右チャンネルの倍率。0 は無音、-1 は逆相です。</param>
        [TestCase(1, -1)]
        [TestCase(0, 1)]
        public void Waveform_WhenStereoIsAntiphaseOrSingleChannel_ShowsSelectedChannel(int leftGain, int rightGain)
        {
            var mono = new[] { 0.2f, -0.1f, -0.2f, 0.1f };
            var stereo = mono.SelectMany(sample => new[] { sample * leftGain, sample * rightGain }).ToArray();
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.0, mono.Length);
            var frame = new AudioFrameAnalyzer().CreateFrame(
                new AudioSampleBuffer(stereo, 48000, 2, DateTimeOffset.UtcNow), settings);

            Assert.That(frame.WaveformValues, Is.EqualTo(mono.Select(sample => (double)sample)));
        }

        /// <summary>
        /// 短い取得ブロックを連結して、固定時間の波形を描画できることを確認します。
        /// </summary>
        [Test]
        public void CaptureWaveform_WhenPacketsAreShort_UsesOneFixedWindow()
        {
            var analyzer = new AudioFrameAnalyzer();
            var history = new WaveformHistory();
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.0, 12);
            var first = new AudioSampleBuffer(new[] { 0.25f, 0.5f, 0.75f, 1.0f }, 1000, 1, DateTimeOffset.UtcNow);
            var second = new AudioSampleBuffer(new[] { -0.25f, -0.5f, -0.75f, -1.0f }, 1000, 1, DateTimeOffset.UtcNow);

            analyzer.CreateFrame(first, settings, history);
            var frame = analyzer.CreateFrame(second, settings, history);

            Assert.That(frame.WaveformValues, Is.EqualTo(new[] { 0.0, 0.0, 0.0, 0.0, 0.25, 0.5, 0.75, 1.0, -0.25, -0.5, -0.75, -1.0 }));
        }

        /// <summary>
        /// 長いブロックの前半にある単発音を表示し、その次は最新の時間窓へ戻ります。
        /// </summary>
        [Test]
        public void CaptureWaveform_WhenLongPacketContainsEarlyTransient_ShowsItForOneFrame()
        {
            var analyzer = new AudioFrameAnalyzer();
            var history = new WaveformHistory();
            var settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.0, 12);
            var samples = new float[48];
            samples[3] = 0.75f;
            var packet = new AudioSampleBuffer(samples, 1000, 1, DateTimeOffset.UtcNow);
            var silence = new AudioSampleBuffer(new float[2], 1000, 1, DateTimeOffset.UtcNow);

            var transient = analyzer.CreateFrame(packet, settings, history);
            var next = analyzer.CreateFrame(silence, settings, history);

            Assert.That(transient.WaveformValues[3], Is.EqualTo(0.75));
            Assert.That(next.WaveformValues, Is.All.Zero);
        }

        #endregion
    }
}
