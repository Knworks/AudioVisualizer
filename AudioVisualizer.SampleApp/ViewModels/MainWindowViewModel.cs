using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AudioVisualizer.Core.Audio;
using AudioVisualizer.Core.Models;
using AudioVisualizer.SampleApp.Commands;

namespace AudioVisualizer.SampleApp.ViewModels
{
    /// <summary>
    /// SampleApp の入力切替とデバイス選択状態を管理する ViewModel です。
    /// </summary>
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        #region 定数

        /// <summary>
        /// SampleApp で初期表示するバー本数です。
        /// </summary>
        private const int DefaultBarCount = 48;

        /// <summary>
        /// SampleApp で初期表示する感度です。
        /// </summary>
        private const double DefaultSensitivity = 3.0;

        /// <summary>
        /// SampleApp で初期表示する平滑化係数です。
        /// </summary>
        private const double DefaultSmoothing = 0.55;

        #endregion

        #region フィールド

        /// <summary>
        /// デバイス一覧取得を担当するサービスです。
        /// </summary>
        private readonly IAudioDeviceService m_AudioDeviceService;

        /// <summary>
        /// 現在選択中の入力種別です。
        /// </summary>
        private InputSource m_SelectedInputSource;

        /// <summary>
        /// 既定デバイスを使用するかどうかを保持します。
        /// </summary>
        private bool m_UseDefaultDevice;

        /// <summary>
        /// 現在選択中の明示デバイス識別子です。
        /// </summary>
        private string? m_SelectedDeviceId;

        /// <summary>
        /// 可視化の開始状態を保持します。
        /// </summary>
        private bool m_IsActive;

        /// <summary>
        /// 画面へ表示する状態メッセージです。
        /// </summary>
        private string m_StatusMessage;

        /// <summary>
        /// 現在のバー本数設定です。
        /// </summary>
        private int m_BarCount;

        /// <summary>
        /// 現在の感度設定です。
        /// </summary>
        private double m_Sensitivity;

        /// <summary>
        /// 現在の平滑化係数設定です。
        /// </summary>
        private double m_Smoothing;

        #endregion

        #region プロパティ

        /// <summary>
        /// 入力種別コンボボックスへ表示する選択肢一覧を取得します。
        /// </summary>
        public IReadOnlyList<InputSourceOption> InputSourceOptions { get; }

        /// <summary>
        /// 選択中の入力種別を取得または設定します。
        /// </summary>
        public InputSource SelectedInputSource
        {
            get => m_SelectedInputSource;
            set
            {
                if (m_SelectedInputSource == value)
                {
                    return;
                }

                m_SelectedInputSource = value;
                OnPropertyChanged();
                LoadDevices();
            }
        }

        /// <summary>
        /// 選択可能なデバイス一覧を取得します。
        /// </summary>
        public ObservableCollection<AudioDeviceInfo> AvailableDevices { get; } = new();

        /// <summary>
        /// 既定デバイスを使用するかどうかを取得または設定します。
        /// </summary>
        public bool UseDefaultDevice
        {
            get => m_UseDefaultDevice;
            set
            {
                if (m_UseDefaultDevice == value)
                {
                    return;
                }

                m_UseDefaultDevice = value;
                if (!m_UseDefaultDevice)
                {
                    EnsureExplicitDeviceSelection();
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSelectDevice));
            }
        }

        /// <summary>
        /// 明示指定するデバイス識別子を取得または設定します。
        /// </summary>
        public string? SelectedDeviceId
        {
            get => m_SelectedDeviceId;
            set
            {
                if (m_SelectedDeviceId == value)
                {
                    return;
                }

                m_SelectedDeviceId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可視化処理が開始中かどうかを取得または設定します。
        /// </summary>
        public bool IsActive
        {
            get => m_IsActive;
            set
            {
                if (m_IsActive == value)
                {
                    return;
                }

                m_IsActive = value;
                OnPropertyChanged();
                ((DelegateCommand)StartCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)StopCommand).RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// デバイス選択コンボボックスを有効化できるかどうかを取得します。
        /// </summary>
        public bool CanSelectDevice => !UseDefaultDevice && AvailableDevices.Count > 0;

        /// <summary>
        /// 画面へ表示する状態メッセージを取得します。
        /// </summary>
        public string StatusMessage
        {
            get => m_StatusMessage;
            private set
            {
                if (m_StatusMessage == value)
                {
                    return;
                }

                m_StatusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可視化コントロールへ渡すバー本数を取得または設定します。
        /// 値を減らすと 1 本ごとの動きが大きく見え、増やすと細かい表示になります。
        /// </summary>
        public int BarCount
        {
            get => m_BarCount;
            set
            {
                if (m_BarCount == value)
                {
                    return;
                }

                m_BarCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可視化コントロールへ渡す感度を取得または設定します。
        /// 値を上げるほど小さな音でもバーが高く反応しやすくなります。
        /// </summary>
        public double Sensitivity
        {
            get => m_Sensitivity;
            set
            {
                if (Math.Abs(m_Sensitivity - value) < 0.0001)
                {
                    return;
                }

                m_Sensitivity = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可視化コントロールへ渡す平滑化係数を取得または設定します。
        /// 値を上げるほど動きがなめらかになり、値を下げるほど反応が速くなります。
        /// </summary>
        public double Smoothing
        {
            get => m_Smoothing;
            set
            {
                if (Math.Abs(m_Smoothing - value) < 0.0001)
                {
                    return;
                }

                m_Smoothing = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 可視化開始コマンドを取得します。
        /// </summary>
        public ICommand StartCommand { get; }

        /// <summary>
        /// 可視化停止コマンドを取得します。
        /// </summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// デバイス一覧再読込コマンドを取得します。
        /// </summary>
        public ICommand RefreshDevicesCommand { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="MainWindowViewModel"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="audioDeviceService">デバイス一覧取得を担当するサービスです。</param>
        public MainWindowViewModel(IAudioDeviceService audioDeviceService)
        {
            m_AudioDeviceService = audioDeviceService ?? throw new ArgumentNullException(nameof(audioDeviceService));
            m_SelectedInputSource = InputSource.SystemOutput;
            m_UseDefaultDevice = true;
            m_StatusMessage = string.Empty;
            m_BarCount = DefaultBarCount;
            m_Sensitivity = DefaultSensitivity;
            m_Smoothing = DefaultSmoothing;

            InputSourceOptions = new[]
            {
                new InputSourceOption(InputSource.SystemOutput, "システム再生音"),
                new InputSourceOption(InputSource.Microphone, "マイク"),
            };

            StartCommand = new DelegateCommand(Start, () => !IsActive);
            StopCommand = new DelegateCommand(Stop, () => IsActive);
            RefreshDevicesCommand = new DelegateCommand(RefreshDevices);

            LoadDevices();
        }

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 可視化の開始状態へ切り替えます。
        /// </summary>
        public void Start()
        {
            IsActive = true;
        }

        /// <summary>
        /// 可視化の停止状態へ切り替えます。
        /// </summary>
        public void Stop()
        {
            IsActive = false;
        }

        /// <summary>
        /// 現在の入力種別に対するデバイス一覧を再読込します。
        /// </summary>
        public void RefreshDevices()
        {
            LoadDevices();
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// プロパティ変更通知が発生したときに呼び出されます。
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region 内部処理

        /// <summary>
        /// 現在の入力種別に対応するデバイス一覧を取得して画面状態へ反映します。
        /// </summary>
        private void LoadDevices()
        {
            var previousDeviceId = SelectedDeviceId;

            try
            {
                var devices = m_AudioDeviceService.GetDevices(SelectedInputSource);
                ReplaceDevices(devices);
                ApplyDeviceSelection(previousDeviceId);

                if (devices.Count == 0)
                {
                    StatusMessage = "利用可能なデバイスがありません。既定デバイス利用のまま操作できます。";
                    return;
                }

                StatusMessage = string.Empty;
            }
            catch (Exception)
            {
                ReplaceDevices(Array.Empty<AudioDeviceInfo>());
                SelectedDeviceId = null;
                UseDefaultDevice = true;
                StatusMessage = "デバイス一覧の取得に失敗しました。既定デバイス利用のまま操作できます。";
            }

            OnPropertyChanged(nameof(CanSelectDevice));
        }

        /// <summary>
        /// 取得したデバイス一覧でコレクションを置き換えます。
        /// </summary>
        /// <param name="devices">画面へ反映するデバイス一覧です。</param>
        private void ReplaceDevices(IReadOnlyList<AudioDeviceInfo> devices)
        {
            AvailableDevices.Clear();
            for (var index = 0; index < devices.Count; index++)
            {
                AvailableDevices.Add(devices[index]);
            }

            OnPropertyChanged(nameof(CanSelectDevice));
        }

        /// <summary>
        /// 現在の設定に合わせて選択デバイスを整えます。
        /// </summary>
        /// <param name="previousDeviceId">一覧更新前の選択デバイス識別子です。</param>
        private void ApplyDeviceSelection(string? previousDeviceId)
        {
            if (AvailableDevices.Count == 0)
            {
                SelectedDeviceId = null;
                if (!UseDefaultDevice)
                {
                    UseDefaultDevice = true;
                }

                return;
            }

            if (UseDefaultDevice)
            {
                SelectedDeviceId = GetDefaultOrFirstDeviceId();
                return;
            }

            var matchedDevice = AvailableDevices.FirstOrDefault(device => string.Equals(device.DeviceId, previousDeviceId, StringComparison.Ordinal));

            SelectedDeviceId = matchedDevice?.DeviceId ?? GetDefaultOrFirstDeviceId();
        }

        /// <summary>
        /// 明示デバイス利用時に選択デバイスが空にならないよう補完します。
        /// </summary>
        private void EnsureExplicitDeviceSelection()
        {
            if (AvailableDevices.Count == 0)
            {
                UseDefaultDevice = true;
                StatusMessage = "利用可能なデバイスがないため、既定デバイス利用に戻しました。";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedDeviceId) ||
                !AvailableDevices.Any(device => string.Equals(device.DeviceId, SelectedDeviceId, StringComparison.Ordinal)))
            {
                SelectedDeviceId = GetDefaultOrFirstDeviceId();
            }
        }

        /// <summary>
        /// 既定デバイスまたは先頭デバイスの識別子を返します。
        /// </summary>
        /// <returns>選択に使用するデバイス識別子です。</returns>
        private string? GetDefaultOrFirstDeviceId()
        {
            return AvailableDevices.FirstOrDefault(device => device.IsDefault)?.DeviceId
                ?? AvailableDevices.FirstOrDefault()?.DeviceId;
        }

        /// <summary>
        /// プロパティ変更通知を発生させます。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名です。</param>
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
