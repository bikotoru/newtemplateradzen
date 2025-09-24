namespace Frontend
{
    public enum BackendType
    {
        GlobalBackend,
        FormBackend,
        NotSet
    }

    public class Variables
    {
        public static string URLBackend = "https://localhost:7124";
        public static string URLFormBackend = "https://localhost:7251";

        public static string GetBackendUrl(BackendType backendType = BackendType.GlobalBackend)
        {
            return backendType switch
            {
                BackendType.FormBackend => URLFormBackend,
                _ => URLBackend
            };
        }
    }
}
