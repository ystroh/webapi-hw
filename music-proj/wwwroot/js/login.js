const uri = '/Users/Login';
let id=0;

document.getElementById('myForm').addEventListener('submit', function(event) {
    event.preventDefault()
    myFunction();
});

// function myFunction() {


//     id+=1;
//     const name = document.getElementById("name").value;
//     const password = document.getElementById("password").value;
//     const usr={
//         Id:id,
//         Name:name,
//         password:password
    
//     };

//   fetch(uri, {
//             method: 'POST',
//             headers: {
//                 'Accept': 'application/json',
//                 'Content-Type': 'application/json'
//             },
//             body: JSON.stringify(usr)
//         })
//         .then(res=>res.json())
//         .then(data=>{
//             localStorage.setItem("Token",data)
//         }) 
//         .catch(error => console.error('Unable to add item.', error));

//       const currentUrl = window.location.href; // מקבל את ה-URL הנוכחי
//         const newUrl = currentUrl.substring(0, currentUrl.lastIndexOf('/')); // מסיר את הקטע האחרון
//         window.location.href = newUrl+'/index.html'; // מבצע את ה-redirect

//    }

function myFunction() {

    id += 1;
    const name = document.getElementById("name").value;
    const password = document.getElementById("password").value;
    const usr = {
        Id: id,
        Name: name,
        password: password
    };

    fetch(uri, {
        method: 'POST',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(usr)
    })
    .then(res => res.json())
    .then(data => {
        localStorage.setItem("Token", data.token); // נניח שה-token נמצא בשדה token
        const currentUrl = window.location.href; 
        const newUrl = currentUrl.substring(0, currentUrl.lastIndexOf('/')); 
        window.location.href = newUrl + '/index.html'; // מבצע את ה-redirect
    }) 
    .catch(error => console.error('Unable to add item.', error));
}








 




