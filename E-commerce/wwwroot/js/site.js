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
