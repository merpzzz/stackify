using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
public sealed class AppGroup {public string Id{get;set;}public string Name{get;set;}public List<string> Apps{get;set;}public override string ToString(){return Name;}}
public sealed class GroupSettings {public int Version{get;set;}public List<AppGroup> Groups{get;set;}}
sealed class GroupStore {
 readonly string file;internal GroupSettings Data;
 internal GroupStore(string folder){Directory.CreateDirectory(folder);file=Path.Combine(folder,"groups.json");if(File.Exists(file)){if(new FileInfo(file).Length>1048576)throw new InvalidDataException("Stack settings are too large.");Data=new JavaScriptSerializer{MaxJsonLength=1048576,RecursionLimit=12}.Deserialize<GroupSettings>(File.ReadAllText(file));}else Data=new GroupSettings{Version=1,Groups=new List<AppGroup>()};Validate();}
 void Validate(){
  if(Data==null||Data.Version!=1||Data.Groups==null||Data.Groups.Count>100)throw new InvalidDataException("Invalid stack settings. The original file has been kept.");
  var ids=new HashSet<string>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var g in Data.Groups){if(g==null||!AppPaths.ValidId(g.Id)||!ids.Add(g.Id)||String.IsNullOrWhiteSpace(g.Name)||g.Name.Length>60||g.Name.Any(Char.IsControl)||!names.Add(g.Name)||g.Apps==null||g.Apps.Count>100)throw new InvalidDataException("Invalid or duplicate stack.");for(int i=0;i<g.Apps.Count;i++){g.Apps[i]=AppPaths.Executable(g.Apps[i]);if(!paths.Add(g.Apps[i]))throw new InvalidDataException("An app belongs to more than one stack.");}}
 }
 internal void Save(){Validate();string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,new JavaScriptSerializer().Serialize(Data),new UTF8Encoding(false));if(File.Exists(file))File.Replace(temp,file,file+".bak");else File.Move(temp,file);}finally{if(File.Exists(temp))File.Delete(temp);}}
 internal AppGroup Create(string name){if(Data.Groups.Count>=100)throw new ArgumentException("Up to 100 stacks are supported.");name=ValidateName(name,null);var g=new AppGroup{Id="Stackify.Group."+Guid.NewGuid().ToString("N"),Name=name,Apps=new List<string>()};Data.Groups.Add(g);return g;}
 internal void Rename(AppGroup group,string name){group.Name=ValidateName(name,group);}
 string ValidateName(string name,AppGroup ignore){name=(name??"").Trim();if(name.Length==0||name.Length>60||name.Any(Char.IsControl))throw new ArgumentException("Enter a name between 1 and 60 characters without control characters.");if(Data.Groups.Any(g=>g!=ignore&&String.Equals(g.Name,name,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("A stack with that name already exists.");return name;}
 internal AppGroup Owner(string path){return Data.Groups.FirstOrDefault(g=>g.Apps.Contains(path,StringComparer.OrdinalIgnoreCase));}
 internal void MoveApps(AppGroup destination,IEnumerable<string> paths){if(!Data.Groups.Contains(destination))throw new ArgumentException("Unknown stack.");var clean=paths.Select(AppPaths.Executable).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();if(clean.Any(p=>!File.Exists(p)))throw new ArgumentException("Choose an existing application .exe.");if(destination.Apps.Union(clean,StringComparer.OrdinalIgnoreCase).Count()>100)throw new ArgumentException("Up to 100 apps per stack are supported.");foreach(var p in clean){foreach(var g in Data.Groups)g.Apps.RemoveAll(x=>String.Equals(x,p,StringComparison.OrdinalIgnoreCase));destination.Apps.Add(p);}}
}
