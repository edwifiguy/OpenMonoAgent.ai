using System.Text.Json;
using Terminal.Gui.Input;

namespace OpenMono.Tui.Keybindings;

public class KeybindingManager
{
    private readonly Dictionary<TuiAction, Key> _bindings;

    public KeybindingManager()
    {
        _bindings = BuildDefaults();
    }

    public KeybindingManager(string configPath)
    {
        _bindings = BuildDefaults();
        ApplyOverrides(configPath);
    }

    private static Dictionary<TuiAction, Key> BuildDefaults() => new()
    {
        [TuiAction.Pause] = Key.P.WithCtrl,
        [TuiAction.ToggleSidebar] = Key.S.WithCtrl,
        [TuiAction.Help] = Key.F1,
    };

    private void ApplyOverrides(string configPath)
    {
        if (!File.Exists(configPath)) return;

        try
        {
            var json = File.ReadAllText(configPath);
            var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!Enum.TryParse<TuiAction>(prop.Name, out var action)) continue;
                var key = ParseKey(prop.Value.GetString() ?? "");
                if (key != null)
                    _bindings[action] = key;
            }
        }
        catch (JsonException) { }
        catch (Exception) { }
    }

    public Key? GetKey(TuiAction action) =>
        _bindings.TryGetValue(action, out var key) ? key : null;

    public TuiAction? Resolve(Key key)
    {
        foreach (var (action, bound) in _bindings)
            if (bound == key) return action;
        return null;
    }

    public string GetHint(TuiAction action)
    {
        var key = GetKey(action);
        if (key is null) return "";

        if (key.IsCtrl)
        {
            var grapheme = key.NoCtrl.AsGrapheme?.ToUpperInvariant() ?? "";
            return "^" + grapheme;
        }

        return key.ToString() ?? "";
    }

    private static Key? ParseKey(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (s.StartsWith("Ctrl+", StringComparison.OrdinalIgnoreCase))
        {
            var letter = s["Ctrl+".Length..].Trim().ToUpperInvariant();
            return letter switch
            {
                "A" => Key.A.WithCtrl, "B" => Key.B.WithCtrl, "C" => Key.C.WithCtrl,
                "D" => Key.D.WithCtrl, "E" => Key.E.WithCtrl, "F" => Key.F.WithCtrl,
                "G" => Key.G.WithCtrl, "H" => Key.H.WithCtrl, "I" => Key.I.WithCtrl,
                "J" => Key.J.WithCtrl, "K" => Key.K.WithCtrl, "L" => Key.L.WithCtrl,
                "M" => Key.M.WithCtrl, "N" => Key.N.WithCtrl, "O" => Key.O.WithCtrl,
                "P" => Key.P.WithCtrl, "Q" => Key.Q.WithCtrl, "R" => Key.R.WithCtrl,
                "S" => Key.S.WithCtrl, "T" => Key.T.WithCtrl, "U" => Key.U.WithCtrl,
                "V" => Key.V.WithCtrl, "W" => Key.W.WithCtrl, "X" => Key.X.WithCtrl,
                "Y" => Key.Y.WithCtrl, "Z" => Key.Z.WithCtrl,
                _ => null
            };
        }

        return s.ToUpperInvariant() switch
        {
            "F1" => Key.F1, "F2" => Key.F2, "F3" => Key.F3, "F4" => Key.F4,
            "F5" => Key.F5, "F6" => Key.F6, "F7" => Key.F7, "F8" => Key.F8,
            "F9" => Key.F9, "F10" => Key.F10, "F11" => Key.F11, "F12" => Key.F12,
            _ => null
        };
    }
}
