const API_URL = '/api/user';
let currentUser = null;

// בדיקת התחברות
const savedUser = localStorage.getItem('dietUser');
if (!savedUser) {
    window.location.href = 'login.html';
} else {
    currentUser = JSON.parse(savedUser);
    loadProfile();
}

async function loadProfile() {
    try {
        const res = await fetch(`${API_URL}/${currentUser.id}`);
        if(res.ok) {
            const data = await res.json();
            
            // מילוי השדות
            document.getElementById('pName').value = data.name || '';
            document.getElementById('pAge').value = data.age || '';
            document.getElementById('pWeight').value = data.weight || '';
            document.getElementById('pHeight').value = data.height || '';
            document.getElementById('pGender').value = data.gender || 'female';
            document.getElementById('pActivity').value = data.activityLevel || 1.2;
            
            // הצגת היעד
            document.getElementById('calculatedGoal').innerText = data.dailyCalorieGoal || '---';
        }
    } catch(e) {
        console.error(e);
    }
}

async function saveProfile() {
    const btn = document.getElementById('saveBtn');
    btn.disabled = true;
    btn.innerText = 'מחשב ושומר...';

    const dto = {
        name: document.getElementById('pName').value,
        age: parseInt(document.getElementById('pAge').value) || 0,
        weight: parseFloat(document.getElementById('pWeight').value) || 0,
        height: parseInt(document.getElementById('pHeight').value) || 0,
        gender: document.getElementById('pGender').value,
        activityLevel: parseFloat(document.getElementById('pActivity').value),
        manualGoal: parseInt(document.getElementById('pManualGoal').value) || 0
    };

    try {
        const res = await fetch(`${API_URL}/${currentUser.id}`, {
            method: 'PUT',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify(dto)
        });

        if(res.ok) {
            const result = await res.json();
            
            // עדכון היעד בתצוגה
            document.getElementById('calculatedGoal').innerText = result.newGoal;
            
            // עדכון הלוקאל סטורג' עם היעד החדש כדי שהדף הראשי יתעדכן מיד
            localStorage.setItem('dailyGoal', result.newGoal);
            
            showMessage('הפרופיל עודכן בהצלחה!', 'success');
        } else {
            showMessage('שגיאה בשמירה', 'error');
        }
    } catch(e) {
        showMessage('שגיאת תקשורת', 'error');
    } finally {
        btn.disabled = false;
        btn.innerText = 'שמירת הגדרות';
    }
}

function showMessage(msg, type) {
    const el = document.getElementById('saveMsg');
    el.innerText = msg;
    el.style.display = 'block';
    el.style.color = type === 'success' ? '#86efac' : '#fca5a5';
    
    setTimeout(() => { el.style.display = 'none'; }, 3000);
}
