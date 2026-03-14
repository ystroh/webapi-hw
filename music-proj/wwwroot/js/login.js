const uri = '/Users/Login';

document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('myForm');
    
    if (loginForm) {
        loginForm.addEventListener('submit', function(event) {
            event.preventDefault();
            performLogin();
        });
    }
});

function performLogin() {
    const nameElement = document.getElementById("name");
    const passwordElement = document.getElementById("password");
    const msgElement = document.getElementById('msg');

    const usr = {
        Name: nameElement.value,
        password: Number(passwordElement.value)
    };

    console.log("DEBUG: מנסה להתחבר עם:", usr.Name);

    fetch(uri, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(usr)
    })
    .then(res => {
        if (!res.ok) {
            if (res.status === 401) throw new Error('שם משתמש או סיסמה שגויים');
            throw new Error('שגיאת שרת');
        }
        return res.json();
    })
    .then(data => {
        // שמירת ה-Token שהתקבל מהשרת
        localStorage.setItem("Token", data);
        console.log("DEBUG: התחברת בהצלחה, מעביר לדף הבית");
        window.location.href = 'index.html';
    })
    .catch(error => {
        console.error('Login failed.', error);
        msgElement.innerText = error.message || 'שגיאת התחברות – בדקי שם וסיסמה';
    });
}