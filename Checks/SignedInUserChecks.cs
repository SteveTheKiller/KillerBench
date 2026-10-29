using KillerBench.Engine.Infrastructure;

/// <summary>Per-user cleanups follow the account signed in to the session, not an over-the-shoulder
/// elevated administrator, and refuse rather than guess when that account cannot be established.</summary>
internal static class SignedInUserChecks
{
    private const string Admin = "S-1-5-21-1-2-3-1001", User = "S-1-5-21-1-2-3-1002";
    private const string Own = @"C:\Users\admin\AppData\Local";

    public static void Run()
    {
        static string? Profile(string sid) => sid == User ? @"C:\Users\steve" : null;

        var same = SignedInUser.Resolve(Admin, [Admin], Own, _ => throw new Exception("unread"), _ => throw new Exception("unread"));
        Require(same == new SignedInUser.Identity(Own, false), "The running account's own folder is used when it owns the session shell");
        var noShell = SignedInUser.Resolve(Admin, [], Own, _ => null, _ => null);
        Require(noShell == new SignedInUser.Identity(Own, false), "Without a session shell the running account is the only account");

        var other = SignedInUser.Resolve(Admin, [User, User], Own, _ => null, Profile);
        Require(other == new SignedInUser.Identity(@"C:\Users\steve\AppData\Local", true),
            "An over-the-shoulder elevation resolves the signed-in user's default local folder");
        var redirected = SignedInUser.Resolve(Admin, [User], Own, _ => @"%USERPROFILE%\Local", Profile);
        Require(redirected.LocalApplicationData == @"C:\Users\steve\Local",
            "The signed-in user's shell folder is expanded against that user's own profile");
        var absolute = SignedInUser.Resolve(Admin, [User], Own, _ => @"D:\Data\Steve\Local", Profile);
        Require(absolute.LocalApplicationData == @"D:\Data\Steve\Local", "An absolute shell folder is used as configured");

        Refused(() => SignedInUser.Resolve(Admin, [User, Admin], Own, _ => null, Profile), "Two shell accounts in one session are ambiguous");
        Refused(() => SignedInUser.Resolve(Admin, [User], Own, _ => null, _ => null), "An unregistered profile is refused");
        Refused(() => SignedInUser.Resolve(Admin, [User], Own, _ => @"%HOMEDRIVE%\Local", Profile),
            "A shell folder that needs the other account's environment is refused, never expanded with this one's");
        Refused(() => SignedInUser.Resolve(Admin, [User], Own, _ => @"Local", Profile), "A relative shell folder is refused");
        Console.WriteLine("PASS: per-user cleanups resolve the signed-in account's folder under another account's elevation and refuse ambiguous or unresolvable accounts.");
    }

    private static void Refused(Action action, string message)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception(message);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
