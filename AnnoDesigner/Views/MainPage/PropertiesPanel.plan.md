# Properties Panel — Plan and Improvements

Goal
- Improve fidelity and UX of the `PropertiesPanel` to be more like Figma's Properties panel: more compact, contextual, discoverable, keyboard-friendly, and visually clearer.

Scope
- This plan covers analysis, prioritized improvements, implementation tasks, accessibility notes, testing, and verification steps. It is intentionally incremental so changes can be reviewed and validated in small pulls.

Analysis of current `PropertiesPanel`
- Static grouped layout: Groups are fixed and show all controls at once. Figma uses contextual panels that change based on selection and collapsible sections.
- Mixed control density: Some controls use compact `ui:NumberBox` (good), but spacing and alignment could be tightened for scanability.
- No clear visual separation between sections beyond small headers and expanders.
- Color picker is embedded but small; no swatches or quick presets shown inline.
- Controls do not expose quick keyboard adjustments (e.g., nudging numbers with arrow/shift-arrow) or inline edit features (double-click to edit). Some are `NumberBox` which may support this, but behaviour must be verified.
- Limited grouping for advanced settings; options are in a bottom expander but could be organized into labeled sections (Layout, Appearance, Influence, Options).
- No inline contextual help or tooltips except a few localized tooltips.

Principles derived from Figma
- Contextuality: Show only controls relevant to current selection (single building vs multiple vs group). Prioritize the most-used properties first.
- Compactness: Use compact labels, tighter vertical spacing, right-aligned inputs to make values easy to scan and compare.
- Inline presets: Offer color swatches and icon quick-picks close to the color/icon fields.
- Consistency: Use consistent controls, units, and formatting across fields (e.g., size uses integer boxes, radius uses decimals with 2dp).
- Discoverability: Provide section headers, separators, and small helper text or icons for advanced settings.
- Keyboard-first: Support focus order, Tab navigation, arrow nudging for numeric inputs, and hotkeys for common actions.
- Accessibility: Ensure focus visuals, automation-friendly names, screen-reader friendly labels, and logical reading order.

Prioritized improvement list (high → low)
1. Contextual header + selection summary: show what is selected and count (e.g., "House — 3 selected").
2. Compact and consistent spacing: reduce vertical gaps, right-align values, use Label + Value pairs.
3. Rework Color control: add inline swatches, recent colors, and preset palette; keep the `PortableColorPicker` but provide a popup with larger editor.
4. Icon quick-pick: show a small 3-column grid of top icons in the ComboBox popup and expose recently used icons inline.
5. Reorganize sections into collapsible panels with clear headers (Layout, Appearance, Influence, Advanced).
6. Keyboard improvements: ensure `ui:NumberBox` arrow nudging works, add Shift+Arrow for larger step, allow direct typing with Enter to commit.
7. Tooltips and helper text: provide short helper text under fields where the meaning may be unclear (e.g., Influence vs Radius).
8. Visual polish: separators, subtle background for groups, and consistent alignment of controls.
9. Accessibility: add AutomationProperties.Name for controls and ensure Expanders/Groups are accessible.
10. Save/Apply feedback: small transient toast or status text when "Apply Color" runs successfully.

Implementation tasks (split to small PR-friendly steps)

Step A — Surface changes and scaffolding (small):
- Add a selection summary header area above the groups.
- Add `PropertiesPanel.plan.md` (this file).
- Refactor layout Grid row spacing to use resource values for vertical spacing.

Step B — Layout & spacing (small → medium):
- Introduce styles for compact labels and compact inputs (in `App.xaml` or theme resource dictionary).
- Update margins in `PropertiesPanel.xaml` to use these styles and reduce vertical spacing.

Step C — Sections & collapsible panels (medium):
- Replace the single `GroupBox` + `Expander` layout with named `Expander` or `ui:CardExpander` sections: `Layout`, `Appearance`, `Influence`, `Advanced`.
- Move relevant controls into these sections.

Step D — Color improvements (medium):
- Add an `ItemsControl` of recent swatches next to the `PortableColorPicker`.
- Add a small `Button` on the color control that opens a full-size color editor dialog (reusing `ColorPicker` control).
- Bind recent colors to ViewModel as `ObservableCollection<Color>`.

Step E — Icon quick-pick (small):
- Modify `ComboBox` popup template to show icons in a uniform grid and add a "Recent" top row.
- Expose `AvailableIcons` as grouped collection: Recent + All.

Step F — Numeric input improvements & keyboard (small):
- Verify `ui:NumberBox` supports up/down arrow nudging and `LargeChange` for Shift. If not, add PreviewKeyDown handler to adjust value accordingly.
- Ensure `NumberBox` has `AutomationProperties.Name` and `ToolTip` where needed.

Step G — Accessibility and automation (small):
- Add `AutomationProperties.Name` to all interactive controls.
- Ensure `Label` textblocks precede inputs for screen readers.

Step H — Apply/Feedback UX (small):
- Add ephemeral status label above Place button to show brief success messages.
- Consider disabling `PlaceBuilding` while operations are in progress.

Step I — Tests and verification (non-code manual steps):
- Manual checklist to validate layout on various DPI scales.
- Keyboard-only navigation test.
- Screen-reader test.
- Usability review: time-to-change property for common actions.

Code snippets and examples (adapt these into project files)

1) Selection header (XAML snippet):
- Add at top of `DockPanel`:
  <TextBlock FontSize="14" FontWeight="Bold" Text="{Binding SelectionSummary}" />

2) Recent color swatches (XAML snippet):
- Next to the `PortableColorPicker`:
  <ItemsControl ItemsSource="{Binding BuildingSettingsViewModel.RecentColors}">
    <ItemsControl.ItemsPanel>
      <ItemsPanelTemplate>
        <StackPanel Orientation="Horizontal" />
      </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Button Width="20" Height="20" Margin="2" Command="{Binding DataContext.BuildingSettingsViewModel.UseRecentColorCommand, RelativeSource={RelativeSource AncestorType=UserControl}}" CommandParameter="{Binding}" Background="{Binding Converter={StaticResource ConverterColorToSolidColorBrush}}" />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>

3) Compact label style (App.xaml resources):
  <Style x:Key="PropertyLabel" TargetType="TextBlock">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Margin" Value="0,0,6,0" />
    <Setter Property="VerticalAlignment" Value="Center" />
  </Style>

Verification checklist (to run after changes)
- Selection header updates when different items selected.
- Spacing is reduced and values align on the right.
- Color swatches show recent colors and apply when clicked.
- Icon popup shows recent icons and grid layout.
- Numeric inputs support keyboard nudging and shift-large-step behavior.
- All interactive elements have AutomationProperties.Name.
- Place Building button is disabled when operations are running.

Risks and notes
- Make small, incremental PRs that update styles first — large visual changes are harder to review.
- Some `ui:*` controls come from external libs; ensure changes respect their templating model.
- ViewModel changes required: `RecentColors`, `SelectionSummary`, commands for quick picks.

Next steps (choose one):
- I can implement Step A & B (layout and spacing) in a patch now.
- Or I can expand the plan with exact XAML diffs for each step to simplify your edits.

Please tell me which next step you prefer.