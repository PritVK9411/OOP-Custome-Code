namespace NatuFurniture.Domain;

// Singleton
public sealed class AuditLog
{
    private static readonly Lazy<AuditLog> lazy = new(() => new AuditLog());
    public static AuditLog Instance => lazy.Value;

    private readonly List<string> entries = new();
    private AuditLog() { }

    public IReadOnlyList<string> Entries { get { lock (entries) return entries.ToList(); } }

    public void Log(User? user, string action)
    {
        lock (entries)
            entries.Insert(0, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {(user == null ? "system" : user.Describe())}: {action}");
    }
}
