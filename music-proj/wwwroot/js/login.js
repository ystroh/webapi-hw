const uri = '/Users/Login';

// האזנה לטעינת הדף כדי לוודא שהאלמנטים קיימים ב-DOM
document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('myForm');
    
    if (loginForm) {
        // מניעת רענון הדף ושליחת הטופס בצורה ידנית
        loginForm.addEventListener('submit', function(event) {
            event.preventDefault();
            performLogin();
        });
    }
});

/**
 * פונקציה המבצעת את תהליך ההתחברות מול השרת
 */
function performLogin() {
    const nameElement = document.getElementById("name");
    const passwordElement = document.getElementById("password");
    const msgElement = document.getElementById('msg');

    // בניית אובייקט המשתמש לשליחה (המרה של הסיסמה למספר בהתאם למודל בשרת)
    const usr = {
        Name: nameElement.value,
        password: Number(passwordElement.value)
    };

    console.log("DEBUG: מנסה להתחבר עם:", usr.Name);

    // ביצוע קריאת ה-POST לשרת
    fetch(uri, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(usr)
    })
    .then(res => {
        // בדיקת תקינות התגובה מהשרת
        if (!res.ok) {
            if (res.status === 401) throw new Error('שם משתמש או סיסמה שגויים');
            throw new Error('שגיאת שרת');
        }
        return res.json();
    })
    .then(data => {
        // שמירת ה-Token (מחרוזת ה-JWT) בזיכרון המקומי של הדפדפן
        localStorage.setItem("Token", data);
        
        console.log("DEBUG: התחברת בהצלחה, מעביר לדף הבית");
        
        // העברה לדף הבית לאחר התחברות מוצלחת
        window.location.href = 'index.html';
    })
    .catch(error => {
        // הצגת הודעת שגיאה למשתמש במקרה של כישלון
        console.error('Login failed.', error);
        msgElement.innerText = error.message || 'שגיאת התחברות – בדקי שם וסיסמה';
    });
}