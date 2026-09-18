using System.Net;
using System.Net.Http;
using System.IO;
using KillerBench.Engine;

internal static class KnowledgeHttpSourceChecks
{
    internal static async Task RunAsync()
    {
        byte[] payload = "{\"fixture\":true}"u8.ToArray();
        byte[] signature = Enumerable.Range(0, 64).Select(value => (byte)value).ToArray();
        var handler = new FixtureHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/errors.json" => Reply(payload),
            "/errors.sig" => Reply(signature),
            _ => new(HttpStatusCode.NotFound)
        });
        using var client = new HttpClient(handler);
        var source = new HttpKnowledgeCandidateSource(client,
            [new("errors", new("https://updates.example/errors.json"), new("https://updates.example/errors.sig"))]);
        var candidate = await source.FetchAsync("errors", CancellationToken.None);
        Require(candidate.Payload.SequenceEqual(payload) && candidate.Signature.SequenceEqual(signature) && handler.Requests == 2,
            "HTTPS payload and detached signature are returned without transformation");

        await Reject(() => source.FetchAsync("missing", CancellationToken.None), "Unconfigured database endpoint accepted");
        using var oversizedClient = new HttpClient(new FixtureHandler(_ =>
        {
            var response = Reply([1]);
            response.Content.Headers.ContentLength = 32_000_001;
            return response;
        }));
        var oversized = new HttpKnowledgeCandidateSource(oversizedClient,
            [new("errors", new("https://updates.example/errors.json"), new("https://updates.example/errors.sig"))]);
        await Reject(() => oversized.FetchAsync("errors", CancellationToken.None), "Oversized database response accepted");
        RejectConstruction(() => new HttpKnowledgeCandidateSource(client,
            [new("errors", new("http://updates.example/errors.json"), new("https://updates.example/errors.sig"))]),
            "Non-HTTPS database endpoint accepted");

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try { await source.FetchAsync("errors", canceled.Token); throw new Exception("Canceled retrieval continued"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded HTTPS knowledge retrieval, endpoint validation, detached signature and cancellation; substituted HTTP only.");
    }

    private static HttpResponseMessage Reply(byte[] bytes) => new(HttpStatusCode.OK)
    {
        Content = new ByteArrayContent(bytes),
        RequestMessage = new(HttpMethod.Get, "https://updates.example/final")
    };

    private sealed class FixtureHandler(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        internal int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests++;
            var response = reply(request);
            response.RequestMessage ??= request;
            return Task.FromResult(response);
        }
    }

    private static async Task Reject(Func<Task> action, string message)
    {
        try { await action(); throw new Exception(message); }
        catch (Exception error) when (error is InvalidDataException or HttpRequestException) { }
    }

    private static void RejectConstruction(Action action, string message)
    {
        try { action(); throw new Exception(message); }
        catch (ArgumentException) { }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
