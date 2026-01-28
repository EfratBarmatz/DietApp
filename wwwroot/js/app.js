const API_URL = '/api/meals';
let currentUser = null;
let allMeals = [];

// *** אבטחה: בדיקה אם המשתמש מחובר ***
const savedUser = localStorage.getItem('dietUser');
if (!savedUser) {
    // אם אין משתמש, מעיפים אותו לדף הכניסה
    window.location.href = 'login.html';
} else {
    currentUser = JSON.parse(savedUser);
    loadData(); // טוענים נתונים רק אם יש משתמש
}

function logout() {
    if(confirm('לצאת?')) {
        localStorage.removeItem('dietUser');
        window.location.href = 'login.html'; // חוזרים ללוגין
    }
}

// ... כאן תדביקי את כל שאר הפונקציות שלך: ...
// loadData(), renderGrid(), addMeal(), deleteMeal(), setDailyGoal(), updateProgress()
// וכל פונקציות העזר של המודלים (openAddModal, closeModal וכו')
