# Media fixtures

Placeholder directory for **legal, redistributable** media samples used by integration and native smoke tests.

## Purpose

- Exercise MediaInfoLib projection (video / audio / text / chapters / multi-track).
- Cover container/extension mismatch cases, unicode paths, and malformed-but-safe files.
- Keep Domain/Core unit tests independent of this folder (they use synthetic models).

## Rules

1. **License first.** Only commit files you have rights to redistribute under a clear license (e.g. CC0, CC-BY, or project-owned synthetics). Record the license next to each file or in a `SOURCES.md` here.
2. **Keep fixtures small.** Prefer short clips (a few seconds) and tiny containers. Avoid multi‑GB samples.
3. **No copyrighted commercial content.** Do not copy from Blu-rays, streaming rips, or other unlicensed sources (including material from the reference `mpng` machine that is not clearly licensed).
4. **Prefer synthetic generation** when possible (e.g. `ffmpeg` with test patterns and public-domain audio) so the repo stays self-contained and license-clean.
5. **Do not commit secrets** or personal media from developer machines.

## Planned layout (future)

```text
fixtures/media/
  README.md                 # this file
  SOURCES.md                # license + provenance table
  video/
    hevc_mkv_short.mkv
    avc_mp4_short.mp4
  audio/
    flac_stereo.flac
  text/
    srt_utf8.srt
  chapters/
    mkv_with_chapters.mkv
  edge/
    unicode_名称.mkv
    empty_video_track.mkv   # if needed for characterization
```

## Generating samples (suggested)

Once tools are available locally:

```bash
# Example only — do not commit until licenses for codecs/tools usage are confirmed.
ffmpeg -f lavfi -i testsrc=duration=1:size=320x240:rate=24 \
       -f lavfi -i sine=frequency=440:duration=1 \
       -c:v libx264 -c:a aac -shortest fixtures/media/video/avc_aac_short.mp4
```

## CI note

Integration tests that need natives + fixtures should **skip** (not fail) when either the MediaInfo shared library or required fixture files are missing, so Domain/Core gates stay green without native setup.
