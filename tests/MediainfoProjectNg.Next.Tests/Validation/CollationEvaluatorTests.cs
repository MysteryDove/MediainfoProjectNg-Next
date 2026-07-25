using MediainfoProjectNg.Next.Domain.Models;
using MediainfoProjectNg.Next.Domain.Validation;

namespace MediainfoProjectNg.Next.Tests.Validation;

public class CollationEvaluatorTests
{
    private const string GoodName =
        "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv";

    private static MediaFileInfo CreateRecognized(
        string path = GoodName,
        long width = 1920,
        long height = 1080,
        string videoLang = "UND",
        string videoDefault = "Yes",
        string audioLang = "JPN",
        string audioDefault = "Yes")
    {
        var info = new MediaFileInfo(new GeneralInfo(
            Path.GetFileNameWithoutExtension(path),
            path,
            "Matroska",
            1000, 1, 1, 0, 0));
        info.VideoInfos.Add(new VideoInfo(
            "HEVC", "Main 10@L4", "CFR", "23.976", 1000, 10, 10000, height, width, videoLang, 0,
            new ProfileInfo("Main 10@L4"), "YUV420", videoDefault));
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, audioLang, 0, audioDefault));
        info.RawSnapshot = BuildRaw(info, width, height, videoLang, videoDefault, audioLang, audioDefault,
            scanType: "Progressive", colourRange: "Limited", matrix: "BT.709");
        return info;
    }

    private static RawMediaSnapshot BuildRaw(
        MediaFileInfo info,
        long width,
        long height,
        string videoLang,
        string videoDefault,
        string audioLang,
        string audioDefault,
        string? scanType,
        string? colourRange,
        string? matrix)
    {
        return new RawMediaSnapshot
        {
            FullPath = info.GeneralInfo.FullPath,
            Extension = Path.GetExtension(info.GeneralInfo.FullPath),
            ContainerFormat = RawField.Of("Matroska"),
            VideoTracks =
            [
                new RawVideoTrack
                {
                    Format = RawField.Of("HEVC"),
                    FormatProfile = RawField.Of("Main 10@L4"),
                    Width = RawField.Of(width.ToString()),
                    Height = RawField.Of(height.ToString()),
                    BitDepth = RawField.Of("10"),
                    Language = videoLang is null ? RawField.Absent : RawField.Of(videoLang),
                    Default = videoDefault is null ? RawField.Absent : RawField.Of(videoDefault),
                    ScanType = scanType is null ? RawField.Absent : RawField.Of(scanType),
                    ColourRange = colourRange is null ? RawField.Absent : RawField.Of(colourRange),
                    MatrixCoefficients = matrix is null ? RawField.Absent : RawField.Of(matrix),
                    ParsedWidth = width,
                    ParsedHeight = height,
                    ParsedBitDepth = 10,
                },
            ],
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = string.IsNullOrEmpty(audioLang) ? RawField.Absent : RawField.Of(audioLang),
                    Default = RawField.Of(audioDefault),
                },
            ],
            TextTracks = [],
            Chapters = [],
            ChapterCount = 0,
        };
    }

    [Fact]
    public void FilenameParser_AcceptsSupportedVcbsMkv()
    {
        Assert.True(CollationFilenameParser.TryParse(GoodName, out var claim));
        Assert.NotNull(claim);
        Assert.Equal("Ma10p", claim!.Profile);
        Assert.Equal("1080p", claim.Resolution);
        Assert.Equal("x265", claim.VideoEncoder);
        Assert.Equal("_flac", claim.AudioEncoders);
    }

    [Fact]
    public void FilenameParser_RejectsUnrecognized_WithoutLegacyChange()
    {
        Assert.False(CollationFilenameParser.TryParse("/media/ordinary.mkv", out _));
        var info = CreateRecognized("/media/ordinary.mkv");
        Assert.True(MediaValidator.FileNameContentMatched(info));
        var legacy = MediaValidator.CheckFile(info);
        Assert.DoesNotContain(legacy, f => f.Description.Contains("内容物"));
    }

    [Theory]
    [InlineData("/media/[VCB-S] Show [Ma10p_1080p][svtav1_flac].mkv")]
    [InlineData("/media/[VCB-S] Show [HDR_1080p][x265_flac].mkv")]
    [InlineData("/media/[VCB-S] Show [Ma10p_2160p][x265_flac].mkv")]
    public void FilenameParser_RejectsUnsupportedPhase0Claims(string path)
    {
        Assert.False(CollationFilenameParser.TryParse(path, out _));
    }

    [Fact]
    public void Resolution_720pClaim_Against1080p_IsError()
    {
        var path = "/media/[VCB-S] Show [Ma10p_720p][x265_flac].mkv";
        var info = CreateRecognized(path, width: 1920, height: 1080);
        var evals = CollationEvaluator.Evaluate(info);
        var res = evals.Single(e => e.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal(RuleOutcome.Violation, res.Outcome);
        Assert.Equal(ErrorLevel.Error, res.Severity);
        Assert.Contains("720p", res.Description);
        Assert.Contains("1920x1080", res.Description);
    }

    [Fact]
    public void Resolution_1920x1072_Against1080p_Passes()
    {
        var info = CreateRecognized(width: 1920, height: 1072);
        var evals = CollationEvaluator.Evaluate(info);
        var res = evals.Single(e => e.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal(RuleOutcome.Pass, res.Outcome);
    }

    [Fact]
    public void Resolution_UndocumentedCrop_IsUnverifiable_NotAccepted()
    {
        var info = CreateRecognized(width: 1918, height: 1078);
        var evals = CollationEvaluator.Evaluate(info);
        var res = evals.Single(e => e.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal(RuleOutcome.Unverifiable, res.Outcome);
    }

    [Fact]
    public void Resolution_MalformedRawDimensions_PreserveEvidence()
    {
        var info = CreateRecognized(width: 0, height: 0);
        info.RawSnapshot = info.RawSnapshot! with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with
                {
                    Width = RawField.Malformed("wide"),
                    Height = RawField.Malformed("high"),
                    ParsedWidth = null,
                    ParsedHeight = null,
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal("widexhigh", eval.Actual);
        Assert.Equal("malformed numeric metadata", eval.Evidence);
    }

    [Fact]
    public void UnrelatedMkv_IsNotApplicable()
    {
        var info = CreateRecognized("/media/random-show.mkv");
        var evals = CollationEvaluator.Evaluate(info);
        Assert.All(
            evals.Where(e => CollationPolicyMatrix.IsRuleEnabled(e.RuleId)),
            e => Assert.Equal(RuleOutcome.NotApplicable, e.Outcome));
    }

    [Fact]
    public void TrackRules_VideoLanguageAndAudioDefault()
    {
        var bad = CreateRecognized(videoLang: "JPN", audioDefault: "No");
        // Add second audio default yes to get cardinality 0 (none yes)
        var evals = CollationEvaluator.Evaluate(bad);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.TrackVideoLanguage).Outcome);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.TrackAudioDefaultCardinality).Outcome);
    }

    [Fact]
    public void Pgs_DefaultYes_IsError()
    {
        var info = CreateRecognized();
        // Collation consumes raw TextTracks only (display SubInfos are not authoritative).
        info.RawSnapshot = info.RawSnapshot! with
        {
            TextTracks =
            [
                new RawTextTrack
                {
                    Format = RawField.Of("PGS"),
                    Language = RawField.Of("jpn"),
                    Default = RawField.Of("Yes"),
                },
            ],
        };
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.TrackPgsDefault).Outcome);
    }

    [Fact]
    public void ScanType_Interlaced_IsWarning()
    {
        var info = CreateRecognized();
        info.RawSnapshot = BuildRaw(info, 1920, 1080, "UND", "Yes", "JPN", "Yes",
            scanType: "Interlaced", colourRange: "Limited", matrix: "BT.709");
        var evals = CollationEvaluator.Evaluate(info);
        var scan = evals.Single(e => e.RuleId == CollationRuleIds.VideoScanType);
        Assert.Equal(RuleOutcome.Violation, scan.Outcome);
        Assert.Equal(ErrorLevel.Warning, scan.Severity);
    }

    [Fact]
    public void MissingColor_IsWarning()
    {
        var info = CreateRecognized();
        info.RawSnapshot = BuildRaw(info, 1920, 1080, "UND", "Yes", "JPN", "Yes",
            scanType: "Progressive", colourRange: null, matrix: null);
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.VideoColorRange).Outcome);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.VideoColorMatrix).Outcome);
    }

    [Fact]
    public void ChapterLanguage_MissingAndMixed()
    {
        var info = CreateRecognized();
        info.GeneralInfo.ChapterCount = 2;
        info.ChapterInfos.Add(new ChapterInfo(0, "a", ""));
        info.ChapterInfos.Add(new ChapterInfo(1000, "b", "ENG"));
        info.RawSnapshot = info.RawSnapshot! with
        {
            ChapterCount = 2,
            Chapters =
            [
                new RawChapter { Language = RawField.Of(""), Name = RawField.Of("a"), TimespanMs = 0 },
                new RawChapter { Language = RawField.Of("ENG"), Name = RawField.Of("b"), TimespanMs = 1000 },
            ],
        };
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.ChapterLanguageMissing).Outcome);
        Assert.Equal(RuleOutcome.Pass, evals.Single(e => e.RuleId == CollationRuleIds.ChapterLanguageMixed).Outcome);
        // only one non-empty language → mixed pass; missing still fires
    }

    [Fact]
    public void CollationV1_SupersedesGenericFilenameMismatch_Once()
    {
        // Profile claim Ma10p vs actual empty profile capability is not enough for legacy mismatch;
        // use wrong video encoder claim so FileNameContentMatched is false (generic slot exists).
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x264_flac].mkv";
        var info = CreateRecognized(path, width: 1920, height: 1080);
        Assert.False(MediaValidator.FileNameContentMatched(info));
        var legacy = MediaValidator.CheckFile(info);
        var genericIndex = legacy.ToList().FindIndex(f =>
            f.Description == CollationPolicyMatrix.LegacyFilenameMismatchDescription);
        Assert.True(genericIndex >= 0);

        var findings = MediaValidator.CheckFile(info, ValidationProfile.CollationV1);
        Assert.DoesNotContain(findings, f =>
            f.Description == CollationPolicyMatrix.LegacyFilenameMismatchDescription && f.RuleId is null);
        Assert.Contains(findings, f => f.RuleId == CollationRuleIds.FnVideoEncoder);
        // Field findings occupy the superseded generic slot (same index as generic had).
        Assert.Equal(CollationRuleIds.FnVideoEncoder, findings[genericIndex].RuleId);
    }

    [Fact]
    public void Filename_MixedUnverifiableAndViolation_OrdersErrorFirst_ForViolet()
    {
        // Undocumented crop → Resolution Unverifiable; wrong encoder claim → VideoEncoder Violation.
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x264_flac].mkv";
        var info = CreateRecognized(path, width: 1918, height: 1078);
        var findings = MediaValidator.CheckFile(info, ValidationProfile.CollationV1);
        var firstFn = findings.First(f => f.RuleId is not null
                                          && CollationRuleIds.FilenameRuleOrder.Contains(f.RuleId));
        Assert.Equal(ErrorLevel.Error, firstFn.Level);
        Assert.Equal(
            MediainfoProjectNg.Next.Core.Presentation.ColorToken.ErrorViolet,
            MediainfoProjectNg.Next.Core.Presentation.LegacyColorRules.TokenForFinding(firstFn));
    }

    [Fact]
    public void MissingVideoDefault_IsUnverifiableWarning_NotHardError()
    {
        var info = CreateRecognized(videoDefault: "Yes");
        info.VideoInfos[0].Default = "";
        info.RawSnapshot = BuildRaw(info, 1920, 1080, "UND", "", "JPN", "Yes",
            scanType: "Progressive", colourRange: "Limited", matrix: "BT.709");
        // Force empty default field as present-empty then absent semantics
        info.RawSnapshot = info.RawSnapshot with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with { Default = RawField.Of("") },
            ],
        };
        var eval = CollationEvaluator.Evaluate(info).Single(e => e.RuleId == CollationRuleIds.TrackVideoDefault);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal(ErrorLevel.Warning, eval.Severity);
    }

    [Fact]
    public void MissingAudioDefault_IsUnverifiableWarning_NotHardError()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Absent,
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.TrackAudioDefaultCardinality);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal(ErrorLevel.Warning, eval.Severity);
    }

    [Fact]
    public void MissingPgsDefault_IsUnverifiableWarning_NotSilentPass()
    {
        var info = CreateRecognized();
        info.SubInfos.Add(new SubInfo("PGS", "No", "JPN"));
        info.RawSnapshot = info.RawSnapshot! with
        {
            TextTracks =
            [
                new RawTextTrack
                {
                    Format = RawField.Of("PGS"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Absent,
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.TrackPgsDefault);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal(ErrorLevel.Warning, eval.Severity);
    }

    [Fact]
    public void MissingRawVideoEvidence_IsUnverifiableInfo_NotMissingMetadata()
    {
        var info = CreateRecognized();
        info.RawSnapshot = null;

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.VideoColorRange);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal(ErrorLevel.Info, eval.Severity);
    }

    [Fact]
    public void NonSdrColourPrimaries_IsReviewWarning()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with { ColourPrimaries = RawField.Of("BT.2020") },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.VideoColorReview);
        Assert.Equal(RuleOutcome.Violation, eval.Outcome);
        Assert.Equal(ErrorLevel.Warning, eval.Severity);
        Assert.Contains("primaries=BT.2020", eval.Actual ?? string.Empty);
    }

    [Fact]
    public void UnlimitedRange_IsNotSdrPass_SubstringBypassRemoved()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with { ColourRange = RawField.Of("Unlimited") },
            ],
        };
        var eval = CollationEvaluator.Evaluate(info).Single(e => e.RuleId == CollationRuleIds.VideoColorReview);
        Assert.Equal(RuleOutcome.Violation, eval.Outcome);
        Assert.Contains("Unlimited", eval.Actual ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void Foo709Bar_IsNotSdrPass()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with { MatrixCoefficients = RawField.Of("foo709bar") },
            ],
        };
        var eval = CollationEvaluator.Evaluate(info).Single(e => e.RuleId == CollationRuleIds.VideoColorReview);
        Assert.Equal(RuleOutcome.Violation, eval.Outcome);
    }

    [Fact]
    public void Mpeg4Payload_WithMkvName_TrackRulesNotApplicable()
    {
        var info = CreateRecognized();
        info.GeneralInfo.Format = "MPEG-4";
        info.RawSnapshot = info.RawSnapshot! with { ContainerFormat = RawField.Of("MPEG-4") };
        var evals = CollationEvaluator.Evaluate(info);
        Assert.All(
            evals.Where(e => e.RuleId.StartsWith("TRACK.", StringComparison.Ordinal)
                             || e.RuleId.StartsWith("VIDEO.", StringComparison.Ordinal)),
            e => Assert.Equal(RuleOutcome.NotApplicable, e.Outcome));
        // Filename rules still apply when grammar recognized.
        Assert.Contains(evals, e => e.RuleId == CollationRuleIds.FnResolution && e.Outcome == RuleOutcome.Pass);
    }

    [Fact]
    public void RuntimeSdrDefaults_MatchApprovedMatrixExactly()
    {
        Assert.Equal(new[] { "Full", "Limited" }, CollationPolicyMatrix.SdrColourRanges.Order());
        Assert.Equal(new[] { "BT.601", "BT.709" }, CollationPolicyMatrix.SdrMatrixCoefficients.Order());
        Assert.Equal(new[] { "BT.601", "BT.709" }, CollationPolicyMatrix.SdrTransferCharacteristics.Order());
        Assert.Equal(new[] { "BT.601", "BT.709" }, CollationPolicyMatrix.SdrColourPrimaries.Order());
    }

    [Fact]
    public void ParameterlessCheckFile_Unchanged_OnDurationDelta()
    {
        var info = CreateRecognized();
        info.VideoInfos[0].Duration = 10000;
        info.AudioInfos[0].Duration = 10601;
        var findings = MediaValidator.CheckFile(info);
        Assert.Contains(findings, f => f.Description.Contains("轨道间长度"));
        // Must still be 600ms threshold, not 1 second
        info.AudioInfos[0].Duration = 10600;
        findings = MediaValidator.CheckFile(info);
        Assert.DoesNotContain(findings, f => f.Description.Contains("轨道间长度"));
    }

    [Fact]
    public void EmptyDuration_EarlyReturn_Preserved_ThenFilenameAppend()
    {
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv";
        var info = new MediaFileInfo(new GeneralInfo("Show", path, "Matroska", 0, 0, 0, 0, 0));
        // Legacy parameterless: early return before filename when no duration tracks
        var legacy = MediaValidator.CheckFile(info);
        Assert.Empty(legacy);

        var collation = MediaValidator.CheckFile(info, ValidationProfile.CollationV1);
        // Collation still evaluates; empty duration legacy stream empty → append filename findings
        Assert.Contains(collation, f => f.RuleId is not null);
    }

    [Fact]
    public void MixedOutcome_MismatchAndUnverifiable_BothPreserved()
    {
        var path = "/media/[VCB-S] Show [Ma10p_720p][x265_flac].mkv";
        var info = CreateRecognized(path, width: 1918, height: 1078);
        // Authoritative raw: undocumented crop + unmapped format → Unverifiable on both rules.
        info.RawSnapshot = BuildRaw(info, 1918, 1078, "UND", "Yes", "JPN", "Yes",
            scanType: "Progressive", colourRange: "Limited", matrix: "BT.709");
        info.RawSnapshot = info.RawSnapshot with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with
                {
                    Format = RawField.Of("VP9"),
                    FormatProfile = RawField.Of(""),
                    ParsedBitDepth = 8,
                    BitDepth = RawField.Of("8"),
                },
            ],
        };
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Contains(evals, e => e.RuleId == CollationRuleIds.FnResolution && e.Outcome == RuleOutcome.Unverifiable);
        Assert.Contains(evals, e => e.RuleId == CollationRuleIds.FnVideoEncoder && e.Outcome == RuleOutcome.Unverifiable);
    }

    [Fact]
    public void NonGoals_NoPathOrTitleFindings()
    {
        var info = CreateRecognized("/media/Some.Directory/[VCB-S] Show [Ma10p_1080p][x265_flac].mkv");
        var findings = MediaValidator.CheckFile(info, ValidationProfile.CollationV1);
        Assert.DoesNotContain(findings, f => f.Description.Contains("目录", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, f => f.Description.Contains("路径长度", StringComparison.Ordinal));
        Assert.DoesNotContain(findings, f => f.Description.Contains("标题", StringComparison.Ordinal));
    }

    [Fact]
    public void DisabledMkaMp4_AreNotEmitted()
    {
        var info = CreateRecognized();
        var evals = CollationEvaluator.Evaluate(info);
        Assert.DoesNotContain(evals, e => e.RuleId == CollationRuleIds.MkaAudioOnlyDefaults);
        Assert.DoesNotContain(evals, e => e.RuleId == CollationRuleIds.Mp4MobileTrackLayout);
        Assert.False(CollationPolicyMatrix.IsMkaEnabled);
        Assert.False(CollationPolicyMatrix.IsMp4Enabled);
    }

    [Fact]
    public void ArbitraryDeclaredColorProfile_DoesNotSuppressSdrReview()
    {
        var info = CreateRecognized();
        info.DeclaredColorReviewProfile = "anything";
        info.RawSnapshot = BuildRaw(info, 1920, 1080, "UND", "Yes", "JPN", "Yes",
            scanType: "Progressive", colourRange: "Limited", matrix: "BT.709");
        info.RawSnapshot = info.RawSnapshot with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with
                {
                    TransferCharacteristics = RawField.Of("PQ"),
                },
            ],
        };
        var eval = CollationEvaluator.Evaluate(info).Single(e => e.RuleId == CollationRuleIds.VideoColorReview);
        Assert.Equal(RuleOutcome.Violation, eval.Outcome);
    }

    [Fact]
    public void ApprovedHdrProfile_SuppressesSdrReview()
    {
        var info = CreateRecognized();
        info.DeclaredColorReviewProfile = "HDR10";
        info.RawSnapshot = BuildRaw(info, 1920, 1080, "UND", "Yes", "JPN", "Yes",
            scanType: "Progressive", colourRange: "Limited", matrix: "BT.709");
        info.RawSnapshot = info.RawSnapshot with
        {
            VideoTracks =
            [
                info.RawSnapshot.VideoTracks[0] with
                {
                    TransferCharacteristics = RawField.Of("PQ"),
                },
            ],
        };
        var eval = CollationEvaluator.Evaluate(info).Single(e => e.RuleId == CollationRuleIds.VideoColorReview);
        Assert.Equal(RuleOutcome.Pass, eval.Outcome);
    }

    [Fact]
    public void AdapterUnavailable_MakesChapterRulesUnverifiable()
    {
        var info = CreateRecognized();
        info.GeneralInfo.ChapterCount = 2;
        info.ChapterInfos.Add(new ChapterInfo(0, "a", "ENG"));
        info.RawSnapshot = info.RawSnapshot! with { AdapterUnavailable = true, ChapterCount = 2 };
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Unverifiable,
            evals.Single(e => e.RuleId == CollationRuleIds.ChapterLanguageMissing).Outcome);
        Assert.Equal(RuleOutcome.Unverifiable,
            evals.Single(e => e.RuleId == CollationRuleIds.ChapterLanguageMixed).Outcome);
    }

    [Fact]
    public void FilenameChecks_PreferRaw_OverContradictoryDisplayModel()
    {
        var info = CreateRecognized(width: 1920, height: 1080);
        // Poison display model: would claim wrong encoder if Collation read legacy models.
        info.VideoInfos[0].Format = "AVC";
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Pass, evals.Single(e => e.RuleId == CollationRuleIds.FnVideoEncoder).Outcome);
    }

    [Fact]
    public void FilenameError_MapsToErrorViolet_ByRuleId()
    {
        var path = "/media/[VCB-S] Show [Ma10p_720p][x265_flac].mkv";
        var info = CreateRecognized(path, width: 1920, height: 1080);
        var findings = MediaValidator.CheckFile(info, ValidationProfile.CollationV1);
        var firstFn = findings.First(f => f.RuleId == CollationRuleIds.FnResolution);
        Assert.Equal(
            MediainfoProjectNg.Next.Core.Presentation.ColorToken.ErrorViolet,
            MediainfoProjectNg.Next.Core.Presentation.LegacyColorRules.TokenForFinding(firstFn));
    }

    [Fact]
    public void ReorderedAudioGroups_Detected()
    {
        var path = "/media/[VCB-S] Show [Ma10p_1080p][x265_aac_flac].mkv";
        var info = CreateRecognized(path);
        info.AudioInfos.Clear();
        info.AudioInfos.Add(new AudioInfo("FLAC", 16, 1000, 10000, "JPN", 0, "Yes"));
        info.AudioInfos.Add(new AudioInfo("AAC", 0, 128, 10000, "JPN", 0, "No"));
        info.RawSnapshot = info.RawSnapshot! with
        {
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Of("FLAC"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("Yes"),
                },
                new RawAudioTrack
                {
                    Format = RawField.Of("AAC"),
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("No"),
                },
            ],
        };
        // Generated order is insertion-dict order: flac then aac → _flac_aac, claim is _aac_flac
        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Violation, evals.Single(e => e.RuleId == CollationRuleIds.FnAudioEncoders).Outcome);
    }

    [Fact]
    public void MissingAudioFormat_MakesFilenameAudioClaimUnverifiable()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            AudioTracks =
            [
                new RawAudioTrack
                {
                    Format = RawField.Absent,
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("Yes"),
                },
            ],
        };

        var eval = CollationEvaluator.Evaluate(info)
            .Single(e => e.RuleId == CollationRuleIds.FnAudioEncoders);
        Assert.Equal(RuleOutcome.Unverifiable, eval.Outcome);
        Assert.Equal(ErrorLevel.Info, eval.Severity);
        Assert.Contains("unknown formats=1", eval.Actual ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownSubtitleFormat_DoesNotSilentlyPassPgsRules()
    {
        var info = CreateRecognized();
        info.RawSnapshot = info.RawSnapshot! with
        {
            TextTracks =
            [
                new RawTextTrack
                {
                    Format = RawField.Absent,
                    Language = RawField.Of("JPN"),
                    Default = RawField.Of("No"),
                },
            ],
        };

        var evals = CollationEvaluator.Evaluate(info);
        Assert.Equal(RuleOutcome.Unverifiable,
            evals.Single(e => e.RuleId == CollationRuleIds.TrackPgsLanguage).Outcome);
        Assert.Equal(RuleOutcome.Unverifiable,
            evals.Single(e => e.RuleId == CollationRuleIds.TrackPgsDefault).Outcome);
    }
}
