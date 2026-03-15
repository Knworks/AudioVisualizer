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

        #region 構築 / 消滅

        /// <summary>
        /// 各テストで使用する解析サービスと設定を初期化します。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            m_Sut = new AudioFrameAnalyzer();
            m_Settings = new VisualizerSettings(InputSource.SystemOutput, null, true, true, 1.0, 0.5, 8);
        }

        #endregion

        #region 公開メソッド

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

        #endregion
    }
}
