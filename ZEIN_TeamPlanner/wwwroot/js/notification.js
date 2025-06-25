const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.on("ReceiveNotification", function (message, type, relatedEntityId, relatedEntityType, id) {
    fetchNotifications();
    showPopupNotification(message);
});

function showPopupNotification(message) {
    const toast = document.createElement("div");
    toast.className = "toast align-items-center text-white bg-primary border-0 position-fixed bottom-0 end-0 m-3";
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    setTimeout(() => toast.remove(), 5000);
}

async function fetchNotifications() {
    const response = await fetch("/Notifications/GetNotifications");
    const notifications = await response.text();
    document.getElementById("notificationList").innerHTML = notifications;
    updateUnreadCount();
}

function updateUnreadCount() {
    const unreadItems = document.querySelectorAll(".notification-item.unread");
    const badge = document.getElementById("unreadCount");
    badge.textContent = unreadItems.length || "";
}

document.getElementById("markAllRead")?.addEventListener("click", async function (e) {
    e.preventDefault();
    await fetch("/Notifications/MarkAllRead", { method: "POST" });
    fetchNotifications();
});

connection.start().then(fetchNotifications).catch(console.error);

document.addEventListener("DOMContentLoaded", function () {
    fetchNotifications();
});