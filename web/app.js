/* Avryd Web App — Frontend JavaScript
 * Communicates with the Avryd backend at /api/*
 * Backend runs on https://avryd.onrender.com
 */

const API = '';  // Same origin; change to 'https://avryd.onrender.com' for local dev

// ──────────────────────────────────────────────────────────
// Auth helpers
// ──────────────────────────────────────────────────────────

function getToken() { return localStorage.getItem('avryd_token'); }
function setToken(t) { localStorage.setItem('avryd_token', t); }
function clearToken() { localStorage.removeItem('avryd_token'); }

async function apiFetch(path, opts = {}) {
    const token = getToken();
    const headers = { 'Content-Type': 'application/json', ...(opts.headers || {}) };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const res = await fetch(API + path, { ...opts, headers });
    if (res.status === 401) { clearToken(); window.location.href = '/auth.html'; return null; }
    return res;
}

// ──────────────────────────────────────────────────────────
// Auth page
// ──────────────────────────────────────────────────────────

function signIn(provider) {
    showLoading(`Opening ${provider} sign-in...`);
    // Redirect to backend OAuth flow
    window.location.href = `${API}/api/auth/${provider}?redirect_uri=${encodeURIComponent(window.location.origin + '/auth.html?callback=1')}`;
}

async function validateKey() {
    const email = document.getElementById('emailInput')?.value?.trim();
    const key = document.getElementById('keyInput')?.value?.trim();
    if (!email || !key) { showStatus('Please enter your email and product key.', 'error'); return; }

    showLoading('Validating key...');
    try {
        const res = await fetch(`${API}/api/activate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, product_key: key })
        });
        const data = await res.json();
        hideLoading();
        if (data.success && data.token) {
            setToken(data.token);
            showStatus('Activated! Redirecting...', 'success');
            setTimeout(() => window.location.href = '/dashboard.html', 1500);
        } else {
            showStatus(data.message || 'Invalid key or email.', 'error');
        }
    } catch {
        hideLoading();
        showStatus('Network error. Please try again.', 'error');
    }
}

function showStatus(msg, type = 'error') {
    const el = document.getElementById('statusMsg');
    if (!el) return;
    el.textContent = msg;
    el.className = `status-msg ${type}`;
    el.style.display = 'block';
}

function showLoading(text = 'Loading...') {
    const el = document.getElementById('loadingIndicator');
    const textEl = document.getElementById('loadingText');
    if (el) el.style.display = 'flex';
    if (textEl) textEl.textContent = text;
}

function hideLoading() {
    const el = document.getElementById('loadingIndicator');
    if (el) el.style.display = 'none';
}

function copyKey() {
    const keyEl = document.getElementById('productKey');
    if (!keyEl) return;
    navigator.clipboard.writeText(keyEl.textContent).then(() => {
        showStatus('Key copied to clipboard!', 'success');
    }).catch(() => showStatus('Could not copy. Please copy manually.', 'error'));
}

// Handle OAuth callback
function handleOAuthCallback() {
    const params = new URLSearchParams(window.location.search);
    if (params.get('callback') !== '1') return;

    const token = params.get('token');
    const error = params.get('error');

    if (token) {
        setToken(token);
        window.history.replaceState({}, '', '/auth.html');
        showStatus('Signed in successfully! Redirecting...', 'success');
        setTimeout(() => window.location.href = '/dashboard.html', 1500);
    } else if (error) {
        showStatus(`Sign-in failed: ${error}`, 'error');
    }
}

// ──────────────────────────────────────────────────────────
// Dashboard
// ──────────────────────────────────────────────────────────

async function loadDashboard() {
    const token = getToken();
    if (!token) { window.location.href = '/auth.html'; return; }

    try {
        const res = await apiFetch('/api/user/me');
        if (!res) return;
        const user = await res.json();

        document.getElementById('userEmail').textContent = user.email || '';
        document.getElementById('welcomeMsg').textContent = `Welcome back, ${user.display_name || user.email}`;
        document.getElementById('memberSince').textContent = user.created_at
            ? new Date(user.created_at).toLocaleDateString() : 'N/A';
        document.getElementById('deviceInfo').textContent = user.hardware_id
            ? user.hardware_id.substring(0, 12) + '...' : 'Not activated';

        if (user.product_key) {
            document.getElementById('productKey').textContent = formatKey(user.product_key);
        }

        const status = document.getElementById('licenseStatus');
        if (status) {
            status.textContent = user.is_activated ? 'Active' : 'Not activated';
            status.className = `status-badge ${user.is_activated ? 'status-active' : 'status-inactive'}`;
        }

        loadSessions(user.sessions || []);

    } catch (err) {
        console.error('Dashboard load error:', err);
    }
}

function loadSessions(sessions) {
    const list = document.getElementById('sessionsList');
    if (!list) return;
    if (!sessions.length) { list.innerHTML = '<p class="loading-text">No sessions recorded yet.</p>'; return; }

    list.innerHTML = sessions.slice(-10).reverse().map(s => {
        const start = new Date(s.start_time).toLocaleString();
        const dur = s.duration_minutes ? `${Math.round(s.duration_minutes)}m` : 'In progress';
        return `<div class="session-item"><span>${start}</span><span>${dur}</span></div>`;
    }).join('');
}

function formatKey(raw) {
    const clean = raw.replace(/-/g, '').toUpperCase();
    return clean.match(/.{1,4}/g)?.join('-') || raw;
}

async function regenerateKey() {
    if (!confirm('Regenerate your product key? The old key will no longer work.')) return;
    try {
        const res = await apiFetch('/api/key/regenerate', { method: 'POST' });
        if (!res) return;
        const data = await res.json();
        if (data.product_key) {
            document.getElementById('productKey').textContent = formatKey(data.product_key);
            alert('New key generated. Your old key is now invalid.');
        }
    } catch { alert('Failed to regenerate key.'); }
}

function signOut() {
    clearToken();
    window.location.href = '/';
}

// ──────────────────────────────────────────────────────────
// Home page — check auth
// ──────────────────────────────────────────────────────────

function checkAuthState() {
    const token = getToken();
    const dashboardLink = document.getElementById('dashboardLink');
    const signInBtn = document.getElementById('signInBtn');
    if (token) {
        if (dashboardLink) dashboardLink.style.display = 'inline';
        if (signInBtn) signInBtn.textContent = 'Dashboard';
        if (signInBtn) signInBtn.href = '/dashboard.html';
    }
}

// ──────────────────────────────────────────────────────────
// Init
// ──────────────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () => {
    handleOAuthCallback();
    checkAuthState();

    // Animate terminal lines on home page
    const lines = document.querySelectorAll('.speech-line');
    if (lines.length) {
        let i = 0;
        setInterval(() => {
            lines.forEach(l => l.classList.remove('focused'));
            lines[i % lines.length].classList.add('focused');
            i++;
        }, 2000);
    }
});
