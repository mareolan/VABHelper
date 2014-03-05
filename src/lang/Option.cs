using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sk.mareolan.ksp.vabhelper.lang {
  public struct Option<T> { // something like Nullable<T> to be able to use it easily for "out"/"ref" parameters
    readonly T Value;
    public Option(T aValue) {
      Value = aValue;
    }
    public static implicit operator T(Option<T> m) {
      return m.Value;
    }
    public static implicit operator Option<T>(T o) {
      return new Option<T>(o);
    }
  }
}
