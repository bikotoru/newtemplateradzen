using System.Linq.Expressions;
using Shared.Models.Export;
using Shared.Models.QueryModels;

namespace Shared.Models.Builders
{
    /// <summary>
    /// Builder fuertemente tipado para crear ExcelExportRequest
    /// </summary>
    public class ExcelExportBuilder<T> where T : class
    {
        private readonly ExcelExportRequest _request;
        private readonly List<ExcelColumnConfig> _columns;

        public ExcelExportBuilder()
        {
            _request = new ExcelExportRequest();
            _columns = new List<ExcelColumnConfig>();
        }

        #region Query Configuration

        /// <summary>
        /// Establece la query que define qué datos exportar
        /// </summary>
        public ExcelExportBuilder<T> WithQuery(QueryRequest query)
        {
            _request.Query = query;
            return this;
        }

        /// <summary>
        /// Establece filtros usando string dinámico
        /// </summary>
        public ExcelExportBuilder<T> WithFilter(string filter)
        {
            _request.Query.Filter = filter;
            return this;
        }

        /// <summary>
        /// Establece ordenamiento usando string dinámico
        /// </summary>
        public ExcelExportBuilder<T> WithOrderBy(string orderBy)
        {
            _request.Query.OrderBy = orderBy;
            return this;
        }

        /// <summary>
        /// Incluye relaciones en la query
        /// </summary>
        public ExcelExportBuilder<T> WithIncludes(params string[] includes)
        {
            _request.Query.Include = includes;
            return this;
        }

        /// <summary>
        /// Limita el número de registros
        /// </summary>
        public ExcelExportBuilder<T> WithLimit(int take, int skip = 0)
        {
            _request.Query.Take = take;
            _request.Query.Skip = skip;
            return this;
        }

        #endregion

        #region Column Configuration

        /// <summary>
        /// Agrega una columna con detección automática de formato
        /// </summary>
        public ExcelExportBuilder<T> WithColumn<TProp>(
            Expression<Func<T, TProp>> property, 
            string displayName)
        {
            var propertyPath = GetPropertyPath(property);
            var format = DetectFormatFromType<TProp>();
            
            _columns.Add(new ExcelColumnConfig
            {
                PropertyPath = propertyPath,
                DisplayName = displayName,
                Format = format,
                Order = _columns.Count
            });
            
            return this;
        }

        /// <summary>
        /// Agrega una columna con formato específico
        /// </summary>
        public ExcelExportBuilder<T> WithColumn<TProp>(
            Expression<Func<T, TProp>> property, 
            string displayName,
            ExcelFormat format)
        {
            var propertyPath = GetPropertyPath(property);
            
            _columns.Add(new ExcelColumnConfig
            {
                PropertyPath = propertyPath,
                DisplayName = displayName,
                Format = format,
                Order = _columns.Count
            });
            
            return this;
        }

        /// <summary>
        /// Agrega una columna con configuración completa
        /// </summary>
        public ExcelExportBuilder<T> WithColumn<TProp>(
            Expression<Func<T, TProp>> property, 
            string displayName,
            ExcelFormat? format = null,
            double? width = null,
            ExcelAlignment? alignment = null,
            bool bold = false,
            bool wrapText = false,
            string? backgroundColor = null,
            string? textColor = null)
        {
            var propertyPath = GetPropertyPath(property);
            var detectedFormat = format ?? DetectFormatFromType<TProp>();
            
            _columns.Add(new ExcelColumnConfig
            {
                PropertyPath = propertyPath,
                DisplayName = displayName,
                Format = detectedFormat,
                Width = width,
                Alignment = alignment,
                Bold = bold,
                WrapText = wrapText,
                BackgroundColor = backgroundColor,
                TextColor = textColor,
                Order = _columns.Count
            });
            
            return this;
        }

        /// <summary>
        /// Agrega una columna con formato personalizado
        /// </summary>
        public ExcelExportBuilder<T> WithColumnCustomFormat<TProp>(
            Expression<Func<T, TProp>> property, 
            string displayName,
            string customFormat)
        {
            var propertyPath = GetPropertyPath(property);
            
            _columns.Add(new ExcelColumnConfig
            {
                PropertyPath = propertyPath,
                DisplayName = displayName,
                CustomFormat = customFormat,
                Order = _columns.Count
            });
            
            return this;
        }

        /// <summary>
        /// Agrega una columna con transformación de valor personalizada
        /// </summary>
        public ExcelExportBuilder<T> WithColumnTransform<TProp>(
            Expression<Func<T, TProp>> property, 
            string displayName,
            Func<object?, string> transform,
            ExcelFormat? format = null)
        {
            var propertyPath = GetPropertyPath(property);
            
            _columns.Add(new ExcelColumnConfig
            {
                PropertyPath = propertyPath,
                DisplayName = displayName,
                Format = format,
                ValueTransform = transform,
                Order = _columns.Count
            });
            
            return this;
        }

        #endregion

        #region Document Configuration

        /// <summary>
        /// Establece el nombre de la hoja
        /// </summary>
        public ExcelExportBuilder<T> WithSheetName(string sheetName)
        {
            _request.SheetName = sheetName;
            return this;
        }

        /// <summary>
        /// Establece título y subtítulo del documento
        /// </summary>
        public ExcelExportBuilder<T> WithTitle(string title, string? subtitle = null)
        {
            _request.Title = title;
            _request.Subtitle = subtitle;
            return this;
        }

        /// <summary>
        /// Configura opciones de formato
        /// </summary>
        public ExcelExportBuilder<T> WithFormatting(
            bool autoFilter = true,
            bool freezeHeaders = true,
            bool formatAsTable = true,
            bool autoFitColumns = true,
            string? tableStyleName = null)
        {
            _request.AutoFilter = autoFilter;
            _request.FreezeHeaders = freezeHeaders;
            _request.FormatAsTable = formatAsTable;
            _request.AutoFitColumns = autoFitColumns;
            _request.TableStyleName = tableStyleName;
            return this;
        }

        /// <summary>
        /// Limita el número máximo de filas a exportar
        /// </summary>
        public ExcelExportBuilder<T> WithMaxRows(int maxRows)
        {
            _request.MaxRows = maxRows;
            return this;
        }

        /// <summary>
        /// Configura fila de totales
        /// </summary>
        public ExcelExportBuilder<T> WithTotals(Dictionary<string, ExcelTotalFunction> columnTotals)
        {
            _request.IncludeTotalsRow = true;
            _request.ColumnTotals = columnTotals;
            return this;
        }

        /// <summary>
        /// Agrega total a una columna específica
        /// </summary>
        public ExcelExportBuilder<T> WithColumnTotal<TProp>(
            Expression<Func<T, TProp>> property,
            ExcelTotalFunction totalFunction)
        {
            var propertyPath = GetPropertyPath(property);
            _request.ColumnTotals ??= new Dictionary<string, ExcelTotalFunction>();
            _request.ColumnTotals[propertyPath] = totalFunction;
            _request.IncludeTotalsRow = true;
            return this;
        }

        /// <summary>
        /// Establece propiedades del documento
        /// </summary>
        public ExcelExportBuilder<T> WithDocumentProperties(
            string? title = null,
            string? author = null,
            string? company = null,
            string? subject = null,
            string? category = null,
            string? keywords = null)
        {
            _request.DocumentProperties = new ExcelDocumentProperties
            {
                Title = title,
                Author = author,
                Company = company,
                Subject = subject,
                Category = category,
                Keywords = keywords,
                CreatedDate = DateTime.Now
            };
            return this;
        }

        /// <summary>
        /// Agrega metadatos adicionales
        /// </summary>
        public ExcelExportBuilder<T> WithMetadata(string key, object value)
        {
            _request.Metadata ??= new Dictionary<string, object>();
            _request.Metadata[key] = value;
            return this;
        }

        #endregion

        #region Build

        /// <summary>
        /// Construye el ExcelExportRequest final
        /// </summary>
        public ExcelExportRequest Build()
        {
            // Ordenar columnas por Order
            _request.Columns = _columns.OrderBy(c => c.Order).ToList();
            return _request;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Extrae la ruta de la propiedad desde una Expression
        /// </summary>
        private string GetPropertyPath<TProp>(Expression<Func<T, TProp>> expression)
        {
            return expression.Body switch
            {
                MemberExpression member when member.Expression is MemberExpression parent =>
                    $"{GetPropertyPath(parent)}.{member.Member.Name}",
                MemberExpression member when member.Expression is ParameterExpression =>
                    member.Member.Name,
                UnaryExpression { Operand: MemberExpression memberExpr } =>
                    GetPropertyPath(memberExpr),
                _ => throw new ArgumentException($"Expression '{expression}' is not a valid property expression.")
            };
        }

        private string GetPropertyPath(MemberExpression member)
        {
            return member.Expression switch
            {
                MemberExpression parent => $"{GetPropertyPath(parent)}.{member.Member.Name}",
                ParameterExpression => member.Member.Name,
                _ => member.Member.Name
            };
        }

        /// <summary>
        /// Detecta automáticamente el formato basado en el tipo de la propiedad
        /// </summary>
        private ExcelFormat? DetectFormatFromType<TProp>()
        {
            var type = typeof(TProp);
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType switch
            {
                Type t when t == typeof(DateTime) => ExcelFormat.DateTime,
                Type t when t == typeof(DateOnly) => ExcelFormat.Date,
                Type t when t == typeof(TimeOnly) => ExcelFormat.TimeOnly,
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => ExcelFormat.Decimal2,
                Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) => ExcelFormat.Integer,
                Type t when t == typeof(bool) => ExcelFormat.YesNo,
                Type t when t == typeof(Guid) => ExcelFormat.Guid,
                Type t when t == typeof(string) && IsEmailProperty(type.Name) => ExcelFormat.Email,
                Type t when t == typeof(string) && IsPhoneProperty(type.Name) => ExcelFormat.Phone,
                Type t when t == typeof(string) && IsUrlProperty(type.Name) => ExcelFormat.Url,
                Type t when t == typeof(string) => ExcelFormat.Text,
                _ => null
            };
        }

        private bool IsEmailProperty(string propertyName)
        {
            var lowerName = propertyName.ToLowerInvariant();
            return lowerName.Contains("email") || lowerName.Contains("correo") || lowerName.Contains("mail");
        }

        private bool IsPhoneProperty(string propertyName)
        {
            var lowerName = propertyName.ToLowerInvariant();
            return lowerName.Contains("phone") || lowerName.Contains("telefono") || lowerName.Contains("celular") || lowerName.Contains("movil");
        }

        private bool IsUrlProperty(string propertyName)
        {
            var lowerName = propertyName.ToLowerInvariant();
            return lowerName.Contains("url") || lowerName.Contains("link") || lowerName.Contains("website") || lowerName.Contains("sitio");
        }

        #endregion
    }
}