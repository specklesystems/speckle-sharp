using System;
using System.Runtime.InteropServices;

namespace ConnectorRhinoShared;

internal static class MacOSHelpers
{
  private const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

  private static readonly IntPtr NSApplication_class_ptr = objc_getClass("NSApplication");
  private static readonly IntPtr selSharedApplicationHandle = GetHandle("sharedApplication");
  private static readonly IntPtr selSetMainMenu_Handle = GetHandle("setMainMenu:");
  private static readonly IntPtr selMainMenuHandle = GetHandle("mainMenu");
  private static readonly IntPtr selRetain = GetHandle("retain");
  private static readonly IntPtr selRelease = GetHandle("release");
  private static readonly IntPtr selDelegateHandle = GetHandle("delegate");
  private static readonly IntPtr selSetDelegate_Handle = GetHandle("setDelegate:");
  private static readonly IntPtr selItemAtIndex_Handle = GetHandle("itemAtIndex:");
  private static readonly IntPtr selSubmenuHandle = GetHandle("submenu");
  private static readonly IntPtr selTitleHandle = GetHandle("title");
  private static readonly IntPtr selSetTitle_Handle = GetHandle("setTitle:");
  private static readonly IntPtr AllocHandle = GetHandle("alloc");
  private static readonly IntPtr InitHandle = GetHandle("init");

  private static IntPtr SharedApplicationPtr =>
    IntPtr_objc_msgSend(NSApplication_class_ptr, selSharedApplicationHandle);

  public static IntPtr MainMenu
  {
    get
    {
      // get the menu ptr
      var menuPtr = IntPtr_objc_msgSend(SharedApplicationPtr, selMainMenuHandle);
      // retain it so it doesn't go away
      void_objc_msgSend(menuPtr, selRetain);
      return menuPtr;
    }
    set => void_objc_msgSend_IntPtr(SharedApplicationPtr, selSetMainMenu_Handle, value);
  }

  public static IntPtr AppDelegate
  {
    get
    {
      var delegatePtr = IntPtr_objc_msgSend(SharedApplicationPtr, selDelegateHandle);
      void_objc_msgSend(delegatePtr, selRetain);
      return delegatePtr;
    }
    set => void_objc_msgSend_IntPtr(SharedApplicationPtr, selSetDelegate_Handle, value);
  }

  [DllImport(LIBOBJC_DYLIB, EntryPoint = "sel_registerName")]
  public static extern IntPtr GetHandle(string name);

  [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
  public static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);

  [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
  public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

  [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
  public static extern void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

  [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
  public static extern IntPtr IntPtr_objc_msgSend_Int64(IntPtr receiver, IntPtr selector, long arg1);

  [DllImport(LIBOBJC_DYLIB)]
  internal static extern IntPtr objc_getClass(string name);

  public static IntPtr NewObject(string className)
  {
    var ptr = IntPtr_objc_msgSend(objc_getClass(className), AllocHandle);
    ptr = IntPtr_objc_msgSend(ptr, InitHandle);
    return ptr;
  }

  public static IntPtr MenuItemAt(IntPtr menu, int index)
  {
    return IntPtr_objc_msgSend_Int64(menu, selItemAtIndex_Handle, index);
  }

  public static IntPtr MenuItemGetSubmenu(IntPtr menuItem)
  {
    return IntPtr_objc_msgSend(menuItem, selSubmenuHandle);
  }

  public static IntPtr MenuItemGetTitle(IntPtr menuItem)
  {
    return IntPtr_objc_msgSend(menuItem, selTitleHandle);
  }

  public static void MenuItemSetTitle(IntPtr menuItem, IntPtr title)
  {
    void_objc_msgSend_IntPtr(menuItem, selSetTitle_Handle, title);
  }
}
