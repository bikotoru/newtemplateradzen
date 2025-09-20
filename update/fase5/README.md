# 📊 Fase 5: Analytics y Reportes

## 🎯 Objetivos
- Dashboard completo de uso y analytics de custom fields
- Sistema de reportes avanzados con custom fields
- Exportación masiva de datos personalizados
- Business Intelligence y insights automatizados

## ⏱️ Duración Estimada: 2-3 semanas
## 🚨 Prioridad: BAJA

---

## 📊 Dashboard de Analytics

### 1. **Usage Analytics Dashboard**

#### Métricas Principales
```csharp
public class CustomFieldsAnalytics
{
    public class UsageMetrics
    {
        public int TotalCustomFields { get; set; }
        public int ActiveCustomFields { get; set; }
        public int FormsWithCustomFields { get; set; }
        public Dictionary<string, int> FieldTypeDistribution { get; set; }
        public Dictionary<string, int> FieldsByEntity { get; set; }
        public List<TopUsedField> MostUsedFields { get; set; }
        public List<EntityUsageMetric> EntityUsage { get; set; }
    }

    public class PerformanceMetrics
    {
        public double AverageRenderTime { get; set; }
        public double AverageValidationTime { get; set; }
        public int TotalValidationErrors { get; set; }
        public Dictionary<string, double> RenderTimeByFieldType { get; set; }
        public List<SlowQuery> SlowestQueries { get; set; }
    }

    public class UserEngagementMetrics
    {
        public int ActiveDesigners { get; set; }
        public int FormsCreated { get; set; }
        public int FieldsAdded { get; set; }
        public double AverageFormDesignTime { get; set; }
        public Dictionary<DateTime, int> DailyActivity { get; set; }
    }
}
```

#### Dashboard UI Components
```razor
@* Frontend/Components/Analytics/CustomFieldsDashboard.razor *@
<div class="analytics-dashboard">
    <!-- KPI Cards -->
    <RadzenRow Gap="1rem" class="rz-mb-4">
        <RadzenColumn Size="3">
            <RadzenCard class="kpi-card">
                <div class="kpi-content">
                    <div class="kpi-value">@metrics.TotalCustomFields</div>
                    <div class="kpi-label">Total Custom Fields</div>
                    <div class="kpi-trend @GetTrendClass(fieldsTrend)">
                        <RadzenIcon Icon="@GetTrendIcon(fieldsTrend)" />
                        @fieldsTrend.ToString("P1")
                    </div>
                </div>
            </RadzenCard>
        </RadzenColumn>

        <RadzenColumn Size="3">
            <RadzenCard class="kpi-card">
                <div class="kpi-content">
                    <div class="kpi-value">@metrics.FormsWithCustomFields</div>
                    <div class="kpi-label">Active Forms</div>
                    <div class="kpi-trend @GetTrendClass(formsTrend)">
                        <RadzenIcon Icon="@GetTrendIcon(formsTrend)" />
                        @formsTrend.ToString("P1")
                    </div>
                </div>
            </RadzenCard>
        </RadzenColumn>

        <RadzenColumn Size="3">
            <RadzenCard class="kpi-card">
                <div class="kpi-content">
                    <div class="kpi-value">@performance.AverageRenderTime.ToString("F2")ms</div>
                    <div class="kpi-label">Avg Render Time</div>
                    <div class="kpi-trend @GetTrendClass(performanceTrend)">
                        <RadzenIcon Icon="@GetTrendIcon(performanceTrend)" />
                        @performanceTrend.ToString("P1")
                    </div>
                </div>
            </RadzenCard>
        </RadzenColumn>

        <RadzenColumn Size="3">
            <RadzenCard class="kpi-card">
                <div class="kpi-content">
                    <div class="kpi-value">@engagement.ActiveDesigners</div>
                    <div class="kpi-label">Active Designers</div>
                    <div class="kpi-trend @GetTrendClass(usersTrend)">
                        <RadzenIcon Icon="@GetTrendIcon(usersTrend)" />
                        @usersTrend.ToString("P1")
                    </div>
                </div>
            </RadzenCard>
        </RadzenColumn>
    </RadzenRow>

    <!-- Charts Row -->
    <RadzenRow Gap="1rem" class="rz-mb-4">
        <RadzenColumn Size="6">
            <RadzenCard>
                <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Field Type Distribution</RadzenText>
                <RadzenChart>
                    <RadzenDonutSeries Data="@fieldTypeData" CategoryProperty="Type" ValueProperty="Count">
                        <RadzenSeriesDataLabels Visible="true" />
                    </RadzenDonutSeries>
                </RadzenChart>
            </RadzenCard>
        </RadzenColumn>

        <RadzenColumn Size="6">
            <RadzenCard>
                <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Usage Trends</RadzenText>
                <RadzenChart>
                    <RadzenLineSeries Data="@usageTrends" CategoryProperty="Date" ValueProperty="Usage" />
                    <RadzenCategoryAxis>
                        <RadzenAxisTitle Text="Date" />
                    </RadzenCategoryAxis>
                    <RadzenValueAxis>
                        <RadzenAxisTitle Text="Usage Count" />
                    </RadzenValueAxis>
                </RadzenChart>
            </RadzenCard>
        </RadzenColumn>
    </RadzenRow>

    <!-- Detailed Tables -->
    <RadzenRow Gap="1rem">
        <RadzenColumn Size="6">
            <RadzenCard>
                <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Most Used Fields</RadzenText>
                <RadzenDataGrid Data="@metrics.MostUsedFields" TItem="TopUsedField">
                    <Columns>
                        <RadzenDataGridColumn Property="FieldName" Title="Field Name" />
                        <RadzenDataGridColumn Property="EntityName" Title="Entity" />
                        <RadzenDataGridColumn Property="UsageCount" Title="Usage Count" />
                        <RadzenDataGridColumn Property="LastUsed" Title="Last Used" FormatString="{0:dd/MM/yyyy}" />
                    </Columns>
                </RadzenDataGrid>
            </RadzenCard>
        </RadzenColumn>

        <RadzenColumn Size="6">
            <RadzenCard>
                <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Performance Issues</RadzenText>
                <RadzenDataGrid Data="@performance.SlowestQueries" TItem="SlowQuery">
                    <Columns>
                        <RadzenDataGridColumn Property="QueryType" Title="Query Type" />
                        <RadzenDataGridColumn Property="Duration" Title="Duration (ms)" />
                        <RadzenDataGridColumn Property="EntityName" Title="Entity" />
                        <RadzenDataGridColumn Property="Timestamp" Title="When" FormatString="{0:HH:mm:ss}" />
                    </Columns>
                </RadzenDataGrid>
            </RadzenCard>
        </RadzenColumn>
    </RadzenRow>
</div>
```

### 2. **Real-time Monitoring**

#### SignalR for Live Updates
```csharp
public class AnalyticsHub : Hub
{
    public async Task JoinAnalyticsGroup(string organizationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"analytics_{organizationId}");
    }

    public async Task LeaveAnalyticsGroup(string organizationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"analytics_{organizationId}");
    }
}

public class AnalyticsService
{
    private readonly IHubContext<AnalyticsHub> _hubContext;

    public async Task BroadcastMetricUpdate(string organizationId, MetricUpdate update)
    {
        await _hubContext.Clients.Group($"analytics_{organizationId}")
                        .SendAsync("MetricUpdated", update);
    }
}
```

## 📄 Sistema de Reportes

### 1. **Report Builder**

#### Report Definition
```csharp
public class CustomFieldReport
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ReportType Type { get; set; }
    public List<string> Entities { get; set; }
    public List<ReportColumn> Columns { get; set; }
    public List<ReportFilter> Filters { get; set; }
    public List<ReportGrouping> Groupings { get; set; }
    public List<ReportSorting> Sorting { get; set; }
    public ReportFormat DefaultFormat { get; set; }
    public bool IsPublic { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportColumn
{
    public string FieldName { get; set; }
    public string DisplayName { get; set; }
    public ColumnType Type { get; set; }
    public string AggregationType { get; set; } // Sum, Count, Average, etc.
    public string Format { get; set; }
    public bool IsVisible { get; set; }
    public int SortOrder { get; set; }
}

public enum ReportType
{
    DataGrid,     // Tabla de datos
    Chart,        // Gráfico
    Summary,      // Resumen ejecutivo
    CrossTab      // Tabla cruzada
}

public enum ReportFormat
{
    HTML,
    PDF,
    Excel,
    CSV,
    JSON
}
```

#### Visual Report Builder
```razor
@* Frontend/Components/Reports/ReportBuilder.razor *@
<div class="report-builder">
    <RadzenTabs @bind-SelectedIndex="selectedTabIndex">
        <Tabs>
            <!-- Data Source Tab -->
            <RadzenTabsItem Text="📊 Data Source">
                <div class="rz-p-4">
                    <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Select Entities and Fields</RadzenText>

                    <RadzenSplitter Orientation="Orientation.Horizontal">
                        <RadzenSplitterPane Size="30%">
                            <RadzenTree Data="@entityTree" Expand="@OnEntityExpand">
                                <RadzenTreeLevel ItemText="@GetItemText" />
                            </RadzenTree>
                        </RadzenSplitterPane>

                        <RadzenSplitterPane Size="70%">
                            <RadzenDataGrid @ref="fieldsGrid" Data="@selectedFields" TItem="ReportColumn">
                                <Columns>
                                    <RadzenDataGridColumn Property="FieldName" Title="Field" />
                                    <RadzenDataGridColumn Property="DisplayName" Title="Display Name">
                                        <EditTemplate Context="column">
                                            <RadzenTextBox @bind-Value="column.DisplayName" />
                                        </EditTemplate>
                                    </RadzenDataGridColumn>
                                    <RadzenDataGridColumn Property="Type" Title="Type" />
                                    <RadzenDataGridColumn Title="Actions">
                                        <Template Context="column">
                                            <RadzenButton Icon="delete" Size="ButtonSize.Small"
                                                         Click="@(() => RemoveField(column))" />
                                        </Template>
                                    </RadzenDataGridColumn>
                                </Columns>
                            </RadzenDataGrid>
                        </RadzenSplitterPane>
                    </RadzenSplitter>
                </div>
            </RadzenTabsItem>

            <!-- Filters Tab -->
            <RadzenTabsItem Text="🔍 Filters">
                <div class="rz-p-4">
                    <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Add Filters</RadzenText>

                    @foreach (var filter in reportFilters)
                    {
                        <RadzenCard class="rz-mb-3">
                            <RadzenRow Gap="1rem" AlignItems="AlignItems.Center">
                                <RadzenColumn Size="3">
                                    <RadzenDropDown @bind-Value="filter.FieldName"
                                                   Data="@availableFields"
                                                   TextProperty="DisplayName"
                                                   ValueProperty="FieldName" />
                                </RadzenColumn>
                                <RadzenColumn Size="2">
                                    <RadzenDropDown @bind-Value="filter.Operator"
                                                   Data="@filterOperators" />
                                </RadzenColumn>
                                <RadzenColumn Size="3">
                                    @RenderFilterValueInput(filter)
                                </RadzenColumn>
                                <RadzenColumn Size="2">
                                    <RadzenDropDown @bind-Value="filter.LogicalOperator"
                                                   Data="@logicalOperators" />
                                </RadzenColumn>
                                <RadzenColumn Size="2">
                                    <RadzenButton Icon="delete" Size="ButtonSize.Small"
                                                 Click="@(() => RemoveFilter(filter))" />
                                </RadzenColumn>
                            </RadzenRow>
                        </RadzenCard>
                    }

                    <RadzenButton Text="Add Filter" Icon="add"
                                 Click="@AddFilter" />
                </div>
            </RadzenTabsItem>

            <!-- Preview Tab -->
            <RadzenTabsItem Text="👁️ Preview">
                <div class="rz-p-4">
                    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceBetween" class="rz-mb-3">
                        <RadzenText TextStyle="TextStyle.H6">Report Preview</RadzenText>
                        <RadzenButtonGroup>
                            <RadzenButton Text="Refresh" Icon="refresh" Click="@RefreshPreview" />
                            <RadzenButton Text="Export" Icon="download" Click="@ShowExportOptions" />
                        </RadzenButtonGroup>
                    </RadzenStack>

                    @if (reportType == ReportType.DataGrid)
                    {
                        <RadzenDataGrid Data="@previewData" TItem="Dictionary<string, object>">
                            @foreach (var column in selectedFields.Where(f => f.IsVisible))
                            {
                                <RadzenDataGridColumn Property="@column.FieldName" Title="@column.DisplayName" />
                            }
                        </RadzenDataGrid>
                    }
                    else if (reportType == ReportType.Chart)
                    {
                        @RenderChart()
                    }
                </div>
            </RadzenTabsItem>
        </Tabs>
    </RadzenTabs>
</div>
```

### 2. **Scheduled Reports**

#### Report Scheduling
```csharp
public class ScheduledReport
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public string Name { get; set; }
    public ScheduleType Type { get; set; } // Daily, Weekly, Monthly
    public string CronExpression { get; set; }
    public List<string> Recipients { get; set; }
    public ReportFormat Format { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastRun { get; set; }
    public DateTime NextRun { get; set; }
}

public class ReportSchedulerService
{
    public async Task ScheduleReport(ScheduledReport scheduledReport)
    {
        // Usar Hangfire o Quartz.NET para scheduling
        var jobId = BackgroundJob.Schedule<ReportGenerationService>(
            service => service.GenerateAndSendReport(scheduledReport.Id),
            scheduledReport.NextRun);

        // Guardar job ID para poder cancelar después
        scheduledReport.JobId = jobId;
        await SaveScheduledReport(scheduledReport);
    }
}
```

## 📤 Exportación Masiva

### 1. **Bulk Export System**

#### Export Service
```csharp
public class CustomFieldsExportService
{
    public async Task<ExportResult> ExportEntityDataAsync(ExportRequest request)
    {
        var query = BuildExportQuery(request);
        var data = await ExecuteQuery(query);

        return request.Format switch
        {
            ExportFormat.Excel => await ExportToExcel(data, request),
            ExportFormat.CSV => await ExportToCsv(data, request),
            ExportFormat.JSON => await ExportToJson(data, request),
            ExportFormat.XML => await ExportToXml(data, request),
            _ => throw new NotSupportedException($"Format {request.Format} not supported")
        };
    }

    private async Task<ExportResult> ExportToExcel(IEnumerable<dynamic> data, ExportRequest request)
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Data");

        // Headers
        int col = 1;
        foreach (var column in request.Columns)
        {
            worksheet.Cells[1, col].Value = column.DisplayName;
            worksheet.Cells[1, col].Style.Font.Bold = true;
            col++;
        }

        // Data
        int row = 2;
        foreach (var item in data)
        {
            col = 1;
            foreach (var column in request.Columns)
            {
                var value = GetPropertyValue(item, column.FieldName);
                worksheet.Cells[row, col].Value = FormatValue(value, column);
                col++;
            }
            row++;
        }

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();

        return new ExportResult
        {
            FileName = $"{request.EntityName}_export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Data = package.GetAsByteArray()
        };
    }
}
```

### 2. **Data Visualization**

#### Advanced Charts
```razor
@* Frontend/Components/Analytics/AdvancedCharts.razor *@
<RadzenCard class="rz-mb-4">
    <RadzenText TextStyle="TextStyle.H6" class="rz-mb-3">Custom Fields Data Visualization</RadzenText>

    <RadzenTabs>
        <Tabs>
            <RadzenTabsItem Text="📊 Column Chart">
                <RadzenChart>
                    <RadzenColumnSeries Data="@chartData" CategoryProperty="Category" ValueProperty="Value" />
                    <RadzenCategoryAxis>
                        <RadzenAxisTitle Text="@xAxisTitle" />
                    </RadzenCategoryAxis>
                    <RadzenValueAxis>
                        <RadzenAxisTitle Text="@yAxisTitle" />
                    </RadzenValueAxis>
                    <RadzenLegend Visible="false" />
                </RadzenChart>
            </RadzenTabsItem>

            <RadzenTabsItem Text="🥧 Pie Chart">
                <RadzenChart>
                    <RadzenPieSeries Data="@chartData" CategoryProperty="Category" ValueProperty="Value">
                        <RadzenSeriesDataLabels Visible="true" />
                    </RadzenPieSeries>
                </RadzenChart>
            </RadzenTabsItem>

            <RadzenTabsItem Text="📈 Line Chart">
                <RadzenChart>
                    <RadzenLineSeries Data="@timeSeriesData" CategoryProperty="Date" ValueProperty="Value" />
                    <RadzenCategoryAxis>
                        <RadzenAxisTitle Text="Time" />
                    </RadzenCategoryAxis>
                    <RadzenValueAxis>
                        <RadzenAxisTitle Text="Value" />
                    </RadzenValueAxis>
                </RadzenChart>
            </RadzenTabsItem>

            <RadzenTabsItem Text="🗺️ Heatmap">
                <div class="heatmap-container">
                    @RenderHeatmap()
                </div>
            </RadzenTabsItem>
        </Tabs>
    </RadzenTabs>
</RadzenCard>
```

## 🤖 Business Intelligence

### 1. **Automated Insights**

#### Insight Engine
```csharp
public class CustomFieldsInsightEngine
{
    public async Task<List<Insight>> GenerateInsightsAsync(Guid organizationId)
    {
        var insights = new List<Insight>();

        // Detectar campos subutilizados
        var underutilizedFields = await DetectUnderutilizedFields(organizationId);
        if (underutilizedFields.Any())
        {
            insights.Add(new Insight
            {
                Type = InsightType.UnderutilizedFields,
                Title = "Campos Subutilizados Detectados",
                Description = $"Se encontraron {underutilizedFields.Count} campos con menos del 10% de uso",
                Severity = InsightSeverity.Medium,
                ActionItems = new List<string>
                {
                    "Revisar la relevancia de estos campos",
                    "Considerar eliminar campos no utilizados",
                    "Promover el uso de campos importantes"
                },
                Data = underutilizedFields
            });
        }

        // Detectar problemas de performance
        var slowFields = await DetectSlowPerformingFields(organizationId);
        if (slowFields.Any())
        {
            insights.Add(new Insight
            {
                Type = InsightType.PerformanceIssues,
                Title = "Problemas de Rendimiento Detectados",
                Description = $"Se encontraron {slowFields.Count} campos con tiempo de renderizado > 1s",
                Severity = InsightSeverity.High,
                ActionItems = new List<string>
                {
                    "Optimizar consultas de campos de referencia",
                    "Implementar cache para opciones de select",
                    "Revisar validaciones complejas"
                },
                Data = slowFields
            });
        }

        // Detectar patrones de uso
        var usagePatterns = await AnalyzeUsagePatterns(organizationId);
        insights.AddRange(usagePatterns);

        return insights;
    }
}

public class Insight
{
    public InsightType Type { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public InsightSeverity Severity { get; set; }
    public List<string> ActionItems { get; set; }
    public object Data { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
```

### 2. **Predictive Analytics**

#### ML-Based Predictions
```csharp
public class CustomFieldsPredictionService
{
    public async Task<FieldUsagePrediction> PredictFieldUsage(string entityName, List<string> fieldNames)
    {
        // Usar ML.NET para predecir uso de campos
        var model = await LoadPredictionModel();
        var features = ExtractFeatures(entityName, fieldNames);

        var prediction = model.Predict(features);

        return new FieldUsagePrediction
        {
            EntityName = entityName,
            FieldNames = fieldNames,
            PredictedUsageScore = prediction.Score,
            Confidence = prediction.Confidence,
            Recommendations = GenerateRecommendations(prediction)
        };
    }

    public async Task<List<FieldSuggestion>> SuggestOptimalFields(string entityName, string industry)
    {
        // Sugerir campos basados en patrones de la industria
        var industryPatterns = await GetIndustryPatterns(industry);
        var entityContext = await GetEntityContext(entityName);

        return GenerateFieldSuggestions(industryPatterns, entityContext);
    }
}
```

## 🛠️ Plan de Implementación

### Semana 1: Analytics Dashboard
- **Días 1-2**: Métricas básicas y KPIs
- **Días 3-4**: Dashboard UI y charts
- **Día 5**: Real-time updates con SignalR

### Semana 2: Report System
- **Días 1-2**: Report builder y definiciones
- **Días 3-4**: Export system (Excel, CSV, PDF)
- **Día 5**: Scheduled reports

### Semana 3: BI e Insights
- **Días 1-2**: Insight engine y automated analysis
- **Días 3-4**: Predictive analytics y ML integration
- **Día 5**: Testing y optimización

## 📈 Métricas de Éxito

- ✅ Dashboard carga en < 2 segundos
- ✅ Reportes generados en < 30 segundos
- ✅ Exportación de 100k registros en < 5 minutos
- ✅ 10+ insights automatizados disponibles
- ✅ Predictive analytics con 80%+ accuracy
- ✅ Real-time updates latency < 500ms

## 📁 Archivos Nuevos

### Backend
- `CustomFields.API/Controllers/AnalyticsController.cs`
- `CustomFields.API/Controllers/ReportsController.cs`
- `CustomFields.API/Services/IAnalyticsService.cs`
- `CustomFields.API/Services/AnalyticsService.cs`
- `CustomFields.API/Services/IReportService.cs`
- `CustomFields.API/Services/ReportService.cs`
- `CustomFields.API/Services/CustomFieldsExportService.cs`
- `CustomFields.API/Services/CustomFieldsInsightEngine.cs`
- `CustomFields.API/Services/CustomFieldsPredictionService.cs`
- `CustomFields.API/Hubs/AnalyticsHub.cs`

### Frontend
- `Frontend/Components/Analytics/CustomFieldsDashboard.razor`
- `Frontend/Components/Analytics/AdvancedCharts.razor`
- `Frontend/Components/Reports/ReportBuilder.razor`
- `Frontend/Components/Reports/ReportViewer.razor`
- `Frontend/Components/Analytics/InsightsPanel.razor`

### Models
- `CustomFields.API/Models/Analytics/UsageMetrics.cs`
- `CustomFields.API/Models/Reports/CustomFieldReport.cs`
- `CustomFields.API/Models/Analytics/Insight.cs`
- `CustomFields.API/Models/Predictions/FieldUsagePrediction.cs`