using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Frontend.Services
{
    /// <summary>
    /// Servicio para descargar archivos usando JavaScript Interop en Blazor
    /// </summary>
    public class FileDownloadService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<FileDownloadService> _logger;

        public FileDownloadService(IJSRuntime jsRuntime, ILogger<FileDownloadService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        /// <summary>
        /// Descarga un archivo Excel usando JavaScript
        /// </summary>
        public async Task DownloadExcelAsync(byte[] fileBytes, string fileName)
        {
            await DownloadFileAsync(fileBytes, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        /// <summary>
        /// Descarga un archivo PDF usando JavaScript  
        /// </summary>
        public async Task DownloadPdfAsync(byte[] fileBytes, string fileName)
        {
            await DownloadFileAsync(fileBytes, fileName, "application/pdf");
        }

        /// <summary>
        /// Descarga un archivo CSV usando JavaScript
        /// </summary>
        public async Task DownloadCsvAsync(byte[] fileBytes, string fileName)
        {
            await DownloadFileAsync(fileBytes, fileName, "text/csv");
        }

        /// <summary>
        /// Descarga cualquier archivo usando JavaScript
        /// </summary>
        public async Task DownloadFileAsync(byte[] fileBytes, string fileName, string mimeType)
        {
            try
            {
                _logger.LogInformation($"Starting file download: {fileName}");

                // Convertir a Base64
                var base64 = Convert.ToBase64String(fileBytes);

                // Llamar función JavaScript para descargar
                await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", base64, fileName, mimeType);

                _logger.LogInformation($"File download completed: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {fileName}");
                throw new InvalidOperationException($"Error descargando archivo: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Descarga archivo directamente desde URL
        /// </summary>
        public async Task DownloadFromUrlAsync(string url, string fileName)
        {
            try
            {
                _logger.LogInformation($"Starting download from URL: {url}");
                
                await _jsRuntime.InvokeVoidAsync("downloadFileFromUrl", url, fileName);
                
                _logger.LogInformation($"URL download completed: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading from URL: {url}");
                throw new InvalidOperationException($"Error descargando desde URL: {ex.Message}", ex);
            }
        }
    }
}