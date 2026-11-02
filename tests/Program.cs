using ResetExplode.Modular;
using Microsoft.Win32;

int passed = 0;
void Test(string name, Action action) { action(); Console.WriteLine("PASS " + name); passed++; }
void Assert(bool ok) { if (!ok) throw new Exception("Assertion failed"); }
void Throws(Action action) { try { action(); } catch { return; } throw new Exception("Expected failure"); }
var temp = Path.Combine(Path.GetTempPath(), "PcismoMasterTests-" + Guid.NewGuid());
Directory.CreateDirectory(temp);
var module = ModuleCatalog.All.Single(m => m.Id == "background");
var fake = new FakeStore();
ModuleEngine Engine(string id) => new(fake, Path.Combine(temp, id));
Test("Catalog has no overlapping registry settings", () => {
    var settings = ModuleCatalog.All.SelectMany(m => m.Settings).Select(s => s.Hive + s.Path + s.Name).ToArray();
    Assert(settings.Distinct(StringComparer.OrdinalIgnoreCase).Count() == settings.Length);
});
Test("Enable, restart engine, restore absent and custom values", () => {
    fake.Values[module.Settings[0]] = new(true, 4, "7");
    var before = module.Settings.Select(fake.Read).ToArray();
    var engine = Engine("roundtrip"); engine.Enable(module); Assert(engine.Matches(module));
    Throws(() => engine.Enable(module));
    Engine("roundtrip").Restore(module);
    Assert(module.Settings.Select(fake.Read).SequenceEqual(before)); Assert(!engine.HasBackup(module));
});
Test("Write failure rolls back partial changes", () => {
    fake.FailAt = fake.Writes + 2;
    var before = module.Settings.Select(fake.Read).ToArray(); var engine = Engine("failure");
    Throws(() => engine.Enable(module)); Assert(module.Settings.Select(fake.Read).SequenceEqual(before)); Assert(!engine.HasBackup(module));
});
Test("Recovery failure preserves backup and can be retried", () => {
    fake.AlwaysFail = true; var engine = Engine("retry"); Throws(() => engine.Enable(module)); Assert(engine.HasBackup(module));
    fake.AlwaysFail = false; engine.Restore(module); Assert(!engine.HasBackup(module));
});
Test("External drift is detected and original backup remains usable", () => {
    var engine = Engine("drift"); var before = module.Settings.Select(fake.Read).ToArray(); engine.Enable(module);
    fake.Values[module.Settings[0]] = new(true, 4, "99"); Assert(!engine.Matches(module)); engine.Restore(module);
    Assert(module.Settings.Select(fake.Read).SequenceEqual(before));
});
Test("Corrupted journal fails closed", () => {
    var engine = Engine("corrupt"); engine.Enable(module); var writes = fake.Writes;
    File.WriteAllText(Path.Combine(temp, "corrupt", module.Id + ".json"), "{}");
    Throws(() => engine.Restore(module)); Assert(fake.Writes == writes); Assert(engine.HasBackup(module));
});
Test("Real registry roundtrip only inside isolated test key", () => {
    var path = @"Software\PcismoMaster.Tests\" + Guid.NewGuid();
    var setting = new Setting("HKCU", path, "Sample", 0); var store = new RegistryStore();
    try {
        foreach (var value in new[] { new SavedValue(true, 4, "-1"), new(true, 1, "\"hello\""), new(true, 2, "\"%TEMP%\""), new(true, 7, "[\"a\",\"b\"]"), new(true, 3, "\"AQID\""), new(true, 11, "1234567890123"), new(false, 0, "") }) {
            store.Write(setting, value); Assert(store.Read(setting) == value);
        }
    } finally { Registry.CurrentUser.DeleteSubKeyTree(path, false); }
});
Console.WriteLine($"{passed} tests passed. No production tweaks applied.");
Directory.Delete(temp, true);

sealed class FakeStore : ISettingsStore
{
    public Dictionary<Setting, SavedValue> Values = new(); public int Writes; public int FailAt = -1; public bool AlwaysFail;
    public SavedValue Read(Setting s) => Values.GetValueOrDefault(s, new(false, 0, ""));
    public void Write(Setting s, SavedValue value) { Writes++; if (AlwaysFail || Writes == FailAt) throw new IOException("Injected failure"); Values[s] = value; }
}
