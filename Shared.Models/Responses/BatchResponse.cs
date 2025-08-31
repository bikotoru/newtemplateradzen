namespace Shared.Models.Responses
{
    public class BatchResponse<T>
    {
        public List<T> SuccessfulItems { get; set; } = new();
        public List<BatchError> FailedItems { get; set; } = new();
        public int TotalProcessed { get; set; }
        public int SuccessCount => SuccessfulItems.Count;
        public int FailedCount => FailedItems.Count;
        public bool HasErrors => FailedItems.Any();
        public bool AllSuccessful => !HasErrors;

        public static BatchResponse<T> Create(List<T> successfulItems, List<BatchError> failedItems)
        {
            return new BatchResponse<T>
            {
                SuccessfulItems = successfulItems,
                FailedItems = failedItems,
                TotalProcessed = successfulItems.Count + failedItems.Count
            };
        }
    }

    public class BatchError
    {
        public int Index { get; set; }
        public string Error { get; set; } = string.Empty;
        public object? Item { get; set; }
        public List<string>? ValidationErrors { get; set; }
    }
}