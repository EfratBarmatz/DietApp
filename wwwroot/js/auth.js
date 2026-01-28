const API_URL = '/api/auth';

// בדיקה אם כבר מחוברים - אם כן, זורק אותך ישר לאפליקציה
if (localStorage.getItem('dietUser')) {
    window.location.href = 'index.html';
}

function showRegister() {
    document.getElementById('loginScreen').classList.add('hidden');
    document.getElementById('registerScreen').classList.remove('hidden');
}

function showLogin() {
    document.getElementById('registerScreen').classList.add('hidden');
    document.getElementById('loginScreen').classList.remove('hidden');
}

function togglePass(id, icon) {
    const input = document.getElementById(id);
    if (input.type === "password") {
        input.type = "text"; icon.classList.replace("bi-eye", "bi-eye-slash");
    } else {
        input.type = "password"; icon.classList.replace("bi-eye-slash", "bi-eye");
    }
}

async function login() {
    const email = document.getElementById('loginEmail').value;
    const pass = document.getElementById('loginPass').value;
    const btn = document.getElementById('loginBtn');
    
    if(!email || !pass) return alert('נא למלא פרטים');

    btn.disabled = true;
    btn.innerHTML = '<span class="loading-spinner"></span> מתחבר...';

    try {
        const res = await fetch(`${API_URL}/login`, {
            method: 'POST', 
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({ email, password: pass })
        });

        if (res.ok) {
            const data = await res.json();
            // שמירת המשתמש
            localStorage.setItem('dietUser', JSON.stringify({ id: data.userId, email: data.email }));
            // *** המעבר לדף השני ***
            window.location.href = 'index.html'; 
        } else {
            document.getElementById('loginError').innerText = 'פרטים שגויים';
            document.getElementById('loginError').style.display = 'block';
        }
    } catch(e) {
        console.error(e);
        alert('שגיאת תקשורת');
    } finally {
        btn.disabled = false;
        btn.innerHTML = 'כניסה למערכת';
    }
}

async function register() {
    // ... (אותו קוד הרשמה שהיה לך קודם, רק להעתיק לפה) ...
    // בסיום הרשמה מוצלחת: showLogin();
}
// הערה: תעתיקי את פונקציית register המלאה מהקוד הקודם שלך לכאן
