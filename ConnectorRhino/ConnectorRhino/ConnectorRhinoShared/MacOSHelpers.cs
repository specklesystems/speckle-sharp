using System;
using System.Runtime.InteropServices;

namespace ConnectorRhinoShared
{
  static class MacOSHelpers
  {
    const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "sel_registerName")]
    public extern static IntPtr GetHandle(string name);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public extern static void void_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public extern static IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public extern static void void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSend")]
    public extern static IntPtr IntPtr_objc_msgSend_Int64(IntPtr receiver, IntPtr selector, Int64 arg1);

    [DllImport(LIBOBJC_DYLIB)]
    internal extern static IntPtr objc_getClass(string name);

    static readonly IntPtr NSApplication_class_ptr = objc_getClass("NSApplication");
    static readonly IntPtr selSharedApplicationHandle = GetHandle("sharedApplication");
    static readonly IntPtr selSetMainMenu_Handle = GetHandle("setMainMenu:");
    static readonly IntPtr selMainMenuHandle = GetHandle("mainMenu");
    static readonly IntPtr selRetain = GetHandle("retain");
    static readonly IntPtr selRelease = GetHandle("release");
    static readonly IntPtr selDelegateHandle = GetHandle("delegate");
    static readonly IntPtr selSetDelegate_Handle = GetHandle("setDelegate:");
    static readonly IntPtr selItemAtIndex_Handle = GetHandle("itemAtIndex:");
    static readonly IntPtr selSubmenuHandle = GetHandle("submenu");
    static readonly IntPtr selTitleHandle = GetHandle("title");
    static readonly IntPtr selSetTitle_Handle = GetHandle("setTitle:");
    static readonly IntPtr AllocHandle = GetHandle("alloc");
    static readonly IntPtr InitHandle = GetHandle("init");

    static IntPtr SharedApplicationPtr => IntPtr_objc_msgSend(NSApplication_class_ptr, selSharedApplicationHandle);

    public static IntPtr NewObject(string className)
    {
      var ptr = IntPtr_objc_msgSend(objc_getClass(className), AllocHandle);
      ptr = IntPtr_objc_msgSend(ptr, InitHandle);
      return ptr;
    }

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
      set
      {
        void_objc_msgSend_IntPtr(SharedApplicationPtr, selSetMainMenu_Handle, value);
      }
    }

    public static IntPtr AppDelegate
    {
      get
      {
        var delegatePtr = IntPtr_objc_msgSend(SharedApplicationPtr, selDelegateHandle);
        void_objc_msgSend(delegatePtr, selRetain);
        return delegatePtr;
      }
      set
      {
        void_objc_msgSend_IntPtr(SharedApplicationPtr, selSetDelegate_Handle, value);
      }
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
}

