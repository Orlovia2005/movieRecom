document.addEventListener("DOMContentLoaded", () => {
    // --- Переключение вкладок ---
    document.querySelectorAll(".profile-sidebar li").forEach(item => {
        item.addEventListener("click", () => {
            document.querySelectorAll(".profile-sidebar li").forEach(el => el.classList.remove("active"));
            item.classList.add("active");

            document.querySelectorAll(".tab-content").forEach(tab => tab.classList.remove("active"));
            document.getElementById(item.dataset.tab).classList.add("active");
        });
    });

    // --- Показ уведомлений Bootstrap Toast ---
    function showNotifications() {
        // Автоматически показываем все toast с классом 'show'
        const toasts = document.querySelectorAll('.toast.show');
        toasts.forEach(toast => {
            const bsToast = new bootstrap.Toast(toast);
            bsToast.show();

            // Автозакрытие через 5 секунд
            setTimeout(() => {
                bsToast.hide();
            }, 5000);
        });
    }

    // Запускаем показ уведомлений
    showNotifications();

    // --- Аватар ---
    const avatarInput = document.getElementById("avatarInput");
    const avatarPreview = document.getElementById("avatarPreview");
    const fileName = document.getElementById("fileName");
    const chooseAvatarBtn = document.getElementById("chooseAvatarBtn");

    if (chooseAvatarBtn) {
        chooseAvatarBtn.addEventListener("click", () => {
            avatarInput.click();
        });
    }

    if (avatarInput) {
        avatarInput.addEventListener("change", () => {
            if (avatarInput.files.length > 0) {
                const file = avatarInput.files[0];
                fileName.textContent = file.name;

                const reader = new FileReader();
                reader.onload = e => {
                    avatarPreview.src = e.target.result;
                };
                reader.readAsDataURL(file);
            } else {
                fileName.textContent = "Файл не выбран";
                // Используем текущий аватар или дефолтный
                const currentAvatar = avatarPreview.src;
                if (!currentAvatar || currentAvatar.includes('default')) {
                    avatarPreview.src = "/images/BaseAvatar.jpg";
                }
            }
        });
    }
});