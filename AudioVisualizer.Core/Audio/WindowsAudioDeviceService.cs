using System;
using System.Collections.Generic;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// Windows の音声デバイス一覧と既定デバイス情報を取得する公開サービスです。
    /// </summary>
public sealed class WindowsAudioDeviceService : IAudioDeviceService, IDisposable
    {
        #region フィールド

        /// <summary>
        /// OS 依存の音声デバイス列挙を担当する内部列挙子です。
        /// </summary>
        private readonly IAudioPlatformDeviceEnumerator m_DeviceEnumerator;

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="WindowsAudioDeviceService"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        public WindowsAudioDeviceService()
            : this(new WasapiAudioDeviceEnumerator())
        {
        }

        /// <summary>
        /// <see cref="WindowsAudioDeviceService"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="deviceEnumerator">OS 依存の音声デバイス列挙を担当する内部列挙子です。</param>
        internal WindowsAudioDeviceService(IAudioPlatformDeviceEnumerator deviceEnumerator)
        {
            m_DeviceEnumerator = deviceEnumerator ?? throw new ArgumentNullException(nameof(deviceEnumerator));
            m_DeviceEnumerator.DefaultDeviceChanged += OnDefaultDeviceChanged;
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// OS の既定音声デバイスが切り替わったときに発生します。
        /// </summary>
        public event EventHandler<DefaultAudioDeviceChangedEventArgs>? DefaultDeviceChanged;

        #endregion

        #region 公開メソッド

        /// <summary>
        /// 通知購読と OS 依存リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            m_DeviceEnumerator.DefaultDeviceChanged -= OnDefaultDeviceChanged;
            if (m_DeviceEnumerator is IDisposable disposableDeviceEnumerator)
            {
                disposableDeviceEnumerator.Dispose();
            }
        }

        /// <summary>
        /// 指定した入力種別に対応する利用可能デバイス一覧を取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>指定入力種別に対応するデバイス一覧です。</returns>
        public IReadOnlyList<AudioDeviceInfo> GetDevices(InputSource inputSource)
        {
            var defaultDevice = m_DeviceEnumerator.GetDefaultDevice(inputSource);
            var deviceSnapshots = m_DeviceEnumerator.GetDevices(inputSource);
            var devices = new List<AudioDeviceInfo>(deviceSnapshots.Count);

            for (var index = 0; index < deviceSnapshots.Count; index++)
            {
                var deviceSnapshot = deviceSnapshots[index];
                devices.Add(CreateDeviceInfo(deviceSnapshot, inputSource, defaultDevice));
            }

            return devices;
        }

        /// <summary>
        /// 指定した入力種別に対応する既定デバイスを取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
        public AudioDeviceInfo? GetDefaultDevice(InputSource inputSource)
        {
            var defaultDevice = m_DeviceEnumerator.GetDefaultDevice(inputSource);
            if (defaultDevice is null)
            {
                return null;
            }

            return CreateDeviceInfo(defaultDevice, inputSource, defaultDevice);
        }

        #endregion

        #region イベントハンドラ

        /// <summary>
        /// 内部列挙子の既定デバイス変更通知を公開イベントへ転送します。
        /// </summary>
        /// <param name="sender">通知送信元です。</param>
        /// <param name="e">既定デバイス変更情報です。</param>
        private void OnDefaultDeviceChanged(object? sender, DefaultAudioDeviceChangedEventArgs e)
        {
            DefaultDeviceChanged?.Invoke(this, e);
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 内部スナップショットを公開向けのデバイス情報へ変換します。
        /// </summary>
        /// <param name="deviceSnapshot">変換対象のスナップショットです。</param>
        /// <param name="inputSource">対応する入力種別です。</param>
        /// <param name="defaultDevice">既定デバイスのスナップショットです。</param>
        /// <returns>公開向けのデバイス情報です。</returns>
        private static AudioDeviceInfo CreateDeviceInfo(AudioDeviceSnapshot deviceSnapshot, InputSource inputSource, AudioDeviceSnapshot? defaultDevice)
        {
            ArgumentNullException.ThrowIfNull(deviceSnapshot);

            return new AudioDeviceInfo(
                deviceSnapshot.DeviceId,
                deviceSnapshot.DisplayName,
                inputSource,
                string.Equals(deviceSnapshot.DeviceId, defaultDevice?.DeviceId, StringComparison.Ordinal));
        }

        #endregion
    }
}
