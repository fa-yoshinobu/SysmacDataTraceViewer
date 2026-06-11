# Phase 4 Refactor Plan - SysmacDataTraceViewer

## Purpose

Phase 4 is a behavior-preserving refactor to thin `MainWindow` and move UI-independent logic into testable units. The order is intentionally conservative: first isolate plot construction, then remove direct dialog dependencies, then centralize cursor state, then move actions into commands, and finally expand XAML bindings.

## Goals

- Keep current CSV loading, plotting, comments, cursor, and PNG export behavior unchanged.
- Reduce `MainWindow` responsibilities without rewriting the application architecture at once.
- Add tests around each extracted unit before relying on it.
- Keep each step small enough to review and revert independently.

## Non-goals

- No UI redesign.
- No OxyPlot upgrade or plotting library change.
- No broad MVVM rewrite in a single pass.
- No text, filter, shortcut, or export behavior changes unless explicitly covered by tests.
- No CI, packaging, or version metadata changes unless requested separately.

## Preconditions

- Existing tests pass before Phase 4 work starts.
- `ci.bat` remains the main verification command.
- If a representative trace CSV is available, use it for manual plot, cursor, and PNG export checks.
- If no sample file is available, record the skipped manual checks in the final work note.

## Recommended Order

### 1. Extract PlotModel construction into `TracePlotModelBuilder`

Scope:
- Move only `PlotModel` construction logic out of `MainWindow`.
- Keep `PlotView`, controller setup, mouse/key events, cursor annotations, and selection UI in `MainWindow` for the first extraction.
- Preserve current axis, series, color, marker, lane, title, legend, and margin behavior.

Deliverables:
- New builder class focused on turning loaded trace data and view settings into an OxyPlot `PlotModel`.
- Focused tests for structural plot behavior, such as axis count, series count, boolean lane handling, label mode, and empty/no-change cases.
- `MainWindow` delegates plot creation to the builder with minimal call-site changes.

Verification:
- Run `ci.bat`.
- Start the app.
- Load a trace CSV if available.
- Visually compare the plot against current behavior.
- Export visible/full PNG if sample data is available.

Risks:
- Accidentally moving cursor or interaction concerns too early.
- Small visual differences from missed axis or annotation settings.

### 2. Introduce `IDialogService`

Scope:
- Isolate file dialogs and message boxes behind an interface.
- Add a WPF implementation that preserves existing owner window, filters, titles, default extensions, and messages.
- Use the abstraction from `MainWindow` or a small coordinator without changing user-facing strings.

Deliverables:
- `IDialogService` interface.
- WPF dialog implementation.
- Tests around any logic that becomes independent from `OpenFileDialog`, `SaveFileDialog`, or `MessageBox`.

Verification:
- Run `ci.bat`.
- Manually verify open, save/export, comment load/save, and error/info messages.

Risks:
- Losing dialog owner behavior.
- Accidentally changing filters or default filenames.

### 3. Centralize cursor state in `CursorState`

Scope:
- Move cursor state, delta cursor state, active cursor selection, range-band state, and related pure calculations into a small model.
- Keep OxyPlot annotation objects and UI event wiring in `MainWindow` until the state object is stable.
- Treat cursor state changes as data first, rendering second.

Deliverables:
- `CursorState` class or record-based model.
- Tests for primary cursor, delta cursor, swap behavior, clear behavior, sample lookup boundaries, and range selection state.
- `MainWindow` updates annotations from `CursorState` instead of owning all cursor data directly.

Verification:
- Run `ci.bat`.
- Manually verify cursor placement, drag, swap, range selection, and previous/next change navigation.

Risks:
- Off-by-one behavior around sample index lookup.
- Mixing view coordinates and sample coordinates in the state model.

### 4. Convert actions to ViewModel commands

Scope:
- Move command eligibility and command execution logic out of code-behind gradually.
- Start with actions that already have isolated services or pure dependencies.
- Preserve existing menu items, toolbar actions, keyboard shortcuts, and enabled/disabled behavior.

Deliverables:
- Commands for safe, well-bounded actions first.
- Tests for command `CanExecute` and state changes where practical.
- Reduced event-handler code in `MainWindow`.

Verification:
- Run `ci.bat`.
- Manually verify all menu, toolbar, and shortcut paths.

Risks:
- Command state not refreshing after file load, plot rebuild, or selection changes.
- Moving UI-only behavior into the ViewModel too early.

### 5. Expand XAML bindings and thin code-behind

Scope:
- Bind more UI state directly to ViewModel properties after commands and services are stable.
- Remove direct UI updates from code-behind only when equivalent bindings are covered by tests or manual checks.
- Keep complex OxyPlot interaction code in code-behind unless there is a clear, tested place for it.

Deliverables:
- Additional bindings for status text, toggles, selected options, and command targets.
- Fewer imperative UI synchronization methods.
- `MainWindow` focused on view composition and OxyPlot interaction glue.

Verification:
- Run `ci.bat`.
- Manually verify startup state, loaded state, option toggles, selected signal behavior, comments, cursor UI, and exports.

Risks:
- Binding update timing differences.
- Silent UI regressions from missing `PropertyChanged` notifications.

## Commit Strategy

- Keep one concern per commit.
- Prefer this sequence within each step:
  1. Add or adjust tests.
  2. Add the extracted type or abstraction.
  3. Switch the call site.
  4. Run `ci.bat`.
- Do not mix unrelated cleanup with Phase 4 extraction commits.
- Do not modify `.github`, build metadata, packaging, or version files as part of this phase.

## Manual Verification Checklist

- App starts normally.
- Trace CSV load works.
- Plot layout, axes, labels, colors, and boolean lanes match current behavior.
- Signal visibility toggles work.
- Label mode toggle works.
- `[No Change]` handling remains correct.
- Cursor placement, dragging, swap, and clear work.
- Previous/next change navigation works for both visible and selected scopes.
- Range selection band works.
- Comment load/save round trip works.
- Drag-and-drop signal reorder works.
- Visible PNG export works.
- Full PNG export works.
- Error and info dialogs show the same user-facing text as before.

## Stop Conditions

Pause and reassess before continuing if:

- A visual plot difference appears and is not clearly intentional.
- Cursor behavior changes without a focused test explaining the new behavior.
- A step requires changing OxyPlot event ownership earlier than planned.
- A refactor touches CI, packaging, publishing, or version metadata.
- Manual checks require sample data that is not available.

## Definition of Done

Phase 4 is done when `MainWindow` no longer owns plot construction, dialog calls are isolated, cursor state has a tested home, common actions are command-backed, XAML owns routine UI synchronization, `ci.bat` passes, and the manual verification checklist has been completed or explicitly recorded as skipped where sample data was unavailable.
