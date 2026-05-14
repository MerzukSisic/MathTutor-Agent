// Chart.js functions for MathTutor AI

window.renderTopicChart = function(labels, scores) {
    const ctx = document.getElementById('topicChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Mastery Score',
                data: scores,
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100
                }
            }
        }
    });
};

window.renderDailyChart = function(labels, totalAttempts, correctAttempts) {
    const ctx = document.getElementById('dailyChart');
    if (!ctx) return;

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Total Attempts',
                    data: totalAttempts,
                    borderColor: 'rgba(75, 192, 192, 1)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                },
                {
                    label: 'Correct Attempts',
                    data: correctAttempts,
                    borderColor: 'rgba(54, 162, 235, 1)',
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
};

window.downloadFile = function(fileName, base64Content) {
    const link = document.createElement('a');
    link.href = 'data:application/pdf;base64,' + base64Content;
    link.download = fileName;
    link.click();
};

// Makes auth API calls from the browser so the cookie lands in the browser's jar
let antiForgeryTokenCache = null;

async function getAntiForgeryToken() {
    if (antiForgeryTokenCache) {
        return antiForgeryTokenCache;
    }

    const tokenResponse = await fetch('/api/auth/csrf-token', {
        method: 'GET',
        credentials: 'include'
    });

    if (!tokenResponse.ok) {
        return null;
    }

    const payload = await tokenResponse.json();
    antiForgeryTokenCache = payload?.token ?? null;
    return antiForgeryTokenCache;
}

window.authFetch = async function(url, body) {
    const headers = { 'Content-Type': 'application/json' };
    const token = await getAntiForgeryToken();
    if (token) {
        headers['RequestVerificationToken'] = token;
    }

    const response = await fetch(url, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify(body ?? {})
    });

    if (response.status === 400) {
        antiForgeryTokenCache = null;
    }

    const text = await response.text();
    let data = null;
    try { data = JSON.parse(text); } catch {}
    return { ok: response.ok, status: response.status, data };
};
