using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
static class AppLaunching
{
    internal static int Launch(IEnumerable<string> paths)
    {
        var running=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var p in Process.GetProcesses())using(p)try{running.Add(p.MainModule.FileName);}catch { }
        int started=0;var errors=new List<string>();
        foreach(var path in paths.Select(AppPaths.Executable).Distinct(StringComparer.OrdinalIgnoreCase)){
            if(running.Contains(path))continue;
            if(!File.Exists(path)){errors.Add(Path.GetFileName(path)+": app not found");continue;}
            try{using(var p=Process.Start(new ProcessStartInfo(path){WorkingDirectory=Path.GetDirectoryName(path),UseShellExecute=false})){if(p!=null){started++;running.Add(path);}}}catch(Exception ex){errors.Add(Path.GetFileName(path)+": "+ex.Message);}
        }
        if(errors.Count>0)throw new InvalidOperationException(String.Join(Environment.NewLine,errors.ToArray()));return started;
    }
}
