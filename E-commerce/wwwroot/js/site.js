// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Обращение в службу поддержки — только на клиенте, без отправки на сервер
(function () {
    var supportModal = document.getElementById('supportModal');
    var supportForm = document.getElementById('supportForm');
    var supportSuccess = document.getElementById('supportSuccess');
    var supportModalFooter = document.getElementById('supportModalFooter');

    if (!supportModal || !supportForm) return;

    supportModal.addEventListener('show.bs.modal', function () {
        supportForm.classList.remove('d-none');
        supportSuccess.classList.add('d-none');
        supportModalFooter.classList.remove('d-none');
        supportForm.reset();
    });

    supportForm.addEventListener('submit', function (e) {
        e.preventDefault();

        var topic = document.getElementById('supportTopic');
        var name = document.getElementById('supportName');
        var email = document.getElementById('supportEmail');
        var message = document.getElementById('supportMessage');

        if (!topic.value || !name.value.trim() || !email.value.trim() || !message.value.trim()) {
            supportForm.reportValidity();
            return;
        }

        var submitBtn = document.getElementById('supportSubmitBtn');
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Отправка...';
        }

        // Имитация отправки (без запроса к серверу)
        setTimeout(function () {
            supportForm.classList.add('d-none');
            supportSuccess.classList.remove('d-none');
            supportModalFooter.classList.add('d-none');

            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = '<i class="bi bi-send me-1"></i>Отправить';
            }
        }, 600);
    });
})();

// Функционал корзины
(function() {
    // Обновление счетчика корзины в навигации
    function updateCartBadge() {
        $.ajax({
            url: '/Cart/GetCartCount',
            method: 'GET',
            success: function(response) {
                const badge = $('#cartBadge');
                if (response.count > 0) {
                    badge.text(response.count).show();
                } else {
                    badge.hide();
                }
            },
            error: function() {
                // Игнорируем ошибки, если пользователь не авторизован
            }
        });
    }

    // Обновляем счетчик при загрузке страницы
    $(document).ready(function() {
        updateCartBadge();
    });

    // Обработка кнопки "Добавить в корзину"
    $(document).on('click', '.add-to-cart-btn', function(e) {
        e.preventDefault();
        const btn = $(this);
        const productId = btn.data('product-id');
        const productName = btn.data('product-name');
        const productPrice = btn.data('product-price');

        if (!productId) {
            alert('Ошибка: товар не найден');
            return;
        }

        // Блокируем кнопку
        const originalHtml = btn.html();
        btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Добавление...');

        $.ajax({
            url: '/Cart/AddToCart',
            method: 'POST',
            data: { productId: productId, quantity: 1 },
            success: function(response) {
                if (response.success) {
                    // Показываем уведомление
                    btn.html('<i class="bi bi-check-circle me-2"></i>Добавлено!');
                    btn.removeClass('btn-primary').addClass('btn-success');
                    
                    // Обновляем счетчик корзины
                    updateCartBadge();
                    
                    // Через 2 секунды возвращаем кнопку в исходное состояние
                    setTimeout(function() {
                        btn.prop('disabled', false).html(originalHtml);
                        btn.removeClass('btn-success').addClass('btn-primary');
                    }, 2000);
                } else {
                    if (response.redirect) {
                        window.location.href = response.url;
                    } else {
                        alert(response.message || 'Ошибка при добавлении товара в корзину');
                        btn.prop('disabled', false).html(originalHtml);
                    }
                }
            },
            error: function() {
                alert('Ошибка при добавлении товара в корзину');
                btn.prop('disabled', false).html(originalHtml);
            }
        });
    });
})();