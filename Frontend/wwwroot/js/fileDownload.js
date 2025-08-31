// Funciones JavaScript para descarga de archivos en Blazor

/**
 * Descarga un archivo desde Base64
 * @param {string} base64Data - Datos del archivo en Base64
 * @param {string} fileName - Nombre del archivo
 * @param {string} mimeType - Tipo MIME del archivo
 */
window.downloadFileFromBase64 = function(base64Data, fileName, mimeType) {
    try {
        console.log(`Starting download: ${fileName} (${mimeType})`);
        
        // Convertir Base64 a bytes
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: mimeType });
        
        // Crear enlace de descarga
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = fileName;
        
        // Agregar al DOM, hacer clic y remover
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Limpiar URL
        URL.revokeObjectURL(link.href);
        
        console.log(`Download completed: ${fileName}`);
    } catch (error) {
        console.error('Error downloading file:', error);
        throw error;
    }
};

/**
 * Descarga un archivo desde una URL
 * @param {string} url - URL del archivo
 * @param {string} fileName - Nombre del archivo
 */
window.downloadFileFromUrl = function(url, fileName) {
    try {
        console.log(`Starting download from URL: ${url}`);
        
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        link.target = '_blank';
        
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        console.log(`Download initiated: ${fileName}`);
    } catch (error) {
        console.error('Error downloading from URL:', error);
        throw error;
    }
};

/**
 * Descarga múltiples archivos (con delay para evitar bloqueo del navegador)
 * @param {Array} files - Array de objetos {base64Data, fileName, mimeType}
 * @param {number} delayMs - Delay entre descargas en millisegundos
 */
window.downloadMultipleFiles = async function(files, delayMs = 1000) {
    try {
        console.log(`Starting multiple file download: ${files.length} files`);
        
        for (let i = 0; i < files.length; i++) {
            const file = files[i];
            console.log(`Downloading file ${i + 1}/${files.length}: ${file.fileName}`);
            
            downloadFileFromBase64(file.base64Data, file.fileName, file.mimeType);
            
            // Delay entre descargas (excepto la última)
            if (i < files.length - 1) {
                await new Promise(resolve => setTimeout(resolve, delayMs));
            }
        }
        
        console.log('Multiple file download completed');
    } catch (error) {
        console.error('Error in multiple file download:', error);
        throw error;
    }
};

/**
 * Abre un archivo en nueva pestaña (útil para PDFs)
 * @param {string} base64Data - Datos del archivo en Base64
 * @param {string} mimeType - Tipo MIME del archivo
 */
window.openFileInNewTab = function(base64Data, mimeType) {
    try {
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: mimeType });
        
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        
        // Limpiar URL después de un delay
        setTimeout(() => {
            URL.revokeObjectURL(url);
        }, 1000);
        
        console.log('File opened in new tab');
    } catch (error) {
        console.error('Error opening file in new tab:', error);
        throw error;
    }
};

/**
 * Muestra un diálogo de guardar archivo (para casos especiales)
 * @param {string} base64Data - Datos del archivo en Base64
 * @param {string} fileName - Nombre sugerido del archivo
 * @param {string} mimeType - Tipo MIME del archivo
 */
window.showSaveDialog = function(base64Data, fileName, mimeType) {
    try {
        if ('showSaveFilePicker' in window) {
            // API moderna de File System Access (Chrome 86+)
            showModernSaveDialog(base64Data, fileName, mimeType);
        } else {
            // Fallback a descarga tradicional
            downloadFileFromBase64(base64Data, fileName, mimeType);
        }
    } catch (error) {
        console.error('Error showing save dialog:', error);
        // Fallback a descarga tradicional
        downloadFileFromBase64(base64Data, fileName, mimeType);
    }
};

/**
 * Diálogo moderno de guardado (solo navegadores compatibles)
 */
async function showModernSaveDialog(base64Data, fileName, mimeType) {
    try {
        const fileHandle = await window.showSaveFilePicker({
            suggestedName: fileName,
            types: [{
                description: 'Files',
                accept: { [mimeType]: [getFileExtension(fileName)] }
            }]
        });
        
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        
        const byteArray = new Uint8Array(byteNumbers);
        
        const writable = await fileHandle.createWritable();
        await writable.write(byteArray);
        await writable.close();
        
        console.log('File saved using modern API');
    } catch (error) {
        if (error.name === 'AbortError') {
            console.log('Save dialog cancelled by user');
        } else {
            throw error;
        }
    }
}

/**
 * Obtiene la extensión de archivo
 */
function getFileExtension(fileName) {
    const lastDot = fileName.lastIndexOf('.');
    return lastDot >= 0 ? fileName.substring(lastDot) : '';
}

/**
 * Utilidad para mostrar notificaciones de descarga
 * @param {string} message - Mensaje a mostrar
 * @param {string} type - Tipo de notificación ('success', 'error', 'info')
 */
window.showDownloadNotification = function(message, type = 'info') {
    // Si existe un sistema de notificaciones, usarlo
    if (window.showNotification) {
        window.showNotification(message, type);
        return;
    }
    
    // Fallback simple
    console.log(`Download notification (${type}): ${message}`);
    
    // Crear notificación simple
    const notification = document.createElement('div');
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 10px 20px;
        background-color: ${type === 'success' ? '#4CAF50' : type === 'error' ? '#f44336' : '#2196F3'};
        color: white;
        border-radius: 4px;
        z-index: 10000;
        font-family: Arial, sans-serif;
        box-shadow: 0 2px 5px rgba(0,0,0,0.2);
    `;
    
    document.body.appendChild(notification);
    
    // Remover después de 3 segundos
    setTimeout(() => {
        document.body.removeChild(notification);
    }, 3000);
};