using System;
using System.Globalization;
using AudioVisualizer.SampleApp.Properties;

namespace AudioVisualizer.SampleApp.Tests
{
    /// <summary>
    /// <see cref="Strings"/> のカルチャ別文言を検証します。
    /// </summary>
    [TestFixture]
    public class StringsTests
    {
        #region 公開メソッド

        /// <summary>
        /// Strings が日本語 UI カルチャで日本語文言を返すことを確認します。
        /// ■入力
        /// ・1. ja-JP のカルチャ
        /// ■確認内容
        /// ・1. 各公開プロパティが日本語文言を返す
        /// ・2. 現在値書式が日本語になる
        /// </summary>
        [Test]
        public void Given_JapaneseUiCulture_When_ReadingStrings_Then_JapaneseTextsAreReturned()
        {
            RunWithCulture(
                new CultureInfo("ja-JP"),
                () =>
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(Strings.WindowTitle, Is.EqualTo("AudioVisualizer サンプル"));
                        Assert.That(Strings.InputSourceLabel, Is.EqualTo("InputSource"));
                        Assert.That(Strings.DeviceLabel, Is.EqualTo("DeviceId"));
                        Assert.That(Strings.ConnectionSettingsLabel, Is.EqualTo("UseDefaultDevice"));
                        Assert.That(Strings.UseDefaultDeviceContent, Is.EqualTo("UseDefaultDevice"));
                        Assert.That(Strings.RefreshButtonLabel, Is.EqualTo("再読込"));
                        Assert.That(Strings.StartButtonLabel, Is.EqualTo("開始"));
                        Assert.That(Strings.StopButtonLabel, Is.EqualTo("停止"));
                        Assert.That(Strings.BuiltInEffectLabel, Is.EqualTo("BuiltInEffect"));
                        Assert.That(Strings.SpectrumProfileLabel, Is.EqualTo("SpectrumProfile"));
                        Assert.That(Strings.BarCountLabel, Is.EqualTo("BarCount"));
                        Assert.That(Strings.SensitivityLabel, Is.EqualTo("Sensitivity"));
                        Assert.That(Strings.SmoothingLabel, Is.EqualTo("Smoothing"));
                        Assert.That(Strings.CurrentValueFormat, Is.EqualTo("現在値: {0}"));
                        Assert.That(Strings.FooterDescription, Does.StartWith("入力種別、デバイス"));
                        Assert.That(Strings.SystemOutputOptionLabel, Is.EqualTo("システム再生音"));
                        Assert.That(Strings.MicrophoneOptionLabel, Is.EqualTo("マイク"));
                        Assert.That(Strings.BalancedProfileOptionLabel, Is.EqualTo("Balanced: 偏りを緩和"));
                        Assert.That(Strings.RawProfileOptionLabel, Is.EqualTo("Raw: 既定の分布"));
                        Assert.That(Strings.HighBoostProfileOptionLabel, Is.EqualTo("HighBoost: 高域を強調"));
                        Assert.That(Strings.SpectrumBarEffectOptionLabel, Is.EqualTo("SpectrumBar: スペクトラムバー"));
                        Assert.That(Strings.WaveformLineEffectOptionLabel, Is.EqualTo("WaveformLine: 波形ライン"));
                        Assert.That(Strings.MirrorBarEffectOptionLabel, Is.EqualTo("MirrorBar: ミラーバー"));
                        Assert.That(Strings.PeakHoldBarEffectOptionLabel, Is.EqualTo("PeakHoldBar: ピーク保持バー"));
                        Assert.That(Strings.BandLevelMeterEffectOptionLabel, Is.EqualTo("BandLevelMeter: 帯域メーター"));
                        Assert.That(Strings.NoAvailableDevicesMessage, Does.StartWith("利用可能なデバイスがありません"));
                        Assert.That(Strings.DeviceLoadFailedMessage, Does.StartWith("デバイス一覧の取得に失敗しました"));
                        Assert.That(Strings.RevertedToDefaultDeviceMessage, Does.StartWith("利用可能なデバイスがないため"));
                        Assert.That(Strings.FormatCurrentValue("52"), Is.EqualTo("現在値: 52"));
                    });
                });
        }

        /// <summary>
        /// Strings が日本語以外の UI カルチャで英語文言を返すことを確認します。
        /// ■入力
        /// ・1. en-US のカルチャ
        /// ■確認内容
        /// ・1. 各公開プロパティが英語文言を返す
        /// ・2. 現在値書式が英語になる
        /// </summary>
        [Test]
        public void Given_NonJapaneseUiCulture_When_ReadingStrings_Then_EnglishTextsAreReturned()
        {
            RunWithCulture(
                new CultureInfo("en-US"),
                () =>
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(Strings.WindowTitle, Is.EqualTo("AudioVisualizer SampleApp"));
                        Assert.That(Strings.InputSourceLabel, Is.EqualTo("InputSource"));
                        Assert.That(Strings.DeviceLabel, Is.EqualTo("DeviceId"));
                        Assert.That(Strings.ConnectionSettingsLabel, Is.EqualTo("UseDefaultDevice"));
                        Assert.That(Strings.UseDefaultDeviceContent, Is.EqualTo("UseDefaultDevice"));
                        Assert.That(Strings.RefreshButtonLabel, Is.EqualTo("Refresh"));
                        Assert.That(Strings.StartButtonLabel, Is.EqualTo("Start"));
                        Assert.That(Strings.StopButtonLabel, Is.EqualTo("Stop"));
                        Assert.That(Strings.BuiltInEffectLabel, Is.EqualTo("BuiltInEffect"));
                        Assert.That(Strings.SpectrumProfileLabel, Is.EqualTo("SpectrumProfile"));
                        Assert.That(Strings.BarCountLabel, Is.EqualTo("BarCount"));
                        Assert.That(Strings.SensitivityLabel, Is.EqualTo("Sensitivity"));
                        Assert.That(Strings.SmoothingLabel, Is.EqualTo("Smoothing"));
                        Assert.That(Strings.CurrentValueFormat, Is.EqualTo("Current: {0}"));
                        Assert.That(Strings.FooterDescription, Does.StartWith("This sample lets you change"));
                        Assert.That(Strings.SystemOutputOptionLabel, Is.EqualTo("System Output"));
                        Assert.That(Strings.MicrophoneOptionLabel, Is.EqualTo("Microphone"));
                        Assert.That(Strings.BalancedProfileOptionLabel, Is.EqualTo("Balanced: Reduce bias"));
                        Assert.That(Strings.RawProfileOptionLabel, Is.EqualTo("Raw: Default distribution"));
                        Assert.That(Strings.HighBoostProfileOptionLabel, Is.EqualTo("HighBoost: Emphasize highs"));
                        Assert.That(Strings.SpectrumBarEffectOptionLabel, Is.EqualTo("SpectrumBar: Spectrum Bars"));
                        Assert.That(Strings.WaveformLineEffectOptionLabel, Is.EqualTo("WaveformLine: Waveform Line"));
                        Assert.That(Strings.MirrorBarEffectOptionLabel, Is.EqualTo("MirrorBar: Mirror Bars"));
                        Assert.That(Strings.PeakHoldBarEffectOptionLabel, Is.EqualTo("PeakHoldBar: Peak Hold Bars"));
                        Assert.That(Strings.BandLevelMeterEffectOptionLabel, Is.EqualTo("BandLevelMeter: Band Level Meter"));
                        Assert.That(Strings.NoAvailableDevicesMessage, Does.StartWith("No available devices were found"));
                        Assert.That(Strings.DeviceLoadFailedMessage, Does.StartWith("Failed to load the device list"));
                        Assert.That(Strings.RevertedToDefaultDeviceMessage, Does.StartWith("No available devices were found"));
                        Assert.That(Strings.FormatCurrentValue("52"), Is.EqualTo("Current: 52"));
                    });
                });
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 指定カルチャでテスト処理を実行し、終了後に元のカルチャへ戻します。
        /// </summary>
        /// <param name="culture">テスト実行中に適用するカルチャです。</param>
        /// <param name="action">指定カルチャで実行する処理です。</param>
        private static void RunWithCulture(CultureInfo culture, Action action)
        {
            ArgumentNullException.ThrowIfNull(culture);
            ArgumentNullException.ThrowIfNull(action);

            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;
            var previousStringsCulture = Strings.Culture;

            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                Strings.Culture = culture;
                action();
            }
            finally
            {
                Strings.Culture = previousStringsCulture;
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        #endregion
    }
}
