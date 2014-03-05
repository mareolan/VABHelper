using sk.mareolan.ksp.vabhelper.lang;
using sk.mareolan.ksp.vabhelper.util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper {
  public static class UILayout {
    static Logger LOGGER = Logger.getLogger();

    public static bool ButtonList<T>(IEnumerable<T> aList, IEnumerable<string> aLabels, out Option<T> aResult) {
      GUILayout.BeginVertical();
      IEnumerator<T> en = aList.GetEnumerator();
      bool hasResult = false;
      aResult = default(T);
      foreach (string label in aLabels) {
        en.MoveNext();
        if (GUILayout.Button(label, GUILayout.ExpandWidth(true))) {
          hasResult = true;
          aResult = en.Current;
        }
      }
      GUILayout.EndVertical();
      return hasResult;
    }

    public static bool ButtonList(IEnumerable<string> aLabels, out int aResult) {
      Option<int> o;
      bool r = ButtonList(aLabels.Select((c, i) => i), aLabels, out o);
      aResult = o;
      return r;
    }

  }
}
