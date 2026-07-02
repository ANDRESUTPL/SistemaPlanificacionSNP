// API Configuration
const WEB_BASE_URL = '';

async function makeRequest(endpoint, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    try {
        const response = await fetch(`${WEB_BASE_URL}${endpoint}`, {
            ...options,
            headers
        });

        if (response.status === 401) {
            window.location.href = '/account/login';
            return null;
        }

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    } catch (error) {
        console.error('Request error:', error);
        return null;
    }
}

// Menu loading
async function loadDynamicMenu() {
    try {
        const response = await makeRequest('/Dashboard/GetMenuActual', {
            method: 'GET'
        });

        if (response && response.success && response.data) {
            renderMenu(response.data);
        } else {
            console.warn('No menu data received');
        }
    } catch (error) {
        console.error('Error loading menu:', error);
    }
}

function renderMenu(menuItems, parentElement = null) {
    if (!menuItems || menuItems.length === 0) return;

    const container = parentElement || document.getElementById('menu-container');
    if (!container) return;

    if (!parentElement) {
        container.innerHTML = '';
    }

    const ul = document.createElement('ul');
    ul.className = 'nav flex-column';

    menuItems.forEach(item => {
        const hasSubitems = item.subpantallas && item.subpantallas.length > 0;
        
        const li = document.createElement('li');
        li.className = 'nav-item';

        if (hasSubitems) {
            li.innerHTML = `
                <a class="nav-link collapsed" href="#" data-bs-toggle="collapse" data-bs-target="#menu-${item.pantallaId}">
                    <i class="${item.icono}"></i>
                    <span>${item.nombre}</span>
                    <i class="fas fa-chevron-down ms-auto"></i>
                </a>
                <div class="collapse" id="menu-${item.pantallaId}">
                    <ul class="nav flex-column ms-3"></ul>
                </div>
            `;

            ul.appendChild(li);

            // Renderizar subpantallas recursivamente
            const submenu = li.querySelector('.collapse ul');
            renderMenu(item.subpantallas, submenu);
        } else {
            const href = item.ruta || '#';
            li.innerHTML = `
                <a class="nav-link" href="${href}">
                    <i class="${item.icono}"></i>
                    <span>${item.nombre}</span>
                </a>
            `;
            ul.appendChild(li);
        }
    });

    if (parentElement) {
        parentElement.appendChild(ul);
    } else {
        container.innerHTML = '';
        container.appendChild(ul);
    }
}

// User info loading
async function loadUserInfo() {
    // El nombre de usuario ya se renderiza desde el servidor en _Layout.
}

// Logout function
async function logout() {
    if (confirm('¿Deseas cerrar sesión?')) {
        window.location.href = '/account/logout';
    }
}

// Show notification
function showNotification(message, type = 'info') {
    Swal.fire({
        title: type.charAt(0).toUpperCase() + type.slice(1),
        text: message,
        icon: type,
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 3000
    });
}

// Format helpers
function formatCurrency(value) {
    return new Intl.NumberFormat('es-ES', {
        style: 'currency',
        currency: 'USD'
    }).format(value);
}

function formatDate(date) {
    return new Date(date).toLocaleDateString('es-ES');
}

function formatDateTime(dateTime) {
    return new Date(dateTime).toLocaleString('es-ES');
}
