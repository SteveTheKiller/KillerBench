using System.IO;
using KillerBench.Engine;

internal static class BitLockerEscrowChecks
{
    private static readonly Guid Recovery = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid Tpm = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");

    public static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "bitlocker-escrow");
        var ok = State(domain: true, entra: false);

        var preview = await BitLockerEscrow.RunAsync("c:", BitLockerEscrowTarget.ActiveDirectory, Recovery, false, root, new Host([ok]));
        Require(preview.Outcome == BitLockerEscrowOutcome.Preview && preview.Volume == "C:" && preview.Before is null, "Escrow preview inspects nothing");

        var notJoined = new Host([State(domain: false, entra: false)]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, notJoined)).Outcome == BitLockerEscrowOutcome.Blocked && notJoined.Escrows == 0,
            "Escrow to Active Directory requires domain membership");
        var notEntra = new Host([ok]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.EntraId, Recovery, true, root, notEntra)).Outcome == BitLockerEscrowOutcome.Blocked && notEntra.Escrows == 0,
            "Escrow to Entra ID requires Entra membership");
        var tpmSelected = new Host([ok]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Tpm, true, root, tpmSelected)).Outcome == BitLockerEscrowOutcome.Blocked && tpmSelected.Escrows == 0,
            "Only a recovery-password protector can be escrowed");
        var decrypted = new Host([ok with { VolumeStatus = "FullyDecrypted" }]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, decrypted)).Outcome == BitLockerEscrowOutcome.Blocked && decrypted.Escrows == 0,
            "A decrypted volume is not escrowed");
        var standard = new Host([ok]) { Administrator = false };
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, standard)).Outcome == BitLockerEscrowOutcome.Blocked && standard.Escrows == 0,
            "Escrow requires administrator access");
        var changed = new Host([ok, ok with { Protectors = [.. ok.Protectors, new BitLockerProtector(Guid.NewGuid(), "RecoveryPassword")] }]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, changed)).Outcome == BitLockerEscrowOutcome.Blocked && changed.Escrows == 0,
            "A protector change before dispatch blocks the request");

        var accepted = new Host([ok, ok, ok]);
        var acceptedResult = await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, accepted);
        Require(acceptedResult.Outcome == BitLockerEscrowOutcome.Accepted && accepted.Escrows == 1 && accepted.LastTarget == BitLockerEscrowTarget.ActiveDirectory &&
            accepted.LastProtector == Recovery && File.Exists(acceptedResult.JournalPath) && acceptedResult.Detail.Contains("not readable", StringComparison.Ordinal),
            "An accepted escrow is journaled and never claimed as proven");
        var entra = State(domain: false, entra: true);
        var entraHost = new Host([entra, entra, entra]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.EntraId, Recovery, true, root, entraHost)).Outcome == BitLockerEscrowOutcome.Accepted &&
            entraHost.LastTarget == BitLockerEscrowTarget.EntraId, "Entra escrow uses the Entra target");
        var failed = new Host([ok, ok, ok]) { Command = new ToolResult(1, "", "The network path was not found.", false) };
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, failed)).Outcome == BitLockerEscrowOutcome.Failed,
            "A rejected escrow with an unchanged protector set is a failure");
        var removed = new Host([ok, ok, ok with { Protectors = [ok.Protectors[1]] }]);
        Require((await BitLockerEscrow.RunAsync("C:", BitLockerEscrowTarget.ActiveDirectory, Recovery, true, root, removed)).Outcome == BitLockerEscrowOutcome.Uncertain,
            "A protector that disappears after the request leaves escrow uncertain");

        const string json = "{\"volume\":\"C:\",\"identity\":\"\\\\\\\\?\\\\Volume{1}\\\\\",\"volumeStatus\":\"FullyEncrypted\",\"domainJoined\":true,\"entraJoined\":false,\"protectors\":[{\"id\":\"{11111111-2222-3333-4444-555555555555}\",\"type\":\"RecoveryPassword\"},{\"id\":\"{66666666-7777-8888-9999-aaaaaaaaaaaa}\",\"type\":\"Tpm\"}]}";
        var parsed = WindowsBitLockerEscrowHost.Interpret(new ToolResult(0, json, "", false), "C:");
        Require(parsed.Complete && parsed.DomainJoined && !parsed.EntraJoined && parsed.Protectors.Count == 2 && parsed.Protectors[0].Type == "RecoveryPassword",
            "Escrow inspection parses exact protector and membership evidence");
        Require(!WindowsBitLockerEscrowHost.Interpret(new ToolResult(0, json.Replace("\"entraJoined\":false,", ""), "", false), "C:").Complete &&
            !WindowsBitLockerEscrowHost.Interpret(new ToolResult(0, json.Replace("true", "\"yes\""), "", false), "C:").Complete &&
            !WindowsBitLockerEscrowHost.Interpret(new ToolResult(0, json.Replace("\"C:\"", "\"D:\""), "", false), "C:").Complete &&
            !WindowsBitLockerEscrowHost.Interpret(new ToolResult(1, json, "denied", false), "C:").Complete,
            "Missing, mistyped, mismatched and failed escrow evidence stays incomplete");
        Console.WriteLine("PASS: BitLocker escrow membership, protector type, administrator, final-eligibility and outcome boundaries; Active Directory and Entra requests never claimed as proven; substituted providers only.");
    }

    private static BitLockerEscrowState State(bool domain, bool entra) => new("C:", @"\\?\Volume{1}\", "FullyEncrypted",
        [new BitLockerProtector(Tpm, "Tpm"), new BitLockerProtector(Recovery, "RecoveryPassword")], domain, entra, true);

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Host(IReadOnlyList<BitLockerEscrowState> states) : IBitLockerEscrowHost
    {
        private int index;
        public bool Administrator { get; init; } = true;
        public bool IsAdministrator => Administrator;
        public ToolResult Command { get; init; } = new(0, "", "", false);
        public int Escrows { get; private set; }
        public BitLockerEscrowTarget? LastTarget { get; private set; }
        public Guid? LastProtector { get; private set; }
        public Task<BitLockerEscrowState> InspectAsync(string volume, CancellationToken token) => Task.FromResult(states[Math.Min(index++, states.Count - 1)]);
        public Task<ToolResult> EscrowAsync(string volume, BitLockerEscrowTarget target, Guid protector, CancellationToken token)
        { Escrows++; LastTarget = target; LastProtector = protector; return Task.FromResult(Command); }
    }
}
