using System.ServiceModel;

namespace TracesNT
{
    public static class ClientUtilities
    {
        public static async Task CloseClient<T>(T client) where T : class
        {
            if (client is IAsyncDisposable disposableClient)
            {
                // IAsyncDisposable closes or aborts the client based on state
                await disposableClient.DisposeAsync();
            }
        }
    }
}
