using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
static class StackShortcuts
{
    static object Call(object obj,string name,BindingFlags flags,params object[] args){return obj.GetType().InvokeMember(name,flags,null,obj,args);}
    internal static string Create(AppGroup group)
    {
        if(!AppPaths.ValidId(group.Id))throw new ArgumentException("Invalid stack identity.");string name=group.Name;foreach(char c in Path.GetInvalidFileNameChars())name=name.Replace(c,'_');name=name.Trim().TrimEnd('.');if(name.Length==0)name="My stack";if(!name.EndsWith("Stack",StringComparison.OrdinalIgnoreCase))name+=" Stack";
        string desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),path=Path.Combine(desktop,name+".lnk");object shell=null,shortcut=null;string description="Stackify stack: "+group.Id;
        try {shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));for(int i=1;;i++){path=Path.Combine(desktop,name+(i==1?"":" ("+i+")")+".lnk");shortcut=Call(shell,"CreateShortcut",BindingFlags.InvokeMethod,path);if(!File.Exists(path)||String.Equals((string)Call(shortcut,"Description",BindingFlags.GetProperty),description,StringComparison.Ordinal))break;Marshal.FinalReleaseComObject(shortcut);shortcut=null;}
            Call(shortcut,"TargetPath",BindingFlags.SetProperty,Application.ExecutablePath);Call(shortcut,"Arguments",BindingFlags.SetProperty,"--launch "+group.Id);Call(shortcut,"WorkingDirectory",BindingFlags.SetProperty,AppDomain.CurrentDomain.BaseDirectory);Call(shortcut,"Description",BindingFlags.SetProperty,description);string iconFolder=Path.Combine(AppPaths.Data,"ShortcutIcons");Directory.CreateDirectory(iconFolder);string iconPath=Path.Combine(iconFolder,"Stackify.ico");if(!File.Exists(iconPath)){File.WriteAllBytes(iconPath,File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Stackify.ico")));}Call(shortcut,"IconLocation",BindingFlags.SetProperty,iconPath+",0");Call(shortcut,"Save",BindingFlags.InvokeMethod);return path;
        }finally{if(shortcut!=null)Marshal.FinalReleaseComObject(shortcut);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
    }
}


