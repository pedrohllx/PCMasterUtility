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
Test("Restore without backup fails clearly and writes nothing", () => {
    var engine = Engine("never-created"); var writes = fake.Writes;
    try { engine.Restore(module); throw new Exception("Expected failure"); } catch (InvalidOperationException) { }
    Assert(fake.Writes == writes); Assert(!engine.HasBackup(module));
});
var stockCurve = Enumerable.Range(0, 40).Select(i => (Mv: 700 + i * 10, Mhz: 1000 + i * 50)).ToList();
Test("V/F: core offset shifts every point, point offsets add on top", () => {
    var p = new OcProfile { CoreOffsetMhz = 100, PointOffsetsMhz = { [5] = 30 } };
    var t = VfCurveMath.TargetMhz(stockCurve, p);
    Assert(t[3] == stockCurve[3].Mhz + 100); Assert(t[5] == stockCurve[5].Mhz + 130);
    var d = VfCurveMath.DeltasKhz(stockCurve, p); Assert(d[0] == 0 && d[5] == 130_000);
});
Test("V/F: undervolt flattens the curve at the cap and nothing passes it", () => {
    var p = new OcProfile { UvCapMv = 900, UvCapMhz = 2000 };
    var t = VfCurveMath.TargetMhz(stockCurve, p);
    int cap = VfCurveMath.NearestIndex(stockCurve, 900);
    Assert(stockCurve[cap].Mv == 900); Assert(t.Skip(cap).All(x => x == 2000)); Assert(t.Max() == 2000);
    Assert(t[cap - 1] == Math.Min(stockCurve[cap - 1].Mhz, 2000));
});
Test("V/F: deltas never pass the driver's ±1000 MHz limit", () => {
    var t = VfCurveMath.TargetMhz(stockCurve, new OcProfile { CoreOffsetMhz = 900, PointOffsetsMhz = { [2] = 900 } });
    Assert(t.Select((x, i) => Math.Abs(x - stockCurve[i].Mhz)).Max() <= 1000);
    var uv = VfCurveMath.TargetMhz(stockCurve, new OcProfile { UvCapMv = 700, UvCapMhz = 300 });
    Assert(uv.Select((x, i) => stockCurve[i].Mhz - x).Max() <= 1000);
});
Test("V/F: stock recovery and readability check", () => {
    var live = stockCurve.Select(c => (c.Mv, c.Mhz + 45)).ToList();
    var back = VfCurveMath.SubtractDeltas(live, Enumerable.Repeat(45_000, 40).ToArray());
    Assert(back.SequenceEqual(stockCurve)); Assert(VfCurveMath.Readable(stockCurve));
    Assert(!VfCurveMath.Readable(stockCurve.Select((c, i) => (c.Mv, i == 20 ? 50 : c.Mhz)).ToList()));
});
Test("Fan curve interpolates, clamps at the ends and holds with hysteresis", () => {
    var curve = new List<FanPoint> { new(40, 30), new(80, 90) };
    Assert(FanCurveMath.SpeedAt(curve, 20) == 30); Assert(FanCurveMath.SpeedAt(curve, 60) == 60); Assert(FanCurveMath.SpeedAt(curve, 95) == 90);
    Assert(FanCurveMath.Next(curve, 70, 60, 3) == 75);   // esquentou: sobe na hora
    Assert(FanCurveMath.Next(curve, 59, 60, 3) == 60);   // esfriou 1 °C: segura
    Assert(FanCurveMath.Next(curve, 50, 60, 3) == 50);   // esfriou bem: desce com folga de 3 °C (pede 45, entrega 50)
});
Test("Profiles clone deeply", () => {
    var a = new OcProfile { PointOffsetsMhz = { [1] = 10 } }; var b = a.Clone();
    b.PointOffsetsMhz[1] = 99; b.FanCurve[0] = new(10, 10);
    Assert(a.PointOffsetsMhz[1] == 10); Assert(a.FanCurve[0] != b.FanCurve[0]);
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
