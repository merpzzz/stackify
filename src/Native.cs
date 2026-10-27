using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Key { internal Guid Format; internal uint Id; }
    [StructLayout(LayoutKind.Explicit, Size=24)] internal struct Value { [FieldOffset(0)] internal ushort Type; [FieldOffset(8)] internal IntPtr Text; }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface Store { void GetCount(out uint count); void GetAt(uint index, out Key key); void GetValue(ref Key key, out Value value); void SetValue(ref Key key, ref Value value); void Commit(); }
    [DllImport("shell32.dll", PreserveSig=false)] static extern void SHGetPropertyStoreForWindow(IntPtr window, ref Guid iid, out Store store);
    [DllImport("ole32.dll")] static extern int PropVariantClear(ref Value value);
    internal delegate bool EnumCallback(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumCallback callback, IntPtr data);
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern IntPtr GetWindow(IntPtr window, uint command);
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr wParam,IntPtr lParam,uint flags,uint timeout,out IntPtr result);
    internal static IntPtr WindowIcon(IntPtr window,int size) { IntPtr result; if(SendMessageTimeout(window,0x7F,new IntPtr(size),IntPtr.Zero,2,500,out result)==IntPtr.Zero)throw new InvalidOperationException("App is not responding");return result; }
    internal static void SetWindowIcon(IntPtr window,int size,IntPtr icon) { IntPtr result; if(SendMessageTimeout(window,0x80,new IntPtr(size),icon,2,500,out result)==IntPtr.Zero)throw new InvalidOperationException("App is not responding"); }
    static Key AppIdKey() { return new Key { Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), Id=5 }; }
    static Store Open(IntPtr window) { var iid=new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"); Store store; SHGetPropertyStoreForWindow(window,ref iid,out store); return store; }
    internal static string GetId(IntPtr window) { return GetProperty(window,5); }
    internal static string GetProperty(IntPtr window,uint property)
    {
        var store=Open(window); var key=AppIdKey(); key.Id=property; Value value;
        try { store.GetValue(ref key,out value); try { if(value.Type==0)return null; if(value.Type!=31)throw new InvalidOperationException("Unexpected window identity type"); return Marshal.PtrToStringUni(value.Text); } finally { PropVariantClear(ref value); } }
        finally { Marshal.ReleaseComObject(store); }
    }
    internal static void SetId(IntPtr window,string text) { SetProperty(window,5,text); }
    internal static void SetProperty(IntPtr window,uint property,string text)
    {
        var store=Open(window); var key=AppIdKey(); key.Id=property; var value=new Value();
        if(text!=null) { value.Type=31; value.Text=Marshal.StringToCoTaskMemUni(text); }
        try { store.SetValue(ref key,ref value); } finally { PropVariantClear(ref value); Marshal.ReleaseComObject(store); }
    }
}


