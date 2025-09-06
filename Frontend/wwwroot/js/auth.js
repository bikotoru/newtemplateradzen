// Sistema de Autenticación
const AuthSystem = {
    API_BASE_URL: 'https://localhost:7124/api',
    
    // Variables de estado
    selectedOrganizationId: null,
    tokenTimer: null,
    allOrganizations: [],

    // Inicialización del sistema
    async init() {
        console.log('🚀 Iniciando sistema de autenticación...');
        
        const authCheckingDiv = document.getElementById('auth-checking');
        const appDiv = document.getElementById('app');
        
        // Mostrar indicador de verificación
        authCheckingDiv.style.display = 'block';
        
        try {
            const isAuthenticated = await this.checkAuthentication();
            
            if (isAuthenticated) {
                // Usuario autenticado, cargar Blazor
                console.log('✅ Usuario autenticado, cargando aplicación...');
                await this.loadApplication();
            } else {
                // Usuario no autenticado, mostrar login
                console.log('❌ Usuario no autenticado, mostrando login');
                this.showLoginForm();
            }
        } catch (error) {
            console.error('❌ Error verificando autenticación:', error);
            this.showLoginForm();
        }
    },

    // Verificar autenticación existente
    async checkAuthentication() {
        try {
            // Verificar si hay token en localStorage (persistente)
            const authToken = localStorage.getItem('authToken');
            
            if (authToken) {
                console.log('✅ Token encontrado en localStorage');
                return true;
            }
            
            console.log('❌ No se encontró token en localStorage');
            return false;
        } catch (error) {
            console.error('❌ Error validando sesión:', error);
            // Limpiar storage en caso de error
            localStorage.removeItem('authToken');
            return false;
        }
    },

    // Cargar la aplicación Blazor
    async loadApplication() {
        console.log('🔄 Cargando scripts de Blazor...');
        
        try {
            // Cargar Blazor WebAssembly dinámicamente
            await this.loadBlazorScripts();
            
            const authCheckingDiv = document.getElementById('auth-checking');
            const appDiv = document.getElementById('app');
            
            // Ocultar verificación y mostrar app
            authCheckingDiv.style.display = 'none';
            appDiv.style.display = 'block';
            
            console.log('✅ Aplicación Blazor cargada correctamente');
            console.log('🔄 AuthService se inicializará automáticamente cuando Blazor acceda a él');

        } catch (error) {
            console.error('❌ Error cargando aplicación Blazor:', error);
            this.showMessage('Error cargando la aplicación. Por favor recarga la página.', 'error');
        }
    },

    // Cargar scripts de Blazor dinámicamente
    async loadBlazorScripts() {
        return new Promise((resolve, reject) => {
            console.log('📦 Cargando Blazor WebAssembly...');
            
            // Crear script de Blazor
            const blazorScript = document.createElement('script');
            blazorScript.src = '_framework/blazor.webassembly.js';
            blazorScript.onload = () => {
                console.log('✅ Blazor WebAssembly cargado');
                
                // Cargar Radzen después de Blazor
                const radzenScript = document.createElement('script');
                radzenScript.src = '_content/Radzen.Blazor/Radzen.Blazor.js?v=1.0.0';
                radzenScript.onload = () => {
                    console.log('✅ Radzen Blazor cargado');
                    resolve();
                };
                radzenScript.onerror = () => {
                    console.warn('⚠️ No se pudo cargar Radzen Blazor, continuando...');
                    resolve(); // No bloquear si Radzen falla
                };
                
                document.head.appendChild(radzenScript);
            };
            blazorScript.onerror = () => {
                console.error('❌ Error cargando Blazor WebAssembly');
                reject(new Error('Failed to load Blazor WebAssembly'));
            };
            
            document.head.appendChild(blazorScript);
        });
    },

    // Mostrar formulario de login
    showLoginForm() {
        const authCheckingDiv = document.getElementById('auth-checking');
        const appDiv = document.getElementById('app');
        const loginDiv = document.getElementById('login-form');
        
        authCheckingDiv.style.display = 'none';
        appDiv.style.display = 'none';
        loginDiv.style.display = 'block';
        
        // Configurar eventos del formulario
        this.setupLoginForm();
        
        // Auto-focus en el campo usuario
        document.getElementById('usernameInline').focus();
        
        console.log('📋 Formulario de login mostrado');
    },

    // Configurar eventos del formulario de login
    setupLoginForm() {
        const loginForm = document.getElementById('loginFormInline');
        if (loginForm && !loginForm.dataset.configured) {
            loginForm.addEventListener('submit', (e) => this.handleLogin(e));
            loginForm.dataset.configured = 'true';
        }
    },

    // Manejar el proceso de login
    async handleLogin(e) {
        e.preventDefault();
        
        const username = document.getElementById('usernameInline').value.trim();
        const password = document.getElementById('passwordInline').value;
        const rememberMe = document.getElementById('rememberMe').checked;
        
        // Validación básica
        if (!username || !password) {
            this.showMessage('Por favor ingresa usuario y contraseña', 'error');
            return;
        }
        
        // Mostrar estado de carga
        this.setButtonLoading(true);
        
        try {
            console.log('🔐 Intentando login para:', username);
            
            const response = await fetch(`${this.API_BASE_URL}/auth/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: username, password })
            });
            
            const data = await response.json();
            
            if (response.ok) {
                console.log('✅ Login exitoso');
                
                // Verificar si requiere selección de organización
                if (data.requiresOrganizationSelection && data.organizations) {
                    console.log('🏢 Requiere selección de organización');
                    
                    // Guardar datos temporales
                    sessionStorage.setItem('temporaryToken', data.temporaryToken);
                    sessionStorage.setItem('availableOrganizations', JSON.stringify(data.organizations));
                    sessionStorage.setItem('rememberMeChoice', rememberMe);
                    
                    this.showOrganizationSelection(data.organizations);
                } else if (data.token) {
                    // Login directo (organización única)
                    console.log('✅ Login directo completado');
                    await this.completeLogin(data.token, rememberMe, data.data);
                }
            } else {
                console.log('❌ Error en credenciales');
                this.showMessage('Error de autenticación. Verifica tus credenciales.', 'error');
            }
        } catch (error) {
            console.error('❌ Error en login:', error);
            this.showMessage('No se pudo conectar con el servidor. Inténtalo de nuevo.', 'error');
        } finally {
            this.setButtonLoading(false);
        }
    },

    // Mostrar selección de organizaciones
    showOrganizationSelection(organizations) {
        document.getElementById('login-form').style.display = 'none';
        document.getElementById('organization-selection').style.display = 'block';
        
        // Almacenar organizaciones para búsqueda
        this.allOrganizations = organizations;
        
        // Poblar lista de organizaciones
        this.populateOrganizations(organizations);
        
        // Configurar eventos
        this.setupOrganizationEvents();
        
        // Iniciar timer de expiración (2 minutos)
        this.startTokenTimer(120);
        
        console.log(`🏢 ${organizations.length} organizaciones disponibles`);
    },

    // Configurar eventos de selección de organización
    setupOrganizationEvents() {
        const selectBtn = document.getElementById('selectOrgBtn');
        const backBtn = document.getElementById('backToLoginBtn');
        const searchInput = document.getElementById('orgSearch');
        
        if (selectBtn && !selectBtn.dataset.configured) {
            selectBtn.addEventListener('click', () => this.handleOrganizationSelection());
            selectBtn.dataset.configured = 'true';
        }
        
        if (backBtn && !backBtn.dataset.configured) {
            backBtn.addEventListener('click', () => this.backToLogin());
            backBtn.dataset.configured = 'true';
        }
        
        if (searchInput && !searchInput.dataset.configured) {
            searchInput.addEventListener('input', (e) => this.handleOrganizationSearch(e));
            searchInput.dataset.configured = 'true';
        }
    },

    // Poblar lista de organizaciones
    populateOrganizations(organizations) {
        const orgList = document.getElementById('organizationList');
        const searchCount = document.getElementById('searchResultsCount');
        
        orgList.innerHTML = '';
        
        if (organizations.length === 0) {
            orgList.innerHTML = `
                <div class="no-results">
                    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <circle cx="11" cy="11" r="8" stroke="#605e5c" stroke-width="2"/>
                        <path d="m21 21-4.35-4.35" stroke="#605e5c" stroke-width="2"/>
                    </svg>
                    <p>No se encontraron organizaciones</p>
                </div>
            `;
            searchCount.textContent = '';
            return;
        }
        
        // Actualizar contador de búsqueda
        searchCount.textContent = `${organizations.length} organizaciones`;
        
        const icons = ['🏢', '🏛️', '🏪', '🏬', '🏭', '🏤', '🏦', '🏨', '🏣', '🏰'];
        
        organizations.forEach((org, index) => {
            const orgCard = document.createElement('div');
            orgCard.className = 'organization-card';
            orgCard.setAttribute('data-org-id', org.id);
            
            const icon = icons[index % icons.length];
            
            orgCard.innerHTML = `
                <div class="organization-card-content">
                    <div class="organization-radio"></div>
                    <div class="organization-icon">${icon}</div>
                    <div class="organization-info">
                        <h3 class="organization-name">${org.nombre}</h3>
                        <p class="organization-description">Organización empresarial</p>
                    </div>
                    <div class="organization-check" style="display: none;"></div>
                </div>
            `;
            
            orgCard.addEventListener('click', () => this.selectOrganization(org.id, org.nombre));
            orgList.appendChild(orgCard);
        });
    },

    // Seleccionar organización
    selectOrganization(orgId, orgName) {
        // Remover selección anterior
        document.querySelectorAll('.organization-card').forEach(card => {
            card.classList.remove('selected');
            const check = card.querySelector('.organization-check');
            if (check) check.style.display = 'none';
        });
        
        // Seleccionar nueva organización
        const selectedCard = document.querySelector(`[data-org-id="${orgId}"]`);
        if (selectedCard) {
            selectedCard.classList.add('selected');
            const check = selectedCard.querySelector('.organization-check');
            if (check) check.style.display = 'flex';
        }
        
        this.selectedOrganizationId = orgId;
        
        // Actualizar botón
        const selectBtn = document.getElementById('selectOrgBtn');
        selectBtn.disabled = false;
        selectBtn.querySelector('.fluent-button-text').textContent = `Continuar con ${orgName}`;
        
        console.log(`✅ Organización seleccionada: ${orgName}`);
    },

    // Manejar selección de organización
    async handleOrganizationSelection() {
        if (!this.selectedOrganizationId) {
            this.showOrgMessage('Por favor selecciona una organización', 'error');
            return;
        }
        
        const temporaryToken = sessionStorage.getItem('temporaryToken');
        const rememberMe = sessionStorage.getItem('rememberMeChoice') === 'true';
        
        // Mostrar estado de carga
        this.setOrgButtonLoading(true);
        
        try {
            console.log('🏢 Procesando selección de organización...');
            
            const response = await fetch(`${this.API_BASE_URL}/auth/select-organization`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ 
                    temporaryToken: temporaryToken,
                    organizationId: this.selectedOrganizationId
                })
            });
            
            const data = await response.json();
            
            if (response.ok && data.token) {
                console.log('✅ Organización seleccionada correctamente');
                
                // Limpiar datos temporales
                this.clearTemporaryData();
                
                // Completar login
                await this.completeLogin(data.token, rememberMe, data.data);
            } else {
                console.log('❌ Error seleccionando organización');
                this.showOrgMessage('Error al seleccionar organización. Inténtalo de nuevo.', 'error');
                this.resetOrgButton();
            }
        } catch (error) {
            console.error('❌ Error en selección:', error);
            this.showOrgMessage('No se pudo conectar con el servidor. Inténtalo de nuevo.', 'error');
            this.resetOrgButton();
        }
    },

    // Completar proceso de login
    async completeLogin(token, rememberMe, encryptedData = null) {
        console.log('🎉 Completando login...');
        
        // Guardar token
        localStorage.setItem('authToken', token);
        sessionStorage.setItem('isAuthenticated', 'true');
        sessionStorage.setItem('authToken', token);
        
        // Guardar datos encriptados si vienen
        if (encryptedData) {
            sessionStorage.setItem('sessionData', encryptedData);
        }
        
        if (rememberMe) {
            localStorage.setItem('rememberSession', 'true');
        }
        
        // Mostrar mensaje de éxito y luego cargar aplicación
        this.showMessage('Login exitoso! Cargando aplicación...', 'success');
        
        // Pequeña pausa para mostrar mensaje y luego cargar aplicación
        setTimeout(async () => {
            // Mostrar pantalla de carga
            this.showLoadingScreen();
            
            try {
                await this.loadApplication();
            } catch (error) {
                this.showMessage('Error cargando la aplicación. Inténtalo de nuevo.', 'error');
            }
        }, 800);
    },

    // Mostrar pantalla de carga durante la inicialización de Blazor
    showLoadingScreen() {
        const loginDiv = document.getElementById('login-form');
        const orgDiv = document.getElementById('organization-selection');
        const authCheckingDiv = document.getElementById('auth-checking');
        
        // Ocultar formularios y mostrar carga
        loginDiv.style.display = 'none';
        orgDiv.style.display = 'none';
        authCheckingDiv.style.display = 'block';
        
        // Actualizar texto de carga
        const loadingText = authCheckingDiv.querySelector('p');
        if (loadingText) {
            loadingText.textContent = 'Inicializando aplicación...';
        }
    },

    // Búsqueda de organizaciones
    handleOrganizationSearch(event) {
        const searchTerm = event.target.value.toLowerCase().trim();
        
        if (!searchTerm) {
            this.populateOrganizations(this.allOrganizations);
            return;
        }
        
        const filteredOrgs = this.allOrganizations.filter(org => 
            org.nombre.toLowerCase().includes(searchTerm)
        );
        
        this.populateOrganizations(filteredOrgs);
    },

    // Volver al login
    backToLogin() {
        this.clearTemporaryData();
        
        // Limpiar selección y búsqueda
        this.selectedOrganizationId = null;
        this.allOrganizations = [];
        
        const searchField = document.getElementById('orgSearch');
        if (searchField) searchField.value = '';
        
        // Mostrar login
        document.getElementById('organization-selection').style.display = 'none';
        document.getElementById('login-form').style.display = 'block';
        
        // Reset form
        document.getElementById('usernameInline').focus();
        
        console.log('🔙 Regresado al login');
    },

    // Timer de expiración de token temporal
    startTokenTimer(seconds) {
        let remaining = seconds;
        
        const updateTimer = () => {
            if (remaining <= 0) {
                clearInterval(this.tokenTimer);
                this.showOrgMessage('La sesión ha expirado. Por favor vuelve a iniciar sesión.', 'error');
                
                // Deshabilitar botones
                document.getElementById('selectOrgBtn').disabled = true;
                
                // Auto redirect después de 3 segundos
                setTimeout(() => this.backToLogin(), 3000);
                return;
            }
            remaining--;
        };
        
        this.tokenTimer = setInterval(updateTimer, 1000);
    },

    // Utilidades para UI
    showMessage(text, type) {
        const message = document.getElementById('messageInline');
        if (message) {
            message.textContent = text;
            message.className = `fluent-message ${type}`;
            
            if (type === 'error') {
                setTimeout(() => {
                    if (message.classList.contains('error')) {
                        message.className = 'fluent-message';
                        message.textContent = '';
                    }
                }, 5000);
            }
        }
    },

    showOrgMessage(text, type) {
        const message = document.getElementById('orgMessageInline');
        if (message) {
            message.textContent = text;
            message.className = `fluent-message ${type}`;
            
            if (type === 'error') {
                setTimeout(() => {
                    if (message.classList.contains('error')) {
                        message.className = 'fluent-message';
                        message.textContent = '';
                    }
                }, 5000);
            }
        }
    },

    setButtonLoading(loading) {
        const loginBtn = document.getElementById('loginBtnInline');
        if (loginBtn) {
            const buttonText = loginBtn.querySelector('.fluent-button-text');
            const buttonSpinner = loginBtn.querySelector('.fluent-button-spinner');
            
            loginBtn.disabled = loading;
            
            if (loading) {
                loginBtn.classList.add('loading');
                if (buttonText) buttonText.textContent = 'Iniciando sesión...';
                if (buttonSpinner) buttonSpinner.style.display = 'flex';
            } else {
                loginBtn.classList.remove('loading');
                if (buttonText) buttonText.textContent = 'Iniciar sesión';
                if (buttonSpinner) buttonSpinner.style.display = 'none';
            }
        }
    },

    setOrgButtonLoading(loading) {
        const selectBtn = document.getElementById('selectOrgBtn');
        if (selectBtn) {
            selectBtn.disabled = loading;
            
            if (loading) {
                selectBtn.classList.add('loading');
                selectBtn.querySelector('.fluent-button-text').textContent = 'Procesando...';
                selectBtn.querySelector('.fluent-button-spinner').style.display = 'flex';
            }
        }
    },

    resetOrgButton() {
        const selectBtn = document.getElementById('selectOrgBtn');
        if (selectBtn) {
            selectBtn.disabled = !this.selectedOrganizationId;
            selectBtn.classList.remove('loading');
            
            const buttonText = selectBtn.querySelector('.fluent-button-text');
            if (buttonText) {
                buttonText.textContent = this.selectedOrganizationId ? 
                    `Continuar con ${document.querySelector(`[data-org-id="${this.selectedOrganizationId}"] .organization-name`).textContent}` : 
                    'Continuar con Organización';
            }
            
            const spinner = selectBtn.querySelector('.fluent-button-spinner');
            if (spinner) spinner.style.display = 'none';
        }
    },

    // Limpiar datos temporales
    clearTemporaryData() {
        sessionStorage.removeItem('temporaryToken');
        sessionStorage.removeItem('availableOrganizations');
        sessionStorage.removeItem('rememberMeChoice');
        
        if (this.tokenTimer) {
            clearInterval(this.tokenTimer);
            this.tokenTimer = null;
        }
    },

    // Función global de logout
    logout() {
        console.log('🚪 Cerrando sesión...');
        
        // Mantener preferencia de tema
        const currentTheme = localStorage.getItem('fluentTheme');
        
        // Limpiar datos de autenticación
        localStorage.removeItem('authToken');
        localStorage.removeItem('rememberSession');
        sessionStorage.clear();
        
        // Restaurar tema
        if (currentTheme) {
            localStorage.setItem('fluentTheme', currentTheme);
        }
        
        // Recargar página
        window.location.reload();
    }
};

// Funciones globales
window.logout = () => AuthSystem.logout();

// Función para alternar tema (reutilizada del theme-switcher.js)
window.toggleTheme = function() {
    if (typeof window.setTheme === 'function') {
        const currentTheme = localStorage.getItem('fluentTheme') || 'light';
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        window.setTheme(newTheme);
    }
};

// Inicialización al cargar el DOM
document.addEventListener('DOMContentLoaded', function() {
    console.log('🌐 DOM cargado, iniciando sistema...');
    
    // Inicializar tema primero
    if (typeof window.initializeTheme === 'function') {
        window.initializeTheme();
    }
    
    // Luego inicializar autenticación
    AuthSystem.init();
});