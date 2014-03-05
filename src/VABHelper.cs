using sk.mareolan.ksp.vabhelper.util;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper {

  [KSPAddon(KSPAddon.Startup.EditorAny, false)]
  public class VABHelper : MonoBehaviour {
    static Logger LOGGER = Logger.getLogger();

    public static string PickShortcut;

    readonly bool showEvenIfSinglePart = true;
    EditorLogic editorLogic;
    UCVesselPartPicker partPicker;
    ShortcutPattern partPickerShortcut;

    void Awake() {
      Configurator.init();
      editorLogic = EditorLogic.fetch;
      partPicker = UCPopup.create<UCVesselPartPicker>();
      partPicker.onPartPicked += pickPart;
    }

    void Start() {
      string shortcut = (PickShortcut != null ? PickShortcut : "Control+Shift+Click");
      LOGGER.debug("Using pick shortcut: {0}", shortcut);
      partPickerShortcut = ShortcutHelper.CompileShortcut(shortcut);
    }

    void Update() {
      if (!isPartSelectionClick()) return;

      // raytrace
      Ray ray = editorLogic.editorCamera.ScreenPointToRay(Input.mousePosition);
      RaycastHit[] hits = Physics.RaycastAll(ray, 1000.0f, EditorLogic.LayerMask);
      if (hits.Length == 0 || (!showEvenIfSinglePart && hits.Length <= 1)) return;

      // order by distance and use only the closest ones belonging to the same vessel
      Array.Sort(hits, ByDistanceComparer.instance);
      List<Part> parts = new List<Part>();
      Part closestPart = null;
      foreach (RaycastHit hit in hits) {
        Part part = EditorLogic.GetComponentUpwards<Part>(hit.collider.gameObject);
        if (part != null) {
          if (part.frozen) { // the part is not attached to the ship
            if (parts.Count == 0) break;
            else continue;
          }
          if (parts.Count == 0) closestPart = part;
          else if (part.vessel != closestPart.vessel) continue; // if clicking through multiple ships (or pod vs. unattached part) then skip parts of the more distant vessels
          parts.Add(part);
        }
      }
      if (parts.Count == 0) return;
      if (!showEvenIfSinglePart && parts.Count <= 1) return;

      // we have multiple parts; log if enabled
      if (LOGGER.isDebugEnabled()) {
        string s = "Parts to pick from (front to back):\n" + string.Join("\n", parts.ConvertAll(p => p.partInfo.title).ToArray());
        s += "\nMouse position: " + Input.mousePosition;
        LOGGER.debug("{0}", s);
      }

      // show the "pick part" popup
      partPicker.openPopup(parts);
    }

    void OnDestroy() {
      Destroy(partPicker);
    }

    bool isPartSelectionClick() {
      // NOTE We're checking for "down" state so that we process the click/press sooner than EditorLogic
      // (and our popup is supposed to block "up" state so that it doesn't go to the EditorLogic).
      return (editorLogic.editorScreen == EditorLogic.EditorScreen.Parts && editorLogic.PartSelected == null &&
              editorLogic.state != EditorLogic.EditorState.PAD_SELECTED && !editorLogic.mouseOverGUI &&
              ShortcutHelper.IsMatch(partPickerShortcut, MainKeyState.DOWN));
    }

    private void pickPart(Part aPart) {
      LOGGER.debug("Part chosen: {0}", (aPart != null ? aPart.partInfo.title : "null"));
      if (!aPart) return; // shouldn't happen unless called directly (outside UCVesselPartPicker)

      // TODO This code isn't suitably forward-compatible, but there seems to be no
      // better way for doing it (KSP 0.23).
      editorLogic.PartSelected = aPart;
      if (editorLogic.ship.Contains(aPart)) {
        detachPart(aPart);
      } else { // picking a part that is already detached from vessel
        aPart.unfreeze();
      }
      EditorLogic.SetLayerRecursive(aPart.gameObject, 2, 1 << 21);
      clearSymmetricParts(aPart);
      editorLogic.audio.PlayOneShot(editorLogic.partGrabClip);
      editorLogic.partRotation = aPart.attRotation;
      editorLogic.state = EditorLogic.EditorState.PAD_SELECTED;
    }

    private void detachPart(Part aPart) {
      if (aPart != EditorLogic.startPod) {
        editorLogic.ship.Remove(aPart);
        Part[] componentsInChildren = aPart.GetComponentsInChildren<Part>();
        foreach (Part p in componentsInChildren) editorLogic.ship.Remove(p);
        if (aPart.parent != null) {
          aPart.parent.attachNodes.FindAll(an => an.attachedPart == aPart).ForEach(an => { an.attachedPart = null; });
          aPart.attachNodes.FindAll(an => an.attachedPart == aPart.parent).ForEach(an => { an.attachedPart = null; });
        }
        aPart.setParent(null);
        aPart.onDetach(true);
      }
    }

    private void clearSymmetricParts(Part aPart) {
      aPart.symmetryCounterparts.ForEach(p => {
        if (editorLogic.ship.Contains(p)) {
          detachPart(p);
          EditorLogic.DeletePart(p);
        } else if (p != null) {
          Destroy(p.gameObject);
        }
      });
      aPart.symmetryCounterparts = new List<Part>();
      foreach (Part child in aPart.GetComponentsInChildren<Part>()) {
        child.symmetryCounterparts.RemoveAll(scp => scp == null);
      }
    }

    private class ByDistanceComparer : IComparer<RaycastHit> {
      public static ByDistanceComparer instance = new ByDistanceComparer();
      public int Compare(RaycastHit x, RaycastHit y) {
        return x.distance.CompareTo(y.distance);
      }
    }

  }

}
