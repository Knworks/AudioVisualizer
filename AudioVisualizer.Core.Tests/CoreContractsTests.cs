using System;
using System.Linq;
using AudioVisualizer.Core.Effects;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Tests
{
    /// <summary>
    /// Core 契約型の生成条件と基本的な振る舞いを検証します。
    /// </summary>
    [TestFixture]
    public class CoreContractsTests
    {
        #region 公開メソッド

        /// <summary>
        /// AudioDeviceInfo の正常生成を確認します。
        /// ■入力
        /// ・1. deviceId = "device-1"
        /// ・2. displayName = "Primary Speakers"
        /// ・3. inputSource = SystemOutput, isDefault = true
        /// ■確認内容
        /// ・1. 各プロパティへ入力値がそのまま設定される
        /// ・2. 既定デバイスであることを保持する
        /// </summary>
        [Test]
        public void Given_ValidArguments_When_CreatingAudioDeviceInfo_Then_PropertiesAreAssigned()
        {
            // 準備
            const string deviceId = "device-1";
            const string displayName = "Primary Speakers";

            // 実行
            var result = new AudioDeviceInfo(deviceId, displayName, InputSource.SystemOutput, true);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.DeviceId, Is.EqualTo(deviceId));
                Assert.That(result.DisplayName, Is.EqualTo(displayName));
                Assert.That(result.InputSource, Is.EqualTo(InputSource.SystemOutput));
                Assert.That(result.IsDefault, Is.True);
            });
        }

        /// <summary>
        /// AudioDeviceInfo の識別子必須制約を確認します。
        /// ■入力
        /// ・1. deviceId = ""
        /// ・2. displayName = "Speakers"
        /// ■確認内容
        /// ・1. deviceId を理由に ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_BlankDeviceId_When_CreatingAudioDeviceInfo_Then_ThrowsArgumentException()
        {
            // 準備
            static void Act() => _ = new AudioDeviceInfo("", "Speakers", InputSource.SystemOutput, false);

            // 実行と検証
            Assert.That(Act, Throws.ArgumentException.With.Property("ParamName").EqualTo("deviceId"));
        }

        /// <summary>
        /// VisualizerSettings の明示デバイス必須制約を確認します。
        /// ■入力
        /// ・1. useDefaultDevice = false
        /// ・2. deviceId = null
        /// ■確認内容
        /// ・1. deviceId を理由に ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_ExplicitDeviceWithoutId_When_CreatingVisualizerSettings_Then_ThrowsArgumentException()
        {
            // 準備
            static void Act() => _ = new VisualizerSettings(
                InputSource.Microphone,
                null,
                false,
                true,
                1.0,
                0.5,
                16);

            // 実行と検証
            Assert.That(Act, Throws.ArgumentException.With.Property("ParamName").EqualTo("deviceId"));
        }

        /// <summary>
        /// VisualizerSettings の正常生成を確認します。
        /// ■入力
        /// ・1. inputSource = Microphone
        /// ・2. deviceId = "mic-1"
        /// ・3. sensitivity = 1.25, smoothing = 0.8, barCount = 32
        /// ■確認内容
        /// ・1. 設定値が各プロパティへ保持される
        /// ・2. 明示デバイス設定が維持される
        /// </summary>
        [Test]
        public void Given_ValidArguments_When_CreatingVisualizerSettings_Then_PropertiesAreAssigned()
        {
            // 準備
            const string deviceId = "mic-1";

            // 実行
            var result = new VisualizerSettings(InputSource.Microphone, deviceId, false, true, 1.25, 0.8, 32);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.InputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(result.DeviceId, Is.EqualTo(deviceId));
                Assert.That(result.UseDefaultDevice, Is.False);
                Assert.That(result.IsActive, Is.True);
                Assert.That(result.Sensitivity, Is.EqualTo(1.25));
                Assert.That(result.Smoothing, Is.EqualTo(0.8));
                Assert.That(result.BarCount, Is.EqualTo(32));
            });
        }

        /// <summary>
        /// VisualizerSettings の数値範囲制約を確認します。
        /// ■入力
        /// ・1. sensitivity = 0 または負数
        /// ・2. smoothing = 範囲外の値
        /// ・3. barCount = 0
        /// ■確認内容
        /// ・1. それぞれ ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidSettings_When_CreatingVisualizerSettings_Then_ThrowsArgumentOutOfRangeException(
            [Values(0d, -1d)] double sensitivity,
            [Values(-0.1d, 1.1d)] double smoothing,
            [Values(0)] int barCount)
        {
            // 準備
            void ActSensitivity() => _ = new VisualizerSettings(InputSource.SystemOutput, null, true, false, sensitivity, 0.5, 16);
            void ActSmoothing() => _ = new VisualizerSettings(InputSource.SystemOutput, null, true, false, 1.0, smoothing, 16);
            void ActBarCount() => _ = new VisualizerSettings(InputSource.SystemOutput, null, true, false, 1.0, 0.5, barCount);

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(ActSensitivity, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(ActSmoothing, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(ActBarCount, Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        /// <summary>
        /// VisualizerFrame が入力配列をコピーして保持することを確認します。
        /// ■入力
        /// ・1. spectrumValues = { 0.1, 0.4, 0.9 }
        /// ・2. peakLevel = 0.9
        /// ・3. timestamp = 固定日時
        /// ■確認内容
        /// ・1. SpectrumValues が入力配列の変更に影響されない
        /// ・2. PeakLevel と Timestamp が保持される
        /// </summary>
        [Test]
        public void Given_ValidSpectrum_When_CreatingVisualizerFrame_Then_SpectrumValuesAreCopied()
        {
            // 準備
            var timestamp = new DateTimeOffset(2026, 3, 14, 12, 0, 0, TimeSpan.Zero);
            var source = new[] { 0.1, 0.4, 0.9 };

            // 実行
            var result = new VisualizerFrame(source, 0.9, timestamp);
            source[0] = 99;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.SpectrumValues, Is.EqualTo(new[] { 0.1, 0.4, 0.9 }));
                Assert.That(result.PeakLevel, Is.EqualTo(0.9));
                Assert.That(result.Timestamp, Is.EqualTo(timestamp));
            });
        }

        /// <summary>
        /// VisualizerFrame のピーク値制約を確認します。
        /// ■入力
        /// ・1. peakLevel = -0.1
        /// ■確認内容
        /// ・1. peakLevel を理由に ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidPeakLevel_When_CreatingVisualizerFrame_Then_ThrowsArgumentOutOfRangeException()
        {
            // 準備
            static void Act() => _ = new VisualizerFrame(new[] { 0.1, 0.2 }, -0.1, DateTimeOffset.UtcNow);

            // 実行と検証
            Assert.That(Act, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// VisualizerFrame のスペクトラム必須制約を確認します。
        /// ■入力
        /// ・1. spectrumValues = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullSpectrumValues_When_CreatingVisualizerFrame_Then_ThrowsArgumentNullException()
        {
            // 準備
            static void Act() => _ = new VisualizerFrame(null!, 0.1, DateTimeOffset.UtcNow);

            // 実行と検証
            Assert.That(Act, Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// VisualizerFrame の波形必須制約を確認します。
        /// ■入力
        /// ・1. waveformValues = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullWaveformValues_When_CreatingVisualizerFrame_Then_ThrowsArgumentNullException()
        {
            // 準備
            static void Act() => _ = new VisualizerFrame(new[] { 0.1, 0.2 }, null!, 0.1, DateTimeOffset.UtcNow);

            // 実行と検証
            Assert.That(Act, Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// EffectMetadata が対応入力元を重複排除して保持することを確認します。
        /// ■入力
        /// ・1. supportedInputSources に重複した InputSource を含める
        /// ■確認内容
        /// ・1. 対応入力元が重複排除された状態で保持される
        /// ・2. 基本メタデータが各プロパティへ設定される
        /// </summary>
        [Test]
        public void Given_MetadataArguments_When_CreatingEffectMetadata_Then_SupportedInputsAreDistinct()
        {
            // 準備と実行
            var result = new EffectMetadata(
                "spectrum-bars",
                "Spectrum Bars",
                "1.0.0",
                "OpenAI",
                "Renders bars from audio levels.",
                new[] { InputSource.SystemOutput, InputSource.SystemOutput, InputSource.Microphone });

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.Id, Is.EqualTo("spectrum-bars"));
                Assert.That(result.DisplayName, Is.EqualTo("Spectrum Bars"));
                Assert.That(result.Version, Is.EqualTo("1.0.0"));
                Assert.That(result.Author, Is.EqualTo("OpenAI"));
                Assert.That(result.Description, Is.EqualTo("Renders bars from audio levels."));
                Assert.That(result.SupportedInputSources, Is.EqualTo(new[] { InputSource.SystemOutput, InputSource.Microphone }));
            });
        }

        /// <summary>
        /// EffectMetadata の対応入力元必須制約を確認します。
        /// ■入力
        /// ・1. supportedInputSources = 空配列
        /// ■確認内容
        /// ・1. supportedInputSources を理由に ArgumentException が送出される
        /// </summary>
        [Test]
        public void Given_EmptySupportedInputs_When_CreatingEffectMetadata_Then_ThrowsArgumentException()
        {
            // 準備
            static void Act() => _ = new EffectMetadata("id", "name", "1.0.0", null, null, Enumerable.Empty<InputSource>());

            // 実行と検証
            Assert.That(Act, Throws.ArgumentException.With.Property("ParamName").EqualTo("supportedInputSources"));
        }

        /// <summary>
        /// EffectMetadata の対応入力元 null 制約を確認します。
        /// ■入力
        /// ・1. supportedInputSources = null
        /// ■確認内容
        /// ・1. ArgumentNullException が送出される
        /// </summary>
        [Test]
        public void Given_NullSupportedInputs_When_CreatingEffectMetadata_Then_ThrowsArgumentNullException()
        {
            // 準備
            static void Act() => _ = new EffectMetadata("id", "name", "1.0.0", null, null, null!);

            // 実行と検証
            Assert.That(Act, Throws.TypeOf<ArgumentNullException>());
        }

        /// <summary>
        /// VisualizerEffectContext の数値範囲制約を確認します。
        /// ■入力
        /// ・1. sensitivity = 0
        /// ・2. smoothing = 範囲外の値
        /// ・3. barCount = 0
        /// ■確認内容
        /// ・1. それぞれ ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidContextValues_When_CreatingVisualizerEffectContext_Then_ThrowsArgumentOutOfRangeException()
        {
            // 準備
            static void ActSensitivity() => _ = new VisualizerEffectContext(InputSource.SystemOutput, 0, 0.5, 16);
            static void ActSmoothing() => _ = new VisualizerEffectContext(InputSource.SystemOutput, 1, 1.2, 16);
            static void ActBarCount() => _ = new VisualizerEffectContext(InputSource.SystemOutput, 1, 0.5, 0);

            // 実行と検証
            Assert.Multiple(() =>
            {
                Assert.That(ActSensitivity, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(ActSmoothing, Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(ActBarCount, Throws.TypeOf<ArgumentOutOfRangeException>());
            });
        }

        /// <summary>
        /// VisualizerEffectContext の正常生成を確認します。
        /// ■入力
        /// ・1. inputSource = Microphone
        /// ・2. sensitivity = 1.5, smoothing = 0.65, barCount = 24
        /// ■確認内容
        /// ・1. 各プロパティへ入力値が保持される
        /// </summary>
        [Test]
        public void Given_ValidContextValues_When_CreatingVisualizerEffectContext_Then_PropertiesAreAssigned()
        {
            // 準備と実行
            var result = new VisualizerEffectContext(InputSource.Microphone, 1.5, 0.65, 24);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(result.InputSource, Is.EqualTo(InputSource.Microphone));
                Assert.That(result.Sensitivity, Is.EqualTo(1.5));
                Assert.That(result.Smoothing, Is.EqualTo(0.65));
                Assert.That(result.BarCount, Is.EqualTo(24));
            });
        }

        /// <summary>
        /// IVisualizerEffect 実装からメタデータと描画指示へアクセスできることを確認します。
        /// ■入力
        /// ・1. FakeVisualizerEffect と VisualizerFrame を生成する
        /// ・2. VisualizerEffectContext を指定して BuildRenderData を呼び出す
        /// ■確認内容
        /// ・1. Metadata が実装から参照できる
        /// ・2. 返却された VisualizerRenderData に effectId と timestamp が設定される
        /// </summary>
        [Test]
        public void Given_EffectImplementation_When_BuildingRenderData_Then_MetadataAndRenderDataAreAccessible()
        {
            // 準備
            var metadata = new EffectMetadata(
                "bars",
                "Bars",
                "1.0.0",
                null,
                null,
                new[] { InputSource.SystemOutput });
            var effect = new FakeVisualizerEffect(metadata);
            var frame = new VisualizerFrame(new[] { 0.2, 0.4 }, 0.4, DateTimeOffset.UtcNow);
            var context = new VisualizerEffectContext(InputSource.SystemOutput, 1.0, 0.5, 16);

            // 実行
            var result = effect.BuildRenderData(frame, context);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(effect.Metadata, Is.SameAs(metadata));
                Assert.That(result.EffectId, Is.EqualTo(metadata.Id));
                Assert.That(result.Timestamp, Is.EqualTo(frame.Timestamp));
            });
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// 可視化エフェクト契約検証用のダミー実装です。
        /// </summary>
        private sealed class FakeVisualizerEffect : IVisualizerEffect
        {
            #region プロパティ

            /// <summary>
            /// このエフェクトのメタデータを取得します。
            /// </summary>
            public EffectMetadata Metadata { get; }

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeVisualizerEffect"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="metadata">保持するメタデータです。</param>
            public FakeVisualizerEffect(EffectMetadata metadata)
            {
                Metadata = metadata;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// ダミーの描画データを生成します。
            /// </summary>
            /// <param name="frame">解析済みフレームです。</param>
            /// <param name="context">描画コンテキストです。</param>
            /// <returns>ダミーの描画データです。</returns>
            public VisualizerRenderData BuildRenderData(VisualizerFrame frame, VisualizerEffectContext context)
            {
                return new FakeRenderData(Metadata.Id, frame.Timestamp);
            }

            #endregion
        }

        /// <summary>
        /// エフェクト ID と時刻だけを保持するダミー描画データです。
        /// </summary>
        private sealed class FakeRenderData : VisualizerRenderData
        {
            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeRenderData"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="effectId">エフェクト識別子です。</param>
            /// <param name="timestamp">描画データ作成時刻です。</param>
            public FakeRenderData(string effectId, DateTimeOffset timestamp)
                : base(effectId, timestamp)
            {
            }

            #endregion
        }

        #endregion
    }
}
