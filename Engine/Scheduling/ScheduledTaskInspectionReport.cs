namespace KillerBench.Engine;

public sealed record ScheduledTaskInspectionReport(DateTimeOffset InspectedUtc, ScheduledTaskPlan Plan,
    ScheduledTaskExecutableResult? Executable, ScheduledTaskAccountResult? Account,
    ScheduledTaskConflictResult? Conflict, ToolResult? InventoryEvidence)
{
    public bool Incomplete { get; init; }
    public string? Failure { get; init; }
    public FileSignatureResult? Signature { get; init; }
    public ScheduledTaskDefinitionResult? ExistingDefinition { get; init; }
    public int SchemaVersion => 1;
    public bool TaskRegistered => false;
    public string Limitation => "Read-only inspection snapshot. Registration eligibility, executable trust and logon permissions are not verified.";
    public string Format() => ScheduledTaskReview.Format(Plan, InspectedUtc) +
        (Incomplete ? "\n\nINSPECTION INCOMPLETE\n" + Failure : "") +
        "\n\nEXECUTABLE FILE\n" + (Executable is { } file ? file.ImageReview + "\n" + file.Detail +
            "\nBytes: " + file.Bytes + "\nSHA-256: " + (file.Sha256 ?? "Unavailable") : "Not collected.") +
        "\n\nSIGNATURE\n" + (Signature is { } signature ? "Status: " + (signature.Status ?? "Unavailable") +
            "\nSigner: " + (signature.Signer ?? "Not reported") + "\n" + signature.Detail + "\n" + signature.Limitation : "Not collected.") +
        "\n\nACCOUNT IDENTITY\n" + (Account is { } account ? "SID: " + (account.Sid ?? "Unresolved") + "\n" + account.Detail : "Not collected.") +
        "\n\nCONFLICT REVIEW\n" + (Conflict is { } conflict ? conflict.State + "\n" + conflict.Detail : "Not collected. Absence of a conflict is not established.") +
        (ExistingDefinition is { } definition ? "\n\nEXISTING DEFINITION\n" + definition.Detail + "\n" + definition.Limitation +
            (definition.Observed ? "\nStored XML is included in the exported inspection report." : "\nNo stored XML was obtained.") : "") +
        "\n\n" + Limitation;
}
