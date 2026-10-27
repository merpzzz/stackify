using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

sealed class StackContext : ApplicationContext
{
    readonly string folder=AppPaths.Data;readonly GroupStore store;
    readonly Dictionary<IntPtr,Original> originals=new Dictionary<IntPtr,Original>();readonly Dictionary<string,string> icons=new Dictionary<string,string>();
    readonly NotifyIcon tray=new NotifyIcon();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();readonly EventWaitHandle stop,manage;
    readonly ToolStripMenuItem enabled=new ToolStripMenuItem("Enable taskbar stacks");readonly ToolStripMenuItem status=new ToolStripMenuItem("Starting…");
    readonly Preferences preferences;bool closing,managerOpen;string lastStatus="";
    sealed class Original {internal int Pid;internal long Started;internal string Id,Path,Command,Resource,Name,AppliedId;}
    sealed class Target {internal long Started;internal string Path;}
    internal StackContext(EventWaitHandle stopEvent,EventWaitHandle manageEvent,bool showManager)
    {
        preferences=Preferences.Load();Appearance.Dark=preferences.DarkMode;stop=stopEvent;manage=manageEvent;store=new GroupStore(folder);enabled.Checked=!File.Exists(Path.Combine(folder,"paused"));enabled.CheckOnClick=true;
        enabled.CheckedChanged+=delegate {try {if(enabled.Checked)File.Delete(Path.Combine(folder,"paused"));else File.WriteAllText(Path.Combine(folder,"paused"),"");Tick();}catch(Exception ex){Dialogs.Error(null,ex);}};
        var menu=new ContextMenuStrip();status.Enabled=false;menu.Items.Add(status);menu.Items.Add(enabled);menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Manage stacks…",null,delegate {ShowManager();});
        menu.Items.Add("New stack…",null,delegate {try {var name=Dialogs.Name(null,"New stack","");if(name==null)return;var g=store.Create(name);store.Save();Tick();using(var picker=new AppPicker(store,g,Tick))picker.ShowDialog();}catch(Exception ex){Dialogs.Error(null,ex);}});
        menu.Items.Add("Add running apps…",null,delegate {using(var picker=new AppPicker(store,null,Tick))picker.ShowDialog();});
        var dark=new ToolStripMenuItem("Dark mode"){CheckOnClick=true,Checked=preferences.DarkMode};dark.CheckedChanged+=delegate {preferences.DarkMode=dark.Checked;Appearance.Dark=dark.Checked;preferences.Save();Appearance.RefreshAll(menu);};menu.Items.Add(dark);Appearance.RefreshAll(menu);
        var launchStack=new ToolStripMenuItem("Launch a stack");launchStack.DropDownOpening+=delegate {launchStack.DropDownItems.Clear();foreach(var g in store.Data.Groups){var selected=g;launchStack.DropDownItems.Add(g.Name,null,delegate {LaunchStack(selected);});}if(launchStack.DropDownItems.Count==0)launchStack.DropDownItems.Add(new ToolStripMenuItem("No stacks yet"){Enabled=false});};menu.Items.Add(launchStack);
        var closeStack=new ToolStripMenuItem("Close a stack and its apps");closeStack.DropDownOpening+=delegate {closeStack.DropDownItems.Clear();foreach(var g in store.Data.Groups){var selected=g;closeStack.DropDownItems.Add(g.Name,null,delegate {CloseStack(selected);});}if(closeStack.DropDownItems.Count==0)closeStack.DropDownItems.Add(new ToolStripMenuItem("No stacks yet"){Enabled=false});};menu.Items.Add(closeStack);
        menu.Items.Add("Open Stackify folder",null,delegate {Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),"\""+folder+"\""){UseShellExecute=false});});menu.Items.Add(new ToolStripSeparator());var exit=new ToolStripMenuItem("Exit Stackify");exit.DropDownItems.Add("Keep apps open",null,delegate {ExitThread();});exit.DropDownItems.Add("Close all stacked apps and exit",null,delegate {AppClosing.Close(store.Data.Groups.SelectMany(g=>g.Apps));ExitThread();});menu.Items.Add(exit);
        tray.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);tray.Text="Stackify";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate {ShowManager();};
        timer.Interval=1500;timer.Tick+=delegate {if(stop.WaitOne(0)){ExitThread();return;}if(manage.WaitOne(0))ShowManager();Tick();};timer.Start();Tick();if(showManager)manage.Set();
    }
    void LaunchStack(AppGroup group){try{AppLaunching.Launch(group.Apps);Tick();}catch(Exception ex){Dialogs.Error(null,ex);}}
    void CloseStack(AppGroup group){int count=AppClosing.Close(group.Apps);tray.ShowBalloonTip(4000,"Stackify",count>0?"Close requested for "+group.Name+". Save prompts and apps that minimise to the tray may need your attention. Your stack is saved.":"No open windows to close in "+group.Name+". Your stack is saved.",ToolTipIcon.Info);}
    void ShowManager() {if(managerOpen)return;managerOpen=true;try {using(var form=new GroupManager(store,Tick,CloseStack,LaunchStack))form.ShowDialog();}finally{managerOpen=false;}}
    void Report(string text) {status.Text=text;string tip="Stackify: "+text;tray.Text=tip.Substring(0,Math.Min(63,tip.Length));if(lastStatus==text)return;lastStatus=text;try {File.WriteAllText(Path.Combine(folder,"status.txt"),DateTime.Now.ToString("s")+" "+text);}catch { }}
    bool StillSame(IntPtr h,Original saved) {if(!Native.IsWindow(h))return false;uint pid;Native.GetWindowThreadProcessId(h,out pid);if(pid!=saved.Pid)return false;try {using(var p=Process.GetProcessById(saved.Pid))return p.StartTime.ToUniversalTime().Ticks==saved.Started;}catch{return false;}}
    void Restore(IntPtr h,Original saved) {if(!StillSame(h,saved))return;if(Native.GetId(h)==saved.AppliedId){Native.SetProperty(h,2,saved.Command);Native.SetProperty(h,3,saved.Resource);Native.SetProperty(h,4,saved.Name);Native.SetId(h,saved.Id);}}
    void RestoreAll() {foreach(var entry in originals.ToArray())try {Restore(entry.Key,entry.Value);originals.Remove(entry.Key);}catch { }}
    internal void Tick()
    {
        if(closing)return;if(!enabled.Checked){RestoreAll();Report("Paused; apps separated");return;}
        try {
            var owners=new Dictionary<string,AppGroup>(StringComparer.OrdinalIgnoreCase);foreach(var g in store.Data.Groups)foreach(var path in g.Apps)owners[path]=g;
            var names=new HashSet<string>(owners.Keys.Select(Path.GetFileNameWithoutExtension),StringComparer.OrdinalIgnoreCase);var targets=new Dictionary<int,Target>();
            foreach(var p in Process.GetProcesses())using(p)try {if(names.Contains(p.ProcessName)){var path=p.MainModule.FileName;if(owners.ContainsKey(path))targets[p.Id]=new Target {Path=path,Started=p.StartTime.ToUniversalTime().Ticks};}}catch { }
            foreach(var entry in originals.ToArray()) {if(!StillSame(entry.Key,entry.Value))originals.Remove(entry.Key);else if(!owners.ContainsKey(entry.Value.Path)){Restore(entry.Key,entry.Value);originals.Remove(entry.Key);}}
            string error=null;
            Native.EnumWindows(delegate(IntPtr h,IntPtr data) {
                uint pid;Native.GetWindowThreadProcessId(h,out pid);Target target;if(!targets.TryGetValue((int)pid,out target)||!Native.IsWindowVisible(h)||Native.GetWindow(h,4)!=IntPtr.Zero)return true;
                try {if(!originals.ContainsKey(h)){var id=Native.GetId(h);originals.Add(h,new Original {Pid=(int)pid,Started=target.Started,Path=target.Path,Id=id,Command=Native.GetProperty(h,2),Resource=Native.GetProperty(h,3),Name=Native.GetProperty(h,4)});}}catch(Exception ex){error=ex.Message;}return true;
            },IntPtr.Zero);
            foreach(var group in store.Data.Groups) {
                var entries=originals.Where(x=>owners.ContainsKey(x.Value.Path)&&owners[x.Value.Path]==group).ToArray();
                var paths=entries.Where(x=>Native.IsWindowVisible(x.Key)).Select(x=>x.Value.Path).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();if(paths.Count==0)continue;
                string signature=String.Join("\n",paths.ToArray());string icon;if(!icons.TryGetValue(signature,out icon)){icon=CollageFactory.Get(folder,paths);icons.Add(signature,icon);}
                string resource=icon+",0";string command="\""+Application.ExecutablePath+"\" --launch "+group.Id;
                foreach(var entry in entries)try {
                    if(Native.GetId(entry.Key)!=group.Id||Native.GetProperty(entry.Key,3)!=resource||Native.GetProperty(entry.Key,4)!=group.Name||Native.GetProperty(entry.Key,2)!=command){
                        Native.SetProperty(entry.Key,2,command);Native.SetProperty(entry.Key,3,resource);Native.SetProperty(entry.Key,4,group.Name);Native.SetId(entry.Key,group.Id);
                    }entry.Value.AppliedId=group.Id;
                }catch(Exception ex){error=ex.Message;}
            }
            int active=originals.Values.Where(x=>owners.ContainsKey(x.Path)).Select(x=>owners[x.Path].Id).Distinct().Count();Report(error==null?originals.Count+" windows in "+active+" groups":"An app could not be grouped");if(error!=null)File.WriteAllText(Path.Combine(folder,"error.txt"),error);
        }catch(Exception ex){Report("Could not update groups");try{File.WriteAllText(Path.Combine(folder,"error.txt"),ex.ToString());}catch { }}
    }
    protected override void ExitThreadCore() {if(!closing){closing=true;timer.Stop();RestoreAll();Report("Stopped; apps separated");tray.Visible=false;tray.Dispose();timer.Dispose();}base.ExitThreadCore();}
}
static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try {
            if(args.Length==2&&args[0]=="--create-shortcut"){var saved=new GroupStore(AppPaths.Data);var group=saved.Data.Groups.FirstOrDefault(g=>g.Id==args[1]);if(group==null)throw new InvalidOperationException("Stack not found");StackShortcuts.Create(group);return;}
            
            using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,AppPaths.EventName("Stop")))using(var manage=new EventWaitHandle(false,EventResetMode.AutoReset,AppPaths.EventName("Manage"))) {
                if(args.Contains("--stop")){stop.Set();return;}
                if(args.Length>0&&args[0]=="--launch") {
                    var store=new GroupStore(AppPaths.Data);var selected=store.Data.Groups.FirstOrDefault(g=>g.Id==(args.Length>1?args[1]:""));
                    if(selected!=null)try{AppLaunching.Launch(selected.Apps);}catch(Exception ex){Dialogs.Error(null,ex);}
                }
                bool created;using(var mutex=new Mutex(true,AppPaths.EventName("App"),out created)){if(!created){if(args.Length==0||args.Contains("--manage"))manage.Set();return;}stop.Reset();Application.Run(new StackContext(stop,manage,args.Length==0||args.Contains("--manage")));mutex.ReleaseMutex();}
            }
        }catch(Exception ex){File.WriteAllText(Path.Combine(AppPaths.Data,"error.txt"),ex.ToString());
            Dialogs.Error(null,ex);}
    }
}






