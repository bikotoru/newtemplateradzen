// Posicionamiento de menús para CommandBar
function positionOverflowMenu(buttonElement, menuElement, isRightAligned = false) {
    console.log('positionOverflowMenu called', { buttonElement, menuElement, isRightAligned });
    
    if (!buttonElement || !menuElement) {
        console.log('Missing elements:', { buttonElement, menuElement });
        return;
    }
    
    // Asegurar que el menú esté visible
    menuElement.style.display = 'block';
    menuElement.style.visibility = 'visible';
    
    const buttonRect = buttonElement.getBoundingClientRect();
    const menuRect = menuElement.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    
    console.log('Positioning calculation:', { buttonRect, menuRect, viewportWidth, viewportHeight });
    
    let left, top;
    
    if (isRightAligned) {
        // Para FarItems (lado derecho), alinear a la derecha del botón
        left = buttonRect.right - menuRect.width;
        // Si se sale por la izquierda, alinear a la izquierda del botón
        if (left < 0) {
            left = buttonRect.left;
        }
    } else {
        // Para items principales, centrar el menú respecto al botón
        left = buttonRect.left + (buttonRect.width - menuRect.width) / 2;
        // Si se sale por la derecha, alinear a la derecha del botón
        if (left + menuRect.width > viewportWidth) {
            left = buttonRect.right - menuRect.width;
        }
        // Si se sale por la izquierda, alinear a la izquierda del botón
        if (left < 0) {
            left = buttonRect.left;
        }
    }
    
    // Posición vertical: directamente debajo del botón sin espacio extra
    top = buttonRect.bottom + 2;
    
    // Si se sale por abajo de la ventana, mostrar arriba del botón
    if (top + menuRect.height > viewportHeight) {
        top = buttonRect.top - menuRect.height - 4;
    }
    
    // Asegurar que no se salga de la ventana
    left = Math.max(8, Math.min(left, viewportWidth - menuRect.width - 8));
    top = Math.max(8, top);
    
    console.log('Final position:', { left, top });
    
    menuElement.style.left = left + 'px';
    menuElement.style.top = top + 'px';
}

// Sistema de gestión de instancias de menú
let currentMenuInstance = null;

function registerMenuInstance(dotnetRef) {
    currentMenuInstance = dotnetRef;
}

function unregisterMenuInstance() {
    currentMenuInstance = null;
}

function closeAllOverflowMenus() {
    if (currentMenuInstance) {
        currentMenuInstance.invokeMethodAsync('CloseMenus');
    }
}

// Posicionar submenús
function positionSubMenu(submenuElement) {
    console.log('positionSubMenu called', submenuElement);
    
    if (!submenuElement) return;
    
    // Obtener el elemento padre (el botón del menú)
    const parentContainer = submenuElement.closest('.ms-CommandBar-submenuItem');
    if (!parentContainer) return;
    
    const parentRect = parentContainer.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const submenuWidth = 180; // Ancho mínimo del submenú
    
    // Calcular si hay espacio a la derecha
    const spaceOnRight = viewportWidth - parentRect.right;
    const needsLeftPosition = spaceOnRight < submenuWidth + 20; // 20px de margen
    
    if (needsLeftPosition) {
        submenuElement.style.left = '-100%';
        submenuElement.style.right = 'auto';
        console.log('Positioned submenu to the left - insufficient space on right');
    } else {
        submenuElement.style.left = '100%';
        submenuElement.style.right = 'auto';
        console.log('Positioned submenu to the right - sufficient space');
    }
}

// Funciones globales
window.positionOverflowMenu = positionOverflowMenu;
window.positionSubMenu = positionSubMenu;
window.registerMenuInstance = registerMenuInstance;
window.unregisterMenuInstance = unregisterMenuInstance;
window.closeAllOverflowMenus = closeAllOverflowMenus;