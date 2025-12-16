document.addEventListener("DOMContentLoaded", () => {
    // --- Переключение вкладок с анимацией ---
    document.querySelectorAll(".profile-sidebar li").forEach(item => {
        item.addEventListener("click", () => {
            const tabId = item.dataset.tab;
            const currentTab = document.querySelector(".tab-content.active");
            const nextTab = document.getElementById(tabId);

            // Если кликаем на уже активную вкладку - выходим
            if (item.classList.contains("active")) return;

            // Анимация перехода между вкладками
            if (currentTab && nextTab) {
                currentTab.style.opacity = "0";
                currentTab.style.transform = "translateX(-20px)";

                setTimeout(() => {
                    document.querySelectorAll(".profile-sidebar li").forEach(el => el.classList.remove("active"));
                    document.querySelectorAll(".tab-content").forEach(tab => tab.classList.remove("active"));

                    item.classList.add("active");
                    nextTab.classList.add("active");

                    // Сбрасываем стили для плавного появления
                    setTimeout(() => {
                        nextTab.style.opacity = "1";
                        nextTab.style.transform = "translateX(0)";
                    }, 10);
                }, 200);
            }
        });
    });

    // --- Показ уведомлений Bootstrap Toast с неоновым стилем ---
    function showNotifications() {
        const toasts = document.querySelectorAll('.toast.show');
        toasts.forEach(toast => {
            // Добавляем неоновые эффекты к toast
            toast.style.boxShadow = "0 0 15px rgba(0, 255, 255, 0.3)";
            toast.style.border = "1px solid rgba(138, 43, 226, 0.3)";

            const bsToast = new bootstrap.Toast(toast, {
                animation: true,
                autohide: true,
                delay: 5000
            });
            bsToast.show();

            // Кастомные стили для кнопки закрытия
            const closeBtn = toast.querySelector('.btn-close');
            if (closeBtn) {
                closeBtn.innerHTML = '<i class="fas fa-times"></i>';
                closeBtn.style.color = "var(--color-neon-blue)";
                closeBtn.addEventListener('mouseover', () => {
                    closeBtn.style.textShadow = "0 0 10px var(--color-neon-blue)";
                });
                closeBtn.addEventListener('mouseout', () => {
                    closeBtn.style.textShadow = "none";
                });
            }
        });
    }

    // Запускаем показ уведомлений
    showNotifications();

    // --- Улучшенная работа с аватаром ---
    const avatarInput = document.getElementById("avatarInput");
    const avatarPreview = document.getElementById("avatarPreview");
    const fileName = document.getElementById("fileName");
    const chooseAvatarBtn = document.getElementById("chooseAvatarBtn");
    const avatarWrapper = document.querySelector(".avatar-wrapper");

    // Клик на обертку аватара
    if (avatarWrapper) {
        avatarWrapper.addEventListener("click", (e) => {
            e.preventDefault();
            avatarInput.click();
        });

        // Эффекты при наведении
        avatarWrapper.addEventListener("mouseenter", () => {
            avatarWrapper.style.transform = "scale(1.05)";
            avatarWrapper.style.boxShadow = "0 0 35px rgba(138, 43, 226, 0.8)";
        });

        avatarWrapper.addEventListener("mouseleave", () => {
            avatarWrapper.style.transform = "scale(1)";
            if (!avatarInput.files.length) {
                avatarWrapper.style.boxShadow = "0 0 25px rgba(138, 43, 226, 0.4)";
            }
        });
    }

    // Клик на кнопку выбора файла
    if (chooseAvatarBtn) {
        chooseAvatarBtn.addEventListener("click", (e) => {
            e.preventDefault();
            e.stopPropagation();
            avatarInput.click();
        });

        // Эффект при наведении на кнопку
        chooseAvatarBtn.addEventListener("mouseenter", () => {
            chooseAvatarBtn.style.transform = "translateY(-2px)";
            chooseAvatarBtn.style.boxShadow = "0 10px 25px rgba(138, 43, 226, 0.6)";
        });

        chooseAvatarBtn.addEventListener("mouseleave", () => {
            chooseAvatarBtn.style.transform = "translateY(0)";
            chooseAvatarBtn.style.boxShadow = "0 0 10px rgba(138, 43, 226, 0.5)";
        });
    }

    // Обработка выбора файла
    if (avatarInput) {
        avatarInput.addEventListener("change", () => {
            if (avatarInput.files.length > 0) {
                const file = avatarInput.files[0];

                // Проверка типа файла
                if (!file.type.match('image.*')) {
                    showCustomToast('Пожалуйста, выберите файл изображения', 'error');
                    return;
                }

                // Проверка размера файла (максимум 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    showCustomToast('Размер файла не должен превышать 5MB', 'error');
                    return;
                }

                fileName.textContent = file.name;
                fileName.style.color = "var(--color-neon-green)";
                fileName.innerHTML = `<i class="fas fa-check-circle"></i> ${file.name}`;

                const reader = new FileReader();
                reader.onloadstart = () => {
                    avatarPreview.style.opacity = "0.5";
                    chooseAvatarBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Загрузка...';
                    chooseAvatarBtn.disabled = true;
                };

                reader.onload = e => {
                    avatarPreview.src = e.target.result;
                    avatarPreview.style.opacity = "1";

                    // Эффект подтверждения загрузки
                    avatarWrapper.style.boxShadow = "0 0 30px rgba(0, 255, 255, 0.6)";
                    avatarWrapper.style.borderColor = "var(--color-neon-blue)";

                    setTimeout(() => {
                        avatarWrapper.style.boxShadow = "0 0 25px rgba(138, 43, 226, 0.6)";
                        avatarWrapper.style.borderColor = "var(--color-neon-purple)";
                    }, 1000);

                    chooseAvatarBtn.innerHTML = '<i class="fas fa-cloud-upload-alt"></i> Файл выбран';
                    setTimeout(() => {
                        chooseAvatarBtn.innerHTML = '<i class="fas fa-cloud-upload-alt"></i> Выбрать другой файл';
                        chooseAvatarBtn.disabled = false;
                    }, 1500);
                };

                reader.onerror = () => {
                    showCustomToast('Ошибка при чтении файла', 'error');
                    chooseAvatarBtn.innerHTML = '<i class="fas fa-cloud-upload-alt"></i> Выбрать файл';
                    chooseAvatarBtn.disabled = false;
                    avatarPreview.style.opacity = "1";
                };

                reader.readAsDataURL(file);
            } else {
                fileName.textContent = "Файл не выбран";
                fileName.style.color = "var(--color-text-secondary)";
                fileName.innerHTML = '<i class="fas fa-times-circle"></i> Файл не выбран';
            }
        });
    }

    // --- Валидация форм с неоновыми эффектами ---
    const forms = document.querySelectorAll('.profile-form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            let isValid = true;
            const inputs = this.querySelectorAll('input[required]');

            inputs.forEach(input => {
                if (!input.value.trim()) {
                    isValid = false;
                    highlightError(input);
                } else {
                    removeError(input);
                }
            });

            if (!isValid) {
                e.preventDefault();
                showCustomToast('Пожалуйста, заполните все обязательные поля', 'error');
            }
        });

        // Добавляем эффекты при фокусе на полях ввода
        const formControls = this.querySelectorAll('.form-control');
        formControls.forEach(control => {
            control.addEventListener('focus', function () {
                this.parentElement.style.transform = "translateY(-2px)";
                this.style.boxShadow = "0 0 20px rgba(0, 255, 255, 0.4)";
            });

            control.addEventListener('blur', function () {
                this.parentElement.style.transform = "translateY(0)";
                if (this.value.trim()) {
                    this.style.boxShadow = "0 0 10px rgba(0, 255, 255, 0.2)";
                } else {
                    this.style.boxShadow = "none";
                }
            });
        });
    });

    // --- Функции-помощники ---
    function highlightError(input) {
        input.style.borderColor = "#dc2626";
        input.style.boxShadow = "0 0 15px rgba(220, 38, 38, 0.4)";

        const parent = input.closest('.form-group');
        if (parent) {
            const label = parent.querySelector('label');
            if (label) {
                label.style.color = "#dc2626";
                label.innerHTML = label.innerHTML.replace(/(.*)/, '$1 <i class="fas fa-exclamation-circle"></i>');
            }
        }
    }

    function removeError(input) {
        input.style.borderColor = "";
        input.style.boxShadow = "";

        const parent = input.closest('.form-group');
        if (parent) {
            const label = parent.querySelector('label');
            if (label) {
                label.style.color = "";
                label.innerHTML = label.innerHTML.replace(/ <i class="fas fa-exclamation-circle"><\/i>/, '');
            }
        }
    }

    function showCustomToast(message, type = 'info') {
        // Создаем кастомное уведомление в неоновом стиле
        const toastContainer = document.querySelector('.toast-container') || createToastContainer();

        const toastId = 'toast-' + Date.now();
        const toast = document.createElement('div');
        toast.className = `toast ${type === 'error' ? 'toast-error' : type === 'success' ? 'toast-success' : 'toast-info'} show`;
        toast.id = toastId;
        toast.innerHTML = `
            <div>
                <i class="fas fa-${type === 'error' ? 'exclamation-triangle' : type === 'success' ? 'check-circle' : 'info-circle'}"></i>
                ${message}
            </div>
            <button type="button" class="btn-close" onclick="document.getElementById('${toastId}').remove()">
                <i class="fas fa-times"></i>
            </button>
        `;

        // Неоновые стили
        toast.style.background = type === 'error' ? 'linear-gradient(135deg, #dc2626 0%, #991b1b 100%)' :
            type === 'success' ? 'linear-gradient(135deg, #10b981 0%, #059669 100%)' :
                'linear-gradient(135deg, var(--color-neon-blend) 0%, var(--color-neon-purple) 100%)';
        toast.style.boxShadow = "0 0 20px rgba(0, 255, 255, 0.4)";
        toast.style.border = "1px solid rgba(0, 255, 255, 0.3)";

        toastContainer.appendChild(toast);

        // Автоматическое удаление через 5 секунд
        setTimeout(() => {
            if (document.getElementById(toastId)) {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(120%)';
                setTimeout(() => toast.remove(), 600);
            }
        }, 5000);
    }

    function createToastContainer() {
        const container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
        return container;
    }

    // --- Анимация загрузки страницы ---
    document.body.style.opacity = '0';
    document.body.style.transition = 'opacity 0.3s ease';

    setTimeout(() => {
        document.body.style.opacity = '1';
    }, 100);

    // --- Подсветка активных элементов ---
    setInterval(() => {
        const activeElements = document.querySelectorAll('.active, .btn-accent, .avatar-wrapper');
        activeElements.forEach(el => {
            if (el.classList.contains('active')) {
                el.style.boxShadow = el.style.boxShadow ||
                    '0 0 20px ' + (el.classList.contains('profile-sidebar') ? 'rgba(138, 43, 226, 0.5)' : 'rgba(0, 255, 255, 0.3)');
            }
        });
    }, 2000);
});