// Authentication specific JavaScript

// Check authentication on page load
document.addEventListener('DOMContentLoaded', function() {
    checkAuthentication();
});

function checkAuthentication() {
    const token = localStorage.getItem('accessToken');
    const loginUrl = '/account/login';
    const currentUrl = window.location.pathname;

    // Si está en login pero tiene token, redirigir a dashboard
    if (currentUrl === loginUrl && token) {
        window.location.href = '/';
        return;
    }

    // Si no tiene token y no está en login, redirigir a login
    if (!token && !currentUrl.includes('login') && currentUrl !== '/') {
        window.location.href = loginUrl;
        return;
    }
}

// JWT Token validation
function isTokenExpired(token) {
    if (!token) return true;

    try {
        const parts = token.split('.');
        if (parts.length !== 3) return true;

        const payload = JSON.parse(atob(parts[1]));
        const now = Math.floor(Date.now() / 1000);

        return payload.exp < now;
    } catch (error) {
        console.error('Error validating token:', error);
        return true;
    }
}

// Refresh token
async function refreshAccessToken() {
    try {
        const accessToken = localStorage.getItem('accessToken');
        const refreshToken = localStorage.getItem('refreshToken');

        if (!accessToken || !refreshToken) {
            clearAuthData();
            window.location.href = '/account/login';
            return false;
        }

        const response = await fetch('https://localhost:7000/api/auth/refresh-token', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                accessToken: accessToken,
                refreshToken: refreshToken
            })
        });

        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                localStorage.setItem('accessToken', data.data.accessToken);
                localStorage.setItem('refreshToken', data.data.refreshToken);
                return true;
            }
        }

        clearAuthData();
        window.location.href = '/account/login';
        return false;
    } catch (error) {
        console.error('Error refreshing token:', error);
        return false;
    }
}

// Interceptor para todas las solicitudes fetch
const originalFetch = window.fetch;
window.fetch = async function(...args) {
    let token = localStorage.getItem('accessToken');

    // Verificar si el token está expirado
    if (token && isTokenExpired(token)) {
        const refreshed = await refreshAccessToken();
        if (!refreshed) {
            throw new Error('Token refresh failed');
        }
        token = localStorage.getItem('accessToken');
    }

    // Agregar token al header si existe
    const options = args[1] || {};
    if (token && !options.headers) {
        options.headers = {};
    }
    if (token && options.headers) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await originalFetch.apply(this, [args[0], options]);

    // Si la respuesta es 401, limpiar datos y redirigir
    if (response.status === 401) {
        clearAuthData();
        window.location.href = '/account/login';
    }

    return response;
};

// Clear authentication data
function clearAuthData() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('usuario');
}

// Logout handler
function logout() {
    if (confirm('¿Deseas cerrar sesión?')) {
        clearAuthData();
        window.location.href = '/account/login';
    }
}
