// Resize Observer para el CommandBar
const resizeObservers = new Map();
let observerId = 0;

function addResizeObserver(dotnetRef, element, methodName) {
    const id = ++observerId;
    
    const observer = new ResizeObserver(entries => {
        for (const entry of entries) {
            const { width, height } = entry.contentRect;
            dotnetRef.invokeMethodAsync(methodName, Math.floor(width), Math.floor(height));
        }
    });
    
    observer.observe(element);
    resizeObservers.set(id, observer);
    
    return id;
}

function removeResizeObserver(id) {
    const observer = resizeObservers.get(id);
    if (observer) {
        observer.disconnect();
        resizeObservers.delete(id);
    }
}

// Función para medir elementos
function measureElement(element) {
    const rect = element.getBoundingClientRect();
    return {
        width: rect.width,
        height: rect.height,
        left: rect.left,
        top: rect.top
    };
}

// Funciones globales para compatibilidad
window.addResizeObserver = addResizeObserver;
window.removeResizeObserver = removeResizeObserver;
window.measureElement = measureElement;