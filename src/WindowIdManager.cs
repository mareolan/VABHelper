using sk.mareolan.ksp.vabhelper.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sk.mareolan.ksp.vabhelper {

  /// <summary>
  /// Provides window IDs to other components. The component is supposed to obtain an ID, keep it while the window is shown
  /// and release it once the window got closed.
  /// </summary>
  public class WindowIdManager {
    static readonly Logger LOGGER = Logger.getLogger();
    static readonly int startId = 1915167;
    static HashSet<int> usedIds = new HashSet<int>();

    public static int getUsableWindowId() {
      int i = startId;
      while (usedIds.Contains(i)) ++i;
      usedIds.Add(i);
      return i;
    }

    public static void releaseWindowId(int aWindowId) {
      if (!usedIds.Contains(aWindowId)) {
        LOGGER.error("The window ID {0} cannot be released because it's not used anymore/yet. Ignoring.", aWindowId);
        return;
      }
      usedIds.Remove(aWindowId);
    }
  }
}
