document.addEventListener("DOMContentLoaded", function () {
    // 1. Sidebar Toggle
    const sidebar = document.getElementById("crmSidebar");
    const toggleBtn = document.getElementById("sidebarToggleBtn");

    if (toggleBtn && sidebar) {
        // Retrieve persisted state
        const isCollapsed = localStorage.getItem("crm_sidebar_collapsed") === "true";
        if (isCollapsed) {
            sidebar.classList.add("collapsed");
        }

        toggleBtn.addEventListener("click", function () {
            sidebar.classList.toggle("collapsed");
            localStorage.setItem("crm_sidebar_collapsed", sidebar.classList.contains("collapsed"));
        });
    }

    // 2. Auto-initialize Toasts
    const toastElements = document.querySelectorAll(".toast");
    toastElements.forEach(function (toastEl) {
        const toast = new bootstrap.Toast(toastEl, { delay: 5000 });
        toast.show();
    });

    // 3. Confirm Delete Modals
    const deleteModal = document.getElementById("deleteConfirmModal");
    if (deleteModal) {
        deleteModal.addEventListener("show.bs.modal", function (event) {
            const button = event.relatedTarget;
            const recordName = button.getAttribute("data-record-name") || "this record";
            const deleteUrl = button.getAttribute("data-delete-url");

            const modalRecordText = deleteModal.querySelector("#deleteRecordName");
            const modalForm = deleteModal.querySelector("#deleteForm");

            if (modalRecordText) modalRecordText.textContent = recordName;
            if (modalForm && deleteUrl) modalForm.action = deleteUrl;
        });
    }
});

