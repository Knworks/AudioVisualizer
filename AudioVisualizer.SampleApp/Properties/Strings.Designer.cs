using System;
using System.Globalization;
using System.Resources;

#nullable enable

namespace AudioVisualizer.SampleApp.Properties
{
    /// <summary>
    /// SampleApp で使用する文字列リソースへの型付きアクセサを提供します。
    /// </summary>
    public class Strings
    {
        #region フィールド

        /// <summary>
        /// リソースマネージャの遅延初期化用フィールドです。
        /// </summary>
        private static ResourceManager? s_ResourceManager;

        /// <summary>
        /// 明示指定されたカルチャです。<see langword="null"/> の場合は現在の UI カルチャを使用します。
        /// </summary>
        private static CultureInfo? s_Culture;

        #endregion

        #region プロパティ

        /// <summary>
        /// 文字列リソースの取得に使用するリソースマネージャを取得します。
        /// </summary>
        public static ResourceManager ResourceManager => s_ResourceManager ??=
            new ResourceManager("AudioVisualizer.SampleApp.Properties.Strings", typeof(Strings).Assembly);

        /// <summary>
        /// 文字列リソースの取得に使用するカルチャを取得または設定します。
        /// </summary>
        public static CultureInfo? Culture
        {
            get => s_Culture;
            set => s_Culture = value;
        }

        /// <summary>
        /// メインウィンドウのタイトルを取得します。
        /// </summary>
        public static string WindowTitle => GetString(nameof(WindowTitle));

        /// <summary>
        /// 入力種別ラベルを取得します。
        /// </summary>
        public static string InputSourceLabel => GetString(nameof(InputSourceLabel));

        /// <summary>
        /// デバイスラベルを取得します。
        /// </summary>
        public static string DeviceLabel => GetString(nameof(DeviceLabel));

        /// <summary>
        /// 接続設定ラベルを取得します。
        /// </summary>
        public static string ConnectionSettingsLabel => GetString(nameof(ConnectionSettingsLabel));

        /// <summary>
        /// 既定デバイス利用チェックボックスの文言を取得します。
        /// </summary>
        public static string UseDefaultDeviceContent => GetString(nameof(UseDefaultDeviceContent));

        /// <summary>
        /// 再読込ボタンの文言を取得します。
        /// </summary>
        public static string RefreshButtonLabel => GetString(nameof(RefreshButtonLabel));

        /// <summary>
        /// 開始ボタンの文言を取得します。
        /// </summary>
        public static string StartButtonLabel => GetString(nameof(StartButtonLabel));

        /// <summary>
        /// 停止ボタンの文言を取得します。
        /// </summary>
        public static string StopButtonLabel => GetString(nameof(StopButtonLabel));

        /// <summary>
        /// 組込エフェクトラベルを取得します。
        /// </summary>
        public static string BuiltInEffectLabel => GetString(nameof(BuiltInEffectLabel));

        /// <summary>
        /// スペクトラム計算プロファイルラベルを取得します。
        /// </summary>
        public static string SpectrumProfileLabel => GetString(nameof(SpectrumProfileLabel));

        /// <summary>
        /// バー本数ラベルを取得します。
        /// </summary>
        public static string BarCountLabel => GetString(nameof(BarCountLabel));

        /// <summary>
        /// 感度ラベルを取得します。
        /// </summary>
        public static string SensitivityLabel => GetString(nameof(SensitivityLabel));

        /// <summary>
        /// 平滑化ラベルを取得します。
        /// </summary>
        public static string SmoothingLabel => GetString(nameof(SmoothingLabel));

        /// <summary>
        /// 現在値表示の書式を取得します。
        /// </summary>
        public static string CurrentValueFormat => GetString(nameof(CurrentValueFormat));

        /// <summary>
        /// 画面下部の説明文を取得します。
        /// </summary>
        public static string FooterDescription => GetString(nameof(FooterDescription));

        /// <summary>
        /// 入力種別のシステム再生音表示名を取得します。
        /// </summary>
        public static string SystemOutputOptionLabel => GetString(nameof(SystemOutputOptionLabel));

        /// <summary>
        /// 入力種別のマイク表示名を取得します。
        /// </summary>
        public static string MicrophoneOptionLabel => GetString(nameof(MicrophoneOptionLabel));

        /// <summary>
        /// Balanced プロファイルの表示名を取得します。
        /// </summary>
        public static string BalancedProfileOptionLabel => GetString(nameof(BalancedProfileOptionLabel));

        /// <summary>
        /// Raw プロファイルの表示名を取得します。
        /// </summary>
        public static string RawProfileOptionLabel => GetString(nameof(RawProfileOptionLabel));

        /// <summary>
        /// HighBoost プロファイルの表示名を取得します。
        /// </summary>
        public static string HighBoostProfileOptionLabel => GetString(nameof(HighBoostProfileOptionLabel));

        /// <summary>
        /// SpectrumBar エフェクトの選択肢表示名を取得します。
        /// </summary>
        public static string SpectrumBarEffectOptionLabel => GetString(nameof(SpectrumBarEffectOptionLabel));

        /// <summary>
        /// WaveformLine エフェクトの選択肢表示名を取得します。
        /// </summary>
        public static string WaveformLineEffectOptionLabel => GetString(nameof(WaveformLineEffectOptionLabel));

        /// <summary>
        /// MirrorBar エフェクトの選択肢表示名を取得します。
        /// </summary>
        public static string MirrorBarEffectOptionLabel => GetString(nameof(MirrorBarEffectOptionLabel));

        /// <summary>
        /// PeakHoldBar エフェクトの選択肢表示名を取得します。
        /// </summary>
        public static string PeakHoldBarEffectOptionLabel => GetString(nameof(PeakHoldBarEffectOptionLabel));

        /// <summary>
        /// BandLevelMeter エフェクトの選択肢表示名を取得します。
        /// </summary>
        public static string BandLevelMeterEffectOptionLabel => GetString(nameof(BandLevelMeterEffectOptionLabel));

        /// <summary>
        /// 利用可能デバイスがない場合のメッセージを取得します。
        /// </summary>
        public static string NoAvailableDevicesMessage => GetString(nameof(NoAvailableDevicesMessage));

        /// <summary>
        /// デバイス一覧取得失敗時のメッセージを取得します。
        /// </summary>
        public static string DeviceLoadFailedMessage => GetString(nameof(DeviceLoadFailedMessage));

        /// <summary>
        /// 明示デバイスを維持できず既定デバイスへ戻したときのメッセージを取得します。
        /// </summary>
        public static string RevertedToDefaultDeviceMessage => GetString(nameof(RevertedToDefaultDeviceMessage));

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 現在値表示用の文言を組み立てます。
        /// </summary>
        /// <param name="valueText">現在値として表示する文字列です。</param>
        /// <returns>現在値表示用のローカライズ済み文字列です。</returns>
        public static string FormatCurrentValue(string valueText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(valueText);
            return string.Format(CultureInfo.CurrentCulture, CurrentValueFormat, valueText);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 指定キーに対応する文字列リソースを取得します。
        /// </summary>
        /// <param name="name">取得対象のリソースキーです。</param>
        /// <returns>ローカライズ済み文字列です。</returns>
        private static string GetString(string name)
        {
            return ResourceManager.GetString(name, Culture) ?? throw new MissingManifestResourceException(
                $"文字列リソース '{name}' が見つかりません。");
        }

        #endregion
    }
}
