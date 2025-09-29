using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Components.CustomRadzen.QueryBuilder
{
    /// <summary>
    /// Base class for Custom Radzen components
    /// </summary>
    public class CustomRadzenComponent : ComponentBase, IDisposable
    {
        protected internal bool disposed = false;

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the component.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Gets a reference to the HTML element rendered by the component.
        /// </summary>
        public ElementReference Element { get; protected internal set; }

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the CSS style.
        /// </summary>
        [Parameter]
        public string Style { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this component is visible.
        /// </summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        protected virtual string GetCssClass()
        {
            var cssClass = GetComponentCssClass();

            if (Attributes != null && Attributes.TryGetValue("class", out var @class))
            {
                cssClass = $"{cssClass} {@class}";
            }

            return cssClass;
        }

        /// <summary>
        /// Gets the component specific CSS class.
        /// </summary>
        protected virtual string GetComponentCssClass()
        {
            return "";
        }

        /// <summary>
        /// Gets the component identifier.
        /// </summary>
        protected string GetId()
        {
            return Name ?? GetHashCode().ToString();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public virtual void Dispose()
        {
            disposed = true;
        }
    }
}