const uri = '/Music';
let musicArr = [];
let hubConnection = null;

/**
 * בדיקת מצב התחברות:
 * אם קיים Token - מפעיל SignalR וטוען נתונים.
 * אם לא - מעביר לדף ההתחברות.
 */
function isLogin() {
    console.log("search token");
    if (localStorage.getItem("Token")) {
        // אתחול חיבור בזמן אמת וטעינת רשימת הכלים
        initSignalR().catch(err => console.error(err));
        getItems();

        // בדיקת הרשאות מנהל להצגת תפריטים חסויים
        try {
            let token = localStorage.getItem('Token');
            console.debug('isLogin: raw token from storage:', token);
            
            if (!token) token = '';
            token = token.toString().trim();

            // ניקוי קידומות ומרכאות מיותרות מהטוקן
            if (token.toLowerCase().startsWith('bearer ')) {
                token = token.slice(7).trim();
            }
            if (token && token.startsWith('"') && token.endsWith('"')) {
                token = token.slice(1, -1).trim();
            }

            const claims = token ? parseJwt(token) : null;
            console.debug('isLogin: parsed claims:', claims);

            if (hasAdminRole(claims)) {
                const usersLink = document.getElementById('users-link');
                if (usersLink) {
                    usersLink.style.display = 'inline-block';
                    usersLink.setAttribute('aria-hidden', 'false');
                    usersLink.classList.remove('btn-white');
                    usersLink.classList.add('btn-gray');
                }
            }
        } catch (e) { 
            console.warn('Unable to parse token for admin check', e); 
        }
    } else {
        window.location.href = 'login.html';
    }
}

// --- הגדרות SignalR (תקשורת בזמן אמת) ---

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

        hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('/musicHub', {
                accessTokenFactory: () => localStorage.getItem('Token') || ''
            })
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // האזנה לעדכונים מהשרת: כשפריט משתנה, רענן את הטבלה לכולם
        hubConnection.on('ItemUpdated', async (action, id) => {
            console.log('SignalR: ItemUpdated received', action, id);
            try {
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

// התראות קופצות (Toasts)
function showToast(message) {
    try {
        const containerId = 'toast-container';
        let container = document.getElementById(containerId);
        if (!container) {
            container = document.createElement('div');
            container.id = containerId;
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = 'toast-message';
        const now = new Date();
        
        toast.innerHTML = `<div>${message}</div><span class="toast-time">${now.toLocaleTimeString()}</span>`;
        
        if (container.firstChild) container.insertBefore(toast, container.firstChild);
        else container.appendChild(toast);

        while (container.children.length > 10) container.removeChild(container.lastChild);
        toast.addEventListener('click', () => toast.remove());
    } catch (e) {
        console.warn('Toast failed', e);
    }
}

// --- פעולות CRUD (מול ה-API) ---

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
        IsWoodMade: woodMade ? woodMade.checked : false,
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
        showToast('Item added successfully');
        addNameTextbox.value = '';
    })
    .catch(error => console.error('Unable to add item.', error));
}

function deleteItem(id) {
    fetch(`${uri}/${id}`, {
        method: 'DELETE',
        headers: {
            'Authorization': 'Bearer ' + (localStorage.getItem('Token') || '')
        }
    })
    .then(() => showToast('Item deleted successfully'))
    .catch(error => console.error('Unable to delete item.', error));
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
    .then(() => showToast('Item updated successfully'))
    .catch(error => console.error('Unable to update item.', error));

    closeInput();
    return false;
}

// --- עזרי תצוגה ופענוח Token ---

function _displayItems(data) {
    const tBody = document.getElementById('music');
    tBody.innerHTML = '';
    _displayCount(data.length);

    data.forEach(item => {
        let tr = tBody.insertRow();
        
        // עמודת הצ'קבוקס
        let isWoodMadeCheckbox = document.createElement('input');
        isWoodMadeCheckbox.type = 'checkbox';
        isWoodMadeCheckbox.disabled = true;
        isWoodMadeCheckbox.checked = item.isWoodMade;
        tr.insertCell(0).appendChild(isWoodMadeCheckbox);

        // עמודת שם
        tr.insertCell(1).appendChild(document.createTextNode(item.name));

        // כפתור עריכה
        let editBtn = document.createElement('button');
        editBtn.className = 'btn-gray';
        editBtn.innerText = 'Edit';
        editBtn.onclick = () => displayEditForm(item.id);
        tr.insertCell(2).appendChild(editBtn);

        // כפתור מחיקה
        let deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn-accent';
        deleteBtn.innerText = 'Delete';
        deleteBtn.onclick = () => deleteItem(item.id);
        tr.insertCell(3).appendChild(deleteBtn);
    });
    musicArr = data;
}

function _displayCount(itemCount) {
    const name = (itemCount === 1) ? 'כלי בחנות' : 'כלים בחנות';
    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function parseJwt(token) {
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length < 2) return null;
    let payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    while (payload.length % 4 !== 0) payload += '=';
    try {
        const decoded = atob(payload);
        return JSON.parse(decodeURIComponent(decoded.split('').map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')));
    } catch (e) { return null; }
}

function hasAdminRole(claims) {
    if (!claims) return false;
    const stack = [claims];
    while (stack.length) {
        const node = stack.pop();
        if (!node) continue;
        if (typeof node === 'string') {
            if (node.toLowerCase() === 'admin') return true;
            if (node.split(/[ ,;]+/).some(s => s.trim().toLowerCase() === 'admin')) return true;
        } else if (Array.isArray(node)) {
            stack.push(...node);
        } else if (typeof node === 'object') {
            Object.values(node).forEach(v => stack.push(v));
        }
    }
    return false;
}

// ניסיון אימות מול השרת לזיהוי מנהל
async function fetchAndShowUserIfAdmin() {
    try {
        let token = localStorage.getItem('Token') || '';
        if (!token) return false;
        token = token.replace(/Bearer /i, '').replace(/"/g, '').trim();

        const res = await fetch('/Users/me', {
            headers: { 'Authorization': 'Bearer ' + token, 'Accept': 'application/json' }
        });
        if (!res.ok) return false;
        const data = await res.json();
        if (data?.role?.toLowerCase() === 'admin') {
            const usersLink = document.getElementById('users-link');
            if (usersLink) {
                usersLink.style.display = 'inline-block';
                return true;
            }
        }
    } catch (e) { console.debug(e); }
    return false;
}

// הרצה בעת טעינת הדף
window.addEventListener('DOMContentLoaded', () => {
    fetchAndShowUserIfAdmin().then(found => {
        if (!found) isLogin();
    });
});