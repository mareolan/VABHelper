using sk.mareolan.ksp.vabhelper.util;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace sk.mareolan.ksp.vabhelper {

  // NOTE It's advised to open popup by offsetting it by at least -1,-1 from mouse position
  // (so that the popup overlaps the mouse position; otherwise EditorLogic might process the "mouse-up").
  public abstract class UCPopup : MonoBehaviour {
    public event PopupEventHandler onPopupClosed;
    public delegate void PopupEventHandler();

    readonly static Logger LOGGER = Logger.getLogger();
    readonly static int DEFAULT_OFFSET = -3;
    protected PopupData popupData;
    protected int windowId;
    string lockId;
    GUIStyle windowStyle;
    bool isWaitingForMouseUpToClose;

    protected void openPopup(params GUILayoutOption[] aLayoutOpts) {
      // offset current mouse position by a few pixels to work around how EditorLogic works
      Vector3 mp = Input.mousePosition;
      Vector2 mousePoint = new Vector2(mp.x + DEFAULT_OFFSET, Screen.height - mp.y + DEFAULT_OFFSET);
      openPopup(mousePoint, aLayoutOpts);
    }

    protected void openPopup(Vector2 aPoint, params GUILayoutOption[] aLayoutOpts) {
      if (isOpen()) closePopup(true);

      popupData = new PopupData();
      popupData.windowRect = new Rect(aPoint.x, aPoint.y, 10, 10);
      popupData.layoutOptions = aLayoutOpts;
      windowStyle = null;
      windowId = WindowIdManager.getUsableWindowId();
      lockId = typeof(UCPopup).FullName + ".Lock" + windowId;
      isWaitingForMouseUpToClose = false;

      gameObject.SetActive(true);
    }
    public void closePopup(bool aForce = false) {
      bool doClose = aForce;
      if (!doClose) {
        // check how the popup is being closed and close immediately / plan later
        if (Input.GetMouseButton(0)) isWaitingForMouseUpToClose = true; // mouse button is down; will close when mouseUp occurs (is tracked in OnGUI)
        else if (Input.GetMouseButtonUp(0)) { // mouseUp is right now => plan close after current frame (might overlap with tracking in OnGUI but it's handled in doCloseAfterFrame)
          isWaitingForMouseUpToClose = true;
          StartCoroutine("doCloseAfterFrame");
        } else doClose = true; // trying to soft close in some other way (probably Esc key)
      }
      if (doClose) {
        LOGGER.debug("Requesting popup close - will do immediately. Input mouse up: {0}, down: {1}, mouse: {2}", Input.GetMouseButtonUp(0), Input.GetMouseButtonDown(0), Input.GetMouseButton(0));
        gameObject.SetActive(false);
      } else {
        LOGGER.debug("Requesting popup close - will wait for next frame after mouse up. Input mouse up: {0}, down: {1}, mouse: {2}", Input.GetMouseButtonUp(0), Input.GetMouseButtonDown(0), Input.GetMouseButton(0));
      }
    }
    public bool isOpen() {
      return gameObject.activeSelf;
    }

    public static T create<T>() where T : UCPopup {
      GameObject go = new GameObject();
      go.SetActive(false);
      return go.AddComponent<T>();
    }

    protected abstract void drawContent();

    void OnGUI() {
      if (popupData == null) return;

      initStyle();

      // draw the window
      int origDepth = GUI.depth;
      GUI.depth = -100;
      GUIStyle origWinStyle = GUI.skin.window;
      GUI.skin.window = windowStyle;
      popupData.windowRect = GUILayout.Window(windowId, popupData.windowRect, draw, (string)null, popupData.layoutOptions);
      GUI.depth = origDepth;

      // bring to front & focus
      if (!popupData.hasBeenToFront) {
        popupData.hasBeenToFront = true;
        GUI.BringWindowToFront(windowId);
        GUI.FocusWindow(windowId);
      }
    }

    void initStyle() {
      if (windowStyle != null) return;
      RectOffset zeroOffset = new RectOffset(0, 0, 0, 0);

      windowStyle = new GUIStyle();//GUI.skin.window);
      windowStyle.alignment = TextAnchor.UpperLeft;
      windowStyle.border = zeroOffset;
      windowStyle.contentOffset = Vector2.zero;
      windowStyle.margin = zeroOffset;
      windowStyle.overflow = zeroOffset;
      int off = Math.Max(0, -DEFAULT_OFFSET + 1);
      windowStyle.padding = new RectOffset(off, off, off, off);
      windowStyle.normal.background = null;
      windowStyle.hover.background = null;
      windowStyle.active.background = null;
      windowStyle.focused.background = null;
    }

    void draw(int aWindowId) {
      // draw window content
      drawContent();

      //// draw button serving as a click catcher
      //Color origBgColor = GUI.backgroundColor;
      //GUI.backgroundColor = new Color(0, 0, 0, 0.1f);
      //if (popupData.contentRect.HasValue) GUI.Button(popupData.contentRect.Value, "");
      //GUI.backgroundColor = origBgColor;
    }

    void Update() {
      if (popupData == null) return; // shouldn't happen (game object is initially inactive)

      bool isEditor = HighLogic.LoadedSceneIsEditor;
      bool isClick = Input.GetMouseButtonDown(0);
      bool isEscape = Input.GetKeyDown(KeyCode.Escape);
      if (isEditor || isClick || isEscape) {
        Vector2 mousePoint = new Vector2(Input.mousePosition.x, (Screen.height - Input.mousePosition.y));
        bool isOverPopup = popupData.windowRect.Contains(mousePoint);
        //LOGGER.debug("mouse: {0}\ncontentRect: {1}\nstartRect: {2}", Input.mousePosition, (pd.contentRect.HasValue ? pd.contentRect.Value.ToString() : "null"), pd.startRect);

        // editor needs special fix in order for mouse downs/clicks not to be processed
        // in EditorLogic.Update
        if (isEditor) doEditorLock(isOverPopup);

        // auto-close functionality
        // NOTE Be sure to keep this after editor lock manipulation so that the lock gets released.
        if (isEscape || (isClick && !isOverPopup)) closePopup(true);
      }

      if (isWaitingForMouseUpToClose && Input.GetMouseButtonUp(0)) {
        StartCoroutine("doCloseAfterFrame");
      }
    }

    IEnumerator doCloseAfterFrame() {
      yield return new WaitForEndOfFrame();
      if (isWaitingForMouseUpToClose) closePopup(true);
    }

    void OnEnable() {
      LOGGER.debug("OnEnable - opening popup @{0}, winId={1}", (popupData != null ? popupData.windowRect.ToString() : "<invalid state - popupData is null>"), windowId);
    }

    void OnDisable() {
      LOGGER.debug("OnDisable - closing popup, winId={0}", windowId);
      WindowIdManager.releaseWindowId(windowId);
      isWaitingForMouseUpToClose = false;
      doEditorLock(false);
      if (onPopupClosed != null) onPopupClosed();
    }

    void OnDestroy() {
      // NOTE Destroyed Unity GameObject-s should return true when compared with null.
      if (!(gameObject == null)) Destroy(gameObject);
    }

    void doEditorLock(bool aShallBeLocked) {
      if (popupData == null) return;
      if (aShallBeLocked) {
        // compare first because SetControlLock fires some event regardless of whether a change really happened
        if (InputLockManager.GetControlLock(lockId) == ControlTypes.None) {
          InputLockManager.SetControlLock(ControlTypes.EDITOR_PAD_PICK_PLACE | ControlTypes.EDITOR_PAD_PICK_COPY, lockId);
          LOGGER.debug("Switched editor to be locked.");
        }
      } else {
        if (InputLockManager.GetControlLock(lockId) != ControlTypes.None) {
          InputLockManager.RemoveControlLock(lockId);
          LOGGER.debug("Switched editor to be unlocked.");
        }
      }
    }
  }

  public class PopupData {
    public Rect windowRect;
    public GUI.WindowFunction drawFn;
    public bool hasBeenToFront;
    public GUILayoutOption[] layoutOptions;
    public override string ToString() {
      return "{windowRect=" + windowRect + "; hasBeenToFront=" + hasBeenToFront + "; layoutOptions=" + layoutOptions + "}";
    }
  }
}
