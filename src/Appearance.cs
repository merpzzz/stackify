using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public sealed class Preferences
{
    public bool DarkMode {get;set;}
    public bool MusicWidget {get;set;}
    public Preferences(){DarkMode=true;}
    internal static Preferences Load(){var p=Path.Combine(AppPaths.Data,"preferences.json");try{return File.Exists(p)?(new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(p))??new Preferences()):new Preferences();}catch{return new Preferences();}}
    internal void Save(){File.WriteAllText(Path.Combine(AppPaths.Data,"preferences.json"),new JavaScriptSerializer().Serialize(this));}
}
static class Appearance
{
    internal static bool Dark=true;
    internal static Color Background {get{return Dark?Color.FromArgb(24,25,31):Color.FromArgb(246,247,250);}}
    internal static Color MusicBackground {get{return Dark?Color.FromArgb(23,23,23):Background;}}
    internal static Color Surface {get{return Dark?Color.FromArgb(35,37,45):Color.White;}}
    internal static Color Text {get{return Dark?Color.FromArgb(235,237,245):Color.FromArgb(25,27,34);}}
    [DllImport("user32.dll")]static extern bool ShowWindowAsync(IntPtr h,int command);
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr window,string app,string id);
    internal static void Apply(Control root)
    {
        root.BackColor=root is ListView||root is ListBox||root is TextBox||root is ComboBox?Surface:Background;root.ForeColor=Text;
        var form=root as Form;if(form!=null){int value=Dark?1:0;DwmSetWindowAttribute(form.Handle,20,ref value,4);}
        var button=root as Button;if(button!=null){button.FlatStyle=FlatStyle.Flat;button.BackColor=Surface;button.FlatAppearance.BorderColor=Dark?Color.FromArgb(75,77,91):Color.FromArgb(190,195,207);button.FlatAppearance.MouseOverBackColor=Dark?Color.FromArgb(57,58,74):Color.FromArgb(225,229,240);}
        if(root is ListView||root is ListBox||root is ComboBox)SetWindowTheme(root.Handle,Dark?"DarkMode_Explorer":"Explorer",null);
        var list=root as ListView;if(list!=null){list.OwnerDraw=true;list.DrawColumnHeader-=Header;list.DrawColumnHeader+=Header;list.DrawItem-=Item;list.DrawItem+=Item;list.DrawSubItem-=SubItem;list.DrawSubItem+=SubItem;}
        foreach(Control child in root.Controls)Apply(child);root.Invalidate(true);
    }
    static void Header(object sender,DrawListViewColumnHeaderEventArgs e){using(var b=new SolidBrush(Surface))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,((ListView)sender).Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height),Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
    static void Item(object sender,DrawListViewItemEventArgs e){e.DrawDefault=true;}
    static void SubItem(object sender,DrawListViewSubItemEventArgs e){e.DrawDefault=true;}
    internal static void Watch(Form form){form.Shown+=delegate {Apply(form);form.BeginInvoke(new Action(delegate {if(!form.IsDisposed){ShowWindowAsync(form.Handle,9);SetForegroundWindow(form.Handle);}}));};}
    internal static void RefreshAll(ContextMenuStrip menu){menu.Renderer=new ToolStripProfessionalRenderer(new MenuColors());menu.BackColor=Background;menu.ForeColor=Text;foreach(Form form in Application.OpenForms)Apply(form);}
    sealed class MenuColors:ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground {get{return Background;}}
        public override Color ImageMarginGradientBegin {get{return Background;}}
        public override Color ImageMarginGradientMiddle {get{return Background;}}
        public override Color ImageMarginGradientEnd {get{return Background;}}
        public override Color MenuItemSelected {get{return Dark?Color.FromArgb(65,55,90):Color.FromArgb(224,218,248);}}
        public override Color MenuBorder {get{return Dark?Color.FromArgb(70,72,88):Color.FromArgb(180,180,190);}}
    }
}



