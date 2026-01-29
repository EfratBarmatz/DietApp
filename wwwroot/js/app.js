const API_URL = '/api/meals';
let currentUser = null;
let allMeals = [];
let itemToDeleteId = null;

// ==========================================
// 1. אתחול ובדיקת משתמש (Auth Check)
// ==========================================

// בדיקה האם המשתמש מחובר מיד בעליית הדף
const savedUser = localStorage.getItem('dietUser');
if (!savedUser) {
    // אם לא מחובר - מעיפים לדף הכניסה
    window.location.href = 'login.html';
} else {
    // אם מחובר - טוענים את הנתונים
    currentUser = JSON.parse(savedUser);
    loadData();
}

function logout() {
    if(confirm('בטוחה שאת רוצה להתנתק?')) {
        localStorage.removeItem('dietUser');
        window.location.href = 'login.html';
    }
}

// ==========================================
// 2. ניהול נתונים (CRUD)
// ==========================================

async function loadData() {
    if (!currentUser) return;
    
    try {
        const res = await fetch(`${API_URL}?userId=${currentUser.id}`);
        if(res.ok) {
            allMeals = await res.json();
            renderGrid();
        } else {
            console.error('Failed to load meals');
        }
    } catch (e) {
        console.error('Error loading data:', e);
    }
}

async function addMeal() {
    const name = document.getElementById('foodName').value;
    const calories = parseInt(document.getElementById('foodCal').value);
    const type = document.getElementById('mealType').value;
    const btn = document.getElementById('addMealBtn'); // ודאי שב-HTML ה-ID הוא addMealBtn

    // ולידציה
    if (!name || !calories) {
        const errorDiv = document.getElementById('addError');
        if(errorDiv) {
            errorDiv.innerText = 'מלאי את כל השדות';
            errorDiv.style.display = 'block';
        }
        return;
    }

    // מצב טעינה
    if(btn) {
        btn.disabled = true;
        btn.innerHTML = '<span class="loading-spinner"></span> שומר...';
    }

    // חישוב זמן
    let date = new Date();
    const customTimeInput = document.getElementById('customTime');
    
    // אם נבחרה שעה ידנית
    if (customTimeInput && !customTimeInput.classList.contains('hidden') && customTimeInput.value) {
        const [h, m] = customTimeInput.value.split(':');
        date.setHours(h, m, 0, 0);
    }

    // שליחת הזמן בפורמט בינלאומי (ISO)
    const dateStr = date.toISOString();

    const mealData = { 
        name, 
        calories, 
        userId: currentUser.id, 
        date: dateStr,
        mealType: type 
    };

    try {
        const res = await fetch(API_URL, {
            method: 'POST', 
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify(mealData)
        });
        
        if (res.ok) { 
            closeModal('addModal'); 
            loadData(); // רענון הנתונים
        } else {
            alert('שגיאה בשמירה');
        }
    } catch(e) {
        console.error(e);
        alert('שגיאת תקשורת');
    } finally { 
        if(btn) {
            btn.disabled = false; 
            btn.innerHTML = 'שמירה';
        }
    }
}

function confirmDelete(id) {
    itemToDeleteId = id;
    document.getElementById('confirmModal').style.display = 'flex';
}

async function performDelete() {
    if(!itemToDeleteId) return;

    try {
        const res = await fetch(`${API_URL}/${itemToDeleteId}`, { method: 'DELETE' });
        if(res.ok) {
            closeModal('confirmModal');
            closeModal('viewModal'); // סוגר גם את רשימת הארוחות אם פתוחה
            loadData();
        }
    } catch(e) {
        console.error(e);
    }
}

// ==========================================
// 3. תצוגה ו-UI
// ==========================================

function renderGrid() {
    // סינון להיום בלבד
    const todayStr = new Date().toDateString();
    const todaysMeals = allMeals.filter(m => new Date(m.date).toDateString() === todayStr);
    
    // חישוב סה"כ קלוריות
    const totalCal = todaysMeals.reduce((sum, m) => sum + m.calories, 0);
    document.getElementById('totalCalories').innerText = totalCal;

    // הגדרות הקטגוריות
    const categories = {
        morning: { title: 'בוקר', icon: '🥐', cal: 0, items: [], css: 'border-morning' },
        noon:    { title: 'צהריים', icon: '🍛', cal: 0, items: [], css: 'border-noon' },
        evening: { title: 'ערב', icon: '🥗', cal: 0, items: [], css: 'border-evening' },
        snack:   { title: 'נשנוש', icon: '🥨', cal: 0, items: [], css: 'border-snack' }
    };

    // מיון לקטגוריות
    todaysMeals.forEach(m => {
        let key = m.mealType || 'snack';
        if(categories[key]) {
            categories[key].cal += m.calories;
            categories[key].items.push(m);
        }
    });

    // בניית ה-HTML
    const grid = document.getElementById('mealsGrid');
    grid.innerHTML = '';
    
    for (const key in categories) {
        const cat = categories[key];
        grid.innerHTML += `
            <div class="meal-card ${cat.css}" onclick="showCategoryDetails('${key}')">
                <div class="card-icon">${cat.icon}</div>
                <div class="card-title">${cat.title}</div>
                <div class="card-cal">${cat.cal}</div>
                <div class="card-sub">${cat.items.length} פריטים</div>
            </div>`;
    }

    // עדכון פס ההתקדמות בסוף
    updateProgress();
}

function showCategoryDetails(key) {
    const titles = { morning: 'ארוחת בוקר', noon: 'ארוחת צהריים', evening: 'ארוחת ערב', snack: 'נשנושים' };
    document.getElementById('viewTitle').innerText = titles[key];
    
    const todayStr = new Date().toDateString();
    const items = allMeals.filter(m => {
        return new Date(m.date).toDateString() === todayStr && (m.mealType || 'snack') === key;
    });

    const list = document.getElementById('itemsList'); // ודאי שיש ID כזה במודל הצפייה
    list.innerHTML = '';
    
    if (items.length === 0) {
        list.innerHTML = '<p style="text-align:center; opacity:0.6;">אין פריטים</p>';
    } else {
        items.forEach(m => {
            const time = new Date(m.date).toLocaleTimeString('he-IL', {hour:'2-digit', minute:'2-digit'});
            list.innerHTML += `
                <div class="item-row">
                    <div><b>${m.name}</b><br><small style="opacity:0.6">${time}</small></div>
                    <div class="d-flex align-items-center gap-2">
                        <span style="font-weight:bold">${m.calories}</span>
                        <i class="bi bi-trash text-danger" style="cursor:pointer" onclick="confirmDelete(${m.id})"></i>
                    </div>
                </div>`;
        });
    }
    
    document.getElementById('viewModal').style.display = 'flex';
}

// ==========================================
// 4. פס התקדמות (Progress Bar)
// ==========================================

function setDailyGoal() {
    const currentGoal = localStorage.getItem('dailyGoal') || 1500;
    const newGoal = prompt('הכניסי את יעד הקלוריות היומי שלך:', currentGoal);
    
    if (newGoal && !isNaN(newGoal) && newGoal > 0) {
        localStorage.setItem('dailyGoal', newGoal);
        updateProgress();
    }
}

function updateProgress() {
    const totalElement = document.getElementById('totalCalories');
    const goalElement = document.getElementById('goalNumber');
    const barElement = document.getElementById('progressBar');

    if(!totalElement || !goalElement || !barElement) return;

    const total = parseInt(totalElement.innerText) || 0;
    const goal = parseInt(localStorage.getItem('dailyGoal')) || 1500;

    goalElement.innerText = goal;

    let percentage = (total / goal) * 100;
    
    if (percentage > 100) {
        barElement.style.background = 'linear-gradient(90deg, #fca5a5, #ef4444)'; // אדום
        percentage = 100;
    } else {
        barElement.style.background = 'linear-gradient(90deg, var(--color-3), var(--color-1))'; // צבע רגיל
    }
    
    barElement.style.width = percentage + '%';
}

// ==========================================
// 5. עזרים ומודלים (Helpers)
// ==========================================

function openAddModal() {
    // קביעת ברירת מחדל חכמה לפי השעה
    const hour = new Date().getHours();
    const select = document.getElementById('mealType');
    
    if (hour >= 5 && hour < 12) select.value = 'morning';
    else if (hour >= 12 && hour < 17) select.value = 'noon';
    else if (hour >= 17 && hour < 22) select.value = 'evening';
    else select.value = 'snack';

    // איפוס שדות
    document.getElementById('customTime').classList.add('hidden');
    document.querySelector('input[name="timeT"][value="now"]').checked = true; // ודאי שה-RADIO בשם timeT

    document.getElementById('addModal').style.display = 'flex';
}

function closeModal(id) {
    document.getElementById(id).style.display = 'none';
    
    // אם סגרנו את ההוספה - מנקים
    if(id === 'addModal') {
        document.getElementById('foodName').value = '';
        document.getElementById('foodCal').value = '';
        const err = document.getElementById('addError');
        if(err) err.style.display = 'none';
    }
}

function toggleTime(show) {
    const el = document.getElementById('customTime');
    if(show) el.classList.remove('hidden');
    else el.classList.add('hidden');
}

// חשיפת פונקציות לחלון כדי שה-HTML יכיר אותן (למקרים של מודולים, כאן זה סקריפט רגיל אז זה לא חובה אבל לא מזיק)
window.loadData = loadData;
window.addMeal = addMeal;
window.setDailyGoal = setDailyGoal;
window.openAddModal = openAddModal;
