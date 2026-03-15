const uri = '/Users';
let usersArr = [];

/**
 * שליפת כל המשתמשים מהשרת והצגתם בטבלה
 */
function getItems() {
    const token = localStorage.getItem('Token');
    
    fetch(uri, { 
        headers: token ? { 'Authorization': 'Bearer ' + token } : {} 
    })
    .then(response => {
        if (response.status === 401) { 
            window.location.href = 'login.html'; 
            return; 
        }
        return response.json();
    })
    .then(data => { 
        if (data) _displayItems(data); 
    })
    .catch(error => console.error('Unable to get users.', error));
}

/**
 * הוספת משתמש חדש למערכת
 */
function addItem() {
    const addNameTextbox = document.getElementById('add-name');
    const addPasswordTextbox = document.getElementById('add-password');
    const addMailTextbox = document.getElementById('add-mail');
    const addRoleSelect = document.getElementById('add-role');

    const item = {
        name: addNameTextbox.value.trim(),
        password: Number(addPasswordTextbox.value.trim()),
        mail: addMailTextbox.value.trim(),
        role: addRoleSelect.value
    };

    const token = localStorage.getItem('Token');
    fetch(uri, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + (token || '')
        },
        body: JSON.stringify(item)
    })
    .then(response => {
        if (response.status === 401) { 
            window.location.href = 'login.html'; 
            return; 
        }
        return response.json();
    })
    .then(() => {
        getItems(); // רענון הרשימה
        // ניקוי השדות
        addNameTextbox.value = '';
        addPasswordTextbox.value = '';
        addMailTextbox.value = '';
    })
    .catch(error => console.error('Unable to add user.', error));
}

/**
 * מחיקת משתמש לפי מזהה
 */
function deleteItem(id) {
    const token = localStorage.getItem('Token');
    fetch(`${uri}/${id}`, { 
        method: 'DELETE', 
        headers: token ? { 'Authorization': 'Bearer ' + token } : {} 
    })
    .then(response => {
        if (response.status === 401) { 
            window.location.href = 'login.html'; 
            return; 
        }
        if (!response.ok) throw new Error('Delete failed');
        getItems();
    })
    .catch(error => console.error('Unable to delete user.', error));
}

/**
 * הצגת טופס העריכה ומילוי נתוני המשתמש שנבחר
 */
function displayEditForm(id) {
    const item = usersArr.find(item => item.id === id);
    if (!item) return;

    document.getElementById('edit-name').value = item.name;
    document.getElementById('edit-id').value = item.id;
    document.getElementById('password').value = item.password;
    
    if (document.getElementById('edit-mail')) document.getElementById('edit-mail').value = item.mail || '';
    if (document.getElementById('edit-role')) document.getElementById('edit-role').value = item.role || 'User';
    
    document.getElementById('editForm').style.display = 'block';
}

/**
 * שליחת העדכון לשרת
 */
function updateItem() {
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        name: document.getElementById('edit-name').value.trim(),
        password: Number(document.getElementById('password').value),
        mail: document.getElementById('edit-mail')?.value.trim(),
        role: document.getElementById('edit-role')?.value
    };

    const token = localStorage.getItem('Token');
    fetch(`${uri}/${itemId}`, {
        method: 'PUT',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + (token || '')
        },
        body: JSON.stringify(item)
    })
    .then(response => {
        if (response.status === 401) { 
            window.location.href = 'login.html'; 
            return; 
        }
        if (!response.ok) throw new Error('Update failed');
        getItems();
    })
    .catch(error => console.error('Unable to update user.', error));

    closeInput();
    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

// --- פונקציות עזר לתצוגה ---

function _displayCount(itemCount) {
    const name = (itemCount === 1) ? 'משתמש רשום' : 'משתמשים רשומים';
    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function _displayItems(data) {
    const tBody = document.getElementById('music'); // משתמש באותו ID של הטבלה מהדף הראשי
    tBody.innerHTML = '';

    _displayCount(data.length);

    data.forEach(item => {
        let tr = tBody.insertRow();

        // עמודת אימייל
        tr.insertCell(0).appendChild(document.createTextNode(item.mail || ''));

        // עמודת שם
        tr.insertCell(1).appendChild(document.createTextNode(item.name));

        // עמודת תפקיד
        tr.insertCell(2).appendChild(document.createTextNode(item.role || 'User'));

        // כפתור עריכה
        let editBtn = document.createElement('button');
        editBtn.innerText = 'ערוך';
        editBtn.onclick = () => displayEditForm(item.id);
        tr.insertCell(3).appendChild(editBtn);

        // כפתור מחיקה
        let deleteBtn = document.createElement('button');
        deleteBtn.innerText = 'מחק';
        deleteBtn.onclick = () => deleteItem(item.id);
        tr.insertCell(4).appendChild(deleteBtn);
    });

    usersArr = data;
}

// הפעלה ראשונית
document.addEventListener('DOMContentLoaded', getItems);