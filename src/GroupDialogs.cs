using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

sealed class RunningApp
{
    internal string Path,Name;
    internal static List<RunningApp> Find()
    {
        var result=new Dictionary<string,RunningApp>(StringComparer.OrdinalIgnoreCase);int self=Process.GetCurrentProcess().Id;
        Native.EnumWindows(delegate(IntPtr window,IntPtr data) {
            if(!Native.IsWindowVisible(window)||Native.GetWindow(window,4)!=IntPtr.Zero)return true;
            uint pid;Native.GetWindowThreadProcessId(window,out pid);if(pid==self)return true;
            try {using(var p=Process.GetProcessById((int)pid)) {var path=p.MainModule.FileName;if(!result.ContainsKey(path)){string name=p.MainModule.FileVersionInfo.FileDescription;result.Add(path,new RunningApp {Path=path,Name=String.IsNullOrWhiteSpace(name)?p.ProcessName:name});}}}catch { }return true;
        },IntPtr.Zero);return result.Values.OrderBy(x=>x.Name,StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
static class Dialogs
{
    internal static string Name(IWin32Window owner,string title,string initial)
    {
        using(var form=new Form {Text=title,ClientSize=new Size(420,135),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent,Font=new Font("Segoe UI",10),AutoScaleMode=AutoScaleMode.Dpi}) {
            var label=new Label {Text="Stack name",Left=16,Top=14,AutoSize=true};var input=new TextBox {Text=initial,Left=16,Top=38,Width=385,MaxLength=60};
            var ok=new Button {Text="Save",Left=230,Top=88,Width=80,DialogResult=DialogResult.OK};var cancel=new Button {Text="Cancel",Left=320,Top=88,Width=80,DialogResult=DialogResult.Cancel};
            form.Controls.AddRange(new Control[]{label,input,ok,cancel});form.AcceptButton=ok;form.CancelButton=cancel;Appearance.Watch(form);form.Shown+=delegate {input.Focus();input.SelectAll();};return form.ShowDialog(owner)==DialogResult.OK?input.Text:null;
        }
    }
    internal static void Error(IWin32Window owner,Exception ex) {MessageBox.Show(owner,ex.Message,"Stackify",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    internal static ListView AppList()
    {
        var list=new ListView {View=View.Details,FullRowSelect=true,MultiSelect=true,HideSelection=false,Dock=DockStyle.Fill};
        list.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.A){foreach(ListViewItem item in list.Items)item.Selected=true;e.SuppressKeyPress=true;}};return list;
    }
    internal static void Setup(Form form,string title,int width,int height)
    {
        form.Text=title;form.Font=new Font("Segoe UI",10);form.ClientSize=new Size(width,height);form.MinimumSize=new Size(width-100,height-80);form.StartPosition=FormStartPosition.CenterScreen;form.AutoScaleMode=AutoScaleMode.Dpi;form.Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Appearance.Watch(form);
    }
}
sealed class AppPicker : Form
{
    readonly ComboBox group=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,Width=230};
    readonly ListView list=Dialogs.AppList();
    readonly Button add=new Button {Text="Add selected apps",Width=170,Height=32,Enabled=false};
    readonly Label count=new Label {AutoSize=true,Padding=new Padding(0,7,0,0)};
    readonly GroupStore store;readonly Action changed;
    internal AppPicker(GroupStore store,AppGroup selected,Action changed)
    {
        this.store=store;this.changed=changed;Dialogs.Setup(this,"Add running apps",850,480);
        var top=new FlowLayoutPanel {Dock=DockStyle.Top,Height=48,Padding=new Padding(12,8,8,0)};top.Controls.Add(new Label {Text="Add to stack:",AutoSize=true,Padding=new Padding(0,5,0,0)});top.Controls.Add(group);
        var create=new Button {Text="New stack…",Width=120,Height=30};top.Controls.Add(create);var refresh=new Button {Text="Refresh apps",Width=120,Height=30};top.Controls.Add(refresh);
        var hint=new Label {Dock=DockStyle.Top,Height=48,Padding=new Padding(12,2,12,2),Text="Ctrl-click individual apps · Shift-click a range · Ctrl+A selects all.\nApps already in another stack will move to the selected stack."};
        list.Columns.Add("App",210);list.Columns.Add("Current stack",140);list.Columns.Add("Location",450);
        var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=48,Padding=new Padding(12,6,12,6),FlowDirection=FlowDirection.RightToLeft};var cancel=new Button {Text="Cancel",Width=90,Height=32,DialogResult=DialogResult.Cancel};bottom.Controls.Add(cancel);bottom.Controls.Add(add);bottom.Controls.Add(count);
        Controls.Add(list);Controls.Add(hint);Controls.Add(top);Controls.Add(bottom);CancelButton=cancel;
        foreach(var g in store.Data.Groups)group.Items.Add(g);if(selected!=null)group.SelectedItem=selected;else if(group.Items.Count>0)group.SelectedIndex=0;
        list.SelectedIndexChanged+=delegate {add.Enabled=list.SelectedItems.Count>0&&group.SelectedItem!=null;count.Text=list.SelectedItems.Count+" selected";};group.SelectedIndexChanged+=delegate {add.Enabled=list.SelectedItems.Count>0&&group.SelectedItem!=null;};
        create.Click+=delegate {try {var name=Dialogs.Name(this,"New stack","");if(name==null)return;var g=store.Create(name);store.Save();changed();group.Items.Add(g);group.SelectedItem=g;}catch(Exception ex){Dialogs.Error(this,ex);}};
        refresh.Click+=delegate {RefreshApps();};
        add.Click+=delegate {try {var g=group.SelectedItem as AppGroup;if(g==null)return;store.MoveApps(g,list.SelectedItems.Cast<ListViewItem>().Select(x=>((RunningApp)x.Tag).Path).ToArray());store.Save();changed();DialogResult=DialogResult.OK;Close();}catch(Exception ex){Dialogs.Error(this,ex);}};
        RefreshApps();
    }
    void RefreshApps()
    {
        list.BeginUpdate();list.Items.Clear();foreach(var app in RunningApp.Find()){var owner=store.Owner(app.Path);list.Items.Add(new ListViewItem(new[]{app.Name,owner==null?"—":owner.Name,app.Path}) {Tag=app});}list.EndUpdate();add.Enabled=false;count.Text="0 selected";
    }
}
sealed class GroupManager : Form
{
    readonly GroupStore store;readonly Action changed;
    readonly ListBox groups=new ListBox {Dock=DockStyle.Left,Width=210,IntegralHeight=false};readonly ListView apps=Dialogs.AppList();
    readonly Button add=new Button {Text="Add running apps…",Width=155,Height=32};readonly Button remove=new Button {Text="Remove selected",Width=145,Height=32};
    internal GroupManager(GroupStore store,Action changed,Action<AppGroup> closeStack=null,Action<AppGroup> launchStack=null)
    {
        this.store=store;this.changed=changed;Dialogs.Setup(this,"Stackify — Manage stacks",860,460);
        var top=new FlowLayoutPanel {Dock=DockStyle.Top,Height=48,Padding=new Padding(10,8,0,0)};var create=new Button {Text="New stack…",Width=115,Height=30};var rename=new Button {Text="Rename…",Width=105,Height=30};var delete=new Button {Text="Delete stack",Width=115,Height=30};var close=new Button {Text="Close stack + apps",Width=180,Height=30,Enabled=closeStack!=null};var launch=new Button {Text="Launch stack",Width=130,Height=30,Enabled=launchStack!=null};top.Controls.AddRange(new Control[]{create,rename,delete,launch,close});launch.Click+=delegate {var selected=groups.SelectedItem as AppGroup;if(selected!=null&&launchStack!=null)launchStack(selected);};close.Click+=delegate {var selected=groups.SelectedItem as AppGroup;if(selected!=null&&closeStack!=null)closeStack(selected);};
        var bottom=new FlowLayoutPanel {Dock=DockStyle.Bottom,Height=50,Padding=new Padding(10,8,0,0)};var browse=new Button {Text="Add app files…",Width=145,Height=32};var shortcut=new Button {Text="Desktop shortcut",Width=155,Height=32};bottom.Controls.AddRange(new Control[]{add,browse,remove,shortcut});shortcut.Click+=delegate {var selected=groups.SelectedItem as AppGroup;if(selected==null)return;try{var path=StackShortcuts.Create(selected);MessageBox.Show(this,"Created "+Path.GetFileNameWithoutExtension(path)+" on your desktop. It opens the saved apps in this stack.","Stackify",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){Dialogs.Error(this,ex);}};browse.Click+=delegate {var selected=groups.SelectedItem as AppGroup;if(selected==null)return;using(var dialog=new OpenFileDialog{Title="Add apps to "+selected.Name,Filter="Applications (*.exe)|*.exe",Multiselect=true}){if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{store.MoveApps(selected,dialog.FileNames);store.Save();changed();RefreshApps();}catch(Exception ex){Dialogs.Error(this,ex);}}};
        var note=new Label {Dock=DockStyle.Bottom,Height=48,Padding=new Padding(10,4,0,0),Text="Ctrl-click or Shift-click to select multiple apps.\nLaunch or close all apps in a saved stack. Closing apps keeps the stack for next time."};apps.Columns.Add("App",180);apps.Columns.Add("Location",430);
        Controls.Add(apps);Controls.Add(groups);Controls.Add(note);Controls.Add(bottom);Controls.Add(top);
        groups.SelectedIndexChanged+=delegate {RefreshApps();rename.Enabled=delete.Enabled=groups.SelectedItem!=null;close.Enabled=closeStack!=null&&groups.SelectedItem!=null;launch.Enabled=launchStack!=null&&groups.SelectedItem!=null;browse.Enabled=shortcut.Enabled=groups.SelectedItem!=null;};apps.SelectedIndexChanged+=delegate {remove.Enabled=apps.SelectedItems.Count>0;};
        create.Click+=delegate {try {var name=Dialogs.Name(this,"New stack","");if(name==null)return;var g=store.Create(name);store.Save();changed();RefreshGroups(g);}catch(Exception ex){Dialogs.Error(this,ex);}};
        rename.Click+=delegate {var g=groups.SelectedItem as AppGroup;if(g==null)return;try {var name=Dialogs.Name(this,"Rename stack",g.Name);if(name==null)return;store.Rename(g,name);store.Save();changed();RefreshGroups(g);}catch(Exception ex){Dialogs.Error(this,ex);}};
        delete.Click+=delegate {var g=groups.SelectedItem as AppGroup;if(g==null)return;if(MessageBox.Show(this,"Delete “"+g.Name+"” and restore separate buttons for its apps?","Delete stack",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;try {store.Data.Groups.Remove(g);store.Save();changed();RefreshGroups(null);}catch(Exception ex){Dialogs.Error(this,ex);}};
        add.Click+=delegate {using(var picker=new AppPicker(store,groups.SelectedItem as AppGroup,changed))picker.ShowDialog(this);RefreshGroups(groups.SelectedItem as AppGroup);};
        remove.Click+=delegate {var g=groups.SelectedItem as AppGroup;if(g==null)return;try {var selected=apps.SelectedItems.Cast<ListViewItem>().Select(x=>(string)x.Tag).ToArray();g.Apps.RemoveAll(x=>selected.Contains(x,StringComparer.OrdinalIgnoreCase));store.Save();changed();RefreshApps();}catch(Exception ex){Dialogs.Error(this,ex);}};
        RefreshGroups(null);
    }
    void RefreshGroups(AppGroup selected) {groups.Items.Clear();foreach(var g in store.Data.Groups)groups.Items.Add(g);if(selected!=null&&groups.Items.Contains(selected))groups.SelectedItem=selected;else if(groups.Items.Count>0)groups.SelectedIndex=0;RefreshApps();}
    void RefreshApps() {apps.Items.Clear();var g=groups.SelectedItem as AppGroup;add.Enabled=g!=null;remove.Enabled=false;if(g!=null)foreach(var path in g.Apps.OrderBy(x=>Path.GetFileName(x)))apps.Items.Add(new ListViewItem(new[]{Path.GetFileNameWithoutExtension(path),path}) {Tag=path});}
}



