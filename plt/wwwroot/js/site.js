// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Отметить посещение
function markVisit(id, event) {
    event.stopPropagation(); // Предотвращаем всплытие события

    if (confirm('Отметить посещение клиента?')) {
        // Показываем индикатор загрузки
        const button = event.target;
        const originalText = button.textContent;
        button.textContent = 'Загрузка...';
        button.disabled = true;

        fetch('/Client/MarkVisit', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: `clientId=${id}`
        })
            .then(response => {
                if (response.ok) {
                    location.reload(); // Перезагружаем страницу для обновления данных
                } else {
                    return response.text().then(text => {
                        throw new Error(text || 'Ошибка при отметке посещения');
                    });
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Ошибка при отметке посещения: ' + error.message);
                // Восстанавливаем кнопку
                button.textContent = originalText;
                button.disabled = false;
            });
    }
}