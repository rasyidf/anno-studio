using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AnnoDesigner.Controls.EditorCanvas.Tooling;

namespace AnnoDesigner.Controls.EditorCanvas.Interaction
{
    public enum HotkeyActionType
    {
        ActivateTool,
        Command
    }

    public class HotkeyBinding
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Key? Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public MouseButton? MouseButton { get; set; }
        public HotkeyActionType ActionType { get; set; }
        public string Target { get; set; } = string.Empty;
    }

    /// <summary>
    /// Central hotkey router for EditorCanvas; maps input gestures to tool activations or commands.
    /// </summary>
    public class HotkeyManager
    {
        private readonly ToolManager _toolManager;
        private readonly Action<string> _commandHandler;
        private readonly List<HotkeyBinding> _bindings = new();

        public HotkeyManager(ToolManager toolManager, Action<string> commandHandler)
        {
            _toolManager = toolManager ?? throw new ArgumentNullException(nameof(toolManager));
            _commandHandler = commandHandler;
        }

        public IEnumerable<HotkeyBinding> Bindings => _bindings;

        public void RegisterBinding(HotkeyBinding binding)
        {
            if (binding == null || string.IsNullOrWhiteSpace(binding.Id)) return;
            var idx = _bindings.FindIndex(b => string.Equals(b.Id, binding.Id, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _bindings[idx] = binding;
            else _bindings.Add(binding);
        }

        public void ReplaceBindings(IEnumerable<HotkeyBinding> bindings)
        {
            _bindings.Clear();
            if (bindings == null) return;
            foreach (var b in bindings) RegisterBinding(b);
        }

        public bool HandleKeyDown(KeyEventArgs e)
        {
            if (e == null) return false;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var binding = FindMatch(key, null, e.KeyboardDevice.Modifiers);
            return Execute(binding);
        }

        public bool HandleMouseDown(MouseButtonEventArgs e)
        {
            if (e == null) return false;
            var binding = FindMatch(null, e.ChangedButton, Keyboard.Modifiers);
            return Execute(binding);
        }

        private HotkeyBinding? FindMatch(Key? key, MouseButton? button, ModifierKeys modifiers)
        {
            modifiers = NormalizeModifiers(modifiers);
            return _bindings.FirstOrDefault(b =>
                NormalizeModifiers(b.Modifiers) == modifiers &&
                ((b.Key.HasValue && key.HasValue && b.Key.Value == key.Value) ||
                 (b.MouseButton.HasValue && button.HasValue && b.MouseButton.Value == button.Value)));
        }

        private static ModifierKeys NormalizeModifiers(ModifierKeys modifiers)
        {
            return modifiers & (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Windows);
        }

        private bool Execute(HotkeyBinding? binding)
        {
            if (binding == null) return false;

            switch (binding.ActionType)
            {
                case HotkeyActionType.ActivateTool:
                    if (!string.IsNullOrWhiteSpace(binding.Target))
                    {
                        return _toolManager.Activate(binding.Target);
                    }
                    break;
                case HotkeyActionType.Command:
                    if (!string.IsNullOrWhiteSpace(binding.Target))
                    {
                        _commandHandler?.Invoke(binding.Target);
                        return true;
                    }
                    break;
            }

            return false;
        }
    }
}
