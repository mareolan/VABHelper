using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper.util {
  public class ShortcutHelper {
    private static string[] LRM = { "l", "r", "m" };
    private static KeyCode[] MODIFIERS = { KeyCode.LeftControl, KeyCode.LeftCommand, KeyCode.LeftShift, KeyCode.LeftAlt, KeyCode.LeftWindows, KeyCode.LeftApple,
                                         KeyCode.RightControl, KeyCode.RightCommand, KeyCode.RightShift, KeyCode.RightAlt, KeyCode.RightWindows, KeyCode.RightApple, KeyCode.AltGr};
    private static Logger LOGGER = Logger.getLogger();

    public static ShortcutPattern CompileShortcut(string aShortcut) {
      // split by '+' and convert each part into KeyCode or mouse button
      List<KeyValuePair<KeyCode?, int?>> sk = new List<KeyValuePair<KeyCode?, int?>>();
      foreach (string key in aShortcut.Split('+').Select(s => s.Trim().ToLower())) {
        if (key.Length == 0) continue;
        if (System.Text.RegularExpressions.Regex.IsMatch(key, "^(left|right|middle)?click$")) {
          int btn = Array.FindIndex(LRM, p => key.StartsWith(p));
          if (btn == -1) btn = 0; // key is "click" => treat as "leftclick"
          sk.Add(new KeyValuePair<KeyCode?, int?>(null, btn));
        } else {
          KeyCode keyCode = KeyCode.None;
          try {
            keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), key, true); // try as-is
          } catch (ArgumentException) {
            try {
              keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), "left" + key, true); // try with "left" prefix (to allow specifying e.g. "Control+Click" instead of "LeftControl+Click")
            } catch (ArgumentException) {
              LOGGER.error("Unrecognized shortcut part: '{0}'. See https://docs.unity3d.com/Documentation/ScriptReference/KeyCode.html for supported keys. The key will be ignored.", key);
            }
          }
          if (keyCode != KeyCode.None) sk.Add(new KeyValuePair<KeyCode?, int?>(keyCode, null));
        }
      }
      if (sk.Count == 0) return null;

      // create the pattern from the list of Keycode-s / mouse buttons
      ShortcutPattern pattern = new ShortcutPattern();
      KeyValuePair<KeyCode?, int?> last = sk.Last();
      if (last.Key.HasValue) pattern.MainKey = last.Key.Value;
      else if (last.Value.HasValue) pattern.MainMouse = last.Value.Value;
      foreach (KeyValuePair<KeyCode?, int?> k in sk.Where(k => !ReferenceEquals(k, last))) {
        if (k.Key.HasValue) pattern.KeyStates[k.Key.Value] = true;
        else if (k.Value.HasValue) pattern.MouseStates[k.Value.Value] = true;
      }

      // treat modifiers that aren't present in shortcut yet as being required not to be pressed
      foreach (KeyCode k in MODIFIERS.Except(pattern.KeyStates.Keys)) pattern.KeyStates[k] = false;

      return pattern;
    }

    public static bool IsMatch(ShortcutPattern aPattern, MainKeyState aMainKeyStateType) {
      if (aPattern == null) return false;

      // match up the main key / mouse button
      bool isMatch = true;
      switch (aMainKeyStateType) {
        case MainKeyState.UP: isMatch = (aPattern.MainMouse.HasValue ? Input.GetMouseButtonUp(aPattern.MainMouse.Value) : Input.GetKeyUp(aPattern.MainKey.Value)); break;
        case MainKeyState.DOWN: isMatch = (aPattern.MainMouse.HasValue ? Input.GetMouseButtonDown(aPattern.MainMouse.Value) : Input.GetKeyDown(aPattern.MainKey.Value)); break;
        case MainKeyState.PRESSED: isMatch = (aPattern.MainMouse.HasValue ? Input.GetMouseButton(aPattern.MainMouse.Value) : Input.GetKey(aPattern.MainKey.Value)); break;
        default: throw new Exception("Unsupported shortcut main key state: " + aMainKeyStateType);
      }
      if (!isMatch) return false;

      // match the remaining keys / mouse buttons (using "pressed" state)
      foreach (KeyValuePair<KeyCode, bool> entry in aPattern.KeyStates) {
        if (Input.GetKey(entry.Key) ^ entry.Value) return false;
      }
      foreach (KeyValuePair<int, bool> entry in aPattern.MouseStates) {
        if (Input.GetMouseButton(entry.Key) ^ entry.Value) return false;
      }
      return true;
    }

  }

  public enum MainKeyState {
    UP,
    DOWN,
    PRESSED
  }

  public class ShortcutPattern {
    public int? MainMouse;
    public KeyCode? MainKey;
    public Dictionary<int, bool> MouseStates = new Dictionary<int, bool>();
    public Dictionary<KeyCode, bool> KeyStates = new Dictionary<KeyCode, bool>();
  }
}
