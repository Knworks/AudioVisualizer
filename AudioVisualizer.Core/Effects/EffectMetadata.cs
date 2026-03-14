using System;
using System.Collections.Generic;
using System.Linq;
using AudioVisualizer.Core.Models;

namespace AudioVisualizer.Core.Effects
{
    /// <summary>
    /// ビジュアライザーエフェクトの識別情報と対応入力元を表します。
    /// </summary>
    public sealed class EffectMetadata
    {
        #region プロパティ

        /// <summary>
        /// エフェクトの一意な識別子を取得します。
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// 利用者向けの表示名を取得します。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// エフェクトのバージョン文字列を取得します。
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// 作者情報を取得します。
        /// </summary>
        public string? Author { get; }

        /// <summary>
        /// エフェクトの説明を取得します。
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// エフェクトが対応する入力元を取得します。
        /// </summary>
        public IReadOnlyList<InputSource> SupportedInputSources { get; }

        #endregion

        #region 構築 / 消滅

        /// <summary>
        /// <see cref="EffectMetadata"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="id">エフェクトの一意な識別子です。</param>
        /// <param name="displayName">利用者へ表示するエフェクト名です。</param>
        /// <param name="version">エフェクトのバージョン文字列です。</param>
        /// <param name="author">作者情報です。</param>
        /// <param name="description">エフェクトの説明です。</param>
        /// <param name="supportedInputSources">エフェクトが対応する入力元です。</param>
        /// <exception cref="ArgumentException">
        /// 必須の文字列が空白、または対応入力元が空の場合にスローされます。
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="supportedInputSources"/> が <see langword="null"/> の場合にスローされます。</exception>
        public EffectMetadata(
            string id,
            string displayName,
            string version,
            string? author,
            string? description,
            IEnumerable<InputSource> supportedInputSources)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Effect ID must not be blank.", nameof(id))
                : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Display name must not be blank.", nameof(displayName))
                : displayName;
            Version = string.IsNullOrWhiteSpace(version)
                ? throw new ArgumentException("Version must not be blank.", nameof(version))
                : version;

            if (supportedInputSources is null)
            {
                throw new ArgumentNullException(nameof(supportedInputSources));
            }

            var inputSources = supportedInputSources.Distinct().ToArray();
            if (inputSources.Length == 0)
            {
                throw new ArgumentException("At least one supported input source is required.", nameof(supportedInputSources));
            }

            Author = string.IsNullOrWhiteSpace(author) ? null : author;
            Description = string.IsNullOrWhiteSpace(description) ? null : description;
            SupportedInputSources = inputSources;
        }

        #endregion
    }
}
