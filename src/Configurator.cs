using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using sk.mareolan.ksp.vabhelper.util;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper {

  class Configurator {
    private static readonly Logger LOGGER = Logger.getLogger();
    private static readonly string CONFIG_FILE = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "config.conf");
    private static bool inited = false;

    public static void init() {
      if (inited) return;
      inited = true;

      // load configuration
      Dictionary<string, string> config = new Dictionary<string, string>();
      try {
        if (File.Exists(CONFIG_FILE)) {
          IEnumerable<string> opts = File.ReadAllLines(CONFIG_FILE, Encoding.UTF8).Select(s => s.Trim()).Where(s => !s.StartsWith("#") && s.Length > 0);
          foreach (string opt in opts) {
            string[] v = opt.Split(new char[] { '=' }, 2);
            if (v.Length != 2) continue;
            config[v[0].Trim()] = v[1].Trim();
          }
        }
      } catch (Exception e) {
        LOGGER.error("Unable to load configuration from file '{0}'. Inner exception:\n{1}", CONFIG_FILE, e);
      }

      // apply configuration
      foreach (KeyValuePair<string, string> entry in config) {
        switch (entry.Key) {
          case "debug.level":
            Logger.logLevel = (Logger.LogLevel)Enum.Parse(typeof(Logger.LogLevel), entry.Value, true);
            break;
          case "pick.shortcut":
            VABHelper.PickShortcut = entry.Value;
            break;
          default:
            LOGGER.warning("Unrecognized option '{0}' in the plugin configuration file: {1}", entry.Key, CONFIG_FILE);
            break;
        }
      }
    }
  }

}
