const uri = '/Users';
let usersArr = [];

function getItems() {
    const token = localStorage.getItem('Token');
    fetch(uri, { headers: token ? { 'Authorization': 'Bearer ' + token } : {} })
        .then(response => {
            if (response.status === 401) { window.location.href = 'login.html'; return; }
            return response.json();
        })
        .then(data => { if (data) _displayItems(data); })
        .catch(error => console.error('Unable to get items.', error));
}

function addItem() {
    const addNameTextbox = document.getElementById('add-name');
    const addPasswordTextbox = document.getElementById('add-password')
    const addMailTextbox = document.getElementById('add-mail')
    const addRoleSelect = document.getElementById('add-role')

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
            if (response.status === 401) { window.location.href = 'login.html'; return; }
            return response.json();
        })
        .then(() => {
            getItems();
            addNameTextbox.value = '';
            addPasswordTextbox.value = '';
            addMailTextbox.value = '';
        })
        .catch(error => console.error('Unable to add item.', error));
}

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
            if (!response.ok) {
                console.error('Delete failed with status:', response.status);
                return;
            }
            getItems();
        })
        .catch(error => console.error('Unable to delete item.', error));
}

function displayEditForm(id) {
    const item = usersArr.find(item => item.id === id);
    document.getElementById('edit-name').value = item.name;
    document.getElementById('edit-id').value = item.id;
    document.getElementById('password').value = item.password;
    // אם יש שדה Mail/Role בתבנית עריכת המשתמש, נמלא אותם
    if (document.getElementById('edit-mail')) document.getElementById('edit-mail').value = item.mail || '';
    if (document.getElementById('edit-role')) document.getElementById('edit-role').value = item.role || 'User';
    document.getElementById('editForm').style.display = 'block';
}

function updateItem() {
    const itemId = document.getElementById('edit-id').value;
    const item = {
        id: parseInt(itemId, 10),
        name: document.getElementById('edit-name').value.trim(),
        password: Number(document.getElementById('password').value),
        mail: document.getElementById('edit-mail') ? document.getElementById('edit-mail').value.trim() : undefined,
        role: document.getElementById('edit-role') ? document.getElementById('edit-role').value : undefined

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
            if (!response.ok) {
                console.error('Update failed with status:', response.status);
                return;
            }
            getItems();
        })
        .catch(error => console.error('Unable to update item.', error));

    closeInput();

    return false;
}

function closeInput() {
    document.getElementById('editForm').style.display = 'none';
}

function _displayCount(itemCount) {
    const name = (itemCount === 1) ? 'music' : 'music kinds';

    document.getElementById('counter').innerText = `${itemCount} ${name}`;
}

function _displayItems(data) {
    const tBody = document.getElementById('music');
    tBody.innerHTML = '';

    _displayCount(data.length);

    const button = document.createElement('button');

    data.forEach(item => {
    let editButton = button.cloneNode(false);
    editButton.innerText = 'Edit';
    editButton.setAttribute('onclick', `displayEditForm(${item.id})`);

    let deleteButton = button.cloneNode(false);
    deleteButton.innerText = 'Delete';
    deleteButton.setAttribute('onclick', `deleteItem(${item.id})`);

    let tr = tBody.insertRow();

    let td0 = tr.insertCell(0);
    let textNode0 = document.createTextNode(item.mail || '');
    td0.appendChild(textNode0);

    let td2 = tr.insertCell(1);
    let textNode = document.createTextNode(item.name);
    td2.appendChild(textNode);

    let tdRole = tr.insertCell(2);
    let roleNode = document.createTextNode(item.role || 'User');
    tdRole.appendChild(roleNode);

    let td3 = tr.insertCell(3);
    td3.appendChild(editButton);

    let td4 = tr.insertCell(4);
    td4.appendChild(deleteButton);
    });

    usersArr = data;
}