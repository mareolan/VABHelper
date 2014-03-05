using sk.mareolan.ksp.vabhelper.lang;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper {

  /// <summary>
  /// Popup for displaying a list of vessel parts.
  /// </summary>
  /// <example>Expected usage:
  /// <code>Awake() {
  ///   picker = UCPopup.create&lt;UCVesselPartPicker&gt;().
  ///   picker.onPartPicked += MyHandler;
  /// }
  /// MyHandler(Part aPickedPart) {
  ///   ...
  /// }
  /// ...
  /// picker.openPopup(listOfPartsToShow);</code>
  /// </example>
  public class UCVesselPartPicker : UCPopup {

    public event PartPickedFn onPartPicked;
    public delegate void PartPickedFn(Part aPart);

    List<Part> parts;
    List<string> partNames;

    public void openPopup(List<Part> aParts) {
      parts = aParts;
      partNames = parts.ConvertAll(c => c.partInfo.title);
      base.openPopup(GUILayout.MinWidth(70), GUILayout.MaxWidth(300));
    }

    protected override void drawContent() {
      Option<Part> chosenPart;
      if (UILayout.ButtonList(parts, partNames, out chosenPart)) {
        if (onPartPicked != null) onPartPicked(chosenPart);
        closePopup();
      }
    }
  }
}
