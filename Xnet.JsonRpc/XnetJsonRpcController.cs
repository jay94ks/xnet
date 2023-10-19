
/// <summary>
/// Json RPC controller base.
/// These controllers will be reused for executing RPC requests.
/// </summary>
public abstract class XnetJsonRpcController
{
    /// <summary>
    /// Initialize a new <see cref="XnetJsonRpcController"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public XnetJsonRpcController()
    {
        if ((Connection = Xnet.Current) is null)
        {
            throw new InvalidOperationException(
                "Json RPC controller can not be created " +
                "without Xnet execution context.");
        }

        // --> register the disposable hook on closing token.
        if (this is IDisposable || this is IAsyncDisposable)
        {
            Connection.Closing.Register(() =>
            {
                if (this is IDisposable Sync)
                    Sync.Dispose();

                else if (this is IAsyncDisposable Async)
                {
                    Async
                        .DisposeAsync().ConfigureAwait(false)
                        .GetAwaiter().GetResult();
                }
            }, false);
        }
    }

    /// <summary>
    /// Connection.
    /// </summary>
    public Xnet Connection { get; }

    /// <summary>
    /// Connection Services.
    /// </summary>
    public IServiceProvider Services => Connection.Services;
}
