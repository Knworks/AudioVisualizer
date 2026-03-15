using System.Reflection;
using System.Globalization;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Models;
using AudioVisualizer.SampleApp.Properties;
using AudioVisualizer.SampleApp.ViewModels;
using AudioVisualizer.Wpf;

namespace AudioVisualizer.SampleApp.Tests
{
    /// <summary>
    /// MainWindowViewModel の入力切替とデバイス選択ロジックを検証します。
    /// </summary>
    [TestFixture]
    public class MainWindowViewModelTests
    {
        #region 公開メソッド

        /// <summary>
        /// MainWindowViewModel が初期化時にシステム再生音デバイス一覧を読み込むことを確認します。
        /// ■入力
        /// ・1. SystemOutput と Microphone のデバイスを返す FakeAudioDeviceService
        /// ■確認内容
        /// ・1. 初期入力種別が SystemOutput になる
        /// ・2. デバイス一覧が SystemOutput 側で初期化される
        /// ・3. 既定デバイスの DeviceId が選択される
        /// ・4. 数値設定が既定値で初期化される
        /// ・5. スペクトラム計算プロファイルが既定値で初期化される
        /// </summary>
        [Test]
        public void Given_DeviceService_When_CreatingViewModel_Then_SystemOutputDevicesAreLoaded()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedInputSource, Is.EqualTo(InputSource.SystemOutput));
                Assert.That(sut.InputSourceOptions.Select(option => option.Value), Is.EqualTo(new[] { InputSource.SystemOutput, InputSource.Microphone }));
                Assert.That(sut.AvailableDevices.Select(device => device.DeviceId), Is.EqualTo(new[] { "render-1", "render-2" }));
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
                Assert.That(sut.BarCount, Is.EqualTo(52));
                Assert.That(sut.Sensitivity, Is.EqualTo(5.0).Within(1e-10));
                Assert.That(sut.Smoothing, Is.EqualTo(0.55).Within(1e-10));
                Assert.That(sut.SelectedSpectrumProfile, Is.EqualTo(SpectrumProfile.Balanced));
                Assert.That(sut.SelectedBuiltInEffectKind, Is.EqualTo(BuiltInVisualizerEffectKind.SpectrumBar));
                Assert.That(
                    sut.InputSourceOptions.Select(option => option.DisplayName),
                    Is.EqualTo(new[] { Strings.SystemOutputOptionLabel, Strings.MicrophoneOptionLabel }));
                Assert.That(sut.SpectrumProfileOptions.Select(option => option.Value), Is.EqualTo(new[] { SpectrumProfile.Balanced, SpectrumProfile.Raw, SpectrumProfile.HighBoost }));
                Assert.That(
                    sut.SpectrumProfileOptions.Select(option => option.DisplayName),
                    Is.EqualTo(
                        new[]
                        {
                            Strings.BalancedProfileOptionLabel,
                            Strings.RawProfileOptionLabel,
                            Strings.HighBoostProfileOptionLabel,
                        }));
                Assert.That(
                    sut.BuiltInEffectOptions.Select(option => option.Value),
                    Is.EqualTo(
                        new[]
                        {
                            BuiltInVisualizerEffectKind.SpectrumBar,
                            BuiltInVisualizerEffectKind.WaveformLine,
                            BuiltInVisualizerEffectKind.MirrorBar,
                            BuiltInVisualizerEffectKind.PeakHoldBar,
                            BuiltInVisualizerEffectKind.BandLevelMeter,
                        }));
                Assert.That(
                    sut.BuiltInEffectOptions.Select(option => option.DisplayName),
                    Is.EqualTo(
                        new[]
                        {
                            Strings.SpectrumBarEffectOptionLabel,
                            Strings.WaveformLineEffectOptionLabel,
                            Strings.MirrorBarEffectOptionLabel,
                            Strings.PeakHoldBarEffectOptionLabel,
                            Strings.BandLevelMeterEffectOptionLabel,
                        }));
                Assert.That(sut.SelectedEffect.Metadata.Id, Is.EqualTo("spectrum-bars"));
                Assert.That(sut.StatusMessage, Is.Empty);
                Assert.That(sut.BarCountDisplayText, Is.EqualTo(Strings.FormatCurrentValue("52")));
                Assert.That(sut.SensitivityDisplayText, Is.EqualTo(Strings.FormatCurrentValue("5.00")));
                Assert.That(sut.SmoothingDisplayText, Is.EqualTo(Strings.FormatCurrentValue("0.55")));
            });
        }

        /// <summary>
        /// MainWindowViewModel が日本語 UI カルチャで日本語文言を読み込むことを確認します。
        /// ■入力
        /// ・1. ja-JP の UI カルチャ
        /// ・2. デバイス未取得の FakeAudioDeviceService
        /// ■確認内容
        /// ・1. 選択肢表示名が日本語になる
        /// ・2. ステータスメッセージが日本語になる
        /// </summary>
        [Test]
        public void Given_JapaneseUiCulture_When_CreatingViewModel_Then_JapaneseTextsAreUsed()
        {
            RunWithCulture(
                new CultureInfo("ja-JP"),
                () =>
                {
                    // 準備
                    var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>()));

                    // 検証
                    Assert.Multiple(() =>
                    {
                        Assert.That(sut.InputSourceOptions[0].DisplayName, Is.EqualTo("システム再生音"));
                        Assert.That(sut.BuiltInEffectOptions[0].DisplayName, Is.EqualTo("SpectrumBar: スペクトラムバー"));
                        Assert.That(sut.BarCountDisplayText, Is.EqualTo("現在値: 52"));
                        Assert.That(sut.StatusMessage, Is.EqualTo("利用可能なデバイスがありません。既定デバイス利用のまま操作できます。"));
                    });
                });
        }

        /// <summary>
        /// MainWindowViewModel が日本語以外の UI カルチャで英語文言を読み込むことを確認します。
        /// ■入力
        /// ・1. en-US の UI カルチャ
        /// ・2. デバイス未取得の FakeAudioDeviceService
        /// ■確認内容
        /// ・1. 選択肢表示名が英語になる
        /// ・2. ステータスメッセージが英語になる
        /// </summary>
        [Test]
        public void Given_NonJapaneseUiCulture_When_CreatingViewModel_Then_EnglishTextsAreUsed()
        {
            RunWithCulture(
                new CultureInfo("en-US"),
                () =>
                {
                    // 準備
                    var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>()));

                    // 検証
                    Assert.Multiple(() =>
                    {
                        Assert.That(sut.InputSourceOptions[0].DisplayName, Is.EqualTo("System Output"));
                        Assert.That(sut.BuiltInEffectOptions[0].DisplayName, Is.EqualTo("SpectrumBar: Spectrum Bars"));
                        Assert.That(sut.BarCountDisplayText, Is.EqualTo("Current: 52"));
                        Assert.That(sut.StatusMessage, Is.EqualTo("No available devices were found. You can continue using the default device."));
                    });
                });
        }

        /// <summary>
        /// MainWindowViewModel が数値設定の変更をそのまま保持できることを確認します。
        /// ■入力
        /// ・1. BarCount = 48
        /// ・2. Sensitivity = 2.4
        /// ・3. Smoothing = 0.6
        /// ・4. SelectedSpectrumProfile = HighBoost
        /// ■確認内容
        /// ・1. 変更後の数値設定が保持される
        /// ・2. 変更後のスペクトラム計算プロファイルが保持される
        /// ・3. 同じ値の再設定でも状態が破綻しない
        /// </summary>
        [Test]
        public void Given_RenderSettings_When_ChangingNumericProperties_Then_NewValuesAreRetained()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.BarCount = 48;
            sut.Sensitivity = 2.4;
            sut.Smoothing = 0.6;
            sut.SelectedSpectrumProfile = SpectrumProfile.HighBoost;
            sut.BarCount = 48;
            sut.Sensitivity = 2.4;
            sut.Smoothing = 0.6;
            sut.SelectedSpectrumProfile = SpectrumProfile.HighBoost;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.BarCount, Is.EqualTo(48));
                Assert.That(sut.Sensitivity, Is.EqualTo(2.4).Within(1e-10));
                Assert.That(sut.Smoothing, Is.EqualTo(0.6).Within(1e-10));
                Assert.That(sut.SelectedSpectrumProfile, Is.EqualTo(SpectrumProfile.HighBoost));
            });
        }

        /// <summary>
        /// MainWindowViewModel が組込エフェクト選択の変更を現在エフェクトへ反映することを確認します。
        /// ■入力
        /// ・1. 5 種類の組込エフェクトを順に選択する
        /// ■確認内容
        /// ・1. 選択中の種別が更新される
        /// ・2. SelectedEffect が対応するメタデータを持つ
        /// </summary>
        [Test]
        public void Given_BuiltInEffectSelection_When_ChangingSelectedEffect_Then_CurrentEffectIsUpdated()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());
            var expectedEffects = new[]
            {
                (BuiltInVisualizerEffectKind.SpectrumBar, "spectrum-bars", "Spectrum Bars"),
                (BuiltInVisualizerEffectKind.WaveformLine, "waveform-line", "Waveform Line"),
                (BuiltInVisualizerEffectKind.MirrorBar, "mirror-bars", "Mirror Bars"),
                (BuiltInVisualizerEffectKind.PeakHoldBar, "peak-hold-bars", "Peak Hold Bars"),
                (BuiltInVisualizerEffectKind.BandLevelMeter, "band-level-meter", "Band Level Meter"),
            };

            // 実行
            foreach (var expectedEffect in expectedEffects)
            {
                sut.SelectedBuiltInEffectKind = expectedEffect.Item1;

                Assert.Multiple(() =>
                {
                    Assert.That(sut.SelectedBuiltInEffectKind, Is.EqualTo(expectedEffect.Item1));
                    Assert.That(sut.SelectedEffect.Metadata.Id, Is.EqualTo(expectedEffect.Item2));
                    Assert.That(sut.SelectedEffect.Metadata.DisplayName, Is.EqualTo(expectedEffect.Item3));
                });
            }

            // 検証
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.BandLevelMeter;
            Assert.That(sut.SelectedEffect.Metadata.Id, Is.EqualTo("band-level-meter"));
        }

        /// <summary>
        /// MainWindowViewModel が同一参照のエフェクト再設定を無視することを確認します。
        /// ■入力
        /// ・1. 現在保持している SelectedEffect と同じ参照
        /// ■確認内容
        /// ・1. 例外なく再設定を無視する
        /// ・2. SelectedEffect の参照が維持される
        /// </summary>
        [Test]
        public void Given_SameEffectReference_When_SettingSelectedEffectViaReflection_Then_CurrentReferenceIsPreserved()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());
            var currentEffect = sut.SelectedEffect;
            var property = typeof(MainWindowViewModel).GetProperty(
                "SelectedEffect",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var setter = property?.SetMethod;

            // 実行
            Assert.That(setter, Is.Not.Null);
            Assert.That(() => setter!.Invoke(sut, new object?[] { currentEffect }), Throws.Nothing);

            // 検証
            Assert.That(sut.SelectedEffect, Is.SameAs(currentEffect));
        }

        /// <summary>
        /// MainWindowViewModel が組込エフェクト切替時も入力状態とデバイス状態を維持することを確認します。
        /// ■入力
        /// ・1. UseDefaultDevice = false かつ IsActive = true の MainWindowViewModel
        /// ・2. SelectedBuiltInEffectKind の変更
        /// ■確認内容
        /// ・1. IsActive が維持される
        /// ・2. UseDefaultDevice と SelectedDeviceId が維持される
        /// </summary>
        [Test]
        public void Given_ActiveExplicitDeviceMode_When_ChangingBuiltInEffect_Then_InputStateAndDeviceSelectionArePreserved()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService())
            {
                UseDefaultDevice = false,
                SelectedDeviceId = "render-2",
            };
            sut.Start();

            // 実行
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.WaveformLine;
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.SpectrumBar;
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.MirrorBar;
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.PeakHoldBar;
            sut.SelectedBuiltInEffectKind = BuiltInVisualizerEffectKind.BandLevelMeter;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.IsActive, Is.True);
                Assert.That(sut.UseDefaultDevice, Is.False);
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-2"));
                Assert.That(sut.SelectedEffect.Metadata.Id, Is.EqualTo("band-level-meter"));
            });
        }

        /// <summary>
        /// BuiltInVisualizerEffects が未対応の組込エフェクト種別を拒否することを確認します。
        /// ■入力
        /// ・1. 定義外の BuiltInVisualizerEffectKind
        /// ■確認内容
        /// ・1. ArgumentOutOfRangeException が送出される
        /// </summary>
        [Test]
        public void Given_InvalidBuiltInEffectKind_When_CreatingEffect_Then_ArgumentOutOfRangeExceptionIsThrown()
        {
            // 準備
            var invalidKind = (BuiltInVisualizerEffectKind)(-1);

            // 実行と検証
            Assert.That(
                () => BuiltInVisualizerEffects.Create(invalidKind),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// MainWindowViewModel が入力種別変更で対応デバイス一覧へ切り替わることを確認します。
        /// ■入力
        /// ・1. Microphone デバイスを返す FakeAudioDeviceService
        /// ・2. SelectedInputSource = Microphone
        /// ■確認内容
        /// ・1. デバイス一覧が Microphone 側へ更新される
        /// ・2. 選択デバイスが Microphone の既定デバイスになる
        /// </summary>
        [Test]
        public void Given_InputSourceChanges_When_LoadingDevices_Then_MatchingDeviceListIsDisplayed()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.SelectedInputSource = InputSource.Microphone;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.AvailableDevices.Select(device => device.DeviceId), Is.EqualTo(new[] { "mic-1", "mic-2" }));
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("mic-1"));
            });
        }

        /// <summary>
        /// MainWindowViewModel が既定デバイス利用解除時に明示デバイス選択を有効化することを確認します。
        /// ■入力
        /// ・1. デバイス一覧を持つ MainWindowViewModel
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. CanSelectDevice が true になる
        /// ・2. SelectedDeviceId が明示選択可能な値を保持する
        /// </summary>
        [Test]
        public void Given_DefaultDeviceDisabled_When_ExplicitSelectionStarts_Then_DeviceSelectionIsEnabled()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.CanSelectDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
            });
        }

        /// <summary>
        /// MainWindowViewModel がデバイス未取得時に既定デバイス利用へ戻すことを確認します。
        /// ■入力
        /// ・1. 空一覧を返す FakeAudioDeviceService
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. UseDefaultDevice が true に戻る
        /// ・2. SelectedDeviceId が null になる
        /// ・3. 状態メッセージが表示される
        /// </summary>
        [Test]
        public void Given_NoDevices_When_DisablingDefaultDevice_Then_DefaultModeIsRestored()
        {
            // 準備
            var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>()));

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.Null);
                Assert.That(sut.StatusMessage, Is.EqualTo(Strings.RevertedToDefaultDeviceMessage));
            });
        }

        /// <summary>
        /// MainWindowViewModel がデバイス一覧取得失敗時も操作可能な状態を保つことを確認します。
        /// ■入力
        /// ・1. GetDevices で例外を送出する FakeAudioDeviceService
        /// ・2. StartCommand と StopCommand の実行
        /// ■確認内容
        /// ・1. 状態メッセージが表示される
        /// ・2. UseDefaultDevice が true になる
        /// ・3. Start と Stop の状態切替が継続できる
        /// </summary>
        [Test]
        public void Given_DeviceLoadFailure_When_RefreshingDevices_Then_ViewModelRemainsOperable()
        {
            // 準備
            var sut = new MainWindowViewModel(new FakeAudioDeviceService(Array.Empty<AudioDeviceInfo>(), Array.Empty<AudioDeviceInfo>(), throwOnGetDevices: true));

            // 実行
            sut.StartCommand.Execute(null);
            sut.StopCommand.Execute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.StatusMessage, Is.EqualTo(Strings.DeviceLoadFailedMessage));
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.IsActive, Is.False);
            });
        }

        /// <summary>
        /// MainWindowViewModel が再読込後も明示デバイス選択を維持できることを確認します。
        /// ■入力
        /// ・1. 明示デバイスを選択した MainWindowViewModel
        /// ・2. RefreshDevicesCommand 実行
        /// ■確認内容
        /// ・1. SelectedDeviceId が維持される
        /// ・2. RefreshDevicesCommand でデバイス一覧が再取得される
        /// </summary>
        [Test]
        public void Given_ExplicitDeviceSelection_When_RefreshingDevices_Then_SelectedDeviceIsPreserved()
        {
            // 準備
            var deviceService = CreateDeviceService();
            var sut = new MainWindowViewModel(deviceService)
            {
                UseDefaultDevice = false,
                SelectedDeviceId = "render-2",
            };
            deviceService.GetDevicesCallCount = 0;

            // 実行
            sut.RefreshDevicesCommand.Execute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-2"));
                Assert.That(deviceService.GetDevicesCallCount, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// MainWindowViewModel が開始停止コマンドの実行可否を状態に応じて切り替えることを確認します。
        /// ■入力
        /// ・1. StartCommand と StopCommand
        /// ■確認内容
        /// ・1. 初期状態では StartCommand のみ実行可能である
        /// ・2. 開始後は StopCommand のみ実行可能である
        /// </summary>
        [Test]
        public void Given_StartAndStopCommands_When_IsActiveChanges_Then_CanExecuteStateIsUpdated()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService());

            // 実行
            var canStartBefore = sut.StartCommand.CanExecute(null);
            var canStopBefore = sut.StopCommand.CanExecute(null);
            var canRefresh = sut.RefreshDevicesCommand.CanExecute(null);
            sut.Start();
            var canStartAfter = sut.StartCommand.CanExecute(null);
            var canStopAfter = sut.StopCommand.CanExecute(null);

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(canStartBefore, Is.True);
                Assert.That(canStopBefore, Is.False);
                Assert.That(canRefresh, Is.True);
                Assert.That(canStartAfter, Is.False);
                Assert.That(canStopAfter, Is.True);
            });
        }

        /// <summary>
        /// MainWindowViewModel が同じ値の再設定を無視することを確認します。
        /// ■入力
        /// ・1. 初期値と同じ SelectedInputSource
        /// ・2. Start 後に再度 Start する
        /// ■確認内容
        /// ・1. デバイス一覧の再取得が発生しない
        /// ・2. IsActive が維持される
        /// </summary>
        [Test]
        public void Given_SameValueAssignments_When_SettingProperties_Then_NoStateTransitionOccurs()
        {
            // 準備
            var deviceService = CreateDeviceService();
            var sut = new MainWindowViewModel(deviceService);
            deviceService.GetDevicesCallCount = 0;

            // 実行
            sut.SelectedInputSource = InputSource.SystemOutput;
            sut.Start();
            sut.Start();

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(deviceService.GetDevicesCallCount, Is.EqualTo(0));
                Assert.That(sut.IsActive, Is.True);
            });
        }

        /// <summary>
        /// MainWindowViewModel が一覧更新でデバイス消失時に既定デバイス利用へ戻すことを確認します。
        /// ■入力
        /// ・1. SystemOutput のみデバイスを持つ FakeAudioDeviceService
        /// ・2. UseDefaultDevice = false の状態で SelectedInputSource = Microphone
        /// ■確認内容
        /// ・1. UseDefaultDevice が true に戻る
        /// ・2. SelectedDeviceId が null になる
        /// </summary>
        [Test]
        public void Given_ExplicitMode_When_InputSourceChangesToEmptyDeviceList_Then_DefaultModeIsRestored()
        {
            // 準備
            var sut = new MainWindowViewModel(
                new FakeAudioDeviceService(
                    new[] { new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true) },
                    Array.Empty<AudioDeviceInfo>()))
            {
                UseDefaultDevice = false,
            };

            // 実行
            sut.SelectedInputSource = InputSource.Microphone;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.UseDefaultDevice, Is.True);
                Assert.That(sut.SelectedDeviceId, Is.Null);
            });
        }

        /// <summary>
        /// MainWindowViewModel が不正な明示デバイス指定を既定候補へ補正することを確認します。
        /// ■入力
        /// ・1. SelectedDeviceId = "missing-device"
        /// ・2. UseDefaultDevice = false
        /// ■確認内容
        /// ・1. SelectedDeviceId が既定デバイスへ補正される
        /// ・2. CanSelectDevice が true になる
        /// </summary>
        [Test]
        public void Given_InvalidExplicitDevice_When_DisablingDefaultDevice_Then_DefaultCandidateIsSelected()
        {
            // 準備
            var sut = new MainWindowViewModel(CreateDeviceService())
            {
                SelectedDeviceId = "missing-device",
            };

            // 実行
            sut.UseDefaultDevice = false;

            // 検証
            Assert.Multiple(() =>
            {
                Assert.That(sut.SelectedDeviceId, Is.EqualTo("render-1"));
                Assert.That(sut.CanSelectDevice, Is.True);
            });
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 複数入力種別のデバイス一覧を返す偽サービスを生成します。
        /// </summary>
        /// <returns>テスト用の偽デバイスサービスです。</returns>
        private static FakeAudioDeviceService CreateDeviceService()
        {
            return new FakeAudioDeviceService(
                new[]
                {
                    new AudioDeviceInfo("render-1", "Speakers", InputSource.SystemOutput, true),
                    new AudioDeviceInfo("render-2", "Headphones", InputSource.SystemOutput, false),
                },
                new[]
                {
                    new AudioDeviceInfo("mic-1", "Microphone", InputSource.Microphone, true),
                    new AudioDeviceInfo("mic-2", "USB Microphone", InputSource.Microphone, false),
                });
        }

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

            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                action();
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }
        }

        #endregion

        #region 内部クラス

        /// <summary>
        /// テスト用の音声デバイスサービスです。
        /// </summary>
        private sealed class FakeAudioDeviceService : IAudioDeviceService
        {
            #region フィールド

            /// <summary>
            /// システム再生音デバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_SystemOutputDevices;

            /// <summary>
            /// マイクデバイス一覧です。
            /// </summary>
            private readonly IReadOnlyList<AudioDeviceInfo> m_MicrophoneDevices;

            /// <summary>
            /// 一覧取得で例外を送出するかどうかを示します。
            /// </summary>
            private readonly bool m_ThrowOnGetDevices;

            #endregion

            #region プロパティ

            /// <summary>
            /// GetDevices の呼び出し回数を取得または設定します。
            /// </summary>
            public int GetDevicesCallCount { get; set; }

            #endregion

            #region 構築 / 消滅

            /// <summary>
            /// <see cref="FakeAudioDeviceService"/> クラスの新しいインスタンスを初期化します。
            /// </summary>
            /// <param name="systemOutputDevices">システム再生音デバイス一覧です。</param>
            /// <param name="microphoneDevices">マイクデバイス一覧です。</param>
            /// <param name="throwOnGetDevices">一覧取得で例外を送出する場合は <see langword="true"/> です。</param>
            public FakeAudioDeviceService(
                IReadOnlyList<AudioDeviceInfo> systemOutputDevices,
                IReadOnlyList<AudioDeviceInfo> microphoneDevices,
                bool throwOnGetDevices = false)
            {
                m_SystemOutputDevices = systemOutputDevices;
                m_MicrophoneDevices = microphoneDevices;
                m_ThrowOnGetDevices = throwOnGetDevices;
            }

            #endregion

            #region 公開メソッド

            /// <summary>
            /// 指定入力種別のデバイス一覧を返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>入力種別に対応するデバイス一覧です。</returns>
            public IReadOnlyList<AudioDeviceInfo> GetDevices(InputSource inputSource)
            {
                GetDevicesCallCount++;
                if (m_ThrowOnGetDevices)
                {
                    throw new InvalidOperationException("device load failure");
                }

                return inputSource switch
                {
                    InputSource.SystemOutput => m_SystemOutputDevices,
                    InputSource.Microphone => m_MicrophoneDevices,
                    _ => Array.Empty<AudioDeviceInfo>(),
                };
            }

            /// <summary>
            /// 指定入力種別の既定デバイスを返します。
            /// </summary>
            /// <param name="inputSource">取得対象の入力種別です。</param>
            /// <returns>既定デバイスです。存在しない場合は <see langword="null"/> です。</returns>
            public AudioDeviceInfo? GetDefaultDevice(InputSource inputSource)
            {
                return GetDevices(inputSource).FirstOrDefault(device => device.IsDefault);
            }

            #endregion

            #region イベントハンドラ

            /// <summary>
            /// テスト対象では既定デバイス変更通知を使用しないため、購読だけを受け付けます。
            /// </summary>
            event EventHandler<DefaultAudioDeviceChangedEventArgs>? IAudioDeviceService.DefaultDeviceChanged
            {
                add
                {
                }

                remove
                {
                }
            }

            #endregion
        }

        #endregion
    }
}
