1. **Create `PaletteViewModel`**:
   - Add a new `PaletteViewModel` that implements `ObservableObject`.
   - The view model will hold properties like `InkColor`, allowing the UI to bind to them.
   - Inject `PaletteViewModel` into `MainWindowViewModel` so the view can access it.
   - Ensure the new `PaletteViewModel` correctly links up with the state management currently handled via `InkPaletteSessionState`. Since `InkPaletteSessionState` seems internal to `Ink_Canvas.Features.Ink.State`, and `inkColor` is accessed via `PaletteState.InkColor` within `MainWindow` (which acts as `IInkCanvasHost`), we can either make `InkPaletteSessionState` observable or keep it simple by wrapping the values in a `PaletteViewModel` exposed on `MainWindowViewModel`.

2. **Create `ColorIndexToVisibilityConverter`**:
   - Create a new converter `ColorIndexToVisibilityConverter` implementing `IValueConverter`.
   - Place it in `Ink Canvas/MainWindow/Converters/ColorIndexToVisibilityConverter.cs`.
   - In `Convert`, it checks if the bound `int` matches the provided `ConverterParameter` (converted to `int`). Returns `Visibility.Visible` if matched, `Visibility.Collapsed` otherwise.

3. **Update `MainWindow.xaml` and `MainWindow.xaml.cs`**:
   - Add the converter to `MainWindow.xaml` resources.
   - Update `MainWindow.xaml` to bind the visibility of all those `Viewbox*Content` elements to the selected color index, e.g. `<Viewbox Visibility="{Binding WorkspaceSession.Palette.InkColor, Converter={StaticResource ColorIndexToVisibilityConverter}, ConverterParameter=1}" ...>`
   - Wait, `PaletteViewModel` on `MainWindowViewModel` or just `PaletteViewModel`? Let's add it to `MainWindowViewModel` -> `public PaletteViewModel Palette { get; }`. Wait, the user mentioned: "exposing it through a small dedicated ink/palette-facing view model, or at minimum making the existing palette state observable and surfacing it through MainWindowViewModel." Let's create `PaletteViewModel` under `Ink Canvas/ViewModels/Main/PaletteViewModel.cs` and add it to `MainWindowViewModel`.

4. **Refactor `CheckColorTheme`**:
   - In `Ink Canvas/MainWindow/Events/PenPaletteEvents.cs:62`, remove all lines toggling `Viewbox*.Visibility`.
   - Ensure `InkColor` in the view model updates when `PaletteState.InkColor` changes. Since `inkColor` setter in `MainWindow.xaml.cs` is private property `inkColor { get => PaletteState.InkColor; set => PaletteState.InkColor = value; }` inside `InkCanvasHost.cs`, I should update the view model's property whenever this setter is hit, or route all `inkColor` sets through the view model. Actually, the best is to make `InkColor` observable on the view model and use that as the single source of truth for the UI bindings.

5. **Verify changes**:
   - Compile the project and ensure there are no build errors.
   - Run tests if available.
