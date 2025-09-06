// Simple resize handler para CommandBar
let resizeHandlers = new Map();
let handlerId = 0;

function addWindowResizeHandler(dotnetRef, methodName) {
    const id = ++handlerId;
    
    const handler = () => {
        const width = window.innerWidth;
        const height = window.innerHeight;
        dotnetRef.invokeMethodAsync(methodName, width, height);
    };
    
    window.addEventListener('resize', handler);
    resizeHandlers.set(id, handler);
    
    // Llamar inmediatamente para obtener el tamaño inicial
    handler();
    
    return id;
}

function removeWindowResizeHandler(id) {
    const handler = resizeHandlers.get(id);
    if (handler) {
        window.removeEventListener('resize', handler);
        resizeHandlers.delete(id);
    }
}

// Funciones globales
window.addWindowResizeHandler = addWindowResizeHandler;
window.removeWindowResizeHandler = removeWindowResizeHandler;