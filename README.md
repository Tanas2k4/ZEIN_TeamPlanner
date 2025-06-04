# ZEIN TEAM PLANNER

**ZEIN Team Planner** là một ứng dụng quản lý công việc nhóm hiện đại, được xây dựng bằng **ASP.NET Core MVC**. Ứng dụng giúp các nhóm làm việc cộng tác hiệu quả hơn bằng cách theo dõi, phân công và giám sát tiến độ các tác vụ một cách trực quan và dễ sử dụng.

---

## 🚧 TRẠNG THÁI DỰ ÁN

> Dự án hiện đang trong **giai đoạn phát triển**. Các tính năng và giao diện sẽ được **cập nhật thường xuyên** để đáp ứng nhu cầu thực tế và cải thiện trải nghiệm người dùng.

---

## ✨ TÍNH NĂNG HIỆN CÓ và ĐANG XÂY DỰNG

-  📋 Quản lý danh sách công việc (Tasks)
- 👤 Gán người thực hiện cho từng task
- 🏷 Phân loại và hiển thị trạng thái task (Pending, In Progress, Done)
- 📅 Quản lý deadline và ngày tạo
- 🧭 Sidebar điều hướng trực quan
- 🖼 Giao diện hiện đại, hỗ trợ responsive cơ bản
- 🎨 Hỗ trợ tùy biến màu sắc, sử dụng Bootstrap 5 + Icons

---

## 🧰 CÔNG NGHỆ SỬ DỤNG
--------------------------------------------------------------------
| Công nghệ             | Mục đích sử dụng                         |
|-----------------------|------------------------------------------|
| ASP.NET Core MVC      | Backend, xử lý logic và routing          |
| Entity Framework Core | ORM – tương tác với CSDL                 |
| Razor View Engine     | Xây dựng giao diện động với C#           |
| Bootstrap 5           | UI/UX – bố cục và responsive             |
| Bootstrap Icons       | Biểu tượng trực quan                     |
| SQL Server / SQLite   | Cơ sở dữ liệu (tuỳ cấu hình)             |
--------------------------------------------------------------------
---

## 📁 CẤU TRÚC THƯ MỤC

```PLAINTEXT
ZEIN_TeamPlanner/
├── Controllers/        # Controller cho TaskItems, Dashboard...
├── Models/             # Các lớp Entity như TaskItem, User...
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   └── _layout.css (tuỳ biến thêm)
│   └── TaskItems/
│       ├── Index.cshtml
│       ├── Create.cshtml
│       └── Edit/Delete.cshtml
├── wwwroot/
│   ├── css/
│   └── js/
├── appsettings.json    # Cấu hình kết nối DB
└── ZEIN_TeamPlanner.csproj
```

MADE WITH ❤️ by ZEIN DEV TEAM 


