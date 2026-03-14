using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using AudioVisualizer.Core.Models;
using NAudio.CoreAudioApi;

namespace AudioVisualizer.Core.Audio
{
    /// <summary>
    /// NAudio を使用して Windows の音声デバイスを列挙する内部実装です。
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class WasapiAudioDeviceEnumerator : IAudioPlatformDeviceEnumerator
    {
        #region 公開メソッド

        /// <summary>
        /// 指定した入力種別に対応する利用可能デバイス一覧を取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>OS 依存情報を含むデバイス一覧です。</returns>
        public IReadOnlyList<AudioDeviceSnapshot> GetDevices(InputSource inputSource)
        {
            var dataFlow = ResolveDataFlow(inputSource);
            using var deviceEnumerator = new MMDeviceEnumerator();
            var devices = deviceEnumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);
            var snapshots = new List<AudioDeviceSnapshot>(devices.Count);

            for (var index = 0; index < devices.Count; index++)
            {
                var device = devices[index];
                snapshots.Add(new AudioDeviceSnapshot(device.ID, device.FriendlyName));
            }

            return snapshots;
        }

        /// <summary>
        /// 指定した入力種別に対応する既定デバイスを取得します。
        /// </summary>
        /// <param name="inputSource">取得対象の入力種別です。</param>
        /// <returns>既定デバイス。存在しない場合は <see langword="null"/>。</returns>
        public AudioDeviceSnapshot? GetDefaultDevice(InputSource inputSource)
        {
            try
            {
                var dataFlow = ResolveDataFlow(inputSource);
                using var deviceEnumerator = new MMDeviceEnumerator();
                var device = deviceEnumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia);
                return new AudioDeviceSnapshot(device.ID, device.FriendlyName);
            }
            catch (COMException)
            {
                return null;
            }
        }

        #endregion

        #region 内部処理

        /// <summary>
        /// 入力種別から NAudio の DataFlow を解決します。
        /// </summary>
        /// <param name="inputSource">解決対象の入力種別です。</param>
        /// <returns>対応する DataFlow です。</returns>
        /// <exception cref="ArgumentOutOfRangeException">未対応の入力種別が指定された場合にスローされます。</exception>
        private static DataFlow ResolveDataFlow(InputSource inputSource)
        {
            return inputSource switch
            {
                InputSource.SystemOutput => DataFlow.Render,
                InputSource.Microphone => DataFlow.Capture,
                _ => throw new ArgumentOutOfRangeException(nameof(inputSource), inputSource, "未対応の入力種別です。"),
            };
        }

        #endregion
    }
}
