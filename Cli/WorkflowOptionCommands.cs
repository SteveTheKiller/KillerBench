using System.Text.Json;
using KillerBench.Engine;

internal static class WorkflowOptionCommands
{
    internal static int Run(string[] args)
    {
        if (args.Length == 1)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ExecutionPerformed = false,
                Workflows = WorkflowOptionCatalog.All
            }, Capture.JsonOptions));
            return 0;
        }

        if (args.Length >= 2 && WorkflowOptionCatalog.Find(args[1]) is { } options)
        {
            if (args.Length > 2)
            {
                var supplied = new List<KeyValuePair<string, string>>();
                for (int i = 2; i < args.Length; i++)
                {
                    if (args[i] != "--set" || i + 1 >= args.Length)
                        return Usage();
                    string assignment = args[++i];
                    int equals = assignment.IndexOf('=');
                    if (equals <= 0) return Usage();
                    supplied.Add(new(assignment[..equals], assignment[(equals + 1)..]));
                }
                WorkflowOptionValidationResult validation = WorkflowOptionValidation.Validate(options.WorkflowKey, supplied);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    ExecutionPerformed = false,
                    Validation = validation,
                    options.ExecutionBoundary
                }, Capture.JsonOptions));
                return validation.IsValid ? 0 : 2;
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ExecutionPerformed = false,
                Workflow = options
            }, Capture.JsonOptions));
            return 0;
        }

        return Usage();
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: workflow-options [WORKFLOW [--set KEY=VALUE ...]] (show or validate host-neutral controls; no scan or repair)");
        Console.Error.WriteLine("Option workflows: " + string.Join("|", WorkflowOptionCatalog.All.Select(item => item.WorkflowKey)));
        return 2;
    }
}
