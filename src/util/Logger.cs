using System;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper.util {
  public class Logger {
    public readonly string name;
    public static LogLevel logLevel = LogLevel.WARNING;

    public static Logger getLogger() {
      return getLogger(new System.Diagnostics.StackFrame(1).GetMethod().DeclaringType.FullName);
    }

    public static Logger getLogger(string aName) {
      return new Logger(aName);
    }

    Logger(String aName) {
      name = aName;
    }

    public void debug(string aMessage, params object[] aParams) {
      if (!isDebugEnabled()) return;
      UnityEngine.Debug.Log(format(LogLevel.DEBUG, aMessage, aParams));
    }
    public bool isDebugEnabled() {
      return logLevel <= LogLevel.DEBUG;
    }

    public void info(string aMessage, params object[] aParams) {
      if (!isInfoEnabled()) return;
      UnityEngine.Debug.Log(format(LogLevel.INFO, aMessage, aParams));
    }
    public bool isInfoEnabled() {
      return logLevel <= LogLevel.INFO;
    }

    public void warning(string aMessage, params object[] aParams) {
      if (!isWarningEnabled()) return;
      UnityEngine.Debug.LogWarning(format(LogLevel.WARNING, aMessage, aParams));
    }
    public bool isWarningEnabled() {
      return logLevel <= LogLevel.WARNING;
    }

    public void error(string aMessage, params object[] aParams) {
      if (!isErrorEnabled()) return;
      UnityEngine.Debug.LogError(format(LogLevel.ERROR, aMessage, aParams));
    }
    public bool isErrorEnabled() {
      return logLevel <= LogLevel.ERROR;
    }

    private String format(LogLevel aLevel, string aMessage, params object[] aParams) {
      string msg = String.Format(aMessage, aParams);
      //string typeName = Enum.GetName(typeof(LogLevel), aLevel).ToUpper();
      //string strMessageLine = String.Format("{0} {1} {2}: {3}", typeName, DateTime.Now.ToString("HH:mm:ss.fff"), name, msg);
      string strMessageLine = String.Format("{0}: {1}", name, msg);
      return strMessageLine;
    }

    public enum LogLevel {
      DEBUG = 0,
      INFO = 1,
      WARNING = 2,
      ERROR = 3,
      OFF = 4
    }
  }
}
