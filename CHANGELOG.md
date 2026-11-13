# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

All notable changes are recorded from this point onward.

## [Unreleased]

### Changed

- Align local and CI executable, fixed-name ZIP and UTF-8/LF SHA-256 output under `artifacts/release/`; preserve required distribution files and require the VirusTotal API SHA-256 to match.


## [1.1.0] - 2026-11-13

### Added

- Detect BOM-marked encodings, validate BOM-less UTF-8 and fall back to Shift_JIS (CP932) for trace and comment CSV files; show detected encodings in the status.
- Load CSV files in the background with progress and cancellation, preserving the previous trace if parsing fails or is cancelled.
- Report skipped rows, invalid values and backwards-time rows with a details view; record unexpected exceptions in bounded local logs without CSV names or values.
- Import comments directly from Sysmac Studio `.smc2` projects, including POU-local variables, array elements and structure/FB members, while preserving display settings.
- Open a CSV through file drop or a startup argument, using the same loader as the menu.
- Check the pinned bundled runtime against Microsoft's stable release metadata in a maintenance workflow.

### Changed

- Reduce plot points for large BOOL traces while retaining the original samples for cursor values, hover readouts and change navigation.
- Refresh the window with a wrapping toolbar, cursor-specific colors, warning/progress indicators, keyboard hints and an empty-state guide.
- Add alternating lane stripes to the chart, name lane and PNG exports; show ON values in green, color swatches and full-name tooltips.
- Show the loaded file name in the title and trim long lane names while keeping BOOL values visible.
- Replace the application icon with a two-lane timing chart design in 9 sizes from 16 to 256 px.
- Consolidate version metadata and documentation, complete older changelog entries, and align the specification with the current implementation.
- Update the bundled .NET runtime to 10.0.15.

### Fixed

- Keep the time scrollbar synchronized with zoom and place it below the chart only.
- Align name rows with chart lanes by accounting for plot padding and avoiding per-row layout rounding.
- Save comment CSV files through a temporary file and replacement; round-trip multiline comments, commas and quotes.
- Run tests in CI and before publishing a release, pin workflow actions and normalize the local publish output path.

## [1.0.5] - 2026-10-27

### Changed

- Standardized fixed-name Windows ZIP assets, ZIP SHA-256 checksums and English release notes.

### Fixed

- Unified VirusTotal completion checks and preserved unrelated release notes during rescans.

## [1.0.4] - 2026-06-15

### Changed

- Extracted plotting, formatting, cursor state and signal ordering into services, with service characterization tests.
- Updated OxyPlot to 2.2.0 and refreshed test dependencies.
- Added security support documentation and manual VirusTotal rescans.

## [1.0.3] - 2026-03-28

### Added

- Added analyzer/format verification, GitHub CI, automated releases, and local build/CI batch scripts.

## [1.0.2] - 2026-02-09

### Added

- Displayed the current cursor sample's BOOL value in the name lane.
- Included the signal label in the hover state display.

## [1.0.1] - 2026-02-06

### Added

- Initial published Windows viewer with BOOL timelines, primary/delta cursors, typed values, signal visibility and ordering, colors, comment CSV files and PNG export.

[1.1.0]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/v1.1.0
[1.0.5]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/v1.0.5
[1.0.4]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/v1.0.4
[1.0.3]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/v1.0.3
[1.0.2]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/Ver.1.0.2
[1.0.1]: https://github.com/fa-yoshinobu/SysmacDataTraceViewer/releases/tag/Ver.1.0.1
