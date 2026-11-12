## Download
- [Windows x64 ZIP]({{DOWNLOAD_URL}})
- [SHA-256 checksum]({{CHECKSUM_URL}})

## Getting started
Extract the ZIP file, then run `SysmacDataTraceViewer.exe`.

Requirements: Windows x64. The .NET runtime is included.

## Changes
### Added
- Import comments directly from Sysmac Studio `.smc2` projects, including local variables, array elements and structure/FB members.
- Detect UTF-8 and Shift_JIS CSV files, show read diagnostics, and load in the background with progress and cancellation.
- Open a CSV by dropping it onto the window or passing its path at startup.

### Changed
- Speed up large BOOL traces while retaining all samples for cursor values and navigation.
- Refresh the layout with a wrapping toolbar, cursor colors, lane stripes, color swatches, full-name tooltips and clearer status readouts.
- Update the bundled .NET runtime to 10.0.15 and refresh the specification and maintenance documentation.

### Fixed
- Synchronize the time scrollbar with zoom and keep it below the chart.
- Align lane names with the chart and keep BOOL values visible beside long names.
- Save comment CSV files safely and support multiline comments.
- Run tests in CI and before publishing releases.

<!-- virustotal:start -->
{{VIRUSTOTAL_CONTENT}}
<!-- virustotal:end -->
