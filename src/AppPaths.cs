using System;
using System.IO;
using System.Security.Principal;
static class AppPaths {
 internal static string Data {get {var p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Stackify");Directory.CreateDirectory(p);return p;}}
 internal static string EventName(string action){return "Local\\Stackify."+WindowsIdentity.GetCurrent().User.Value+"."+action;}
 internal static string Executable(string value){
  if(String.IsNullOrWhiteSpace(value)||value.Length>260||value.Length<4||!Char.IsLetter(value[0])||value[1]!=':'||value[2]!='\\'||value.Substring(2).Contains(":")||value.IndexOfAny(new[]{'\r','\n','\0','"'})>=0||!String.Equals(Path.GetExtension(value),".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose a local application .exe using its full drive path.");
  string full=Path.GetFullPath(value);
  if(new DriveInfo(Path.GetPathRoot(full)).DriveType==DriveType.Network)throw new ArgumentException("Network application paths are not supported.");
  return full;
 }
 internal static bool ValidId(string id){Guid g;return id!=null&&id.StartsWith("Stackify.Group.",StringComparison.Ordinal)&&Guid.TryParseExact(id.Substring("Stackify.Group.".Length),"N",out g);}
}

