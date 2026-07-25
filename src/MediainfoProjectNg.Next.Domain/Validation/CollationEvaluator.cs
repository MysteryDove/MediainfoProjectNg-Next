using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Projection;

namespace MediainfoProjectNg.Next.Domain.Validation;

/// <summary>
/// Profile-gated CollationV1 evaluator. Produces structured evaluations only;
/// merge with Legacy is handled by <see cref="MediaValidator"/>.
/// Media-backed rules consume <see cref="RawMediaSnapshot"/> exclusively when present;
/// missing/unavailable raw evidence yields Unverifiable rather than a false Pass/Violation.
/// </summary>
public static class CollationEvaluator
{
    public static IReadOnlyList<RuleEvaluation> Evaluate(MediaFileInfo info)
    {
        var results = new List<RuleEvaluation>();
        var grammarRecognized = CollationFilenameParser.TryParse(info.GeneralInfo.FullPath, out var claim)
                                && claim is not null;

        if (!grammarRecognized)
        {
            foreach (var ruleId in CollationRuleIds.EnabledOrder)
            {
                results.Add(Na(ruleId, "文件名未识别为 Collation VCB-S MKV 发行格式。"));
            }

            // Disabled Phase-0 rows are absent from the evaluation stream (not emitted).
            return results
                .OrderBy(r => OrderOf(r.RuleId))
                .ThenBy(r => r.RuleId, StringComparer.Ordinal)
                .ToList();
        }

        // FN.* — grammar only (matrix RecognizedVcbsMkvFilename).
        EvaluateFilename(info, claim!, results);

        // TRACK.*/VIDEO.* — grammar ∩ Matroska ∩ .mkv
        if (CollationApplicability.IsApplicable(
                CollationApplicability.RecognizedFilenameAndContainer, info, grammarRecognized: true))
        {
            EvaluateTracks(info, results);
            EvaluateVideoMetadata(info, results);
        }
        else
        {
            foreach (var ruleId in CollationRuleIds.EnabledOrder
                         .Where(id => id.StartsWith("TRACK.", StringComparison.Ordinal)
                                      || id.StartsWith("VIDEO.", StringComparison.Ordinal)))
            {
                results.Add(Na(ruleId, "容器或扩展名不满足 Matroska/.mkv 适用性（TRACK/VIDEO）。"));
            }
        }

        // CH.* — grammar ∩ Matroska ∩ .mkv ∩ chapters present
        if (CollationApplicability.IsApplicable(
                CollationApplicability.RecognizedFilenameAndContainerWithChapters, info, grammarRecognized: true))
        {
            EvaluateChapters(info, results);
        }
        else
        {
            results.Add(Na(CollationRuleIds.ChapterLanguageMissing, "章节规则不适用（无章节或容器不适用）。"));
            results.Add(Na(CollationRuleIds.ChapterLanguageMixed, "章节规则不适用（无章节或容器不适用）。"));
        }

        return results
            .OrderBy(r => OrderOf(r.RuleId))
            .ThenBy(r => r.RuleId, StringComparer.Ordinal)
            .ToList();
    }

    private static bool RawUnavailable(MediaFileInfo info) =>
        info.RawSnapshot is null || info.RawSnapshot.AdapterUnavailable;

    private static void EvaluateFilename(MediaFileInfo info, CollationFilenameClaim claim, List<RuleEvaluation> results)
    {
        if (RawUnavailable(info))
        {
            foreach (var ruleId in CollationRuleIds.FilenameRuleOrder)
            {
                results.Add(Unverifiable(
                    ruleId,
                    ErrorLevel.Info,
                    "原始 MediaInfo 证据不可用，无法验证文件名声明。",
                    expected: ruleId switch
                    {
                        CollationRuleIds.FnResolution => claim.Resolution,
                        CollationRuleIds.FnProfile => claim.Profile,
                        CollationRuleIds.FnVideoEncoder => claim.VideoEncoder,
                        _ => claim.AudioEncoders,
                    },
                    actual: "(source unavailable)",
                    evidence: "RawSnapshot missing or AdapterUnavailable"));
            }

            return;
        }

        var raw = info.RawSnapshot!;
        var rawVideo = raw.VideoTracks.Count > 0 ? raw.VideoTracks[0] : null;

        // Resolution — raw only
        if (rawVideo is null)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnResolution,
                ErrorLevel.Info,
                "无法验证分辨率声明：原始快照中无视频轨道。",
                expected: claim.Resolution,
                actual: "(no video)",
                evidence: "raw.VideoTracks empty"));
        }
        else if (rawVideo.Width.ParseFailed || rawVideo.Height.ParseFailed)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnResolution,
                ErrorLevel.Info,
                "宽高字段格式无效，无法验证分辨率声明。",
                expected: claim.Resolution,
                actual: $"{rawVideo.Width.TextOrEmpty}x{rawVideo.Height.TextOrEmpty}",
                evidence: "malformed numeric metadata"));
        }
        else if (rawVideo.ParsedWidth is null || rawVideo.ParsedHeight is null)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnResolution,
                ErrorLevel.Info,
                "无法验证分辨率声明：缺少可解析的宽高。",
                expected: claim.Resolution,
                actual: "(absent dimensions)",
                evidence: "ParsedWidth/Height null"));
        }
        else
        {
            var width = rawVideo.ParsedWidth.Value;
            var height = rawVideo.ParsedHeight.Value;
            var bucket = CollationPolicyMatrix.MapResolutionBucket(width, height);
            if (bucket is null)
            {
                results.Add(Unverifiable(
                    CollationRuleIds.FnResolution,
                    ErrorLevel.Info,
                    $"分辨率 {width}x{height} 不在已钉选桶中，无法验证声明 {claim.Resolution}。",
                    expected: claim.Resolution,
                    actual: $"{width}x{height}",
                    evidence: "undocumented crop/dimension"));
            }
            else if (!string.Equals(bucket, claim.Resolution, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(Violation(
                    CollationRuleIds.FnResolution,
                    ErrorLevel.Error,
                    $"分辨率声明与内容不符：声明 {claim.Resolution}，实际 {width}x{height}（{bucket}）。",
                    expected: claim.Resolution,
                    actual: $"{width}x{height} ({bucket})",
                    evidence: $"raw width={width} height={height}"));
            }
            else
            {
                results.Add(Pass(CollationRuleIds.FnResolution, $"分辨率匹配 {bucket}。"));
            }
        }

        // Profile — raw only
        if (rawVideo is null)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnProfile,
                ErrorLevel.Info,
                "无法验证 profile 声明：原始快照中无视频轨道。",
                expected: claim.Profile,
                actual: "(no video)",
                evidence: "raw.VideoTracks empty"));
        }
        else
        {
            var actualProfile = RawMediaSnapshotProjector.GenerateProfileStringFromRaw(rawVideo);
            if (claim.Profile.Length > 0 && actualProfile.Length == 0)
            {
                results.Add(Unverifiable(
                    CollationRuleIds.FnProfile,
                    ErrorLevel.Info,
                    "无法从原始媒体生成 profile 字符串以验证声明。",
                    expected: claim.Profile,
                    actual: "(unverifiable)",
                    evidence:
                    $"format={rawVideo.Format.TextOrEmpty}; profile={rawVideo.FormatProfile.TextOrEmpty}; bitDepth={rawVideo.ParsedBitDepth}"));
            }
            else if (!string.Equals(claim.Profile, actualProfile, StringComparison.Ordinal))
            {
                results.Add(Violation(
                    CollationRuleIds.FnProfile,
                    ErrorLevel.Error,
                    $"Profile 声明与内容不符：声明 {claim.Profile}，实际 {actualProfile}。",
                    expected: claim.Profile,
                    actual: actualProfile,
                    evidence: "raw Format/Format_Profile/BitDepth/ColorSpace"));
            }
            else
            {
                results.Add(Pass(CollationRuleIds.FnProfile, "Profile 匹配。"));
            }
        }

        // Video encoder — raw only
        if (rawVideo is null)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnVideoEncoder,
                ErrorLevel.Info,
                "无法验证视频编码声明：原始快照中无视频轨道。",
                expected: claim.VideoEncoder,
                actual: "(no video)",
                evidence: "raw.VideoTracks empty"));
        }
        else
        {
            var venc = RawMediaSnapshotProjector.GenerateVencoderStringFromRaw(rawVideo);
            if (venc.Length == 0)
            {
                results.Add(Unverifiable(
                    CollationRuleIds.FnVideoEncoder,
                    ErrorLevel.Info,
                    "未知视频格式，无法验证编码声明。",
                    expected: claim.VideoEncoder,
                    actual: rawVideo.Format.TextOrEmpty,
                    evidence: "raw Format unmapped"));
            }
            else if (!string.Equals(claim.VideoEncoder, venc, StringComparison.Ordinal))
            {
                results.Add(Violation(
                    CollationRuleIds.FnVideoEncoder,
                    ErrorLevel.Error,
                    $"视频编码声明与内容不符：声明 {claim.VideoEncoder}，实际 {venc}。",
                    expected: claim.VideoEncoder,
                    actual: venc,
                    evidence: $"raw Format={rawVideo.Format.TextOrEmpty}"));
            }
            else
            {
                results.Add(Pass(CollationRuleIds.FnVideoEncoder, "视频编码匹配。"));
            }
        }

        // Ordered audio groups — raw only
        var audioWithUnknownFormat = raw.AudioTracks
            .Count(track => track.Format.IsAbsent || track.Format.IsPresentEmpty);
        var actualAudio = RawMediaSnapshotProjector.GenerateAencodersStringFromRaw(raw.AudioTracks);
        if (audioWithUnknownFormat > 0)
        {
            results.Add(Unverifiable(
                CollationRuleIds.FnAudioEncoders,
                ErrorLevel.Info,
                "存在缺少格式证据的音频轨道，无法验证文件名音频编码组声明。",
                expected: claim.AudioEncoders,
                actual: $"{actualAudio} (unknown formats={audioWithUnknownFormat})",
                evidence: "raw AudioTracks Format absent/empty"));
        }
        else if (!string.Equals(claim.AudioEncoders, actualAudio, StringComparison.Ordinal))
        {
            results.Add(Violation(
                CollationRuleIds.FnAudioEncoders,
                ErrorLevel.Error,
                $"音频编码组声明与内容不符：声明 {claim.AudioEncoders}，实际 {actualAudio}。",
                expected: claim.AudioEncoders,
                actual: actualAudio,
                evidence: "raw AudioTracks Format order/groups"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.FnAudioEncoders, "音频编码组匹配。"));
        }
    }

    private static void EvaluateTracks(MediaFileInfo info, List<RuleEvaluation> results)
    {
        if (RawUnavailable(info))
        {
            foreach (var ruleId in new[]
                     {
                         CollationRuleIds.TrackVideoPresent,
                         CollationRuleIds.TrackAudioPresent,
                         CollationRuleIds.TrackVideoLanguage,
                         CollationRuleIds.TrackVideoDefault,
                         CollationRuleIds.TrackAudioLanguage,
                         CollationRuleIds.TrackAudioDefaultCardinality,
                         CollationRuleIds.TrackPgsLanguage,
                         CollationRuleIds.TrackPgsDefault,
                     })
            {
                results.Add(Unverifiable(
                    ruleId,
                    ErrorLevel.Info,
                    "原始 MediaInfo 证据不可用，无法验证轨道策略。",
                    expected: "raw track evidence",
                    actual: "(source unavailable)",
                    evidence: "RawSnapshot missing or AdapterUnavailable"));
            }

            return;
        }

        var raw = info.RawSnapshot!;

        if (raw.VideoTracks.Count == 0)
        {
            results.Add(Violation(
                CollationRuleIds.TrackVideoPresent,
                ErrorLevel.Error,
                "缺少视频轨道。",
                expected: ">=1 video",
                actual: "0",
                evidence: "raw.VideoTracks.Count"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.TrackVideoPresent, "存在视频轨道。"));
        }

        if (raw.AudioTracks.Count == 0)
        {
            results.Add(Violation(
                CollationRuleIds.TrackAudioPresent,
                ErrorLevel.Error,
                "缺少音频轨道。",
                expected: ">=1 audio",
                actual: "0",
                evidence: "raw.AudioTracks.Count"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.TrackAudioPresent, "存在音频轨道。"));
        }

        if (raw.VideoTracks.Count == 0)
        {
            results.Add(Na(CollationRuleIds.TrackVideoLanguage, "无视频轨道。"));
            results.Add(Na(CollationRuleIds.TrackVideoDefault, "无视频轨道。"));
        }
        else
        {
            var langRaw = raw.VideoTracks[0].Language;
            if (langRaw.IsAbsent || langRaw.IsPresentEmpty)
            {
                results.Add(Unverifiable(
                    CollationRuleIds.TrackVideoLanguage,
                    ErrorLevel.Warning,
                    "主视频轨道语言字段缺失。",
                    expected: "UND",
                    actual: "(absent)",
                    evidence: "raw Video Language"));
            }
            else
            {
                var lang = langRaw.TextOrEmpty.ToUpperInvariant();
                if (!string.Equals(lang, "UND", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(Violation(
                        CollationRuleIds.TrackVideoLanguage,
                        ErrorLevel.Error,
                        $"主视频轨道语言应为 UND，实际 {lang}。",
                        expected: "UND",
                        actual: lang,
                        evidence: "raw Video Language"));
                }
                else
                {
                    results.Add(Pass(CollationRuleIds.TrackVideoLanguage, "主视频语言 UND。"));
                }
            }

            var defRaw = raw.VideoTracks[0].Default;
            if (IsMissingFlag(defRaw))
            {
                results.Add(Unverifiable(
                    CollationRuleIds.TrackVideoDefault,
                    ErrorLevel.Warning,
                    "主视频轨道 Default 字段缺失。",
                    expected: "Yes",
                    actual: "(absent)",
                    evidence: "raw Video Default"));
            }
            else
            {
                var state = DefaultStateOf(defRaw);
                if (state == DefaultState.Unknown)
                {
                    results.Add(Unverifiable(
                        CollationRuleIds.TrackVideoDefault,
                        ErrorLevel.Warning,
                        "主视频轨道 Default 值无法解释。",
                        expected: "Yes",
                        actual: defRaw.TextOrEmpty,
                        evidence: "raw Video Default"));
                }
                else if (state != DefaultState.Yes)
                {
                    results.Add(Violation(
                        CollationRuleIds.TrackVideoDefault,
                        ErrorLevel.Error,
                        "主视频轨道 Default 应为 Yes。",
                        expected: "Yes",
                        actual: defRaw.TextOrEmpty,
                        evidence: "raw Video Default"));
                }
                else
                {
                    results.Add(Pass(CollationRuleIds.TrackVideoDefault, "主视频 Default=Yes。"));
                }
            }
        }

        if (raw.AudioTracks.Count == 0)
        {
            results.Add(Na(CollationRuleIds.TrackAudioLanguage, "无音频轨道。"));
            results.Add(Na(CollationRuleIds.TrackAudioDefaultCardinality, "无音频轨道。"));
        }
        else
        {
            var missingLang = raw.AudioTracks.Any(a => a.Language.IsAbsent || a.Language.IsPresentEmpty);
            results.Add(missingLang
                ? Violation(
                    CollationRuleIds.TrackAudioLanguage,
                    ErrorLevel.Error,
                    "存在缺少语言标记的音频轨道。",
                    expected: "language present",
                    actual: "missing",
                    evidence: "raw Audio Language")
                : Pass(CollationRuleIds.TrackAudioLanguage, "音频语言齐全。"));

            var defaultYesCount = 0;
            var unknownDefaultCount = 0;
            foreach (var track in raw.AudioTracks)
            {
                switch (DefaultStateOf(track.Default))
                {
                    case DefaultState.Yes:
                        defaultYesCount++;
                        break;
                    case DefaultState.Unknown:
                        unknownDefaultCount++;
                        break;
                }
            }

            if (unknownDefaultCount > 0)
            {
                results.Add(Unverifiable(
                    CollationRuleIds.TrackAudioDefaultCardinality,
                    ErrorLevel.Warning,
                    "存在缺失的音频 Default 字段，无法确认恰好一条 Default=Yes。",
                    expected: "1",
                    actual: $"yes={defaultYesCount}, unknown={unknownDefaultCount}",
                    evidence: "raw Audio Default"));
            }
            else if (defaultYesCount != 1)
            {
                results.Add(Violation(
                    CollationRuleIds.TrackAudioDefaultCardinality,
                    ErrorLevel.Error,
                    $"音频 Default=Yes 数量应为 1，实际 {defaultYesCount}。",
                    expected: "1",
                    actual: defaultYesCount.ToString(),
                    evidence: "raw Audio Default"));
            }
            else
            {
                results.Add(Pass(CollationRuleIds.TrackAudioDefaultCardinality, "恰好一条音频 Default=Yes。"));
            }
        }

        // PGS — raw TextTracks only
        var pgsTracks = raw.TextTracks
            .Where(t => RawMediaSnapshotProjector.IsPgsFormat(t.Format))
            .ToList();
        var unknownFormatCount = raw.TextTracks
            .Count(t => t.Format.IsAbsent || t.Format.IsPresentEmpty);

        if (pgsTracks.Count == 0 && unknownFormatCount == 0)
        {
            results.Add(Pass(CollationRuleIds.TrackPgsLanguage, "无 PGS 字幕轨道。"));
            results.Add(Pass(CollationRuleIds.TrackPgsDefault, "无 PGS 字幕轨道。"));
        }
        else
        {
            var langOk = pgsTracks.All(t => t.Language.IsPresent && !t.Language.IsPresentEmpty);
            results.Add(!langOk
                ? Violation(
                    CollationRuleIds.TrackPgsLanguage,
                    ErrorLevel.Error,
                    "存在缺少语言的 PGS 字幕轨道。",
                    expected: "language present",
                    actual: "missing",
                    evidence: "raw Text Format=PGS Language")
                : unknownFormatCount > 0
                    ? Unverifiable(
                        CollationRuleIds.TrackPgsLanguage,
                        ErrorLevel.Info,
                        "存在格式字段缺失的字幕轨道，无法确认其是否受 PGS 语言规则约束。",
                        expected: "all PGS tracks have language",
                        actual: $"unknown formats={unknownFormatCount}",
                        evidence: "raw Text Format absent/empty")
                    : Pass(CollationRuleIds.TrackPgsLanguage, "PGS 语言齐全。"));

            var defYes = false;
            var defUnknown = false;
            foreach (var t in pgsTracks)
            {
                switch (DefaultStateOf(t.Default))
                {
                    case DefaultState.Yes:
                        defYes = true;
                        break;
                    case DefaultState.Unknown:
                        defUnknown = true;
                        break;
                }
            }

            results.Add(defYes
                ? Violation(
                    CollationRuleIds.TrackPgsDefault,
                    ErrorLevel.Error,
                    "PGS 字幕轨道 Default 应为 No。",
                    expected: "No",
                    actual: "Yes",
                    evidence: "raw Text Format=PGS Default")
                : defUnknown
                    ? Unverifiable(
                        CollationRuleIds.TrackPgsDefault,
                        ErrorLevel.Warning,
                        "存在缺失的 PGS Default 字段，无法确认 Default=No。",
                        expected: "No",
                        actual: "(absent)",
                        evidence: "raw Text Format=PGS Default")
                    : unknownFormatCount > 0
                        ? Unverifiable(
                            CollationRuleIds.TrackPgsDefault,
                            ErrorLevel.Info,
                            "存在格式字段缺失的字幕轨道，无法确认其是否受 PGS Default 规则约束。",
                            expected: "all PGS tracks Default=No",
                            actual: $"unknown formats={unknownFormatCount}",
                            evidence: "raw Text Format absent/empty")
                        : Pass(CollationRuleIds.TrackPgsDefault, "PGS Default=No。"));
        }
    }

    private static void EvaluateVideoMetadata(MediaFileInfo info, List<RuleEvaluation> results)
    {
        if (RawUnavailable(info))
        {
            foreach (var ruleId in new[]
                     {
                         CollationRuleIds.VideoScanType,
                         CollationRuleIds.VideoColorRange,
                         CollationRuleIds.VideoColorMatrix,
                         CollationRuleIds.VideoColorReview,
                     })
            {
                results.Add(Unverifiable(
                    ruleId,
                    ErrorLevel.Info,
                    "原始视频元数据证据不可用。",
                    expected: "projected raw metadata",
                    actual: "(source unavailable)",
                    evidence: "RawSnapshot missing or AdapterUnavailable"));
            }

            return;
        }

        var raw = info.RawSnapshot!;
        if (raw.VideoTracks.Count == 0)
        {
            results.Add(Na(CollationRuleIds.VideoScanType, "无视频轨道。"));
            results.Add(Na(CollationRuleIds.VideoColorRange, "无视频轨道。"));
            results.Add(Na(CollationRuleIds.VideoColorMatrix, "无视频轨道。"));
            results.Add(Na(CollationRuleIds.VideoColorReview, "无视频轨道。"));
            return;
        }

        var video = raw.VideoTracks[0];

        // Scan type from raw only (FPS denominator-1000 remains presentation signal, not this rule)
        if (video.ScanType.IsAbsent || video.ScanType.IsPresentEmpty)
        {
            results.Add(Unverifiable(
                CollationRuleIds.VideoScanType,
                ErrorLevel.Info,
                "ScanType 字段缺失，无法验证是否为 progressive。",
                expected: "Progressive",
                actual: "(absent)",
                evidence: "raw ScanType"));
        }
        else
        {
            var scan = video.ScanType.TextOrEmpty;
            if (scan.Contains("Interlaced", StringComparison.OrdinalIgnoreCase)
                || scan.Contains("MBAFF", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(Violation(
                    CollationRuleIds.VideoScanType,
                    ErrorLevel.Warning,
                    $"扫描类型非 progressive：{scan}。",
                    expected: "Progressive",
                    actual: scan,
                    evidence: "raw ScanType"));
            }
            else if (scan.Contains("Progressive", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(Pass(CollationRuleIds.VideoScanType, "Progressive 扫描。"));
            }
            else
            {
                results.Add(Unverifiable(
                    CollationRuleIds.VideoScanType,
                    ErrorLevel.Info,
                    $"无法解释 ScanType 值：{scan}。",
                    expected: "Progressive",
                    actual: scan,
                    evidence: "raw ScanType"));
            }
        }

        var range = video.ColourRange;
        if (range.IsAbsent || range.IsPresentEmpty)
        {
            results.Add(Violation(
                CollationRuleIds.VideoColorRange,
                ErrorLevel.Warning,
                "缺少 colour range 元数据。",
                expected: "present",
                actual: range.IsAbsent ? "(absent)" : "(empty)",
                evidence: "raw colour_range/ColorRange"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.VideoColorRange, "colour range 存在。"));
        }

        var matrix = video.MatrixCoefficients;
        if (matrix.IsAbsent || matrix.IsPresentEmpty)
        {
            results.Add(Violation(
                CollationRuleIds.VideoColorMatrix,
                ErrorLevel.Warning,
                "缺少 matrix coefficients 元数据。",
                expected: "present",
                actual: matrix.IsAbsent ? "(absent)" : "(empty)",
                evidence: "raw matrix_coefficients"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.VideoColorMatrix, "matrix coefficients 存在。"));
        }

        // Non-SDR review — only approved HDR/DVD profiles suppress
        if (CollationPolicyMatrix.IsApprovedColorReviewProfile(info.DeclaredColorReviewProfile))
        {
            results.Add(Pass(
                CollationRuleIds.VideoColorReview,
                $"已声明核准色彩审查配置 {info.DeclaredColorReviewProfile!.Trim()}，跳过通用 SDR 偏离硬失败。"));
        }
        else
        {
            // Exact matrix membership only — no Pass-path substring Contains for 709/601/Limited/Full.
            var nonSdr = false;
            var details = new List<string>();
            if (range is { IsPresent: true } && !range.IsPresentEmpty
                && !CollationPolicyMatrix.SdrColourRanges.Contains(range.TextOrEmpty))
            {
                nonSdr = true;
                details.Add($"range={range.TextOrEmpty}");
            }

            if (matrix is { IsPresent: true } && !matrix.IsPresentEmpty
                && !CollationPolicyMatrix.SdrMatrixCoefficients.Contains(matrix.TextOrEmpty))
            {
                nonSdr = true;
                details.Add($"matrix={matrix.TextOrEmpty}");
            }

            var transfer = video.TransferCharacteristics;
            if (transfer is { IsPresent: true } && !transfer.IsPresentEmpty
                && !CollationPolicyMatrix.SdrTransferCharacteristics.Contains(transfer.TextOrEmpty))
            {
                nonSdr = true;
                details.Add($"transfer={transfer.TextOrEmpty}");
            }

            var primaries = video.ColourPrimaries;
            if (primaries.IsPresent && !primaries.IsPresentEmpty
                && !CollationPolicyMatrix.SdrColourPrimaries.Contains(primaries.TextOrEmpty))
            {
                nonSdr = true;
                details.Add($"primaries={primaries.TextOrEmpty}");
            }

            if (!string.IsNullOrWhiteSpace(info.DeclaredColorReviewProfile)
                && !CollationPolicyMatrix.IsApprovedColorReviewProfile(info.DeclaredColorReviewProfile))
            {
                // Unknown declared profile is itself advisory evidence, does not suppress.
                details.Add($"declaredProfile={info.DeclaredColorReviewProfile}");
            }

            if (nonSdr)
            {
                results.Add(Violation(
                    CollationRuleIds.VideoColorReview,
                    ErrorLevel.Warning,
                    "色彩元数据偏离常见 SDR 默认值，需人工审查：" + string.Join(", ", details),
                    expected: "SDR defaults or approved HDR/DVD profile",
                    actual: string.Join(", ", details),
                    evidence: "raw colour metadata"));
            }
            else
            {
                results.Add(Pass(CollationRuleIds.VideoColorReview, "未见非 SDR 色彩偏离。"));
            }
        }
    }

    private static void EvaluateChapters(MediaFileInfo info, List<RuleEvaluation> results)
    {
        if (RawUnavailable(info))
        {
            results.Add(Unverifiable(
                CollationRuleIds.ChapterLanguageMissing,
                ErrorLevel.Info,
                "原始章节证据不可用。",
                expected: "chapter languages",
                actual: "(source unavailable)",
                evidence: "RawSnapshot missing or AdapterUnavailable"));
            results.Add(Unverifiable(
                CollationRuleIds.ChapterLanguageMixed,
                ErrorLevel.Info,
                "原始章节证据不可用。",
                expected: "uniform language",
                actual: "(source unavailable)",
                evidence: "RawSnapshot missing or AdapterUnavailable"));
            return;
        }

        var raw = info.RawSnapshot!;
        if (raw.ChapterCount == 0 && raw.Chapters.Count == 0)
        {
            results.Add(Na(CollationRuleIds.ChapterLanguageMissing, "无章节。"));
            results.Add(Na(CollationRuleIds.ChapterLanguageMixed, "无章节。"));
            return;
        }

        if (raw.Chapters.Count == 0)
        {
            results.Add(Unverifiable(
                CollationRuleIds.ChapterLanguageMissing,
                ErrorLevel.Info,
                "章节计数非零但缺少原始章节投影数据。",
                expected: "chapter languages",
                actual: "(unavailable)",
                evidence: $"raw ChapterCount={raw.ChapterCount}, Chapters empty"));
            results.Add(Unverifiable(
                CollationRuleIds.ChapterLanguageMixed,
                ErrorLevel.Info,
                "章节计数非零但缺少原始章节投影数据。",
                expected: "uniform language",
                actual: "(unavailable)",
                evidence: $"raw ChapterCount={raw.ChapterCount}, Chapters empty"));
            return;
        }

        var langs = new List<string>();
        var missing = false;
        foreach (var c in raw.Chapters)
        {
            if (c.Language.IsAbsent || c.Language.IsPresentEmpty)
            {
                missing = true;
                langs.Add(string.Empty);
            }
            else
            {
                langs.Add(c.Language.TextOrEmpty);
            }
        }

        results.Add(missing
            ? Violation(
                CollationRuleIds.ChapterLanguageMissing,
                ErrorLevel.Warning,
                "存在缺少语言标记的章节。",
                expected: "language present",
                actual: "missing",
                evidence: "raw Chapter Language")
            : Pass(CollationRuleIds.ChapterLanguageMissing, "章节语言齐全。"));

        var distinct = langs
            .Where(l => !string.IsNullOrEmpty(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (distinct.Count > 1)
        {
            results.Add(Violation(
                CollationRuleIds.ChapterLanguageMixed,
                ErrorLevel.Warning,
                "章节语言不一致。",
                expected: "single language",
                actual: string.Join(",", distinct),
                evidence: "raw Chapter Language"));
        }
        else
        {
            results.Add(Pass(CollationRuleIds.ChapterLanguageMixed, "章节语言一致或无可比语言。"));
        }
    }

    private static bool IsMissingFlag(RawField raw) =>
        raw.IsAbsent || raw.IsPresentEmpty || string.IsNullOrWhiteSpace(raw.TextOrEmpty);

    private static DefaultState DefaultStateOf(RawField raw)
    {
        if (raw.IsAbsent || raw.IsPresentEmpty || string.IsNullOrWhiteSpace(raw.TextOrEmpty))
        {
            return DefaultState.Unknown;
        }

        var value = raw.TextOrEmpty.Trim();
        if (value.Equals("Yes", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultState.Yes;
        }

        if (value.Equals("No", StringComparison.OrdinalIgnoreCase)
            || value.Equals("0", StringComparison.OrdinalIgnoreCase)
            || value.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultState.No;
        }

        return DefaultState.Unknown;
    }

    private enum DefaultState
    {
        Unknown,
        No,
        Yes,
    }

    private static int OrderOf(string ruleId)
    {
        for (var i = 0; i < CollationRuleIds.EnabledOrder.Count; i++)
        {
            if (CollationRuleIds.EnabledOrder[i] == ruleId)
            {
                return i;
            }
        }

        return 2000;
    }

    private static RuleEvaluation Pass(string ruleId, string description) =>
        new(ruleId, RuleOutcome.Pass, CollationPolicyMatrix.PolicyRevision, description);

    private static RuleEvaluation Na(string ruleId, string description) =>
        new(ruleId, RuleOutcome.NotApplicable, CollationPolicyMatrix.PolicyRevision, description);

    private static RuleEvaluation Violation(
        string ruleId,
        ErrorLevel severity,
        string description,
        string? expected = null,
        string? actual = null,
        string? evidence = null) =>
        new(ruleId, RuleOutcome.Violation, CollationPolicyMatrix.PolicyRevision, description,
            severity, expected, actual, evidence);

    private static RuleEvaluation Unverifiable(
        string ruleId,
        ErrorLevel severity,
        string description,
        string? expected = null,
        string? actual = null,
        string? evidence = null) =>
        new(ruleId, RuleOutcome.Unverifiable, CollationPolicyMatrix.PolicyRevision, description,
            severity, expected, actual, evidence);
}
