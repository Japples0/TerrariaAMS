using System;
using System.Linq;
using System.Reflection;

string root = @"C:\Program Files (x86)\Steam\steamapps\common\tModLoader";
var all = System.IO.Directory.GetFiles(root, "*.dll", System.IO.SearchOption.AllDirectories);
var dllMap = all.GroupBy(p => System.IO.Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
    .ToDictionary(g => g.Key, g => g.OrderBy(path => path.IndexOf("\\ref\\", StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0).First(), StringComparer.OrdinalIgnoreCase);
AppDomain.CurrentDomain.AssemblyResolve += (_, args) => {
    string? n = new AssemblyName(args.Name).Name;
    if (n == null) return null;
    string key = n + ".dll";
    return dllMap.TryGetValue(key, out string? p) ? Assembly.LoadFrom(p) : null;
};
var asm = Assembly.LoadFrom(System.IO.Path.Combine(root, "Libraries", "TerrariaHooks", "0.0.0.0", "TerrariaHooks.dll"));
var t = asm.GetType("Terraria.UI.On_ItemSlot", false) ?? asm.GetType("Terraria.On_ItemSlot", false);
Console.WriteLine(t?.FullName ?? "no type");
if (t != null)
{
    foreach (var e in t.GetEvents(BindingFlags.Public|BindingFlags.Static).Where(e => e.Name.Contains("Draw") || e.Name.Contains("GetItemLight") || e.Name.Contains("Handle")))
    {
        Console.WriteLine(e.Name + " :: " + e.EventHandlerType?.Name);
    }
}
