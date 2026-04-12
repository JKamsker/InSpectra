using System.Text;

namespace InSpectra.Gen.Engine.Rendering.Html.Bundle;

internal sealed class ViewerBundleStreamCapture
{
    private readonly object _sync = new();
    private readonly StringBuilder _buffer = new();

    public ViewerBundleStreamCapture(StreamReader reader)
    {
        Completion = CaptureAsync(reader);
    }

    private Task<string> Completion { get; }

    public async Task<string> GetTextAsync(TimeSpan maxWait, CancellationToken cancellationToken)
    {
        if (Completion.IsCompletedSuccessfully)
        {
            return Completion.Result;
        }

        if (maxWait <= TimeSpan.Zero)
        {
            return Snapshot();
        }

        try
        {
            return await Completion.WaitAsync(maxWait, cancellationToken);
        }
        catch (TimeoutException)
        {
            return Snapshot();
        }
    }

    public string GetLatestText()
        => Completion.IsCompletedSuccessfully ? Completion.Result : Snapshot();

    public void ObserveFaults()
    {
        _ = Completion.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task<string> CaptureAsync(StreamReader reader)
    {
        var buffer = new char[4096];

        while (true)
        {
            var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read == 0)
            {
                return Snapshot();
            }

            lock (_sync)
            {
                _buffer.Append(buffer, 0, read);
            }
        }
    }

    private string Snapshot()
    {
        lock (_sync)
        {
            return _buffer.ToString();
        }
    }
}
