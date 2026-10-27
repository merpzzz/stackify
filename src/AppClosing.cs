using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

static class AppClosing
{
    [DllImport("user32.dll",SetLastError=true)]static extern bool PostMessage(IntPtr h,uint message,IntPtr w,IntPtr l);
    internal static int Close(IEnumerable<string> paths)
    {
        var allowed=new HashSet<string>(paths,StringComparer.OrdinalIgnoreCase);
        var candidates=new Dictionary<int,long>();int self=Process.GetCurrentProcess().Id;
        foreach(var p in Process.GetProcesses())using(p)try {if(p.Id!=self&&allowed.Contains(p.MainModule.FileName))candidates[p.Id]=p.StartTime.ToUniversalTime().Ticks;}catch { }
        int sent=0;
        Native.EnumWindows(delegate(IntPtr h,IntPtr unused){
            if(!Native.IsWindowVisible(h))return true;IntPtr owner=Native.GetWindow(h,4);if(owner!=IntPtr.Zero&&Native.IsWindowVisible(owner))return true;
            uint pid;Native.GetWindowThreadProcessId(h,out pid);long started;if(!candidates.TryGetValue((int)pid,out started))return true;
            try {using(var p=Process.GetProcessById((int)pid)){if(p.StartTime.ToUniversalTime().Ticks!=started||!allowed.Contains(p.MainModule.FileName))return true;}
                if(PostMessage(h,0x10,IntPtr.Zero,IntPtr.Zero))sent++;
            }catch { }return true;
        },IntPtr.Zero);
        return sent;
    }
}

