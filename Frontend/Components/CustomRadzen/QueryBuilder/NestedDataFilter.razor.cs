using Microsoft.AspNetCore.Components;
using Frontend.Components.CustomRadzen.QueryBuilder.Models;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// NestedDataFilter component for handling related entity filtering
    /// </summary>
    /// <typeparam name="TItem">The type of the parent item</typeparam>
    public partial class NestedDataFilter<TItem> : ComponentBase, IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                // Cleanup resources if needed
                disposed = true;
            }
        }
    }
}