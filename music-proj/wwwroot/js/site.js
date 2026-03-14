const uri = '/Music';
let musicArr = [];
let hubConnection = null;

 function  isLogin()
 {
    // const token = JSON.parse(localStorage.getItem("Token"))
    if(localStorage.getItem("Token"))
       {
           // start SignalR connection early and then load items
           initSignalR().catch(err => console.error(err));
           getItems();
           // reveal admin-only links if current user has Admin role
           try {
               let token = localStorage.getItem('Token');
               console.debug('isLogin: raw token from storage:', token);
               if (!token) token = '';
               token = token.toString().trim();
               // remove common prefixes or surrounding quotes
               if (token.toLowerCase().startsWith('bearer ')) {
                   token = token.slice(7).trim();
                   console.debug('isLogin: removed "Bearer " prefix from token');
               }
               if (token && token.startsWith('"') && token.endsWith('"')) {
                   // strip accidental surrounding quotes
                   token = token.slice(1, -1).trim();
                   console.debug('isLogin: stripped quotes from token');
               }
               const claims = token ? parseJwt(token) : null;
            console.debug('isLogin: parsed claims:', claims);
               if (hasAdminRole(claims)) {
                   const usersLink = document.getElementById('users-link');
                   if (usersLink) {
                       // make sure it's visible and styled as admin action
                       usersLink.style.display = 'inline-block';
                       usersLink.setAttribute('aria-hidden', 'false');
                       usersLink.classList.remove('btn-white');
                       usersLink.classList.add('btn-gray');
                   }
               } else {
                   console.debug('isLogin: current user is not Admin or role claim missing', claims);
               }
           } catch (e) { console.warn('Unable to parse token for admin check', e); }
       }
   else
   window.location.href = 'login.html';

 }

// --- SignalR client setup ---
// Dynamically load SignalR script from CDN if not present, then start connection
async function loadSignalRScript() {
    if (window.signalR) return;
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js';
        script.onload = () => resolve();
        script.onerror = (e) => reject(new Error('Failed to load SignalR script'));
        document.head.appendChild(script);
    });
}

async function initSignalR() {
    try {
        await loadSignalRScript();

        // build connection and include token from localStorage
        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('/musicHub', {
                accessTokenFactory: () => localStorage.getItem('Token') || ''
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Listener: when server notifies ItemUpdated -> refresh table and show toast
        hubConnection.on('ItemUpdated', async (action, id) => {
            console.log('SignalR: ItemUpdated received', action, id);
            try {
                // ensure getItems returns a Promise so we can await refresh completion
                await getItems();
            } catch (e) {
                console.error('Error refreshing items after SignalR message', e);
            }
            showToast(`Data updated (${action}): ${id}`);
        });

        await hubConnection.start();
        console.info('SignalR connected to /musicHub');
    } catch (err) {
        console.error('SignalR connection error:', err);
    }
}

// Simple toast helper: small non-blocking notification
function showToast(message) {
    try {
        const containerId = 'toast-container';
        let container = document.getElementById(containerId);
        // If container missing for any reason, create it (fallback)
        if (!container) {
            container = document.createElement('div');
            container.id = containerId;
            container.setAttribute('aria-live', 'polite');
            container.setAttribute('aria-atomic', 'true');
            document.body.appendChild(container);
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = 'toast-message';
        const now = new Date();
        // message content
        const textNode = document.createElement('div');
        textNode.innerText = message;
        // small timestamp
        const timeNode = document.createElement('span');
        timeNode.className = 'toast-time';
        timeNode.innerText = now.toLocaleTimeString();

        toast.appendChild(textNode);
        toast.appendChild(timeNode);

        // Insert newest at the top
        if (container.firstChild) container.insertBefore(toast, container.firstChild);
        else container.appendChild(toast);

        // Keep only last 10 messages — remove oldest beyond 10
        while (container.children.length > 10) {
            container.removeChild(container.lastChild);
        }

        // (optional) clicking a toast removes it for that user
        toast.addEventListener('click', () => toast.remove());

    } catch (e) {
        console.warn('Toast failed', e);
    }
}



function getItems() {
    const token = localStorage.getItem('Token');
    return fetch(uri, {
        headers: token ? { 'Authorization': 'Bearer ' + token } : {}
    })
        .then(response => {
            if (response.status === 401) {
                window.location.href = 'login.html';
                return;
            }
            return response.json();
        })
        .then(data => { if (data) _displayItems(data); })
        .catch(error => {
            console.error('Unable to get items.', error);
            throw error;
        });
}

function addItem() {
    const addNameTextbox = document.getElementById('add-name');
    const woodMade = document.getElementById('add-woodMade');
    const item = {
        IsWoodMade: woodMade?woodMade.checked:false,
        name: addNameTextbox.value.trim()
    };

    fetch(uri, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + (localStorage.getItem('Token') || '')
            },
            body: JSON.stringify(item)
        })
        .then(response => response.json())
            .then(() => {
                // don't refresh here; SignalR will trigger refresh for all clients
                showToast('Item added successfully');
                addNameTextbox.value = '';
            })
        .catch(error => console.error('Unable to add item.', error));
}

function deleteItem(id) {
    fetch(`${uri}/${id}`, {
            method: 'DELETE',
             headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + (localStorage.getItem('Token') || '')
            }
        })
        .then(() => {
            // don't refresh here; SignalR will trigger refresh for all clients
            showToast('Item deleted successfully');
        })
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    const item = musicArr.find(item => item.id === id);

    document.getElementById('edit-name').value = item.name;
    document.getElementById('edit-id').value = item.id;
    document.getElementById('edit-isWoodMade').checked = item.IsWoodMade;
    document.getElementById('editForm').style.display = 'block';
}

function updateItem() {
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        IsWoodMade: document.getElementById('edit-isWoodMade').checked,
        name: document.getElementById('edit-name').value.trim()
    };
    fetch(`${uri}/${itemId}`, {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + (localStorage.getItem('Token') || '')
            },
            body: JSON.stringify(item)
        })
        .then(() => {
            // don't refresh here; SignalR will trigger refresh for all clients
            showToast('Item updated successfully');
        })
        .catch(error => console.error('Unable to update item.', error));

    closeInput();

    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(itemCount) {
    // Hebrew-friendly count text (instruments)
    const name = (itemCount === 1) ? 'כלי בחנות' : 'כלים בחנות';
    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function _displayItems(data) {
    const tBody = document.getElementById('music');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');
    button.className = 'btn-white';

    data.forEach(item => {
        let isWoodMadeCheckbox = document.createElement('input');
        isWoodMadeCheckbox.type = 'checkbox';
        isWoodMadeCheckbox.disabled = true;
        isWoodMadeCheckbox.checked = item.isWoodMade;

    let editButton = button.cloneNode(false);
    editButton.className = 'btn-gray';
    editButton.innerText = 'Edit';
        editButton.setAttribute('onclick', `displayEditForm(${item.id})`);

    let deleteButton = button.cloneNode(false);
    deleteButton.className = 'btn-accent';
    deleteButton.innerText = 'Delete';
        deleteButton.setAttribute('onclick', `deleteItem(${item.id})`);

        let tr = tBody.insertRow();

        let td1 = tr.insertCell(0);
        td1.appendChild(isWoodMadeCheckbox);

        let td2 = tr.insertCell(1);
        let textNode = document.createTextNode(item.name);
        td2.appendChild(textNode);

        let td3 = tr.insertCell(2);
        td3.appendChild(editButton);

        let td4 = tr.insertCell(3);
        td4.appendChild(deleteButton);
    });

    musicArr = data;
}

// Decode JWT payload (base64url) — returns parsed payload object or null
function parseJwt(token) {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length < 2) return null;
    // base64url decode
    let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    // add padding if missing
    while (payload.length % 4 !== 0) payload += '=';
    try {
        const decoded = atob(payload);
        // decode percent-encoding to support unicode
        const json = decodeURIComponent(decoded.split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(json);
    } catch (e) {
        console.warn('parseJwt failed', e);
        return null;
    }
}

// Check token payload for Admin role. Handles different claim key shapes.
// Robust scan of the token payload for an Admin role anywhere inside the claims object
function hasAdminRole(claims) {
    if (!claims) return false;
    // recursively traverse values and check for Admin (case-insensitive)
    const stack = [claims];
    while (stack.length) {
        const node = stack.pop();
        if (node == null) continue;
        if (typeof node === 'string') {
            if (node.toLowerCase() === 'admin') return true;
            // comma/space separated
            const parts = node.split(/[ ,;]+/).map(s => s.trim().toLowerCase()).filter(Boolean);
            if (parts.includes('admin')) return true;
            continue;
        }
        if (Array.isArray(node)) {
            for (const it of node) stack.push(it);
            continue;
        }
        if (typeof node === 'object') {
            for (const k of Object.keys(node)) {
                stack.push(node[k]);
            }
            continue;
        }
        // other primitive types ignored
    }
    return false;
}

// Show Users link if token indicates Admin. Retry briefly in case of race with login redirect.
function showUsersIfAdmin() {
    try {
        let token = localStorage.getItem('Token') || '';
        token = token.toString().trim();
        if (token.toLowerCase().startsWith('bearer ')) token = token.slice(7).trim();
        if (token && token.startsWith('"') && token.endsWith('"')) token = token.slice(1, -1).trim();
        const claims = token ? parseJwt(token) : null;
        console.debug('showUsersIfAdmin: claims=', claims);
        if (hasAdminRole(claims)) {
            const usersLink = document.getElementById('users-link');
            if (usersLink) {
                usersLink.style.display = 'inline-block';
                usersLink.classList.remove('btn-white');
                usersLink.classList.add('btn-gray');
                usersLink.setAttribute('aria-hidden', 'false');
                return true;
            }
        }
        // Additional direct-check for the full MS claim key (some tokens use the long URL as the claim name)
        try {
            const roleKey = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
            if (!claims && token) {
                // fallback: try to parse raw payload quickly (if parseJwt somehow failed earlier)
                // reuse parseJwt but protect against throwing
                try { claims = parseJwt(token); } catch (e) { claims = null; }
            }
            if (claims && typeof claims === 'object' && Object.prototype.hasOwnProperty.call(claims, roleKey)) {
                const roleVal = claims[roleKey];
                const roleStr = (Array.isArray(roleVal) ? roleVal.join(',') : (roleVal||'')).toString().toLowerCase();
                if (roleStr.includes('admin')) {
                    const usersLink = document.getElementById('users-link');
                    if (usersLink) {
                        // use important to override any hiding CSS
                        usersLink.style.setProperty('display', 'inline-block', 'important');
                        usersLink.classList.remove('btn-white');
                        usersLink.classList.add('btn-gray');
                        usersLink.setAttribute('aria-hidden', 'false');
                        console.debug('showUsersIfAdmin: Admin detected via full claim key');
                        return true;
                    }
                }
            }
        } catch (e) { console.warn('showUsersIfAdmin: full-claim check failed', e); }
    } catch (e) {
        console.warn('showUsersIfAdmin failed', e);
    }
    return false;
}

// Try to fetch authenticated user info from server as the primary, reliable source
async function fetchAndShowUserIfAdmin() {
    try {
        let token = localStorage.getItem('Token') || '';
        token = token.toString().trim();
        if (!token) return false;
        if (token.toLowerCase().startsWith('bearer ')) token = token.slice(7).trim();
        if (token && token.startsWith('"') && token.endsWith('"')) token = token.slice(1, -1).trim();

        const res = await fetch('/Users/me', {
            headers: {
                'Authorization': 'Bearer ' + token,
                'Accept': 'application/json'
            }
        });
        if (!res.ok) return false;
        const data = await res.json();
        console.debug('fetchAndShowUserIfAdmin: server returned', data);
        if (data && data.role && data.role.toString().toLowerCase() === 'admin') {
            const usersLink = document.getElementById('users-link');
            if (usersLink) {
                usersLink.style.display = 'inline-block';
                usersLink.classList.remove('btn-white');
                usersLink.classList.add('btn-gray');
                usersLink.setAttribute('aria-hidden', 'false');
                return true;
            }
        }
    } catch (e) {
        console.debug('fetchAndShowUserIfAdmin failed', e);
    }
    return false;
}

// Run on DOM loaded and retry a few times in case the token arrives after page load
window.addEventListener('DOMContentLoaded', () => {
    // try server-side check first (most reliable)
    fetchAndShowUserIfAdmin().then(found => {
        if (found) return;
        // fallback to local JWT-based detection with retries
        if (showUsersIfAdmin()) return;
        let attempts = 0;
        const maxAttempts = 12; // ~2.4s
        const iv = setInterval(() => {
            attempts++;
            if (showUsersIfAdmin() || attempts >= maxAttempts) clearInterval(iv);
        }, 200);
    }).catch(err => {
        console.debug('fetchAndShowUserIfAdmin threw', err);
        // fallback path
        if (showUsersIfAdmin()) return;
        let attempts = 0;
        const maxAttempts = 12;
        const iv = setInterval(() => {
            attempts++;
            if (showUsersIfAdmin() || attempts >= maxAttempts) clearInterval(iv);
        }, 200);
    });
});

// Expose some helpers to the global window so they can be run from DevTools
try {
    window.showUsersIfAdmin = showUsersIfAdmin;
    window.fetchAndShowUserIfAdmin = fetchAndShowUserIfAdmin;
    window.parseJwt = parseJwt;
    window.isLogin = isLogin;
    console.debug('site.js: helper functions exposed to window');
} catch (e) { /* ignore if running in strict/module context */ }