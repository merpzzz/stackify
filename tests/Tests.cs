using System;using System.IO;using System.Linq;using System.Windows.Forms;
static class Tests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,message);}
 [STAThread]static int Main(){string folder=Path.Combine(Path.GetTempPath(),"stackify-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{
  var store=new GroupStore(folder);Check(store.Data.Groups.Count==0,"Fresh install must contain no personal presets");var g=store.Create("Streaming");Check(AppPaths.ValidId(g.Id),"Generated IDs must be valid");string id=g.Id;store.Rename(g,"Work");Check(g.Id==id,"Renames preserve identity");Reject(()=>store.Create("work"),"Duplicate names rejected");Reject(()=>store.Create("bad\nname"),"Control characters rejected");
  string exe=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"notepad.exe");store.MoveApps(g,new[]{exe,exe});Check(g.Apps.Count==1,"Paths deduplicate");var other=store.Create("Other");store.MoveApps(other,new[]{exe});Check(g.Apps.Count==0&&other.Apps.Count==1,"Moves have a unique owner");
  Reject(()=>store.MoveApps(g,new[]{exe,Path.Combine(folder,"missing.exe")}),"Missing paths rejected before modification");Check(other.Apps.Count==1,"Failed move must be atomic");
  foreach(string value in new[]{"cmd.exe","https://example.com/app.exe","\\\\server\\share\\app.exe","C:\\app.cmd","C:\\app.exe:payload.exe","C:\\app.exe\n","C:\\app.exe\" --evil"})Reject(()=>AppPaths.Executable(value),"Unsafe path accepted");
  Check(!AppPaths.ValidId("Stackify.Group.\" --evil"),"Unsafe identity rejected");
  store.Save();store.Rename(other,"Other renamed");store.Save();Check(File.Exists(Path.Combine(folder,"groups.json.bak")),"Atomic replacement keeps backup");var loaded=new GroupStore(folder);Check(loaded.Data.Groups.Count==2&&loaded.Owner(exe).Name=="Other renamed","Saved settings round trip");
  using(var list=Dialogs.AppList())Check(list.MultiSelect&&list.FullRowSelect&&!list.HideSelection,"Native multi-selection enabled");
  File.WriteAllText(Path.Combine(folder,"groups.json"),"{bad");bool malformed=false;try{new GroupStore(folder);}catch{malformed=true;}Check(malformed,"Corrupt config must fail closed");Check(File.ReadAllText(Path.Combine(folder,"groups.json"))=="{bad","Corrupt config preserved");Console.WriteLine("PASS: empty defaults, stable IDs, names, app moves, launch input validation, atomic persistence, corrupt settings and multi-selection.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}finally{Directory.Delete(folder,true);}}
}

