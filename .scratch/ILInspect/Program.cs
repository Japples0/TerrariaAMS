using System;
using System.Linq;
using System.Reflection;

string root=@"C:\Program Files (x86)\Steam\steamapps\common\tModLoader";
var dlls=System.IO.Directory.GetFiles(root,"*.dll",System.IO.SearchOption.AllDirectories)
 .GroupBy(System.IO.Path.GetFileName,StringComparer.OrdinalIgnoreCase)
 .ToDictionary(g=>g.Key,g=>g.OrderBy(p=>p.IndexOf("\\ref\\",StringComparison.OrdinalIgnoreCase)>=0?1:0).First(),StringComparer.OrdinalIgnoreCase);
AppDomain.CurrentDomain.AssemblyResolve += (_,a)=>{var n=new AssemblyName(a.Name).Name+".dll"; return dlls.TryGetValue(n,out var p)?Assembly.LoadFrom(p):null;};
var asm=Assembly.LoadFrom(System.IO.Path.Combine(root,"tModLoader.dll"));
var t=asm.GetType("Terraria.UI.ItemSlot",true)!;
foreach(var m in t.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).Where(m=>m.Name.Contains("Radial")))
{
 var pars=string.Join(", ",m.GetParameters().Select(p=>$"{p.ParameterType.Name} {p.Name}"));
 Console.WriteLine($"{m.ReturnType.Name} {m.Name}({pars}) static={m.IsStatic}");
}
