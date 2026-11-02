using System.Runtime.InteropServices;

namespace ResetExplode;

/// <summary>
/// Núcleos físicos da CPU e as threads (processadores lógicos) de cada um, com a classe de eficiência
/// (em CPUs híbridas Intel, P-cores têm classe maior que E-cores). Usado para a afinidade dos jogos.
/// Considera só o grupo 0 do Windows (até 64 processadores lógicos), como a afinidade de processo.
/// </summary>
public static class CpuTopology
{
    public sealed record Core(ulong Mask, byte EfficiencyClass)
    {
        /// <summary>Primeira thread do núcleo (a "física"; a outra é a de SMT/Hyper-Threading).</summary>
        public ulong FirstThread => Mask & (~Mask + 1);
    }

    private static List<Core>? _cores;

    public static IReadOnlyList<Core> Cores => _cores ??= Read();

    /// <summary>CPU com núcleos de desempenho e de eficiência (Intel 12ª geração em diante).</summary>
    public static bool IsHybrid => Cores.Select(c => c.EfficiencyClass).Distinct().Count() > 1;

    /// <summary>A CPU tem SMT/Hyper-Threading em algum núcleo.</summary>
    public static bool HasSmt => Cores.Any(c => System.Numerics.BitOperations.PopCount(c.Mask) > 1);

    /// <summary>Afinidade do jogo a partir das opções do perfil. Null = deixar o Windows decidir (todas as threads).</summary>
    public static ulong? AffinityFor(GameProfile p)
    {
        if (!p.FreeCore0 && !p.PhysicalOnly && !p.PCoresOnly) return null;
        var cores = Cores.ToList();
        if (cores.Count == 0) return null;
        if (p.PCoresOnly && IsHybrid)
        {
            byte best = cores.Max(c => c.EfficiencyClass);
            cores = cores.Where(c => c.EfficiencyClass == best).ToList();
        }
        if (p.FreeCore0 && cores.Count > 1)
            cores = cores.Where(c => (c.Mask & 1) == 0).ToList(); // o núcleo físico que contém a CPU 0
        ulong mask = 0;
        foreach (var c in cores) mask |= p.PhysicalOnly ? c.FirstThread : c.Mask;
        return mask == 0 ? null : mask;
    }

    /// <summary>Descrição curta para o status: "8 de 16 threads".</summary>
    public static string Describe(ulong mask)
        => $"{System.Numerics.BitOperations.PopCount(mask)} de {Cores.Sum(c => System.Numerics.BitOperations.PopCount(c.Mask))} threads";

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationship, IntPtr buffer, ref uint length);

    private static List<Core> Read()
    {
        var list = new List<Core>();
        try
        {
            uint len = 0;
            GetLogicalProcessorInformationEx(0, IntPtr.Zero, ref len); // 0 = RelationProcessorCore
            if (len == 0) return list;
            IntPtr buf = Marshal.AllocHGlobal((int)len);
            try
            {
                if (!GetLogicalProcessorInformationEx(0, buf, ref len)) return list;
                for (int off = 0; off < len;)
                {
                    IntPtr e = buf + off;
                    int size = Marshal.ReadInt32(e, 4);
                    // PROCESSOR_RELATIONSHIP: Flags(8) EfficiencyClass(9) … GroupCount(30) GroupMask[](32: Mask 8 bytes, Group 2 bytes)
                    byte eff = Marshal.ReadByte(e, 9);
                    ulong mask = (ulong)Marshal.ReadInt64(e, 32);
                    ushort group = (ushort)Marshal.ReadInt16(e, 40);
                    if (group == 0 && mask != 0) list.Add(new Core(mask, eff));
                    if (size <= 0) break;
                    off += size;
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
        catch { }
        return list.OrderBy(c => c.Mask).ToList();
    }
}
